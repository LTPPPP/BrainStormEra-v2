using BusinessLogicLayer.DTOs.Chat;
using BusinessLogicLayer.DTOs.Common;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services.Implementations
{
    public class ChatBusinessService : IChatBusinessService
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatBusinessService> _logger;

        public ChatBusinessService(IChatService chatService, ILogger<ChatBusinessService> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public async Task<ServiceResult<ChatIndexViewModel>> GetChatIndexViewModelAsync(string currentUserId)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult<ChatIndexViewModel>.Failure("User not authenticated");
                }

                var users = await _chatService.GetChatUsersAsync(currentUserId);
                var chatUsers = await MapToChatUserDTOsAsync(users, currentUserId);

                var viewModel = new ChatIndexViewModel
                {
                    CurrentUserId = currentUserId,
                    Users = chatUsers
                };

                return ServiceResult<ChatIndexViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat index view model for user {UserId}", currentUserId);
                return ServiceResult<ChatIndexViewModel>.Failure("An error occurred while loading chat users");
            }
        }

        public async Task<ServiceResult<ConversationViewModel>> GetConversationViewModelAsync(string currentUserId, string receiverId)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult<ConversationViewModel>.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(receiverId))
                {
                    return ServiceResult<ConversationViewModel>.Failure("Receiver ID is required");
                }

                // Get chat users for sidebar
                var users = await _chatService.GetChatUsersAsync(currentUserId);
                var chatUsers = await MapToChatUserDTOsAsync(users, currentUserId);

                // Get messages
                var messages = await _chatService.GetConversationMessagesAsync(currentUserId, receiverId);
                var messageDTOs = MapToChatMessageDTOs(messages);

                var viewModel = new ConversationViewModel
                {
                    CurrentUserId = currentUserId,
                    ReceiverId = receiverId,
                    ChatUsers = chatUsers,
                    Messages = messageDTOs
                };

                return ServiceResult<ConversationViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation view model for users {CurrentUserId} and {ReceiverId}", currentUserId, receiverId);
                return ServiceResult<ConversationViewModel>.Failure("An error occurred while loading conversation");
            }
        }

        public async Task<ServiceResult<List<ChatMessageDTO>>> GetMessagesAsync(string currentUserId, GetMessagesRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult<List<ChatMessageDTO>>.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.ReceiverId))
                {
                    return ServiceResult<List<ChatMessageDTO>>.Failure("Receiver ID is required");
                }

                var messages = await _chatService.GetConversationMessagesAsync(currentUserId, request.ReceiverId, request.Page, request.PageSize);
                var messageDTOs = MapToChatMessageDTOs(messages).OrderBy(m => m.CreatedAt).ToList(); // Show oldest first

                return ServiceResult<List<ChatMessageDTO>>.Success(messageDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for user {UserId}", currentUserId);
                return ServiceResult<List<ChatMessageDTO>>.Failure("An error occurred while loading messages");
            }
        }

        public async Task<ServiceResult<ChatMessageDTO>> SendMessageAsync(string currentUserId, SendMessageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult<ChatMessageDTO>.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.ReceiverId) || string.IsNullOrEmpty(request.Message))
                {
                    return ServiceResult<ChatMessageDTO>.Failure("Invalid message data");
                }

                var message = await _chatService.SendMessageAsync(currentUserId, request.ReceiverId, request.Message, request.ReplyToMessageId);

                if (message != null)
                {
                    var messageDTO = MapToChatMessageDTO(message);
                    return ServiceResult<ChatMessageDTO>.Success(messageDTO);
                }

                return ServiceResult<ChatMessageDTO>.Failure("Failed to send message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from user {UserId}", currentUserId);
                return ServiceResult<ChatMessageDTO>.Failure("An error occurred while sending message");
            }
        }

        public async Task<ServiceResult> MarkMessageAsReadAsync(string currentUserId, MarkAsReadRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.MessageId))
                {
                    return ServiceResult.Failure("Message ID is required");
                }

                var result = await _chatService.MarkMessageAsReadAsync(request.MessageId, currentUserId);

                if (result)
                {
                    return ServiceResult.Success("Message marked as read");
                }

                return ServiceResult.Failure("Failed to mark message as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read for user {UserId}", currentUserId);
                return ServiceResult.Failure("An error occurred while marking message as read");
            }
        }

        public async Task<ServiceResult<int>> GetUnreadCountAsync(string currentUserId, GetUnreadCountRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult<int>.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.SenderId))
                {
                    return ServiceResult<int>.Failure("Sender ID is required");
                }

                var count = await _chatService.GetUnreadMessageCountAsync(currentUserId, request.SenderId);

                return ServiceResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", currentUserId);
                return ServiceResult<int>.Failure("An error occurred while getting unread count");
            }
        }

        public async Task<ServiceResult> DeleteMessageAsync(string currentUserId, DeleteMessageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.MessageId))
                {
                    return ServiceResult.Failure("Message ID is required");
                }

                var result = await _chatService.DeleteMessageAsync(request.MessageId, currentUserId);

                if (result)
                {
                    return ServiceResult.Success("Message deleted successfully");
                }

                return ServiceResult.Failure("Failed to delete message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message for user {UserId}", currentUserId);
                return ServiceResult.Failure("An error occurred while deleting message");
            }
        }

        public async Task<ServiceResult> EditMessageAsync(string currentUserId, EditMessageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.MessageId) || string.IsNullOrEmpty(request.NewContent))
                {
                    return ServiceResult.Failure("Message ID and new content are required");
                }

                var result = await _chatService.EditMessageAsync(request.MessageId, request.NewContent, currentUserId);

                if (result)
                {
                    return ServiceResult.Success("Message edited successfully");
                }

                return ServiceResult.Failure("Failed to edit message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message for user {UserId}", currentUserId);
                return ServiceResult.Failure("An error occurred while editing message");
            }
        }

        private List<ChatMessageDTO> MapToChatMessageDTOs(List<MessageEntity> messages)
        {
            return messages.Select(MapToChatMessageDTO).ToList();
        }
        private ChatMessageDTO MapToChatMessageDTO(MessageEntity message)
        {
            return new ChatMessageDTO
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.MessageContent,
                MessageType = message.MessageType ?? "text",
                IsRead = message.IsRead ?? false,
                ReplyToMessageId = message.ReplyToMessageId,
                IsEdited = message.IsEdited ?? false,
                CreatedAt = message.MessageCreatedAt,
                SenderName = message.Sender?.Username ?? "Unknown",
                SenderAvatar = message.Sender?.UserImage
            };
        }
        private async Task<List<ChatUserDTO>> MapToChatUserDTOsAsync(List<Account> users, string currentUserId)
        {
            var chatUsers = new List<ChatUserDTO>();

            foreach (var user in users)
            {
                var lastMessage = await _chatService.GetLastMessageBetweenUsersAsync(currentUserId, user.UserId);
                var unreadCount = await _chatService.GetUnreadMessageCountAsync(currentUserId, user.UserId);

                chatUsers.Add(new ChatUserDTO
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    UserImage = user.UserImage,
                    LastActive = user.LastLogin, // Use LastLogin as LastActive
                    IsOnline = false, // Default to false, can be updated with real-time status
                    LastMessage = lastMessage?.MessageContent,
                    LastMessageTime = lastMessage?.MessageCreatedAt,
                    UnreadCount = unreadCount
                });
            }

            return chatUsers.OrderByDescending(u => u.LastMessageTime).ToList();
        }
    }
}
