using BusinessLogicLayer.DTOs.Common;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Utilities;
using DataAccessLayer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Service for managing chat URLs with secure hashing
    /// </summary>
    public class ChatUrlService : IChatUrlService
    {
        private readonly ChatUrlHasher _urlHasher;
        private readonly IChatService _chatService;
        private readonly ILogger<ChatUrlService> _logger;
        private readonly IConfiguration _configuration;

        public ChatUrlService(
            ChatUrlHasher urlHasher,
            IChatService chatService,
            ILogger<ChatUrlService> logger,
            IConfiguration configuration)
        {
            _urlHasher = urlHasher;
            _chatService = chatService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Generate a secure chat URL for a conversation
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="userId1">First user ID</param>
        /// <param name="userId2">Second user ID</param>
        /// <returns>Service result with the generated URL</returns>
        public async Task<ServiceResult<string>> GenerateChatUrlAsync(string conversationId, string userId1, string userId2)
        {
            try
            {
                // Verify that both users have access to the conversation
                var canUser1Access = await _chatService.CanUserAccessConversationAsync(userId1, conversationId);
                var canUser2Access = await _chatService.CanUserAccessConversationAsync(userId2, conversationId);

                if (!canUser1Access || !canUser2Access)
                {
                    return ServiceResult<string>.Failure("One or more users do not have access to this conversation");
                }

                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";
                var url = _urlHasher.GenerateChatUrl(conversationId, userId1, userId2, baseUrl);

                _logger.LogInformation($"Generated chat URL for conversation {conversationId}");
                return ServiceResult<string>.Success(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating chat URL for conversation {conversationId}");
                return ServiceResult<string>.Failure("Failed to generate chat URL");
            }
        }

        /// <summary>
        /// Generate a quick chat URL between two users
        /// </summary>
        /// <param name="currentUserId">Current user ID</param>
        /// <param name="targetUserId">Target user ID</param>
        /// <returns>Service result with the generated URL</returns>
        public async Task<ServiceResult<string>> GenerateQuickChatUrlAsync(string currentUserId, string targetUserId)
        {
            try
            {
                // Get or create conversation between users
                var conversation = await _chatService.GetOrCreateConversationAsync(currentUserId, targetUserId);
                if (conversation == null)
                {
                    return ServiceResult<string>.Failure("Failed to create conversation");
                }

                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";
                var url = _urlHasher.GenerateQuickChatUrl(currentUserId, targetUserId, baseUrl);

                _logger.LogInformation($"Generated quick chat URL between {currentUserId} and {targetUserId}");
                return ServiceResult<string>.Success(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating quick chat URL between {currentUserId} and {targetUserId}");
                return ServiceResult<string>.Failure("Failed to generate quick chat URL");
            }
        }

        /// <summary>
        /// Generate a message-specific URL
        /// </summary>
        /// <param name="messageId">Message ID</param>
        /// <param name="userId">User ID accessing the message</param>
        /// <returns>Service result with the generated URL</returns>
        public async Task<ServiceResult<string>> GenerateMessageUrlAsync(string messageId, string userId)
        {
            try
            {
                var message = await _chatService.GetMessageByIdAsync(messageId);
                if (message == null)
                {
                    return ServiceResult<string>.Failure("Message not found");
                }

                // Check if user has access to this message
                if (message.SenderId != userId && message.ReceiverId != userId)
                {
                    return ServiceResult<string>.Failure("Access denied to this message");
                }

                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";
                var url = _urlHasher.GenerateMessageUrl(messageId, message.ConversationId, userId, baseUrl);

                _logger.LogInformation($"Generated message URL for message {messageId}");
                return ServiceResult<string>.Success(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating message URL for message {messageId}");
                return ServiceResult<string>.Failure("Failed to generate message URL");
            }
        }

        /// <summary>
        /// Verify and process a secure chat URL
        /// </summary>
        /// <param name="hash">The hash from the URL</param>
        /// <param name="currentUserId">Current user ID</param>
        /// <returns>Service result with conversation details</returns>
        public async Task<ServiceResult<ChatUrlAccessResult>> VerifyAndAccessChatUrlAsync(string hash, string currentUserId)
        {
            try
            {
                var chatData = _urlHasher.VerifyChatHash(hash);
                if (chatData == null)
                {
                    return ServiceResult<ChatUrlAccessResult>.Failure("Invalid or expired chat URL");
                }

                // Check if current user is one of the conversation participants
                if (chatData.UserId1 != currentUserId && chatData.UserId2 != currentUserId)
                {
                    return ServiceResult<ChatUrlAccessResult>.Failure("Access denied to this conversation");
                }

                // Verify conversation still exists and user has access
                var canAccess = await _chatService.CanUserAccessConversationAsync(currentUserId, chatData.ConversationId);
                if (!canAccess)
                {
                    return ServiceResult<ChatUrlAccessResult>.Failure("Access denied to this conversation");
                }

                var otherUserId = chatData.UserId1 == currentUserId ? chatData.UserId2 : chatData.UserId1;

                var result = new ChatUrlAccessResult
                {
                    ConversationId = chatData.ConversationId,
                    CurrentUserId = currentUserId,
                    OtherUserId = otherUserId,
                    AccessGranted = true,
                    RedirectUrl = $"/Chat/Conversation?userId={otherUserId}"
                };

                _logger.LogInformation($"Successfully verified chat URL access for user {currentUserId}");
                return ServiceResult<ChatUrlAccessResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying chat URL for user {currentUserId}");
                return ServiceResult<ChatUrlAccessResult>.Failure("Failed to verify chat URL");
            }
        }

        /// <summary>
        /// Verify and process a quick chat URL
        /// </summary>
        /// <param name="hash">The hash from the URL</param>
        /// <param name="currentUserId">Current user ID</param>
        /// <returns>Service result with quick chat details</returns>
        public async Task<ServiceResult<ChatUrlAccessResult>> VerifyAndAccessQuickChatAsync(string hash, string currentUserId)
        {
            try
            {
                var quickData = _urlHasher.VerifyQuickChatHash(hash);
                if (quickData == null)
                {
                    return ServiceResult<ChatUrlAccessResult>.Failure("Invalid or expired quick chat URL");
                }

                // Check if current user is one of the participants
                if (quickData.CurrentUserId != currentUserId && quickData.TargetUserId != currentUserId)
                {
                    return ServiceResult<ChatUrlAccessResult>.Failure("Access denied to this chat");
                }

                // Get or create conversation
                var conversation = await _chatService.GetOrCreateConversationAsync(quickData.CurrentUserId, quickData.TargetUserId);
                if (conversation == null)
                {
                    return ServiceResult<ChatUrlAccessResult>.Failure("Failed to create conversation");
                }

                var otherUserId = quickData.CurrentUserId == currentUserId ? quickData.TargetUserId : quickData.CurrentUserId;

                var result = new ChatUrlAccessResult
                {
                    ConversationId = conversation.ConversationId,
                    CurrentUserId = currentUserId,
                    OtherUserId = otherUserId,
                    AccessGranted = true,
                    RedirectUrl = $"/Chat/Conversation?userId={otherUserId}"
                };

                _logger.LogInformation($"Successfully verified quick chat URL access for user {currentUserId}");
                return ServiceResult<ChatUrlAccessResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying quick chat URL for user {currentUserId}");
                return ServiceResult<ChatUrlAccessResult>.Failure("Failed to verify quick chat URL");
            }
        }

        /// <summary>
        /// Verify and process a message URL
        /// </summary>
        /// <param name="hash">The hash from the URL</param>
        /// <param name="currentUserId">Current user ID</param>
        /// <returns>Service result with message access details</returns>
        public async Task<ServiceResult<MessageUrlAccessResult>> VerifyAndAccessMessageUrlAsync(string hash, string currentUserId)
        {
            try
            {
                var messageData = _urlHasher.VerifyMessageHash(hash);
                if (messageData == null)
                {
                    return ServiceResult<MessageUrlAccessResult>.Failure("Invalid or expired message URL");
                }

                // Check if current user matches the URL user
                if (messageData.UserId != currentUserId)
                {
                    return ServiceResult<MessageUrlAccessResult>.Failure("Access denied to this message");
                }

                var message = await _chatService.GetMessageByIdAsync(messageData.MessageId);
                if (message == null)
                {
                    return ServiceResult<MessageUrlAccessResult>.Failure("Message not found");
                }

                // Double-check access to the conversation
                var canAccess = await _chatService.CanUserAccessConversationAsync(currentUserId, messageData.ConversationId);
                if (!canAccess)
                {
                    return ServiceResult<MessageUrlAccessResult>.Failure("Access denied to this conversation");
                }

                var otherUserId = message.SenderId == currentUserId ? message.ReceiverId : message.SenderId;

                var result = new MessageUrlAccessResult
                {
                    MessageId = messageData.MessageId,
                    ConversationId = messageData.ConversationId,
                    CurrentUserId = currentUserId,
                    OtherUserId = otherUserId,
                    Message = message,
                    AccessGranted = true,
                    RedirectUrl = $"/Chat/Conversation?userId={otherUserId}&highlight={messageData.MessageId}"
                };

                _logger.LogInformation($"Successfully verified message URL access for user {currentUserId}");
                return ServiceResult<MessageUrlAccessResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying message URL for user {currentUserId}");
                return ServiceResult<MessageUrlAccessResult>.Failure("Failed to verify message URL");
            }
        }

        /// <summary>
        /// Generate multiple URL types for a conversation
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="currentUserId">Current user ID</param>
        /// <param name="otherUserId">Other user ID</param>
        /// <returns>Service result with all URL types</returns>
        public Task<ServiceResult<ChatUrlBundle>> GenerateChatUrlBundleAsync(string conversationId, string currentUserId, string otherUserId)
        {
            try
            {
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";

                var secureUrl = _urlHasher.GenerateChatUrl(conversationId, currentUserId, otherUserId, baseUrl);
                var quickUrl = _urlHasher.GenerateQuickChatUrl(currentUserId, otherUserId, baseUrl);

                var bundle = new ChatUrlBundle
                {
                    ConversationId = conversationId,
                    SecureUrl = secureUrl,
                    QuickUrl = quickUrl,
                    DirectUrl = $"{baseUrl.TrimEnd('/')}/Chat/Conversation?userId={otherUserId}",
                    GeneratedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                _logger.LogInformation($"Generated chat URL bundle for conversation {conversationId}");
                return Task.FromResult(ServiceResult<ChatUrlBundle>.Success(bundle));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating chat URL bundle for conversation {conversationId}");
                return Task.FromResult(ServiceResult<ChatUrlBundle>.Failure("Failed to generate chat URL bundle"));
            }
        }
    }

    /// <summary>
    /// Result of chat URL access verification
    /// </summary>
    public class ChatUrlAccessResult
    {
        public string ConversationId { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
        public string OtherUserId { get; set; } = string.Empty;
        public bool AccessGranted { get; set; }
        public string RedirectUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of message URL access verification
    /// </summary>
    public class MessageUrlAccessResult
    {
        public string MessageId { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
        public string OtherUserId { get; set; } = string.Empty;
        public MessageEntity? Message { get; set; }
        public bool AccessGranted { get; set; }
        public string RedirectUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bundle of different chat URL types
    /// </summary>
    public class ChatUrlBundle
    {
        public string ConversationId { get; set; } = string.Empty;
        public string SecureUrl { get; set; } = string.Empty;
        public string QuickUrl { get; set; } = string.Empty;
        public string DirectUrl { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
