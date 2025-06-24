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

        public ChatController(IChatBusinessService chatBusinessService)
        {
            _chatBusinessService = chatBusinessService;
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
    }
}
