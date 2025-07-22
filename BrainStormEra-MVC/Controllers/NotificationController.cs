using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Interfaces;
using System.Security.Claims;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ICourseService _courseService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ICourseService courseService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _courseService = courseService;
            _logger = logger;
        }// GET: Notification
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated.";
                    return View(new NotificationIndexViewModel());
                }
                var notifications = await _notificationService.GetNotificationsAsync(userId, page, pageSize);
                // You may need to map Notification to NotificationIndexViewModel if required
                return View(notifications);
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
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
            return Json(new { count = count });
        }

        // POST: Mark notification as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(string notificationId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(notificationId) || string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Notification ID and User ID are required" });
                }
                await _notificationService.MarkAsReadAsync(notificationId, userId);
                return Json(new { success = true, message = "Notification marked as read" });
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
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User ID is required" });
                }
                await _notificationService.MarkAllAsReadAsync(userId);
                return Json(new { success = true, message = "All notifications marked as read" });
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
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(notificationId) || string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Notification ID and User ID are required" });
                }
                await _notificationService.DeleteNotificationAsync(notificationId, userId);
                return Json(new { success = true, message = "Notification deleted" });
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
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationService.SendToUserAsync(targetUserId, title, content, type, courseId, currentUserId);
            return Json(new { success = success });
        }

        // POST: Send notification to course (instructor/admin only)
        [HttpPost]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> SendToCourse(string courseId, string title, string content, string? type = null)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationService.SendToCourseAsync(courseId, title, content, type, userId, userId);
            return Json(new { success = success });
        }

        // POST: Send notification to role (admin only)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendToRole(string role, string title, string content, string? type = null)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationService.SendToRoleAsync(role, title, content, type, currentUserId);
            return Json(new { success = success });
        }

        // POST: Send notification to all users (admin only)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendToAll(string title, string content, string? type = null)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var success = await _notificationService.SendToAllAsync(title, content, type, currentUserId);
            return Json(new { success = success });
        }        // GET: Create notification page (admin and instructor only)
        [HttpGet]
        [Authorize(Roles = "admin,instructor")]
        public IActionResult Create()
        {
            // No GetCreateNotificationViewModelAsync in interface; just return the view
            return View();
        }

        // POST: Create notification
        [HttpPost]
        [Authorize(Roles = "admin,instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NotificationCreateViewModel model)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated.";
                    return View(model);
                }
                // Use the interface method signature
                var notification = await _notificationService.CreateNotificationAsync(userId, model.Title, model.Content, model.Type, model.CourseId, userId);
                if (notification != null)
                {
                    TempData["SuccessMessage"] = "Notification created successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create notification.";
                    return View(model);
                }
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
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated.";
                    return RedirectToAction("Index");
                }
                var notification = await _notificationService.GetNotificationForEditAsync(id, userId);
                if (notification == null)
                {
                    TempData["ErrorMessage"] = "Notification not found or you don't have permission to edit it.";
                    return RedirectToAction("Index");
                }
                // Map notification to NotificationEditViewModel if needed
                var model = new NotificationEditViewModel
                {
                    NotificationId = notification.NotificationId,
                    Title = notification.NotificationTitle,
                    Content = notification.NotificationContent,
                    Type = notification.NotificationType,
                    CourseId = notification.CourseId
                };
                return View(model);
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
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated.";
                    return View(model);
                }
                // Use the interface method signature
                var success = await _notificationService.UpdateNotificationAsync(model.NotificationId, userId, model.Title, model.Content, model.Type);
                if (success)
                {
                    TempData["SuccessMessage"] = "Notification updated successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update notification.";
                    return View(model);
                }
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
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }
                var success = await _notificationService.RestoreNotificationAsync(notificationId, userId);
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
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                }
                _logger.LogInformation("CheckNotification - NotificationId: {NotificationId}, UserId: {UserId}", notificationId, userId);
                if (userId == null)
                {
                    return Json(new { exists = false, message = "User not authenticated", userId = "null" });
                }
                var notification = await _notificationService.GetNotificationForEditAsync(notificationId, userId);
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
                    userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                }
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { exists = false, message = "User ID not found in claims" });
                }
                var allNotifications = await _notificationService.GetNotificationsAsync(userId, 1, 100);
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
                    userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                }
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { error = "User ID not found in claims" });
                }
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var notification = await _notificationService.GetNotificationForEditAsync(notificationId, userId);
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
                var users = await _notificationService.SearchUsersAsync(searchTerm);
                return Json(new { success = true, users = users });
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
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

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
                var users = await _notificationService.SearchUsersAsync("");
                return Json(users);
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
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var users = await _notificationService.GetEnrolledUsersAsync(userId, courseId, searchTerm);
                return Json(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrolled users for notification");
                return Json(new { success = false, message = "An error occurred while loading enrolled users" });
            }
        }
    }
}


