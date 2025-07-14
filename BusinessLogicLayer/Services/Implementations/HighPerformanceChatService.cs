using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using BusinessLogicLayer.Hubs;

namespace BusinessLogicLayer.Services.Implementations
{
    public class HighPerformanceChatService : IChatService
    {
        private readonly IChatRepo _chatRepository;
        private readonly IMessageQueueService _messageQueueService;
        private readonly IDistributedCache _distributedCache;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<HighPerformanceChatService> _logger;

        // Circuit breaker variables
        private int _failureCount = 0;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private readonly int _failureThreshold = 5;
        private readonly TimeSpan _circuitBreakerTimeout = TimeSpan.FromMinutes(1);
        private bool _circuitOpen = false;

        // Cache keys
        private const string CONVERSATION_CACHE_KEY = "conversation:{0}:{1}";
        private const string USER_CONVERSATIONS_CACHE_KEY = "user_conversations:{0}";
        private const string MESSAGES_CACHE_KEY = "messages:{0}:{1}:{2}";
        private const string ONLINE_USERS_CACHE_KEY = "online_users";

        public HighPerformanceChatService(
            IChatRepo chatRepository,
            IMessageQueueService messageQueueService,
            IDistributedCache distributedCache,
            IHubContext<ChatHub> hubContext,
            ILogger<HighPerformanceChatService> logger)
        {
            _chatRepository = chatRepository;
            _messageQueueService = messageQueueService;
            _distributedCache = distributedCache;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<MessageEntity?> SendMessageAsync(string senderId, string receiverId, string message, string? replyToMessageId = null)
        {
            try
            {
                // Check circuit breaker
                if (IsCircuitOpen())
                {
                    _logger.LogWarning("Circuit breaker is open, rejecting message");
                    return null;
                }

                // Create message entity
                var messageEntity = new MessageEntity
                {
                    MessageId = Guid.NewGuid().ToString(),
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    MessageContent = message,
                    MessageType = "text",
                    IsRead = false,
                    IsDeletedBySender = false,
                    IsDeletedByReceiver = false,
                    ReplyToMessageId = replyToMessageId,
                    IsEdited = false,
                    MessageCreatedAt = DateTime.UtcNow,
                    MessageUpdatedAt = DateTime.UtcNow
                };

                // Enqueue message for async processing
                await _messageQueueService.EnqueueMessageAsync(messageEntity);

                // Send immediate response to clients (optimistic delivery)
                await SendImmediateResponse(messageEntity);

                _logger.LogInformation($"Message {messageEntity.MessageId} queued for processing");
                ResetCircuitBreaker();
                return messageEntity;
            }
            catch (Exception ex)
            {
                HandleCircuitBreakerFailure();
                _logger.LogError(ex, $"Error sending message from {senderId} to {receiverId}");
                return null;
            }
        }

        private async Task SendImmediateResponse(MessageEntity messageEntity)
        {
            try
            {
                var responseData = new
                {
                    messageId = messageEntity.MessageId,
                    senderId = messageEntity.SenderId,
                    receiverId = messageEntity.ReceiverId,
                    content = messageEntity.MessageContent,
                    messageType = messageEntity.MessageType,
                    replyToMessageId = messageEntity.ReplyToMessageId,
                    createdAt = messageEntity.MessageCreatedAt,
                    status = "pending" // Indicates message is being processed
                };

                // Send to receiver
                await _hubContext.Clients.User(messageEntity.ReceiverId).SendAsync("ReceiveMessage", responseData);

                // Send confirmation to sender
                await _hubContext.Clients.User(messageEntity.SenderId).SendAsync("MessageSent", responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending immediate response for message {messageEntity.MessageId}");
            }
        }

        public async Task<List<MessageEntity>> GetConversationMessagesAsync(string senderId, string receiverId, int page = 1, int pageSize = 50)
        {
            try
            {
                var cacheKey = string.Format(MESSAGES_CACHE_KEY, senderId, receiverId, page);
                var cachedMessages = await _distributedCache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedMessages))
                {
                    var messages = JsonSerializer.Deserialize<List<MessageEntity>>(cachedMessages);
                    if (messages != null)
                    {
                        _logger.LogDebug($"Retrieved {messages.Count} messages from cache");
                        return messages;
                    }
                }

                // Fallback to database
                var dbMessages = await _chatRepository.GetConversationMessagesAsync(senderId, receiverId, page, pageSize);

                // Cache the result
                await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dbMessages),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });

                _logger.LogDebug($"Retrieved {dbMessages.Count} messages from database and cached");
                return dbMessages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting conversation messages between {senderId} and {receiverId}");
                return new List<MessageEntity>();
            }
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(string userId)
        {
            try
            {
                var cacheKey = string.Format(USER_CONVERSATIONS_CACHE_KEY, userId);
                var cachedConversations = await _distributedCache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedConversations))
                {
                    var conversations = JsonSerializer.Deserialize<List<Conversation>>(cachedConversations);
                    if (conversations != null)
                    {
                        _logger.LogDebug($"Retrieved {conversations.Count} conversations from cache");
                        return conversations;
                    }
                }

                // Fallback to database
                var dbConversations = await _chatRepository.GetUserConversationsAsync(userId);

                // Cache the result
                await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dbConversations),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    });

                _logger.LogDebug($"Retrieved {dbConversations.Count} conversations from database and cached");
                return dbConversations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting conversations for user {userId}");
                return new List<Conversation>();
            }
        }

        public async Task<bool> SetUserOnlineStatusAsync(string userId, bool isOnline)
        {
            try
            {
                var onlineUsers = await GetOnlineUsersFromCacheAsync();

                if (isOnline)
                {
                    onlineUsers.Add(userId);
                }
                else
                {
                    onlineUsers.Remove(userId);
                }

                await UpdateOnlineUsersInCacheAsync(onlineUsers);
                _logger.LogInformation($"User {userId} is now {(isOnline ? "online" : "offline")}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting online status for user {userId}");
                return false;
            }
        }

        private async Task<HashSet<string>> GetOnlineUsersFromCacheAsync()
        {
            try
            {
                var cachedUsers = await _distributedCache.GetStringAsync(ONLINE_USERS_CACHE_KEY);
                if (!string.IsNullOrEmpty(cachedUsers))
                {
                    var users = JsonSerializer.Deserialize<HashSet<string>>(cachedUsers);
                    return users ?? new HashSet<string>();
                }
                return new HashSet<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online users from cache");
                return new HashSet<string>();
            }
        }

        private async Task UpdateOnlineUsersInCacheAsync(HashSet<string> onlineUsers)
        {
            try
            {
                await _distributedCache.SetStringAsync(ONLINE_USERS_CACHE_KEY,
                    JsonSerializer.Serialize(onlineUsers),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating online users in cache");
            }
        }

        private bool IsCircuitOpen()
        {
            if (_circuitOpen)
            {
                if (DateTime.UtcNow - _lastFailureTime > _circuitBreakerTimeout)
                {
                    _circuitOpen = false;
                    _failureCount = 0;
                    _logger.LogInformation("Circuit breaker reset");
                }
            }
            return _circuitOpen;
        }

        private void HandleCircuitBreakerFailure()
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _circuitOpen = true;
                _logger.LogWarning($"Circuit breaker opened after {_failureCount} failures");
            }
        }

        private void ResetCircuitBreaker()
        {
            _failureCount = 0;
            _circuitOpen = false;
        }

        // Other methods implementation...
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

        public async Task<Conversation?> GetOrCreateConversationAsync(string userId1, string userId2)
        {
            try
            {
                var cacheKey = string.Format(CONVERSATION_CACHE_KEY, userId1, userId2);
                var cachedConversation = await _distributedCache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedConversation))
                {
                    var conversation = JsonSerializer.Deserialize<Conversation>(cachedConversation);
                    if (conversation != null)
                    {
                        return conversation;
                    }
                }

                // Fallback to database
                var dbConversation = await _chatRepository.GetOrCreateConversationAsync(userId1, userId2);

                // Cache the result
                if (dbConversation != null)
                {
                    await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dbConversation),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                        });
                }

                return dbConversation;
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

                if (message.SenderId != userId && message.ReceiverId != userId)
                    return false;

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
                if (message == null || message.SenderId != userId) return false;

                message.MessageContent = newContent;
                message.IsEdited = true;
                message.MessageUpdatedAt = DateTime.UtcNow;

                return await _chatRepository.UpdateMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing message {messageId} for user {userId}");
                return false;
            }
        }
    }
}