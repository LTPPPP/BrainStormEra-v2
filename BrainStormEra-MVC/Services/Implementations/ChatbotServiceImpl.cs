using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace BrainStormEra_MVC.Services.Implementations
{
    public class ChatbotServiceImpl
    {
        private readonly IChatbotService _chatbotService;
        private readonly IPageContextService _pageContextService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ChatbotServiceImpl> _logger;

        public ChatbotServiceImpl(
            IChatbotService chatbotService,
            IPageContextService pageContextService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ChatbotServiceImpl> logger)
        {
            _chatbotService = chatbotService;
            _pageContextService = pageContextService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        #region Result Classes

        public class ChatResult
        {
            public bool IsSuccess { get; set; }
            public string? Message { get; set; }
            public string? BotResponse { get; set; }
            public DateTime? Timestamp { get; set; }
            public string? ConversationId { get; set; }
            public List<string> ValidationErrors { get; set; } = new List<string>();
            public string? ErrorMessage { get; set; }
        }

        public class ChatHistoryResult
        {
            public bool IsSuccess { get; set; }
            public List<ChatbotConversation> Conversations { get; set; } = new List<ChatbotConversation>();
            public List<string> ValidationErrors { get; set; } = new List<string>();
            public string? ErrorMessage { get; set; }
        }

        public class FeedbackResult
        {
            public bool IsSuccess { get; set; }
            public string? Message { get; set; }
            public List<string> ValidationErrors { get; set; } = new List<string>();
            public string? ErrorMessage { get; set; }
        }

        public class ChatValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> ValidationErrors { get; set; } = new List<string>();
        }

        #endregion

        #region Public Methods

        public async Task<ChatResult> ProcessChatAsync(string message, string? context = null,
            string? pagePath = null, string? courseId = null, string? chapterId = null, string? lessonId = null)
        {
            var result = new ChatResult();

            try
            {
                // Check authentication
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    result.ErrorMessage = "User not authenticated";
                    _logger.LogWarning("Unauthorized chat attempt");
                    return result;
                }

                // Validate message
                var validationResult = ValidateChatMessage(message);
                if (!validationResult.IsValid)
                {
                    result.ValidationErrors = validationResult.ValidationErrors;
                    _logger.LogWarning($"Chat message validation failed for user {userId}: {string.Join(", ", validationResult.ValidationErrors)}");
                    return result;
                }

                // Enhanced context with page information
                var enhancedContext = context;
                if (!string.IsNullOrEmpty(pagePath))
                {
                    try
                    {
                        var pageContext = await _pageContextService.GetPageContextAsync(
                            pagePath, courseId, chapterId, lessonId);

                        enhancedContext = string.IsNullOrEmpty(enhancedContext)
                            ? pageContext
                            : $"{context}. {pageContext}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to get page context for user {userId}");
                        // Continue without page context rather than failing
                    }
                }

                // Get bot response
                var botResponse = await _chatbotService.GetResponseAsync(message, userId, enhancedContext);

                if (string.IsNullOrEmpty(botResponse))
                {
                    result.ErrorMessage = "Failed to generate response";
                    _logger.LogError($"Empty bot response for user {userId}");
                    return result;
                }

                result.IsSuccess = true;
                result.BotResponse = botResponse;
                result.Timestamp = DateTime.Now;
                result.Message = "Response generated successfully";

                _logger.LogInformation($"Successful chat interaction for user {userId}");
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "An error occurred while processing your message";
                _logger.LogError(ex, $"Error in ProcessChatAsync for user {GetCurrentUserId()}");
                return result;
            }
        }

        public async Task<ChatHistoryResult> GetConversationHistoryAsync(int limit = 20)
        {
            var result = new ChatHistoryResult();

            try
            {
                // Check authentication
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    result.ErrorMessage = "User not authenticated";
                    _logger.LogWarning("Unauthorized chat history request");
                    return result;
                }

                // Validate limit
                var validationResult = ValidateHistoryLimit(limit);
                if (!validationResult.IsValid)
                {
                    result.ValidationErrors = validationResult.ValidationErrors;
                    _logger.LogWarning($"Invalid history limit for user {userId}: {limit}");
                    return result;
                }

                // Get conversation history
                var conversations = await _chatbotService.GetConversationHistoryAsync(userId, limit);

                result.IsSuccess = true;
                result.Conversations = conversations;

                _logger.LogInformation($"Retrieved {conversations.Count} conversations for user {userId}");
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "An error occurred while retrieving conversation history";
                _logger.LogError(ex, $"Error in GetConversationHistoryAsync for user {GetCurrentUserId()}");
                return result;
            }
        }

        public async Task<FeedbackResult> SubmitFeedbackAsync(string conversationId, int rating)
        {
            var result = new FeedbackResult();

            try
            {
                // Check authentication
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    result.ErrorMessage = "User not authenticated";
                    _logger.LogWarning("Unauthorized feedback submission");
                    return result;
                }

                // Validate feedback
                var validationResult = ValidateFeedback(conversationId, rating);
                if (!validationResult.IsValid)
                {
                    result.ValidationErrors = validationResult.ValidationErrors;
                    _logger.LogWarning($"Invalid feedback for user {userId}: ConversationId={conversationId}, Rating={rating}");
                    return result;
                }

                // Submit feedback
                var success = await _chatbotService.RateFeedbackAsync(conversationId, (byte)rating);

                if (success)
                {
                    result.IsSuccess = true;
                    result.Message = "Feedback submitted successfully";
                    _logger.LogInformation($"Feedback submitted for conversation {conversationId} by user {userId}");
                }
                else
                {
                    result.ErrorMessage = "Conversation not found";
                    _logger.LogWarning($"Conversation {conversationId} not found for feedback by user {userId}");
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "An error occurred while submitting feedback";
                _logger.LogError(ex, $"Error in SubmitFeedbackAsync for user {GetCurrentUserId()}");
                return result;
            }
        }

        #endregion

        #region Validation Methods

        private ChatValidationResult ValidateChatMessage(string message)
        {
            var result = new ChatValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(message))
            {
                AddValidationError(result, "Message is required");
            }
            else
            {
                if (message.Length > 2000)
                {
                    AddValidationError(result, "Message cannot exceed 2000 characters");
                }

                if (message.Length < 2)
                {
                    AddValidationError(result, "Message must be at least 2 characters long");
                }

                if (ContainsInappropriateContent(message))
                {
                    AddValidationError(result, "Message contains inappropriate content");
                }

                if (ContainsSpam(message))
                {
                    AddValidationError(result, "Message appears to be spam");
                }
            }

            return result;
        }

        private ChatValidationResult ValidateHistoryLimit(int limit)
        {
            var result = new ChatValidationResult { IsValid = true };

            if (limit < 1)
            {
                AddValidationError(result, "Limit must be at least 1");
            }
            else if (limit > 100)
            {
                AddValidationError(result, "Limit cannot exceed 100");
            }

            return result;
        }

        private ChatValidationResult ValidateFeedback(string conversationId, int rating)
        {
            var result = new ChatValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(conversationId))
            {
                AddValidationError(result, "Conversation ID is required");
            }
            else if (!IsValidGuid(conversationId))
            {
                AddValidationError(result, "Invalid conversation ID format");
            }

            if (rating < 1 || rating > 5)
            {
                AddValidationError(result, "Rating must be between 1 and 5");
            }

            return result;
        }

        #endregion

        #region Helper Methods

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;
        }

        private void AddValidationError(ChatValidationResult result, string error)
        {
            result.IsValid = false;
            result.ValidationErrors.Add(error);
        }

        private bool ContainsInappropriateContent(string message)
        {
            // Basic inappropriate content detection
            var inappropriateWords = new[]
            {
                "spam", "scam", "hack", "cheat", "illegal", "drugs",
                "violence", "threat", "abuse", "harassment"
            };

            var lowerMessage = message.ToLower();
            return inappropriateWords.Any(word => lowerMessage.Contains(word));
        }

        private bool ContainsSpam(string message)
        {
            // Check for excessive repetition
            if (Regex.IsMatch(message, @"(.)\1{10,}"))
                return true;

            // Check for excessive URLs
            var urlCount = Regex.Matches(message, @"http[s]?://[^\s]+").Count;
            if (urlCount > 3)
                return true;

            // Check for excessive caps
            var capsCount = message.Count(char.IsUpper);
            if (capsCount > message.Length * 0.7 && message.Length > 10)
                return true;

            return false;
        }

        private bool IsValidGuid(string input)
        {
            return Guid.TryParse(input, out _);
        }

        #endregion
    }
}
