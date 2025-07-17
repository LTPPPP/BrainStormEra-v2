using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using DataAccessLayer.Models.ViewModels;
using System.Security.Claims;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin,instructor")]
    public class NotificationsModel : PageModel
    {
        private readonly NotificationServiceImpl _notificationServiceImpl;
        private readonly INotificationService _notificationService;
        private readonly ICourseService _courseService; // Add CourseService
        private readonly ILogger<NotificationsModel> _logger;

        public NotificationsModel(
            NotificationServiceImpl notificationServiceImpl,
            INotificationService notificationService,
            ICourseService courseService, // Add CourseService parameter
            ILogger<NotificationsModel> logger)
        {
            _notificationServiceImpl = notificationServiceImpl;
            _notificationService = notificationService;
            _courseService = courseService; // Initialize CourseService
            _logger = logger;
        }

        [BindProperty]
        public NotificationCreateViewModel CreateModel { get; set; } = new NotificationCreateViewModel();

        [BindProperty]
        public NotificationEditViewModel EditModel { get; set; } = new NotificationEditViewModel();

        public NotificationIndexViewModel NotificationData { get; set; } = new NotificationIndexViewModel();

        public async Task<IActionResult> OnGetAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var result = await _notificationServiceImpl.GetNotificationsAsync(User, page, pageSize);

                if (!result.Success)
                {
                    if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                    {
                        return RedirectToPage($"/{result.RedirectController}/{result.RedirectAction}");
                    }

                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to load notifications.";
                    NotificationData = new NotificationIndexViewModel();
                    return Page();
                }

                NotificationData = result.ViewModel ?? new NotificationIndexViewModel();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications page");
                TempData["ErrorMessage"] = "An error occurred while loading notifications.";
                NotificationData = new NotificationIndexViewModel();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadNotificationData();
                    return Page();
                }

                var result = await _notificationServiceImpl.CreateNotificationAsync(User, CreateModel);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.SuccessMessage ?? "Notification created successfully!";
                    return RedirectToPage();
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to create notification.";

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

                    await LoadNotificationData();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                TempData["ErrorMessage"] = "An error occurred while creating the notification.";
                await LoadNotificationData();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadNotificationData();
                    return Page();
                }

                var result = await _notificationServiceImpl.UpdateNotificationAsync(User, EditModel.NotificationId, EditModel);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.SuccessMessage ?? "Notification updated successfully!";
                    return RedirectToPage();
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to update notification.";

                    if (result.ValidationErrors != null)
                    {
                        foreach (var error in result.ValidationErrors)
                        {
                            foreach (var message in error.Value)
                            {
                                ModelState.AddModelError($"EditModel.{error.Key}", message);
                            }
                        }
                    }

                    await LoadNotificationData();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification");
                TempData["ErrorMessage"] = "An error occurred while updating the notification.";
                await LoadNotificationData();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostMarkAsReadAsync()
        {
            try
            {
                string? notificationId = null;

                // Try to get from form data first
                if (Request.Form.ContainsKey("notificationId"))
                {
                    notificationId = Request.Form["notificationId"];
                }
                else
                {
                    // Try to read from JSON body
                    Request.Body.Position = 0; // Reset position if possible
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();

                    if (!string.IsNullOrEmpty(body))
                    {
                        // Remove quotes if it's a JSON string
                        notificationId = body.Trim('"');

                        // Try to parse as JSON object
                        if (body.StartsWith("{"))
                        {
                            try
                            {
                                var json = System.Text.Json.JsonDocument.Parse(body);
                                if (json.RootElement.TryGetProperty("notificationId", out var idProperty))
                                {
                                    notificationId = idProperty.GetString();
                                }
                            }
                            catch
                            {
                                // If JSON parsing fails, use body as-is
                                notificationId = body.Trim('"');
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(notificationId))
                {
                    _logger.LogWarning("Mark as read attempted without notification ID");
                    return new JsonResult(new { success = false, message = "Notification ID is required" });
                }

                // Validate user authentication
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Mark as read attempted by unauthenticated user for notification {NotificationId}", notificationId);
                    return new JsonResult(new { success = false, message = "User authentication required" });
                }

                var result = await _notificationServiceImpl.MarkAsReadAsync(User, notificationId);

                _logger.LogInformation("Mark as read result for notification {NotificationId} by user {UserId}: Success={Success}, Message={Message}",
                    notificationId, userId, result.Success, result.Message);

                if (result.Success)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        message = result.Message ?? "Notification marked as read successfully",
                        notificationId = notificationId
                    });
                }
                else
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = result.Message ?? "Failed to mark notification as read"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return new JsonResult(new { success = false, message = "An error occurred while marking notification as read" });
            }
        }

        public async Task<IActionResult> OnPostMarkAllAsReadAsync()
        {
            try
            {
                // Validate user authentication
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Mark all as read attempted by unauthenticated user");
                    return new JsonResult(new { success = false, message = "User authentication required" });
                }

                var result = await _notificationServiceImpl.MarkAllAsReadAsync(User);

                _logger.LogInformation("Mark all as read result for user {UserId}: Success={Success}, Message={Message}",
                    userId, result.Success, result.Message);

                if (result.Success)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        message = result.Message ?? "All notifications marked as read successfully"
                    });
                }
                else
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = result.Message ?? "Failed to mark all notifications as read"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return new JsonResult(new { success = false, message = "An error occurred while marking all notifications as read" });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] string notificationId)
        {
            try
            {
                if (string.IsNullOrEmpty(notificationId))
                {
                    return new JsonResult(new { success = false, message = "Notification ID is required" });
                }

                var result = await _notificationServiceImpl.DeleteNotificationAsync(User, notificationId);
                return new JsonResult(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
                return new JsonResult(new { success = false, message = "An error occurred while deleting the notification" });
            }
        }

        public async Task<IActionResult> OnGetNotificationForEditAsync(string notificationId)
        {
            try
            {
                if (string.IsNullOrEmpty(notificationId))
                {
                    return new JsonResult(new { success = false, message = "Notification ID is required" });
                }

                var result = await _notificationServiceImpl.GetNotificationForEditAsync(User, notificationId);

                if (result.Success && result.ViewModel != null)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        notification = result.ViewModel
                    });
                }
                else
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = result.ErrorMessage ?? "Notification not found or you don't have permission to edit it."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification for edit {NotificationId}", notificationId);
                return new JsonResult(new { success = false, message = "An error occurred" });
            }
        }

        public async Task<IActionResult> OnGetSearchUsersAsync(string searchTerm = "")
        {
            try
            {
                var users = await _notificationService.SearchUsersAsync(searchTerm);
                var userList = users.Select(u => new
                {
                    id = u.UserId,
                    name = u.FullName ?? u.Username,
                    email = u.UserEmail
                }).ToList();

                return new JsonResult(userList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users");
                return new JsonResult(new List<object>());
            }
        }

        public async Task<IActionResult> OnGetUnreadCountAsync()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new JsonResult(new { count = 0 });
                }

                var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
                return new JsonResult(new { count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return new JsonResult(new { count = 0 });
            }
        }

        public async Task<IActionResult> OnGetNotificationsAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var result = await _notificationServiceImpl.GetNotificationsAsync(User, page, pageSize);

                if (result.Success && result.ViewModel != null)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        notifications = result.ViewModel.Notifications,
                        unreadCount = result.ViewModel.UnreadCount,
                        hasNextPage = result.ViewModel.HasNextPage,
                        currentPage = result.ViewModel.CurrentPage
                    });
                }
                else
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = result.ErrorMessage ?? "Failed to load notifications"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications via AJAX");
                return new JsonResult(new { success = false, message = "An error occurred" });
            }
        }

        // GET: Get notification count for real-time updates
        public async Task<IActionResult> OnGetNotificationCountAsync()
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
                    return new JsonResult(new { count = 0, success = false, message = "User not authenticated" });
                }

                var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
                return new JsonResult(new { count = count, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification count");
                return new JsonResult(new { count = 0, success = false, message = "An error occurred" });
            }
        }

        // POST: Bulk mark notifications as read
        public async Task<IActionResult> OnPostBulkMarkAsReadAsync([FromBody] List<string> notificationIds)
        {
            try
            {
                if (notificationIds == null || !notificationIds.Any())
                {
                    return new JsonResult(new { success = false, message = "No notification IDs provided" });
                }

                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return new JsonResult(new { success = false, message = "User authentication required" });
                }

                var successCount = 0;
                var errors = new List<string>();

                foreach (var notificationId in notificationIds)
                {
                    try
                    {
                        var result = await _notificationServiceImpl.MarkAsReadAsync(User, notificationId);
                        if (result.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"Failed to mark notification {notificationId}: {result.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
                        errors.Add($"Error processing notification {notificationId}");
                    }
                }

                return new JsonResult(new
                {
                    success = successCount > 0,
                    message = $"Successfully marked {successCount} notifications as read",
                    successCount = successCount,
                    errorCount = errors.Count,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk mark as read");
                return new JsonResult(new { success = false, message = "An error occurred during bulk operation" });
            }
        }

        private async Task LoadNotificationData(int page = 1, int pageSize = 10)
        {
            try
            {
                var result = await _notificationServiceImpl.GetNotificationsAsync(User, page, pageSize);
                NotificationData = result.ViewModel ?? new NotificationIndexViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notification data");
                NotificationData = new NotificationIndexViewModel();
            }
        }

        // Handler for getting courses for instructor notifications
        public async Task<IActionResult> OnGetCoursesAsync()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return new JsonResult(new { success = false, message = "User not authenticated" });
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

                return new JsonResult(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting courses for notification");
                return new JsonResult(new { success = false, message = "An error occurred while loading courses" });
            }
        }
    }
}
