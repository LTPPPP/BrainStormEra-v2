using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrainStormEra.Services
{
    public class ChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly ILogger<ChatbotService>? _logger;

        private const string ADMIN_TEMPLATE = @"
You are an AI assistant named BrainStormEra, created by PhatLam. Your primary function is to help users perform various tasks and answer their questions to the best of your ability. Please adhere to the following guidelines:

1. Respond in Vietnamese: Always respond in Vietnamese, regardless of the language used in the input.

2. Be concise and clear: Try to be concise while still providing comprehensive and understandable answers.

3. Maintain a friendly and professional tone: Be polite and approachable, but avoid overly casual language.

4. Provide accurate information: If you're unsure about something, admit it rather than guessing.

5. Respect privacy and ethics: Do not share personal information or engage in anything illegal or unethical.

6. Suggest next topics: When appropriate, suggest related topics or questions that the user might find interesting.

7. Use markdown formatting: Use markdown to structure your answers for better readability.

8. Summarize long answers: If the answer is long, provide a brief summary at the beginning.

9. You may refuse to answer if the question is off-topic or unrelated to the provided issue.

User Input: {0}

Your answer (in Vietnamese):";

        private const string INSTRUCTOR_TEMPLATE = @"
You will provide all the information you know so that the user can create a complete course as requested. Provide very detailed answers on the topic.";

        private const string USER_TEMPLATE = @"
Analyze whether the user's question is related to the course topic.
If the question is somewhat related to the course, still answer it.
If the question is not related at all, decline to answer.
If the user asks unrelated questions not tied to the course, chapter, or lesson, respond with: (Sorry, I cannot answer this question as it is not related to the topic of the course, chapter, or lesson you are studying. You may ask a different question or contact the instructor for help.)";

        public ChatbotService(HttpClient httpClient, IConfiguration configuration, ILogger<ChatbotService>? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;

            _apiKey = configuration["GeminiApiKey"]
                ?? throw new ArgumentNullException(nameof(configuration), "GeminiApiKey is missing in configuration.");
            _apiUrl = configuration["GeminiApiUrl"]
                ?? throw new ArgumentNullException(nameof(configuration), "GeminiApiUrl is missing in configuration.");
        }

        public async Task<string> GetResponseFromGemini(
            string message,
            int userRole,
            string? courseName = null,
            string? courseDescription = null,
            string? createdBy = null,
            string? chapterName = null,
            string? chapterDescription = null,
            string? lessonName = null,
            string? lessonDescription = null,
            string? lessonContent = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));
            }

            try
            {
                string selectedTemplate = BuildTemplate(userRole, courseName, courseDescription, createdBy,
                    chapterName, chapterDescription, lessonName, lessonDescription, lessonContent);

                var formattedMessage = string.Format(selectedTemplate, message);
                _logger?.LogInformation("Formatted message created for user role: {UserRole}", userRole);

                var request = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = formattedMessage } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 2048,
                    }
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiUrl}?key={_apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    return ExtractResponseText(responseString);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Gemini API request failed with status code {StatusCode}: {ErrorContent}",
                        response.StatusCode, errorContent);

                    return response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.ServiceUnavailable =>
                            "Sorry, the system is currently overloaded and cannot respond to your question.",
                        System.Net.HttpStatusCode.Unauthorized =>
                            "API authentication error. Please check your configuration.",
                        System.Net.HttpStatusCode.TooManyRequests =>
                            "Request limit exceeded. Please try again later.",
                        _ => "An error occurred while connecting to the AI service. Please try again later."
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP request exception during Gemini API call");
                return "Network error. Please check your internet connection and try again.";
            }
            catch (TaskCanceledException ex)
            {
                _logger?.LogError(ex, "Request timeout during Gemini API call");
                return "Request timed out. Please try again.";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected exception during Gemini API call");
                return "An unexpected error occurred. Please try again later.";
            }
        }

        private string BuildTemplate(int userRole, string? courseName, string? courseDescription,
            string? createdBy, string? chapterName, string? chapterDescription,
            string? lessonName, string? lessonDescription, string? lessonContent)
        {
            var courseDetails = string.IsNullOrWhiteSpace(courseName) ? "" : $@"
Course Name: {courseName}
Created By: {createdBy ?? "Unknown"}
Course Description: {courseDescription ?? "No description"}
";

            var chapterDetails = string.IsNullOrWhiteSpace(chapterName) ? "" : $@"
Chapter Name: {chapterName}
Chapter Description: {chapterDescription ?? "No description"}
";

            var lessonDetails = string.IsNullOrWhiteSpace(lessonContent) ? "" : $@"
Lesson Name: {lessonName ?? "Unknown"}
Lesson Description: {lessonDescription ?? "No description"}
Lesson Content: {lessonContent}
";

            return userRole switch
            {
                1 => ADMIN_TEMPLATE, // Admin role
                2 => ADMIN_TEMPLATE + INSTRUCTOR_TEMPLATE, // Instructor role
                3 => courseDetails + chapterDetails + lessonDetails + ADMIN_TEMPLATE + USER_TEMPLATE, // Student role
                _ => ADMIN_TEMPLATE // Default
            };
        }

        private static string ExtractResponseText(string responseString)
        {
            try
            {
                dynamic? responseObject = JsonConvert.DeserializeObject(responseString);

                if (responseObject?.candidates != null)
                {
                    var candidates = responseObject.candidates;
                    if (candidates.Count > 0)
                    {
                        var firstCandidate = candidates[0];
                        if (firstCandidate?.content?.parts != null)
                        {
                            var parts = firstCandidate.content.parts;
                            if (parts.Count > 0)
                            {
                                var firstPart = parts[0];
                                if (firstPart?.text != null)
                                {
                                    return firstPart.text.ToString() ?? "No response received from AI.";
                                }
                            }
                        }
                    }
                }

                throw new InvalidOperationException("Unexpected response format from Gemini API");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse Gemini API response: {ex.Message}", ex);
            }
        }
    }
}