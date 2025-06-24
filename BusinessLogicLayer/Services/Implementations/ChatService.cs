using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services.Implementations
{
    public class ChatService : IChatService
    {
        private readonly IChatRepo _chatRepository;
        private readonly ILogger<ChatService> _logger;

        public ChatService(IChatRepo chatRepository, ILogger<ChatService> logger)
        {
            _chatRepository = chatRepository;
            _logger = logger;
        }

        public async Task<List<MessageEntity>> GetConversationMessagesAsync(string senderId, string receiverId, int page = 1, int pageSize = 50)
        {
            try
            {
                return await _chatRepository.GetConversationMessagesAsync(senderId, receiverId, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting conversation messages between {senderId} and {receiverId}");
                return new List<MessageEntity>();
            }
        }

        public async Task<MessageEntity?> SendMessageAsync(string senderId, string receiverId, string message, string? replyToMessageId = null)
        {
            try
            {
                _logger.LogInformation($"Attempting to send message from {senderId} to {receiverId}");

                // Create conversation if it doesn't exist
                var conversation = await _chatRepository.GetOrCreateConversationAsync(senderId, receiverId);
                if (conversation == null)
                {
                    _logger.LogError($"Failed to create/get conversation between {senderId} and {receiverId}");
                    return null;
                }

                _logger.LogInformation($"Using conversation: {conversation.ConversationId}");

                var messageEntity = new MessageEntity
                {
                    MessageId = Guid.NewGuid().ToString(),
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    ConversationId = conversation.ConversationId,
                    MessageContent = message,
                    MessageType = "text", // Use lowercase to match DB constraint
                    IsRead = false,
                    IsDeletedBySender = false,
                    IsDeletedByReceiver = false,
                    ReplyToMessageId = replyToMessageId,
                    IsEdited = false,
                    MessageCreatedAt = DateTime.UtcNow,
                    MessageUpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation($"Created message entity with ID: {messageEntity.MessageId}");

                var result = await _chatRepository.AddAsync(messageEntity);
                if (result != null)
                {
                    _logger.LogInformation($"Message saved to database successfully");
                    // Update conversation's last message
                    conversation.LastMessageId = messageEntity.MessageId;
                    conversation.LastMessageAt = messageEntity.MessageCreatedAt;
                    conversation.ConversationUpdatedAt = DateTime.UtcNow;

                    // Update conversation
                    await _chatRepository.UpdateConversationAsync(conversation);                    // Include sender information for the response
                    return await _chatRepository.GetMessageByIdAsync(messageEntity.MessageId);
                }
                else
                {
                    _logger.LogError($"Failed to save message to database");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message from {senderId} to {receiverId}");
                return null;
            }
        }

        public async Task<bool> MarkMessageAsReadAsync(string messageId, string userId)
        {
            try
            {
                return await _chatRepository.MarkMessageAsReadAsync(messageId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking message {messageId} as read by {userId}");
                return false;
            }
        }

        public async Task<MessageEntity?> GetMessageByIdAsync(string messageId)
        {
            try
            {
                return await _chatRepository.GetMessageByIdAsync(messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting message {messageId}");
                return null;
            }
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(string userId)
        {
            try
            {
                return await _chatRepository.GetUserConversationsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting conversations for user {userId}");
                return new List<Conversation>();
            }
        }

        public async Task<int> GetUnreadMessageCountAsync(string userId, string senderId)
        {
            try
            {
                return await _chatRepository.GetUnreadMessageCountAsync(userId, senderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting unread message count for {userId} from {senderId}");
                return 0;
            }
        }

        public async Task<List<MessageEntity>> GetUnreadMessagesAsync(string userId)
        {
            try
            {
                return await _chatRepository.GetUnreadMessagesAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting unread messages for user {userId}");
                return new List<MessageEntity>();
            }
        }

        public async Task<bool> CanUserAccessConversationAsync(string userId, string conversationId)
        {
            try
            {
                return await _chatRepository.CanUserAccessConversationAsync(userId, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking conversation access for user {userId} and conversation {conversationId}");
                return false;
            }
        }

        public async Task<List<Account>> GetChatUsersAsync(string currentUserId)
        {
            try
            {
                return await _chatRepository.GetChatUsersAsync(currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chat users for {currentUserId}");
                return new List<Account>();
            }
        }

        public async Task<MessageEntity?> GetLastMessageBetweenUsersAsync(string userId1, string userId2)
        {
            try
            {
                return await _chatRepository.GetLastMessageBetweenUsersAsync(userId1, userId2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting last message between {userId1} and {userId2}");
                return null;
            }
        }
        public async Task<bool> SetUserOnlineStatusAsync(string userId, bool isOnline)
        {
            try
            {
                // This could be implemented with a separate online status table or cached data
                // For now, we'll just log the status change
                await Task.Run(() =>
                {
                    _logger.LogInformation($"User {userId} is now {(isOnline ? "online" : "offline")}");
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting online status for user {userId}");
                return false;
            }
        }

        public async Task<Conversation?> GetOrCreateConversationAsync(string userId1, string userId2)
        {
            try
            {
                return await _chatRepository.GetOrCreateConversationAsync(userId1, userId2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting or creating conversation between {userId1} and {userId2}");
                return null;
            }
        }

        public async Task<bool> DeleteMessageAsync(string messageId, string userId)
        {
            try
            {
                var message = await _chatRepository.GetMessageByIdAsync(messageId);
                if (message == null) return false;

                // Check if user can delete this message (either sender or receiver)
                if (message.SenderId != userId && message.ReceiverId != userId)
                    return false;

                // Mark as deleted for the user
                if (message.SenderId == userId)
                    message.IsDeletedBySender = true;
                else
                    message.IsDeletedByReceiver = true;

                return await _chatRepository.UpdateMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting message {messageId} for user {userId}");
                return false;
            }
        }

        public async Task<bool> EditMessageAsync(string messageId, string newContent, string userId)
        {
            try
            {
                var message = await _chatRepository.GetMessageByIdAsync(messageId);
                if (message == null || message.SenderId != userId)
                    return false;

                message.OriginalContent = message.MessageContent;
                message.MessageContent = newContent;
                message.IsEdited = true;
                message.EditedAt = DateTime.UtcNow;
                message.MessageUpdatedAt = DateTime.UtcNow;

                return await _chatRepository.UpdateMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing message {messageId} by user {userId}");
                return false;
            }
        }
    }
}
