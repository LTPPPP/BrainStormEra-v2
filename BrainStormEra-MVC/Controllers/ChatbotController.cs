using BusinessLogicLayer.Services.Implementations;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra_MVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;

        public ChatbotController(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatbotService.ChatRequest request)
        {
            var result = await _chatbotService.ProcessChatForControllerAsync(request);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.ErrorResponse);
            }

            return Ok(result.SuccessResponse);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int limit = 20)
        {
            var result = await _chatbotService.GetHistoryForControllerAsync(limit);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.ErrorResponse);
            }

            return Ok(result.SuccessResponse);
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> SubmitFeedback([FromBody] ChatbotService.FeedbackRequest request)
        {
            var result = await _chatbotService.SubmitFeedbackForControllerAsync(request);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.ErrorResponse);
            }

            return Ok(result.SuccessResponse);
        }
    }
}

