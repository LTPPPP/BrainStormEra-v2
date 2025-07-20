using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;

namespace BusinessLogicLayer.Services.Implementations
{
    public class ChatbotService : IChatbotService
    {
        private readonly IChatbotRepo _chatbotRepo;
        private readonly IUserRepo _userRepo;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatbotService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IPageContextService _pageContextService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChatbotService(
            IChatbotRepo chatbotRepo,
            IUserRepo userRepo,
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<ChatbotService> logger,
            IMemoryCache cache,
            IPageContextService pageContextService,
            IHttpContextAccessor httpContextAccessor)
        {
            _chatbotRepo = chatbotRepo;
            _userRepo = userRepo;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _pageContextService = pageContextService;
            _httpContextAccessor = httpContextAccessor;
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

        #region IChatbotService Implementation

        public async Task<string> GetResponseAsync(string userMessage, string userId, string? context = null)
        {
            try
            {
                // Create cache key for similar questions
                var cacheKey = $"chatbot_response_{GetMessageHash(userMessage.ToLower().Trim())}";
                // Try to get cached response for common questions
                if (_cache.TryGetValue(cacheKey, out string? cachedResponse) && !string.IsNullOrEmpty(cachedResponse))
                {
                    _logger.LogInformation($"Using cached response for message: {userMessage}");

                    // Still save the conversation for tracking
                    await _chatbotRepo.SaveConversationAsync(new ChatbotConversation
                    {
                        ConversationId = Guid.NewGuid().ToString(),
                        UserId = userId,
                        ConversationTime = DateTime.Now,
                        UserMessage = userMessage,
                        BotResponse = cachedResponse,
                        ConversationContext = context
                    });

                    return cachedResponse;
                }

                var apiKey = EnvironmentHelper.GetChatbotApiKey();
                var apiUrl = EnvironmentHelper.GetChatbotApiUrl();

                // Fallback to configuration if environment variables are not set
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = _configuration["ChatbotApiKey"];
                }
                if (string.IsNullOrEmpty(apiUrl))
                {
                    apiUrl = _configuration["ChatbotApiUrl"];
                }

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiUrl))
                {
                    throw new InvalidOperationException("Chatbot API configuration is missing");
                }

                // Get user context to personalize responses
                var userContext = await GetUserContextAsync(userId);

                // Build the system prompt for educational context
                var systemPrompt = BuildSystemPrompt(userContext, context);

                // Get recent conversation history for context
                var recentHistory = await GetRecentConversationHistoryAsync(userId, EnvironmentHelper.GetChatbotHistoryLimit());
                var historyContext = BuildHistoryContext(recentHistory);

                // Prepare the request payload
                var requestPayload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = systemPrompt },
                                new { text = historyContext },
                                new { text = $"User question: {userMessage}" }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = EnvironmentHelper.GetChatbotTemperature(),
                        topK = EnvironmentHelper.GetChatbotTopK(),
                        topP = EnvironmentHelper.GetChatbotTopP(),
                        maxOutputTokens = EnvironmentHelper.GetChatbotMaxTokens(),
                        candidateCount = 1
                    },
                    safetySettings = new[]
                    {
                        new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                        new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                        new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                        new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_MEDIUM_AND_ABOVE" }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{apiUrl}?key={apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var chatbotResponse = JsonSerializer.Deserialize<ChatbotResponse>(responseJson);
                    var botResponse = chatbotResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text
                        ?? EnvironmentHelper.GetChatbotErrorMessage();

                    // Cache common educational responses
                    if (IsCommonEducationalQuestion(userMessage))
                    {
                        var cacheOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(EnvironmentHelper.GetChatbotCacheHours()),
                            Size = 1
                        };
                        _cache.Set(cacheKey, botResponse, cacheOptions);
                    }

                    // Save conversation to database
                    await _chatbotRepo.SaveConversationAsync(new ChatbotConversation
                    {
                        ConversationId = Guid.NewGuid().ToString(),
                        UserId = userId,
                        ConversationTime = DateTime.Now,
                        UserMessage = userMessage,
                        BotResponse = botResponse,
                        ConversationContext = context
                    });

                    return botResponse;
                }
                else
                {
                    _logger.LogError($"Chatbot API error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return EnvironmentHelper.GetChatbotApiErrorMessage();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatbotService.GetResponseAsync");
                return EnvironmentHelper.GetChatbotGeneralErrorMessage();
            }
        }

        public async Task<List<ChatbotConversation>> GetConversationHistoryAsync(string userId, int limit = 20)
        {
            var conversations = await _chatbotRepo.GetConversationHistoryAsync(userId, limit);
            return conversations.ToList();
        }

        public async Task SaveConversationAsync(ChatbotConversation conversation)
        {
            await _chatbotRepo.SaveConversationAsync(conversation);
        }

        public async Task<bool> RateFeedbackAsync(string conversationId, byte rating)
        {
            return await _chatbotRepo.UpdateFeedbackRatingAsync(conversationId, rating);
        }

        #endregion

        #region Private Methods

        private async Task<string> GetUserContextAsync(string userId)
        {
            var user = await _userRepo.GetUserWithEnrollmentsAndProgressAsync(userId);

            if (user == null) return "";

            var enrolledCourses = user.Enrollments?.Select(e => e.Course?.CourseName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>()
                .ToList() ?? new List<string>();

            var completedLessons = user.UserProgresses?.Count(up => up.IsCompleted == true) ?? 0;
            var totalLessons = user.UserProgresses?.Count() ?? 0;

            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine($"User {user.FullName} ({user.UserEmail})");

            if (enrolledCourses.Any())
            {
                contextBuilder.AppendLine($"Currently enrolled in: {string.Join(", ", enrolledCourses)}");
            }

            if (totalLessons > 0)
            {
                var completionRate = (double)completedLessons / totalLessons * 100;
                contextBuilder.AppendLine($"Learning progress: {completedLessons}/{totalLessons} lessons ({completionRate:F1}%)");
            }

            return contextBuilder.ToString();
        }

        private string BuildSystemPrompt(string userContext, string? pageContext)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine(EnvironmentHelper.GetChatbotSystemPrompt());

            if (!string.IsNullOrEmpty(userContext))
            {
                prompt.AppendLine($"User information: {userContext}");
            }

            if (!string.IsNullOrEmpty(pageContext))
            {
                prompt.AppendLine($"Current page context: {pageContext}");
            }

            return prompt.ToString();
        }

        private string GetMessageHash(string message)
        {
            // Simple hash for caching similar questions
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToBase64String(hash).Substring(0, 16);
        }

        private async Task<List<ChatbotConversation>> GetRecentConversationHistoryAsync(string userId, int limit = 3)
        {
            var conversations = await _chatbotRepo.GetConversationHistoryAsync(userId, limit);
            return conversations.ToList();
        }

        private string BuildHistoryContext(List<ChatbotConversation> history)
        {
            if (!history.Any()) return "";

            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Recent conversation history:");
            foreach (var conversation in history.TakeLast(3))
            {
                contextBuilder.AppendLine($"User: {conversation.UserMessage}");
                contextBuilder.AppendLine($"Bot: {conversation.BotResponse}");
            }
            return contextBuilder.ToString();
        }

        private bool IsCommonEducationalQuestion(string message)
        {
            var commonQuestions = new[]
            {
                "what is", "how to", "explain", "define", "difference between",
                "benefits of", "advantages of", "disadvantages of", "types of",
                "examples of", "steps to", "guide to", "tutorial", "help"
            };

            var lowerMessage = message.ToLower();
            return commonQuestions.Any(q => lowerMessage.Contains(q));
        }

        #endregion

        #region Response Classes

        public class ChatbotResponse
        {
            public List<Candidate>? candidates { get; set; }
        }

        public class Candidate
        {
            public Content? content { get; set; }
        }

        public class Content
        {
            public List<Part>? parts { get; set; }
        }

        public class Part
        {
            public string? text { get; set; }
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
                var botResponse = await GetResponseAsync(message, userId, enhancedContext);

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
                var conversations = await GetConversationHistoryAsync(userId, limit);

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
                var success = await RateFeedbackAsync(conversationId, (byte)rating);

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

        #region Controller Business Logic Methods

        public async Task<ChatControllerResult> ProcessChatForControllerAsync(ChatRequest request)
        {
            var result = new ChatControllerResult();

            try
            {
                // Check authentication
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    result.StatusCode = 401;
                    result.ErrorResponse = new { error = "User not authenticated" };
                    _logger.LogWarning("Unauthorized chat attempt");
                    return result;
                }

                // Validate request
                var validationResult = ValidateChatRequest(request);
                if (!validationResult.IsValid)
                {
                    result.StatusCode = 400;
                    result.ErrorResponse = new
                    {
                        error = "Validation failed",
                        errors = validationResult.ValidationErrors
                    };
                    _logger.LogWarning($"Chat request validation failed for user {userId}: {string.Join(", ", validationResult.ValidationErrors)}");
                    return result;
                }

                // Process chat
                var chatResult = await ProcessChatAsync(
                    request.Message,
                    request.Context,
                    request.PagePath,
                    request.CourseId,
                    request.ChapterId,
                    request.LessonId
                );

                if (!chatResult.IsSuccess)
                {
                    if (chatResult.ValidationErrors.Any())
                    {
                        result.StatusCode = 400;
                        result.ErrorResponse = new
                        {
                            error = "Validation failed",
                            errors = chatResult.ValidationErrors
                        };
                    }
                    else if (!string.IsNullOrEmpty(chatResult.ErrorMessage))
                    {
                        result.StatusCode = 500;
                        result.ErrorResponse = new { error = chatResult.ErrorMessage };
                    }
                    else
                    {
                        result.StatusCode = 400;
                        result.ErrorResponse = new { error = "Failed to process chat request" };
                    }
                    return result;
                }

                result.IsSuccess = true;
                result.StatusCode = 200;
                result.SuccessResponse = new
                {
                    message = chatResult.BotResponse ?? "",
                    timestamp = chatResult.Timestamp ?? DateTime.Now
                };

                return result;
            }
            catch (Exception ex)
            {
                result.StatusCode = 500;
                result.ErrorResponse = new { error = "Internal server error" };
                _logger.LogError(ex, $"Error in ProcessChatForControllerAsync for user {GetCurrentUserId()}");
                return result;
            }
        }

        public async Task<ChatControllerResult> GetHistoryForControllerAsync(int limit = 20)
        {
            var result = new ChatControllerResult();

            try
            {
                // Check authentication
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    result.StatusCode = 401;
                    result.ErrorResponse = new { error = "User not authenticated" };
                    _logger.LogWarning("Unauthorized chat history request");
                    return result;
                }

                // Get conversation history
                var historyResult = await GetConversationHistoryAsync(limit);

                if (!historyResult.IsSuccess)
                {
                    if (historyResult.ValidationErrors.Any())
                    {
                        result.StatusCode = 400;
                        result.ErrorResponse = new
                        {
                            error = "Validation failed",
                            errors = historyResult.ValidationErrors
                        };
                    }
                    else if (!string.IsNullOrEmpty(historyResult.ErrorMessage))
                    {
                        result.StatusCode = 500;
                        result.ErrorResponse = new { error = historyResult.ErrorMessage };
                    }
                    else
                    {
                        result.StatusCode = 400;
                        result.ErrorResponse = new { error = "Failed to retrieve conversation history" };
                    }
                    return result;
                }

                // Format response
                var formattedHistory = historyResult.Conversations.Select(h => new
                {
                    id = h.ConversationId,
                    userMessage = h.UserMessage,
                    botResponse = h.BotResponse,
                    timestamp = h.ConversationTime,
                    rating = h.FeedbackRating
                }).ToList();

                result.IsSuccess = true;
                result.StatusCode = 200;
                result.SuccessResponse = formattedHistory;

                return result;
            }
            catch (Exception ex)
            {
                result.StatusCode = 500;
                result.ErrorResponse = new { error = "Internal server error" };
                _logger.LogError(ex, $"Error in GetHistoryForControllerAsync for user {GetCurrentUserId()}");
                return result;
            }
        }

        public async Task<ChatControllerResult> SubmitFeedbackForControllerAsync(FeedbackRequest request)
        {
            var result = new ChatControllerResult();

            try
            {
                // Check authentication
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    result.StatusCode = 401;
                    result.ErrorResponse = new { error = "User not authenticated" };
                    _logger.LogWarning("Unauthorized feedback submission");
                    return result;
                }

                // Validate request
                var validationResult = ValidateFeedbackRequest(request);
                if (!validationResult.IsValid)
                {
                    result.StatusCode = 400;
                    result.ErrorResponse = new
                    {
                        error = "Invalid request data",
                        errors = validationResult.ValidationErrors
                    };
                    _logger.LogWarning($"Feedback request validation failed for user {userId}: {string.Join(", ", validationResult.ValidationErrors)}");
                    return result;
                }

                // Submit feedback
                var feedbackResult = await SubmitFeedbackAsync(request.ConversationId, request.Rating);

                if (!feedbackResult.IsSuccess)
                {
                    if (feedbackResult.ValidationErrors.Any())
                    {
                        result.StatusCode = 400;
                        result.ErrorResponse = new
                        {
                            error = "Invalid request data",
                            errors = feedbackResult.ValidationErrors
                        };
                    }
                    else if (!string.IsNullOrEmpty(feedbackResult.ErrorMessage))
                    {
                        if (feedbackResult.ErrorMessage.Contains("not found"))
                        {
                            result.StatusCode = 404;
                            result.ErrorResponse = new { error = feedbackResult.ErrorMessage };
                        }
                        else
                        {
                            result.StatusCode = 500;
                            result.ErrorResponse = new { error = feedbackResult.ErrorMessage };
                        }
                    }
                    else
                    {
                        result.StatusCode = 400;
                        result.ErrorResponse = new { error = "Failed to submit feedback" };
                    }
                    return result;
                }

                result.IsSuccess = true;
                result.StatusCode = 200;
                result.SuccessResponse = new { message = feedbackResult.Message };

                return result;
            }
            catch (Exception ex)
            {
                result.StatusCode = 500;
                result.ErrorResponse = new { error = "Internal server error" };
                _logger.LogError(ex, $"Error in SubmitFeedbackForControllerAsync for user {GetCurrentUserId()}");
                return result;
            }
        }

        #endregion

        #region Controller Result Classes

        public class ChatControllerResult
        {
            public bool IsSuccess { get; set; }
            public int StatusCode { get; set; }
            public object? SuccessResponse { get; set; }
            public object? ErrorResponse { get; set; }
        }

        public class ChatRequest
        {
            public string Message { get; set; } = string.Empty;
            public string? Context { get; set; }
            public string? PagePath { get; set; }
            public string? CourseId { get; set; }
            public string? ChapterId { get; set; }
            public string? LessonId { get; set; }
        }

        public class FeedbackRequest
        {
            public string ConversationId { get; set; } = string.Empty;
            public int Rating { get; set; }
        }

        #endregion

        #region Additional Validation Methods

        private ChatValidationResult ValidateChatRequest(ChatRequest request)
        {
            var result = new ChatValidationResult { IsValid = true };

            if (request == null)
            {
                AddValidationError(result, "Request cannot be null");
                return result;
            }

            // Reuse existing message validation
            var messageValidation = ValidateChatMessage(request.Message);
            if (!messageValidation.IsValid)
            {
                result.IsValid = false;
                result.ValidationErrors.AddRange(messageValidation.ValidationErrors);
            }

            return result;
        }

        private ChatValidationResult ValidateFeedbackRequest(FeedbackRequest request)
        {
            var result = new ChatValidationResult { IsValid = true };

            if (request == null)
            {
                AddValidationError(result, "Request cannot be null");
                return result;
            }

            // Reuse existing feedback validation
            var feedbackValidation = ValidateFeedback(request.ConversationId, request.Rating);
            if (!feedbackValidation.IsValid)
            {
                result.IsValid = false;
                result.ValidationErrors.AddRange(feedbackValidation.ValidationErrors);
            }

            return result;
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








