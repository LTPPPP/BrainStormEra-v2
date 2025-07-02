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
        private readonly ILogger<NotificationsModel> _logger;

        public NotificationsModel(
            NotificationServiceImpl notificationServiceImpl,
            INotificationService notificationService,
            ILogger<NotificationsModel> logger)
        {
            _notificationServiceImpl = notificationServiceImpl;
            _notificationService = notificationService;
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

        public async Task<IActionResult> OnPostMarkAsReadAsync([FromBody] string notificationId)
        {
            try
            {
                if (string.IsNullOrEmpty(notificationId))
                {
                    return new JsonResult(new { success = false, message = "Notification ID is required" });
                }

                var result = await _notificationServiceImpl.MarkAsReadAsync(User, notificationId);
                return new JsonResult(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return new JsonResult(new { success = false, message = "An error occurred" });
            }
        }

        public async Task<IActionResult> OnPostMarkAllAsReadAsync()
        {
            try
            {
                var result = await _notificationServiceImpl.MarkAllAsReadAsync(User);
                return new JsonResult(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return new JsonResult(new { success = false, message = "An error occurred" });
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
    }
}
