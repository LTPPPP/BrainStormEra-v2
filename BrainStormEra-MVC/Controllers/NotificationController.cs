using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BrainStormEra_MVC.Services.Interfaces;
using BrainStormEra_MVC.Services.Implementations;
using System.Security.Claims;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationServiceImpl _notificationServiceImpl;
        private readonly INotificationService _notificationService; // Keep for simple operations
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            NotificationServiceImpl notificationServiceImpl,
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationServiceImpl = notificationServiceImpl;
            _notificationService = notificationService;
            _logger = logger;
        }// GET: Notification
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var result = await _notificationServiceImpl.GetNotificationsAsync(User, page, pageSize);

                if (!result.Success)
                {
                    if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                    {
                        return RedirectToAction(result.RedirectAction, result.RedirectController);
                    }

                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to load notifications.";
                    return View(new NotificationIndexViewModel());
                }

                return View(result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications");
                TempData["ErrorMessage"] = "An error occurred while loading notifications.";
                return View(new NotificationIndexViewModel());
            }
        }        // GET: Get notifications as JSON for AJAX requests
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
        }        // GET: Get unread notification count
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
            try
            {
                var result = await _notificationServiceImpl.MarkAsReadAsync(User, notificationId);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // POST: Mark all notifications as read
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var result = await _notificationServiceImpl.MarkAllAsReadAsync(User);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return Json(new { success = false, message = "An error occurred" });
            }
        }        // POST: Delete notification
        [HttpPost]
        public async Task<IActionResult> Delete(string notificationId)
        {
            try
            {
                var result = await _notificationServiceImpl.DeleteNotificationAsync(User, notificationId);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // POST: Send notification to user (admin only)
        [HttpPost]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> SendToUser(string targetUserId, string title, string content, string? type = null, string? courseId = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationService.SendToUserAsync(targetUserId, title, content, type, courseId, currentUserId);
            return Json(new { success = success });
        }

        // POST: Send notification to course (instructor/admin only)
        [HttpPost]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> SendToCourse(string courseId, string title, string content, string? type = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationService.SendToCourseAsync(courseId, title, content, type, userId, userId);
            return Json(new { success = success });
        }

        // POST: Send notification to role (admin only)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendToRole(string role, string title, string content, string? type = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationService.SendToRoleAsync(role, title, content, type, currentUserId);
            return Json(new { success = success });
        }

        // POST: Send notification to all users (admin only)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendToAll(string title, string content, string? type = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationService.SendToAllAsync(title, content, type, currentUserId);
            return Json(new { success = success });
        }        // GET: Create notification page (admin and instructor only)
        [HttpGet]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var result = await _notificationServiceImpl.GetCreateNotificationViewModelAsync(User);

                if (!result.Success)
                {
                    if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                    {
                        return RedirectToAction(result.RedirectAction, result.RedirectController);
                    }
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index");
                }

                return View(result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create notification page");
                TempData["ErrorMessage"] = "An error occurred while loading the create notification page.";
                return RedirectToAction("Index");
            }
        }        // POST: Create notification
        [HttpPost]
        [Authorize(Roles = "admin,instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NotificationCreateViewModel model)
        {
            try
            {
                var result = await _notificationServiceImpl.CreateNotificationAsync(User, model);

                if (result.Success)
                {
                    if (!string.IsNullOrEmpty(result.SuccessMessage))
                    {
                        TempData["SuccessMessage"] = result.SuccessMessage;
                    }

                    if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                    {
                        return RedirectToAction(result.RedirectAction, result.RedirectController);
                    }

                    return RedirectToAction("Index");
                }
                else
                {
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }

                    if (result.ValidationErrors != null)
                    {
                        foreach (var error in result.ValidationErrors)
                        {
                            foreach (var message in error.Value)
                            {
                                ModelState.AddModelError(error.Key, message);
                            }
                        }
                    }

                    if (result.ReturnView && result.ViewModel != null)
                    {
                        return View(result.ViewModel);
                    }

                    if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                    {
                        return RedirectToAction(result.RedirectAction, result.RedirectController);
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                TempData["ErrorMessage"] = "An error occurred while creating the notification.";
                return View(model);
            }
        }        // GET: Edit notification (for creators only)
        [HttpGet]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var result = await _notificationServiceImpl.GetNotificationForEditAsync(User, id);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Notification not found or you don't have permission to edit it.";

                    if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                    {
                        return RedirectToAction(result.RedirectAction, result.RedirectController);
                    }

                    return RedirectToAction("Index");
                }

                return View(result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit notification page");
                TempData["ErrorMessage"] = "An error occurred while loading the edit notification page.";
                return RedirectToAction("Index");
            }
        }        // POST: Edit notification
        [HttpPost]
        [Authorize(Roles = "admin,instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(NotificationEditViewModel model)
        {
            try
            {
                var result = await _notificationServiceImpl.UpdateNotificationAsync(User, model.NotificationId, model);

                if (result.Success)
                {
                    if (!string.IsNullOrEmpty(result.SuccessMessage))
                    {
                        TempData["SuccessMessage"] = result.SuccessMessage;
                    }

                    if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                    {
                        return RedirectToAction(result.RedirectAction, result.RedirectController);
                    }

                    return RedirectToAction("Index");
                }
                else
                {
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }

                    if (result.ValidationErrors != null)
                    {
                        foreach (var error in result.ValidationErrors)
                        {
                            foreach (var message in error.Value)
                            {
                                ModelState.AddModelError(error.Key, message);
                            }
                        }
                    }

                    if (result.ReturnView && result.ViewModel != null)
                    {
                        return View(result.ViewModel);
                    }

                    if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                    {
                        return RedirectToAction(result.RedirectAction, result.RedirectController);
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification");
                TempData["ErrorMessage"] = "An error occurred while updating the notification.";
                return View(model);
            }
        }

    }
}
