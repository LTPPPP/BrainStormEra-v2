using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace BrainStormEra_MVC.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly BrainStormEraContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatbotService> _logger;
        private readonly IMemoryCache _cache;

        public ChatbotService(
            BrainStormEraContext context,
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<ChatbotService> logger,
            IMemoryCache cache)
        {
            _context = context;
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
                    await SaveConversationAsync(new ChatbotConversation
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
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

                    var botResponse = geminiResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text
                        ?? "Xin lỗi, tôi không thể xử lý câu hỏi của bạn lúc này. Vui lòng thử lại sau.";

                    // Cache common educational responses for 1 hour
                    if (IsCommonEducationalQuestion(userMessage))
                    {
                        _cache.Set(cacheKey, botResponse, TimeSpan.FromHours(1));
                    }

                    // Save conversation to database
                    await SaveConversationAsync(new ChatbotConversation
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
                    return "Xin lỗi, có lỗi xảy ra khi xử lý câu hỏi của bạn. Vui lòng thử lại sau.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatbotService.GetResponseAsync");
                return "Xin lỗi, có lỗi xảy ra. Vui lòng thử lại sau.";
            }
        }

        public async Task<List<ChatbotConversation>> GetConversationHistoryAsync(string userId, int limit = 20)
        {
            return await _context.ChatbotConversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.ConversationTime)
                .Take(limit)
                .ToListAsync();
        }

        public async Task SaveConversationAsync(ChatbotConversation conversation)
        {
            _context.ChatbotConversations.Add(conversation);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> RateFeedbackAsync(string conversationId, byte rating)
        {
            var conversation = await _context.ChatbotConversations
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (conversation != null)
            {
                conversation.FeedbackRating = rating;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        private async Task<string> GetUserContextAsync(string userId)
        {
            var user = await _context.Accounts
                .Include(a => a.Enrollments)
                    .ThenInclude(e => e.Course)
                .Include(a => a.UserProgresses)
                    .ThenInclude(up => up.Lesson)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (user == null) return ""; var enrolledCourses = user.Enrollments?.Select(e => e.Course?.CourseName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>()
                .ToList() ?? new List<string>();

            var completedLessons = user.UserProgresses?.Count(up => up.IsCompleted == true) ?? 0;
            var totalLessons = user.UserProgresses?.Count() ?? 0;

            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine($"Người dùng {user.FullName} ({user.UserEmail})");

            if (enrolledCourses.Any())
            {
                contextBuilder.AppendLine($"Đang theo học: {string.Join(", ", enrolledCourses)}");
            }

            if (totalLessons > 0)
            {
                var completionRate = (double)completedLessons / totalLessons * 100;
                contextBuilder.AppendLine($"Tiến độ học tập: {completedLessons}/{totalLessons} bài ({completionRate:F1}%)");
            }

            return contextBuilder.ToString();
        }

        private string BuildSystemPrompt(string userContext, string? pageContext)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Bạn là BrainStorm Bot, trợ lý AI thông minh của nền tảng học tập BrainStormEra.");
            prompt.AppendLine("Nhiệm vụ của bạn là:");
            prompt.AppendLine("1. Hỗ trợ học sinh trong việc học tập và trả lời câu hỏi học thuật");
            prompt.AppendLine("2. Cung cấp thông tin về các khóa học, bài học và nội dung trên nền tảng");
            prompt.AppendLine("3. Giúp giải thích các khái niệm khó hiểu một cách dễ hiểu");
            prompt.AppendLine("4. Động viên và hướng dẫn học sinh trong quá trình học tập");
            prompt.AppendLine();
            prompt.AppendLine("Hãy trả lời bằng tiếng Việt, ngắn gọn, thân thiện và hữu ích.");
            prompt.AppendLine("Nếu không biết câu trả lời, hãy thành thật nói và gợi ý cách tìm hiểu thêm.");

            if (!string.IsNullOrEmpty(userContext))
            {
                prompt.AppendLine($"Thông tin người dùng: {userContext}");
            }

            if (!string.IsNullOrEmpty(pageContext))
            {
                prompt.AppendLine($"Ngữ cảnh trang hiện tại: {pageContext}");
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
            return await _context.ChatbotConversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.ConversationTime)
                .Take(limit)
                .ToListAsync();
        }

        private string BuildHistoryContext(List<ChatbotConversation> history)
        {
            if (!history.Any()) return "";

            var context = new StringBuilder();
            context.AppendLine("Lịch sử trò chuyện gần đây:");

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
                "là gì", "định nghĩa", "khái niệm", "giải thích", "hướng dẫn",
                "cách thức", "phương pháp", "ví dụ", "công thức", "nguyên lý"
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
