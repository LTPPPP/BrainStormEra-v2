using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using System.Security.Claims;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Implementation class that handles complex business logic for Notification operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// authorization, validation, and error handling.
    /// </summary>
    public class NotificationServiceImpl
    {
        private readonly INotificationService _notificationService;
        private readonly ICourseService _courseService;
        private readonly ILogger<NotificationServiceImpl> _logger;

        public NotificationServiceImpl(
            INotificationService notificationService,
            ICourseService courseService,
            ILogger<NotificationServiceImpl> logger)
        {
            _notificationService = notificationService;
            _courseService = courseService;
            _logger = logger;
        }

        #region Notification Index Operations

        /// <summary>
        /// Get notifications for index page with authentication check
        /// </summary>
        public async Task<NotificationIndexResult> GetNotificationsAsync(ClaimsPrincipal user, int page = 1, int pageSize = 10)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new NotificationIndexResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var notifications = await _notificationService.GetAllNotificationsForUserAsync(userId, page, pageSize);
                var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(userId);

                var viewModel = new NotificationIndexViewModel
                {
                    Notifications = notifications,
                    UnreadCount = unreadCount,
                    CurrentPage = page,
                    PageSize = pageSize,
                    CurrentUserId = userId
                };

                return new NotificationIndexResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading notifications for user");
                return new NotificationIndexResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading notifications. Please try again later.",
                    ViewModel = new NotificationIndexViewModel()
                };
            }
        }

        /// <summary>
        /// Mark notification as read with authorization check
        /// </summary>
        public async Task<MarkAsReadResult> MarkAsReadAsync(ClaimsPrincipal user, string notificationId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new MarkAsReadResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                await _notificationService.MarkAsReadAsync(notificationId, userId);

                return new MarkAsReadResult
                {
                    Success = true,
                    Message = "Notification marked as read"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while marking notification {NotificationId} as read", notificationId);
                return new MarkAsReadResult
                {
                    Success = false,
                    Message = "An error occurred while marking notification as read"
                };
            }
        }

        /// <summary>
        /// Mark all notifications as read with authorization check
        /// </summary>
        public async Task<MarkAllAsReadResult> MarkAllAsReadAsync(ClaimsPrincipal user)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new MarkAllAsReadResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                await _notificationService.MarkAllAsReadAsync(userId);

                return new MarkAllAsReadResult
                {
                    Success = true,
                    Message = "All notifications marked as read"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while marking all notifications as read for user {UserId}", user.FindFirst("UserId")?.Value);
                return new MarkAllAsReadResult
                {
                    Success = false,
                    Message = "An error occurred while marking all notifications as read"
                };
            }
        }

        #endregion

        #region Notification Creation Operations        /// <summary>
        /// Get create notification view model with authorization check
        /// </summary>
        public async Task<CreateNotificationResult> GetCreateNotificationViewModelAsync(ClaimsPrincipal user)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new CreateNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                // Check if user has permission to create notifications (Admin or Instructor)
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!IsAuthorizedToCreateNotifications(userRole))
                {
                    return new CreateNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "You are not authorized to create notifications",
                        RedirectAction = "Index",
                        RedirectController = "Notification"
                    };
                }

                var viewModel = new NotificationCreateViewModel();

                return await Task.FromResult(new CreateNotificationResult
                {
                    Success = true,
                    ViewModel = viewModel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading create notification page");
                return new CreateNotificationResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the create notification page.",
                    RedirectAction = "Index",
                    RedirectController = "Notification"
                };
            }
        }

        /// <summary>
        /// Create a new notification with comprehensive validation
        /// </summary>
        public async Task<CreateNotificationResult> CreateNotificationAsync(ClaimsPrincipal user, NotificationCreateViewModel model)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new CreateNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                // Check if user has permission to create notifications
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!IsAuthorizedToCreateNotifications(userRole))
                {
                    return new CreateNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "You are not authorized to create notifications",
                        RedirectAction = "Index",
                        RedirectController = "Notification"
                    };
                }

                // Validate notification model
                var validationResult = await ValidateNotificationModelAsync(model, userId);
                if (!validationResult.IsValid)
                {
                    return new CreateNotificationResult
                    {
                        Success = false,
                        ValidationErrors = validationResult.Errors,
                        ViewModel = model,
                        ReturnView = true
                    };
                }

                // Create notification based on target type
                bool success = false;
                switch (model.TargetType)
                {
                    case NotificationTargetType.User:
                        if (!string.IsNullOrEmpty(model.TargetUserId))
                        {
                            success = await _notificationService.SendToUserAsync(
                                model.TargetUserId, model.Title, model.Content, model.Type, model.CourseId, userId);
                        }
                        break;

                    case NotificationTargetType.Course:
                        if (!string.IsNullOrEmpty(model.CourseId))
                        {
                            success = await _notificationService.SendToCourseAsync(
                                model.CourseId, model.Title, model.Content, model.Type, userId, userId);
                        }
                        break;

                    case NotificationTargetType.Role:
                        if (!string.IsNullOrEmpty(model.TargetRole))
                        {
                            success = await _notificationService.SendToRoleAsync(
                                model.TargetRole, model.Title, model.Content, model.Type, userId);
                        }
                        break;

                    case NotificationTargetType.All:
                        success = await _notificationService.SendToAllAsync(
                            model.Title, model.Content, model.Type, userId);
                        break;
                }

                if (success)
                {
                    return new CreateNotificationResult
                    {
                        Success = true,
                        SuccessMessage = "Notification created and sent successfully!",
                        RedirectAction = "Index",
                        RedirectController = "Notification"
                    };
                }
                else
                {
                    return new CreateNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create notification. Please try again.",
                        ViewModel = model,
                        ReturnView = true
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating notification");
                return new CreateNotificationResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while creating the notification. Please try again.",
                    ViewModel = model,
                    ReturnView = true
                };
            }
        }

        #endregion

        #region Notification Edit Operations

        /// <summary>
        /// Get notification for editing with authorization check
        /// </summary>
        public async Task<EditNotificationResult> GetNotificationForEditAsync(ClaimsPrincipal user, string notificationId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var notification = await _notificationService.GetNotificationForEditAsync(notificationId, userId);
                if (notification == null)
                {
                    return new EditNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "Notification not found or you are not authorized to edit this notification.",
                        RedirectAction = "Index",
                        RedirectController = "Notification"
                    };
                }

                // Check if user has permission to edit this notification
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!IsAuthorizedToEditNotification(notification, userId, userRole))
                {
                    return new EditNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "You are not authorized to edit this notification.",
                        RedirectAction = "Index",
                        RedirectController = "Notification"
                    };
                }
                var viewModel = new NotificationEditViewModel
                {
                    NotificationId = notification.NotificationId,
                    Title = notification.NotificationTitle,
                    Content = notification.NotificationContent,
                    Type = notification.NotificationType,
                    CourseId = notification.CourseId,
                    RecipientUserName = notification.UserId, // This might need to be resolved to username
                    CreatedAt = notification.NotificationCreatedAt
                };

                return new EditNotificationResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading edit notification page for notification {NotificationId}", notificationId);
                return new EditNotificationResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the edit notification page.",
                    RedirectAction = "Index",
                    RedirectController = "Notification"
                };
            }
        }

        /// <summary>
        /// Update notification with comprehensive validation
        /// </summary>
        public async Task<EditNotificationResult> UpdateNotificationAsync(ClaimsPrincipal user, string notificationId, NotificationEditViewModel model)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                // Get existing notification to check authorization
                var existingNotification = await _notificationService.GetNotificationForEditAsync(notificationId, userId);
                if (existingNotification == null)
                {
                    return new EditNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "Notification not found or you are not authorized to edit this notification.",
                        RedirectAction = "Index",
                        RedirectController = "Notification"
                    };
                }

                // Check if user has permission to edit this notification
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!IsAuthorizedToEditNotification(existingNotification, userId, userRole))
                {
                    return new EditNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "You are not authorized to edit this notification.",
                        RedirectAction = "Index",
                        RedirectController = "Notification"
                    };
                }

                // Validate notification model for editing
                var validationResult = await ValidateNotificationModelForEditAsync(model, userId, notificationId);
                if (!validationResult.IsValid)
                {
                    return new EditNotificationResult
                    {
                        Success = false,
                        ValidationErrors = validationResult.Errors,
                        ViewModel = model,
                        ReturnView = true
                    };
                }

                var success = await _notificationService.UpdateNotificationAsync(notificationId, userId, model.Title, model.Content, model.Type);

                if (success)
                {
                    return new EditNotificationResult
                    {
                        Success = true,
                        SuccessMessage = "Notification updated successfully!",
                        RedirectAction = "Index",
                        RedirectController = "Notification"
                    };
                }
                else
                {
                    return new EditNotificationResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to update notification. Please try again.",
                        ViewModel = model,
                        ReturnView = true
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating notification {NotificationId}", notificationId);
                return new EditNotificationResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while updating the notification. Please try again.",
                    ViewModel = model,
                    ReturnView = true
                };
            }
        }

        #endregion

        #region Notification Delete Operations

        /// <summary>
        /// Delete notification with authorization check
        /// </summary>
        public async Task<DeleteNotificationResult> DeleteNotificationAsync(ClaimsPrincipal user, string notificationId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new DeleteNotificationResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                // Get existing notification to check authorization
                var existingNotification = await _notificationService.GetNotificationForEditAsync(notificationId, userId);
                if (existingNotification == null)
                {
                    return new DeleteNotificationResult
                    {
                        Success = false,
                        Message = "Notification not found or you are not authorized to delete this notification."
                    };
                }

                // Check if user has permission to delete this notification
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!IsAuthorizedToDeleteNotification(existingNotification, userId, userRole))
                {
                    return new DeleteNotificationResult
                    {
                        Success = false,
                        Message = "You are not authorized to delete this notification."
                    };
                }

                await _notificationService.DeleteNotificationAsync(notificationId, userId);

                return new DeleteNotificationResult
                {
                    Success = true,
                    Message = "Notification deleted successfully!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting notification {NotificationId}", notificationId);
                return new DeleteNotificationResult
                {
                    Success = false,
                    Message = "An error occurred while deleting the notification."
                };
            }
        }

        #endregion

        #region Private Validation Methods

        /// <summary>
        /// Validates notification model with comprehensive business rules
        /// </summary>
        private async Task<NotificationValidationResult> ValidateNotificationModelAsync(NotificationCreateViewModel model, string userId)
        {
            var errors = new Dictionary<string, List<string>>();

            try
            {
                // Validate title
                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    AddValidationError(errors, nameof(model.Title), "Title is required.");
                }
                else if (model.Title.Length > 200)
                {
                    AddValidationError(errors, nameof(model.Title), "Title cannot exceed 200 characters.");
                }
                else if (ContainsInappropriateContent(model.Title))
                {
                    AddValidationError(errors, nameof(model.Title), "Title contains inappropriate content.");
                }

                // Validate content
                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    AddValidationError(errors, nameof(model.Content), "Content is required.");
                }
                else if (model.Content.Length > 2000)
                {
                    AddValidationError(errors, nameof(model.Content), "Content cannot exceed 2000 characters.");
                }
                else if (ContainsInappropriateContent(model.Content))
                {
                    AddValidationError(errors, nameof(model.Content), "Content contains inappropriate content.");
                }

                // Validate target type specific fields
                switch (model.TargetType)
                {
                    case NotificationTargetType.User:
                        if (string.IsNullOrWhiteSpace(model.TargetUserId))
                        {
                            AddValidationError(errors, nameof(model.TargetUserId), "Target user is required when sending to specific user.");
                        }
                        break;

                    case NotificationTargetType.Course:
                        if (string.IsNullOrWhiteSpace(model.CourseId))
                        {
                            AddValidationError(errors, nameof(model.CourseId), "Course is required when sending to course members.");
                        }
                        else
                        {
                            // Validate course exists and user has access
                            var course = await _courseService.GetCourseDetailAsync(model.CourseId, userId);
                            if (course == null)
                            {
                                AddValidationError(errors, nameof(model.CourseId), "Selected course not found or you don't have access to it.");
                            }
                        }
                        break;

                    case NotificationTargetType.Role:
                        if (string.IsNullOrWhiteSpace(model.TargetRole))
                        {
                            AddValidationError(errors, nameof(model.TargetRole), "Target role is required when sending to role members.");
                        }
                        else if (!IsValidRole(model.TargetRole))
                        {
                            AddValidationError(errors, nameof(model.TargetRole), "Invalid role selected.");
                        }
                        break;
                }

                // Validate type if provided
                if (!string.IsNullOrEmpty(model.Type) && !IsValidNotificationType(model.Type))
                {
                    AddValidationError(errors, nameof(model.Type), "Invalid notification type.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during notification validation");
                AddValidationError(errors, "", "An error occurred during validation. Please try again.");
            }

            return new NotificationValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }        /// <summary>
                 /// Validates notification model for editing with additional business rules
                 /// </summary>
        private async Task<NotificationValidationResult> ValidateNotificationModelForEditAsync(NotificationEditViewModel model, string userId, string notificationId)
        {
            var errors = new Dictionary<string, List<string>>();

            try
            {
                // Validate title
                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    AddValidationError(errors, nameof(model.Title), "Title is required.");
                }
                else if (model.Title.Length > 200)
                {
                    AddValidationError(errors, nameof(model.Title), "Title cannot exceed 200 characters.");
                }
                else if (ContainsInappropriateContent(model.Title))
                {
                    AddValidationError(errors, nameof(model.Title), "Title contains inappropriate content.");
                }

                // Validate content
                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    AddValidationError(errors, nameof(model.Content), "Content is required.");
                }
                else if (model.Content.Length > 2000)
                {
                    AddValidationError(errors, nameof(model.Content), "Content cannot exceed 2000 characters.");
                }
                else if (ContainsInappropriateContent(model.Content))
                {
                    AddValidationError(errors, nameof(model.Content), "Content contains inappropriate content.");
                }

                // Validate type if provided
                if (!string.IsNullOrEmpty(model.Type) && !IsValidNotificationType(model.Type))
                {
                    AddValidationError(errors, nameof(model.Type), "Invalid notification type.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during notification edit validation for notification {NotificationId}", notificationId);
                AddValidationError(errors, "", "An error occurred during validation. Please try again.");
            }

            return await Task.FromResult(new NotificationValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            });
        }

        #endregion

        #region Private Authorization Methods

        /// <summary>
        /// Check if user is authorized to create notifications
        /// </summary>
        private static bool IsAuthorizedToCreateNotifications(string? userRole)
        {
            return userRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
                   userRole?.Equals("Instructor", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Check if user is authorized to edit a specific notification
        /// </summary>
        private static bool IsAuthorizedToEditNotification(Notification notification, string userId, string? userRole)
        {
            // Admin can edit all notifications
            if (userRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true)
                return true;

            // Creator can edit their own notifications
            if (notification.CreatedBy == userId)
                return true;

            // Recipient can edit notifications sent to them (limited editing)
            if (notification.UserId == userId)
                return true;

            return false;
        }

        /// <summary>
        /// Check if user is authorized to delete a specific notification
        /// </summary>
        private static bool IsAuthorizedToDeleteNotification(Notification notification, string userId, string? userRole)
        {
            // Admin can delete all notifications
            if (userRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true)
                return true;

            // Creator can delete their own notifications
            if (notification.CreatedBy == userId)
                return true;

            // Recipient can delete notifications sent to them
            if (notification.UserId == userId)
                return true;

            return false;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Helper method to add validation errors
        /// </summary>
        private static void AddValidationError(Dictionary<string, List<string>> errors, string key, string message)
        {
            if (!errors.ContainsKey(key))
            {
                errors[key] = new List<string>();
            }
            errors[key].Add(message);
        }

        /// <summary>
        /// Simple content filter for inappropriate content
        /// </summary>
        private static bool ContainsInappropriateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            var inappropriateWords = new[] { "spam", "scam", "phishing", "virus", "malware", "hack" };
            return inappropriateWords.Any(word => content.ToLower().Contains(word.ToLower()));
        }

        /// <summary>
        /// Validate if the role is a valid system role
        /// </summary>
        private static bool IsValidRole(string role)
        {
            var validRoles = new[] { "Admin", "Instructor", "Learner" };
            return validRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validate if the notification type is valid
        /// </summary>
        private static bool IsValidNotificationType(string type)
        {
            var validTypes = new[] { "Info", "Warning", "Success", "Error", "Course", "Assignment", "System" };
            return validTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
        }

        #endregion
    }

    #region Result Classes

    public class NotificationIndexResult
    {
        public bool Success { get; set; }
        public NotificationIndexViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
    }

    public class CreateNotificationResult
    {
        public bool Success { get; set; }
        public NotificationCreateViewModel? ViewModel { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public bool ReturnView { get; set; }
        public Dictionary<string, List<string>>? ValidationErrors { get; set; }
    }

    public class EditNotificationResult
    {
        public bool Success { get; set; }
        public NotificationEditViewModel? ViewModel { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public bool ReturnView { get; set; }
        public Dictionary<string, List<string>>? ValidationErrors { get; set; }
    }

    public class DeleteNotificationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class MarkAsReadResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class MarkAllAsReadResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class NotificationValidationResult
    {
        public bool IsValid { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; } = new();
    }

    #endregion
}








