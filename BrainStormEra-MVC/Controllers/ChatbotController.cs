using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BrainStormEra_MVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly IPageContextService _pageContextService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            IChatbotService chatbotService,
            IPageContextService pageContextService,
            ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _pageContextService = pageContextService;
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message is required" });
                }
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Enhanced context with page information
                var enhancedContext = request.Context;
                if (!string.IsNullOrEmpty(request.PagePath))
                {
                    var pageContext = await _pageContextService.GetPageContextAsync(
                        request.PagePath,
                        request.CourseId,
                        request.ChapterId,
                        request.LessonId
                    );

                    enhancedContext = string.IsNullOrEmpty(enhancedContext)
                        ? pageContext
                        : $"{request.Context}. {pageContext}";
                }

                var response = await _chatbotService.GetResponseAsync(
                    request.Message,
                    userId,
                    enhancedContext
                );

                return Ok(new ChatResponse
                {
                    Message = response,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Chat endpoint");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int limit = 20)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var history = await _chatbotService.GetConversationHistoryAsync(userId, limit);

                var formattedHistory = history.Select(h => new
                {
                    Id = h.ConversationId,
                    UserMessage = h.UserMessage,
                    BotResponse = h.BotResponse,
                    Timestamp = h.ConversationTime,
                    Rating = h.FeedbackRating
                }).ToList();

                return Ok(formattedHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHistory endpoint");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ConversationId) || request.Rating < 1 || request.Rating > 5)
                {
                    return BadRequest(new { error = "Invalid request data" });
                }

                var success = await _chatbotService.RateFeedbackAsync(request.ConversationId, (byte)request.Rating);

                if (success)
                {
                    return Ok(new { message = "Feedback submitted successfully" });
                }
                else
                {
                    return NotFound(new { error = "Conversation not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitFeedback endpoint");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
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

    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class FeedbackRequest
    {
        public string ConversationId { get; set; } = string.Empty;
        public int Rating { get; set; }
    }
}
