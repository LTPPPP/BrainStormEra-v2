using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Interfaces;
using System.Security.Claims;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class ChatController : BaseController
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            var users = await _chatService.GetChatUsersAsync(currentUserId);
            ViewBag.CurrentUserId = currentUserId;

            return View(users);
        }

        public async Task<IActionResult> Conversation(string userId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Index");

            ViewBag.CurrentUserId = currentUserId;
            ViewBag.ReceiverId = userId;

            // Get chat users for sidebar
            var users = await _chatService.GetChatUsersAsync(currentUserId);
            ViewBag.ChatUsers = users;

            // Get messages
            var messages = await _chatService.GetConversationMessagesAsync(currentUserId, userId);

            return View(messages);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string receiverId, int page = 1, int pageSize = 50)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            var messages = await _chatService.GetConversationMessagesAsync(currentUserId, receiverId, page, pageSize);

            var result = messages.Select(m => new
            {
                messageId = m.MessageId,
                senderId = m.SenderId,
                receiverId = m.ReceiverId,
                content = m.MessageContent,
                messageType = m.MessageType,
                isRead = m.IsRead,
                replyToMessageId = m.ReplyToMessageId,
                isEdited = m.IsEdited,
                createdAt = m.MessageCreatedAt,
                senderName = m.Sender?.Username ?? "Unknown",
                senderAvatar = m.Sender?.UserImage
            }).Reverse().ToList(); // Reverse to show oldest first

            return Json(new { success = true, messages = result });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            if (string.IsNullOrEmpty(request.ReceiverId) || string.IsNullOrEmpty(request.Message))
                return Json(new { success = false, message = "Invalid message data" });

            var message = await _chatService.SendMessageAsync(currentUserId, request.ReceiverId, request.Message, request.ReplyToMessageId);

            if (message != null)
            {
                return Json(new
                {
                    success = true,
                    message = new
                    {
                        messageId = message.MessageId,
                        senderId = message.SenderId,
                        receiverId = message.ReceiverId,
                        content = message.MessageContent,
                        messageType = message.MessageType,
                        createdAt = message.MessageCreatedAt,
                        senderName = message.Sender?.Username ?? "Unknown",
                        senderAvatar = message.Sender?.UserImage
                    }
                });
            }

            return Json(new { success = false, message = "Failed to send message" });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _chatService.MarkMessageAsReadAsync(request.MessageId, currentUserId);

            return Json(new { success = result });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount(string senderId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            var count = await _chatService.GetUnreadMessageCountAsync(currentUserId, senderId);

            return Json(new { success = true, count });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage([FromBody] DeleteMessageRequest request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _chatService.DeleteMessageAsync(request.MessageId, currentUserId);

            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> EditMessage([FromBody] EditMessageRequest request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _chatService.EditMessageAsync(request.MessageId, request.NewContent, currentUserId);

            return Json(new { success = result });
        }
    }

    public class SendMessageRequest
    {
        public string ReceiverId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ReplyToMessageId { get; set; }
    }

    public class MarkAsReadRequest
    {
        public string MessageId { get; set; } = string.Empty;
    }

    public class DeleteMessageRequest
    {
        public string MessageId { get; set; } = string.Empty;
    }

    public class EditMessageRequest
    {
        public string MessageId { get; set; } = string.Empty;
        public string NewContent { get; set; } = string.Empty;
    }
}
