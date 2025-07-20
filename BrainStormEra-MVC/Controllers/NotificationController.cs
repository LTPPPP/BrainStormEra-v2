using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using System.Security.Claims;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationService _notificationService;
        private readonly INotificationService _notificationServiceInterface;
        private readonly CourseService _courseService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            NotificationService notificationService,
            INotificationService notificationServiceInterface,
            CourseService courseService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _notificationServiceInterface = notificationServiceInterface;
            _courseService = courseService;
            _logger = logger;
        }// GET: Notification
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var result = await _notificationService.GetNotificationsAsync(User, page, pageSize);

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

            var notifications = await _notificationServiceInterface.GetNotificationsAsync(userId, page, pageSize);
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

            var count = await _notificationServiceInterface.GetUnreadNotificationCountAsync(userId);
            return Json(new { count = count });
        }

        // POST: Mark notification as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(string notificationId)
        {
            try
            {
                if (string.IsNullOrEmpty(notificationId))
                {
                    return Json(new { success = false, message = "Notification ID is required" });
                }

                var result = await _notificationService.MarkAsReadAsync(User, notificationId);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var result = await _notificationService.MarkAllAsReadAsync(User);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return Json(new { success = false, message = "An error occurred" });
            }
        }        // POST: Delete notification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string notificationId)
        {
            try
            {
                if (string.IsNullOrEmpty(notificationId))
                {
                    return Json(new { success = false, message = "Notification ID is required" });
                }

                var result = await _notificationService.DeleteNotificationAsync(User, notificationId);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
                return Json(new { success = false, message = "An error occurred while deleting the notification" });
            }
        }

        // POST: Send notification to user (admin only)
        [HttpPost]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> SendToUser(string targetUserId, string title, string content, string? type = null, string? courseId = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationServiceInterface.SendToUserAsync(targetUserId, title, content, type, courseId, currentUserId);
            return Json(new { success = success });
        }

        // POST: Send notification to course (instructor/admin only)
        [HttpPost]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> SendToCourse(string courseId, string title, string content, string? type = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationServiceInterface.SendToCourseAsync(courseId, title, content, type, userId, userId);
            return Json(new { success = success });
        }

        // POST: Send notification to role (admin only)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendToRole(string role, string title, string content, string? type = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationServiceInterface.SendToRoleAsync(role, title, content, type, currentUserId);
            return Json(new { success = success });
        }

        // POST: Send notification to all users (admin only)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendToAll(string title, string content, string? type = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationServiceInterface.SendToAllAsync(title, content, type, currentUserId);
            return Json(new { success = success });
        }        // GET: Create notification page (admin and instructor only)
        [HttpGet]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var result = await _notificationService.GetCreateNotificationViewModelAsync(User);

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
                var result = await _notificationService.CreateNotificationAsync(User, model);

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
                var result = await _notificationService.GetNotificationForEditAsync(User, id);

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
                var result = await _notificationService.UpdateNotificationAsync(User, model.NotificationId, model);

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
        }        // POST: Restore deleted notification (optional feature for admins or users)
        [HttpPost]
        public async Task<IActionResult> RestoreNotification(string notificationId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var success = await _notificationServiceInterface.RestoreNotificationAsync(notificationId, userId);

                if (success)
                {
                    return Json(new { success = true, message = "Notification restored successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Notification not found or cannot be restored" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring notification");
                return Json(new { success = false, message = "An error occurred while restoring notification" });
            }
        }        // GET: Debug method to check notification exists
        [HttpGet]
        public async Task<IActionResult> CheckNotification(string notificationId)
        {
            try
            {
                // Try different ways to get user ID
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }

                _logger.LogInformation("CheckNotification - NotificationId: {NotificationId}, UserId: {UserId}", notificationId, userId);

                if (userId == null)
                {
                    return Json(new { exists = false, message = "User not authenticated", userId = "null" });
                }

                var notification = await _notificationServiceInterface.GetNotificationForEditAsync(notificationId, userId);

                _logger.LogInformation("CheckNotification - Notification found: {Found}", notification != null);

                return Json(new
                {
                    exists = notification != null,
                    notificationId = notificationId,
                    userId = userId,
                    notificationType = notification?.NotificationType,
                    debug = new
                    {
                        claimsCount = User.Claims.Count(),
                        claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking notification {NotificationId}", notificationId);
                return Json(new { exists = false, message = "Error occurred", error = ex.Message });
            }
        }        // GET: Simple check if notification exists in database
        [HttpGet]
        public async Task<IActionResult> DirectCheckNotification(string notificationId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { exists = false, message = "User ID not found in claims" });
                }

                // Direct database check
                var allNotifications = await _notificationServiceInterface.GetNotificationsAsync(userId, 1, 100);
                var targetNotification = allNotifications.FirstOrDefault(n => n.NotificationId == notificationId);

                return Json(new
                {
                    exists = targetNotification != null,
                    notificationId = notificationId,
                    userId = userId,
                    notificationType = targetNotification?.NotificationType,
                    isRead = targetNotification?.IsRead,
                    totalNotifications = allNotifications.Count(),
                    allNotificationIds = allNotifications.Select(n => n.NotificationId).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct check notification {NotificationId}", notificationId);
                return Json(new { exists = false, message = "Error occurred", error = ex.Message });
            }
        }        // GET: Test method to debug delete authorization
        [HttpGet]
        public async Task<IActionResult> TestDeleteAuth(string notificationId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { error = "User ID not found in claims" });
                }

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Test GetNotificationForEditAsync
                var notification = await _notificationServiceInterface.GetNotificationForEditAsync(notificationId, userId);

                return Json(new
                {
                    notificationId = notificationId,
                    userId = userId,
                    userRole = userRole,
                    notificationFound = notification != null,
                    notification = notification != null ? new
                    {
                        notification.NotificationId,
                        notification.UserId,
                        notification.CreatedBy,
                        notification.NotificationType,
                        notification.IsRead
                    } : null,
                    canDelete = notification != null && (
                        userRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
                        notification.CreatedBy == userId ||
                        notification.UserId == userId
                    )
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing delete auth for notification {NotificationId}", notificationId);
                return Json(new { error = ex.Message });
            }
        }

        // GET: Search users for notification targeting
        [HttpGet]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> SearchUsers(string searchTerm = "")
        {
            try
            {
                var result = await _notificationService.SearchUsersAsync(User, searchTerm);

                if (result.Success)
                {
                    return Json(new { success = true, users = result.Users });
                }
                else
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users for notification");
                return Json(new { success = false, message = "An error occurred while searching users" });
            }
        }

        // GET: Get courses for instructor notification targeting
        [HttpGet]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> GetCourses()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                List<object> courses = new List<object>();

                if (userRole?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Admin can see all courses
                    var allCoursesResult = await _courseService.GetCoursesAsync("", "", 1, 1000);
                    courses = allCoursesResult.Courses.Select(c => new
                    {
                        courseId = c.CourseId,
                        title = c.CourseName,
                        studentsCount = c.EnrollmentCount
                    }).ToList<object>();
                }
                else if (userRole?.Equals("instructor", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Instructor can only see their own courses
                    var instructorCoursesResult = await _courseService.GetInstructorCoursesAsync(userId, "", "", 1, 1000);
                    courses = instructorCoursesResult.Courses.Select(c => new
                    {
                        courseId = c.CourseId,
                        title = c.CourseName,
                        studentsCount = c.EnrollmentCount
                    }).ToList<object>();
                }

                return Json(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting courses for notification");
                return Json(new { success = false, message = "An error occurred while loading courses" });
            }
        }

        // GET: Get all users for notification targeting
        [HttpGet]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var result = await _notificationService.SearchUsersAsync(User, "");

                if (result.Success)
                {
                    return Json(result.Users);
                }
                else
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users for notification");
                return Json(new { success = false, message = "An error occurred while loading users" });
            }
        }

        // GET: Get enrolled users for notification targeting (instructor only)
        [HttpGet]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> GetEnrolledUsers(string? courseId = null, string? searchTerm = null)
        {
            try
            {
                var result = await _notificationService.GetEnrolledUsersAsync(User, courseId, searchTerm);

                if (result.Success)
                {
                    return Json(result.Users);
                }
                else
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrolled users for notification");
                return Json(new { success = false, message = "An error occurred while loading enrolled users" });
            }
        }
    }
}

