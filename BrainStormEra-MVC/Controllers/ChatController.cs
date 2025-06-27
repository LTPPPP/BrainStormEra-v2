using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.DTOs.Chat;
using BusinessLogicLayer.DTOs.Common;
using System.Security.Claims;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class ChatController : BaseController
    {
        private readonly IChatBusinessService _chatBusinessService;
        private readonly IChatUrlService _chatUrlService;

        public ChatController(IChatBusinessService chatBusinessService, IChatUrlService chatUrlService)
        {
            _chatBusinessService = chatBusinessService;
            _chatUrlService = chatUrlService;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            var result = await _chatBusinessService.GetChatIndexViewModelAsync(currentUserId);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Message;
                return View(new ChatIndexViewModel { CurrentUserId = currentUserId });
            }

            return View(result.Data);
        }

        public async Task<IActionResult> Conversation(string userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Index");

            var result = await _chatBusinessService.GetConversationViewModelAsync(currentUserId, userId);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Index");
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string receiverId, int page = 1, int pageSize = 50)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            var request = new GetMessagesRequest
            {
                ReceiverId = receiverId,
                Page = page,
                PageSize = pageSize
            };

            var result = await _chatBusinessService.GetMessagesAsync(currentUserId, request);

            if (!result.IsSuccess)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true, messages = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _chatBusinessService.SendMessageAsync(currentUserId, request);

            if (!result.IsSuccess)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true, message = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _chatBusinessService.MarkMessageAsReadAsync(currentUserId, request);

            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount(string senderId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            var request = new GetUnreadCountRequest { SenderId = senderId };
            var result = await _chatBusinessService.GetUnreadCountAsync(currentUserId, request);

            if (!result.IsSuccess)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true, count = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage([FromBody] DeleteMessageRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _chatBusinessService.DeleteMessageAsync(currentUserId, request);

            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> EditMessage([FromBody] EditMessageRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _chatBusinessService.EditMessageAsync(currentUserId, request);

            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        /// <summary>
        /// Access chat via secure hash
        /// </summary>
        /// <param name="hash">Secure hash from URL</param>
        /// <returns>Redirect to conversation or error</returns>
        [HttpGet("Secure/{hash}")]
        public async Task<IActionResult> SecureAccess(string hash)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            var result = await _chatUrlService.VerifyAndAccessChatUrlAsync(hash, currentUserId);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Index");
            }

            return Redirect(result.Data!.RedirectUrl);
        }

        /// <summary>
        /// Access chat via quick hash
        /// </summary>
        /// <param name="hash">Quick hash from URL</param>
        /// <returns>Redirect to conversation or error</returns>
        [HttpGet("Quick/{hash}")]
        public async Task<IActionResult> QuickAccess(string hash)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            var result = await _chatUrlService.VerifyAndAccessQuickChatAsync(hash, currentUserId);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Index");
            }

            return Redirect(result.Data!.RedirectUrl);
        }

        /// <summary>
        /// Access specific message via hash
        /// </summary>
        /// <param name="hash">Message hash from URL</param>
        /// <returns>Redirect to conversation with message highlighted</returns>
        [HttpGet("Message/{hash}")]
        public async Task<IActionResult> MessageAccess(string hash)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            var result = await _chatUrlService.VerifyAndAccessMessageUrlAsync(hash, currentUserId);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Index");
            }

            return Redirect(result.Data!.RedirectUrl);
        }

        /// <summary>
        /// Generate various types of URLs for a chat
        /// </summary>
        /// <param name="userId">Other user ID</param>
        /// <returns>JSON with various URL types</returns>
        [HttpGet]
        public async Task<IActionResult> GenerateUrls(string userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "User ID is required" });

            try
            {
                // Get conversation between users
                var conversationResult = await _chatBusinessService.GetConversationViewModelAsync(currentUserId, userId);
                if (!conversationResult.IsSuccess)
                {
                    return Json(new { success = false, message = "Failed to get conversation" });
                }

                // Get or create conversation for URL generation
                var conversationEntityResult = await _chatBusinessService.GetOrCreateConversationAsync(currentUserId, userId);
                if (!conversationEntityResult.IsSuccess)
                {
                    return Json(new { success = false, message = "Failed to create conversation" });
                }

                var urlResult = await _chatUrlService.GenerateChatUrlBundleAsync(conversationEntityResult.Data!.ConversationId, currentUserId, userId);

                if (!urlResult.IsSuccess)
                {
                    return Json(new { success = false, message = urlResult.Message });
                }

                return Json(new
                {
                    success = true,
                    urls = urlResult.Data,
                    message = "URLs generated successfully"
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Failed to generate URLs" });
            }
        }

        /// <summary>
        /// Generate a secure URL for a specific message
        /// </summary>
        /// <param name="messageId">Message ID</param>
        /// <returns>JSON with message URL</returns>
        [HttpPost]
        public async Task<IActionResult> GenerateMessageUrl([FromBody] GenerateMessageUrlRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            if (string.IsNullOrEmpty(request.MessageId))
                return Json(new { success = false, message = "Message ID is required" });

            var result = await _chatUrlService.GenerateMessageUrlAsync(request.MessageId, currentUserId);

            if (!result.IsSuccess)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new
            {
                success = true,
                url = result.Data,
                message = "Message URL generated successfully"
            });
        }

        /// <summary>
        /// Generate a quick chat URL for sharing
        /// </summary>
        /// <param name="userId">Target user ID</param>
        /// <returns>JSON with quick chat URL</returns>
        [HttpPost]
        public async Task<IActionResult> GenerateQuickUrl([FromBody] GenerateQuickUrlRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            if (string.IsNullOrEmpty(request.TargetUserId))
                return Json(new { success = false, message = "Target user ID is required" });

            var result = await _chatUrlService.GenerateQuickChatUrlAsync(currentUserId, request.TargetUserId);

            if (!result.IsSuccess)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new
            {
                success = true,
                url = result.Data,
                message = "Quick chat URL generated successfully"
            });
        }
    }

    /// <summary>
    /// Request model for generating message URL
    /// </summary>
    public class GenerateMessageUrlRequest
    {
        public string MessageId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for generating quick URL
    /// </summary>
    public class GenerateQuickUrlRequest
    {
        public string TargetUserId { get; set; } = string.Empty;
    }
}
