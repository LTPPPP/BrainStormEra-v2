using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BrainStormEra_MVC.Services.Interfaces;
using System.Security.Claims;
using BrainStormEra_MVC.Models.ViewModels;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: Notification
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var notifications = await _notificationService.GetNotificationsAsync(userId, page, pageSize);
            var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(userId);

            var viewModel = new NotificationIndexViewModel
            {
                Notifications = notifications,
                UnreadCount = unreadCount,
                CurrentPage = page,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: Get notifications as JSON for AJAX requests
        [HttpGet]
        public async Task<IActionResult> GetNotifications(int page = 1, int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var notifications = await _notificationService.GetNotificationsAsync(userId, page, pageSize);
            return Json(notifications.Select(n => new
            {
                id = n.NotificationId,
                title = n.NotificationTitle,
                content = n.NotificationContent,
                type = n.NotificationType,
                courseId = n.CourseId,
                isRead = n.IsRead,
                createdAt = n.NotificationCreatedAt,
                readAt = n.ReadAt
            }));
        }

        // GET: Get unread notification count
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
            return Json(new { count = count });
        }

        // POST: Mark notification as read
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string notificationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            await _notificationService.MarkAsReadAsync(notificationId, userId);
            return Json(new { success = true });
        }

        // POST: Mark all notifications as read
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            await _notificationService.MarkAllAsReadAsync(userId);
            return Json(new { success = true });
        }

        // POST: Delete notification
        [HttpPost]
        public async Task<IActionResult> Delete(string notificationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            await _notificationService.DeleteNotificationAsync(notificationId, userId);
            return Json(new { success = true });
        }

        // POST: Send notification to user (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> SendToUser(string targetUserId, string title, string content, string? type = null, string? courseId = null)
        {
            var success = await _notificationService.SendToUserAsync(targetUserId, title, content, type, courseId);
            return Json(new { success = success });
        }

        // POST: Send notification to course (Instructor/Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> SendToCourse(string courseId, string title, string content, string? type = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationService.SendToCourseAsync(courseId, title, content, type, userId);
            return Json(new { success = success });
        }

        // POST: Send notification to role (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendToRole(string role, string title, string content, string? type = null)
        {
            var success = await _notificationService.SendToRoleAsync(role, title, content, type);
            return Json(new { success = success });
        }

        // POST: Send notification to all users (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendToAll(string title, string content, string? type = null)
        {
            var success = await _notificationService.SendToAllAsync(title, content, type);
            return Json(new { success = success });
        }


    }
}
