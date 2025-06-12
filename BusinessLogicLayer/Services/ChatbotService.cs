using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace BusinessLogicLayer.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly IChatbotRepo _chatbotRepo;
        private readonly IUserRepo _userRepo;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatbotService> _logger;
        private readonly IMemoryCache _cache;

        public ChatbotService(
            IChatbotRepo chatbotRepo,
            IUserRepo userRepo,
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<ChatbotService> logger,
            IMemoryCache cache)
        {
            _chatbotRepo = chatbotRepo;
            _userRepo = userRepo;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
        }
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

                var apiKey = _configuration["GeminiApiKey"];
                var apiUrl = _configuration["GeminiApiUrl"];

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiUrl))
                {
                    throw new InvalidOperationException("Gemini API configuration is missing");
                }

                // Get user context to personalize responses
                var userContext = await GetUserContextAsync(userId);

                // Build the system prompt for educational context
                var systemPrompt = BuildSystemPrompt(userContext, context);

                // Get recent conversation history for context
                var recentHistory = await GetRecentConversationHistoryAsync(userId, 3);
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
                        temperature = 0.7,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 1024,
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
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson); var botResponse = geminiResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text
                        ?? "Sorry, I cannot process your question at this time. Please try again later.";

                    // Cache common educational responses for 1 hour
                    if (IsCommonEducationalQuestion(userMessage))
                    {
                        _cache.Set(cacheKey, botResponse, TimeSpan.FromHours(1));
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
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return "Sorry, an error occurred while processing your question. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatbotService.GetResponseAsync");
                return "Sorry, an error occurred. Please try again later.";
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
            prompt.AppendLine("You are BrainStorm Bot, the intelligent AI assistant of the BrainStormEra learning platform.");
            prompt.AppendLine("Your tasks are:");
            prompt.AppendLine("1. Support students in learning and answering academic questions");
            prompt.AppendLine("2. Provide information about courses, lessons and content on the platform");
            prompt.AppendLine("3. Help explain difficult concepts in an easy-to-understand way");
            prompt.AppendLine("4. Encourage and guide students during their learning process");
            prompt.AppendLine();
            prompt.AppendLine("Please respond in English, concisely, friendly and helpfully.");
            prompt.AppendLine("If you don't know the answer, be honest and suggest ways to find out more.");

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

            var context = new StringBuilder();
            context.AppendLine("Recent conversation history:");

            foreach (var conversation in history.OrderBy(c => c.ConversationTime))
            {
                context.AppendLine($"User: {conversation.UserMessage}");
                context.AppendLine($"Bot: {conversation.BotResponse}");
            }

            return context.ToString();
        }
        private bool IsCommonEducationalQuestion(string message)
        {
            var commonKeywords = new[]
            {
                "what is", "definition", "concept", "explain", "guide",
                "how to", "method", "example", "formula", "principle"
            };

            return commonKeywords.Any(keyword =>
                message.ToLower().Contains(keyword.ToLower()));
        }
    }

    // DTOs for Gemini API response
    public class GeminiResponse
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
}









