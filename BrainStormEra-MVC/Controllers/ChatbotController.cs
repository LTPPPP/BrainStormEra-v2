using BrainStormEra_MVC.Services.Interfaces;
using BrainStormEra_MVC.Services.Implementations;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BrainStormEra_MVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ChatbotServiceImpl _chatbotServiceImpl;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            IChatbotService chatbotService,
            ChatbotServiceImpl chatbotServiceImpl,
            ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _chatbotServiceImpl = chatbotServiceImpl;
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                var result = await _chatbotServiceImpl.ProcessChatAsync(
                    request.Message,
                    request.Context,
                    request.PagePath,
                    request.CourseId,
                    request.ChapterId,
                    request.LessonId
                );

                if (!result.IsSuccess)
                {
                    if (result.ValidationErrors.Any())
                    {
                        return BadRequest(new
                        {
                            error = "Validation failed",
                            errors = result.ValidationErrors
                        });
                    }

                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        if (result.ErrorMessage.Contains("not authenticated"))
                        {
                            return Unauthorized(new { error = result.ErrorMessage });
                        }
                        return StatusCode(500, new { error = result.ErrorMessage });
                    }

                    return BadRequest(new { error = "Failed to process chat request" });
                }

                return Ok(new ChatResponse
                {
                    Message = result.BotResponse ?? "",
                    Timestamp = result.Timestamp ?? DateTime.Now
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
                var result = await _chatbotServiceImpl.GetConversationHistoryAsync(limit);

                if (!result.IsSuccess)
                {
                    if (result.ValidationErrors.Any())
                    {
                        return BadRequest(new
                        {
                            error = "Validation failed",
                            errors = result.ValidationErrors
                        });
                    }

                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        if (result.ErrorMessage.Contains("not authenticated"))
                        {
                            return Unauthorized(new { error = result.ErrorMessage });
                        }
                        return StatusCode(500, new { error = result.ErrorMessage });
                    }

                    return BadRequest(new { error = "Failed to retrieve conversation history" });
                }

                var formattedHistory = result.Conversations.Select(h => new
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
                var result = await _chatbotServiceImpl.SubmitFeedbackAsync(request.ConversationId, request.Rating);

                if (!result.IsSuccess)
                {
                    if (result.ValidationErrors.Any())
                    {
                        return BadRequest(new
                        {
                            error = "Invalid request data",
                            errors = result.ValidationErrors
                        });
                    }

                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        if (result.ErrorMessage.Contains("not authenticated"))
                        {
                            return Unauthorized(new { error = result.ErrorMessage });
                        }
                        if (result.ErrorMessage.Contains("not found"))
                        {
                            return NotFound(new { error = result.ErrorMessage });
                        }
                        return StatusCode(500, new { error = result.ErrorMessage });
                    }

                    return BadRequest(new { error = "Failed to submit feedback" });
                }

                return Ok(new { message = result.Message });
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
