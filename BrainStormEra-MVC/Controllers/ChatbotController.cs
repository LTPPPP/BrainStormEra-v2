using BrainStormEra_MVC.Services.Implementations;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra_MVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotServiceImpl _chatbotServiceImpl;

        public ChatbotController(ChatbotServiceImpl chatbotServiceImpl)
        {
            _chatbotServiceImpl = chatbotServiceImpl;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatbotServiceImpl.ChatRequest request)
        {
            var result = await _chatbotServiceImpl.ProcessChatForControllerAsync(request);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.ErrorResponse);
            }

            return Ok(result.SuccessResponse);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int limit = 20)
        {
            var result = await _chatbotServiceImpl.GetHistoryForControllerAsync(limit);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.ErrorResponse);
            }

            return Ok(result.SuccessResponse);
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> SubmitFeedback([FromBody] ChatbotServiceImpl.FeedbackRequest request)
        {
            var result = await _chatbotServiceImpl.SubmitFeedbackForControllerAsync(request);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.ErrorResponse);
            }

            return Ok(result.SuccessResponse);
        }
    }
}
