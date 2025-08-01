using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using BusinessLogicLayer.Hubs;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Implementation class that handles complex business logic for Notification operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// authorization, validation, and error handling.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepo _notificationRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly IUserRepo _userRepo;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ICourseService _courseService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepo notificationRepo,
            ICourseRepo courseRepo,
            IUserRepo userRepo,
            IHubContext<NotificationHub> hubContext,
            ICourseService courseService,
            ILogger<NotificationService> logger)
        {
            _notificationRepo = notificationRepo;
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _hubContext = hubContext;
            _courseService = courseService;
            _logger = logger;
        }

        #region INotificationService Implementation

        public async Task<List<Notification>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _notificationRepo.GetUserNotificationsAsync(userId, page, pageSize);
        }

        public async Task<List<Notification>> GetAllNotificationsForUserAsync(string userId, int page = 1, int pageSize = 10)
        {
            // Get notifications that the user received OR created
            return await _notificationRepo.GetAllNotificationsForUserAsync(userId, page, pageSize);
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            return await _notificationRepo.GetUnreadNotificationCountAsync(userId);
        }

        public async Task<List<Notification>> CreateBulkNotificationsAsync(List<string> userIds, string title, string content, string? type = null, string? courseId = null, string? createdBy = null)
        {
            var notifications = userIds.Select(userId => new Notification
            {
                NotificationId = Guid.NewGuid().ToString(),
                UserId = userId,
                CourseId = courseId,
                NotificationTitle = title,
                NotificationContent = content,
                NotificationType = type ?? "General",
                IsRead = false,
                NotificationCreatedAt = DateTime.Now,
                CreatedBy = createdBy
            }).ToList();

            await _notificationRepo.CreateBulkNotificationsAsync(notifications);
            return notifications;
        }

        public async Task<Notification> CreateNotificationAsync(string userId, string title, string content, string? type = null, string? courseId = null, string? createdBy = null)
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid().ToString(),
                UserId = userId,
                CourseId = courseId,
                NotificationTitle = title,
                NotificationContent = content,
                NotificationType = type ?? "General",
                IsRead = false,
                NotificationCreatedAt = DateTime.Now,
                CreatedBy = createdBy
            };

            await _notificationRepo.CreateNotificationAsync(notification);
            return notification;
        }

        public async Task MarkAsReadAsync(string notificationId, string userId)
        {
            await _notificationRepo.MarkAsReadAsync(notificationId, userId);
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _notificationRepo.MarkAllAsReadAsync(userId);
        }

        public async Task DeleteNotificationAsync(string notificationId, string userId)
        {
            await _notificationRepo.DeleteNotificationAsync(notificationId, userId);
        }

        /// <summary>
        /// Admin function to delete notification globally (affects all users who received this notification)
        /// </summary>
        public async Task<bool> AdminDeleteNotificationGloballyAsync(string notificationId, string adminUserId)
        {
            try
            {
                // First get the notification to identify related notifications
                var notification = await _notificationRepo.GetNotificationByIdAsync(notificationId);
                if (notification == null)
                {
                    _logger.LogWarning("Notification {NotificationId} not found for global delete", notificationId);
                    return false;
                }

                // Find all notifications with same title, content, and created time (batch notifications)
                var relatedNotifications = await _notificationRepo.GetRelatedNotificationsAsync(
                    notification.NotificationTitle,
                    notification.NotificationContent,
                    notification.NotificationCreatedAt,
                    notification.CourseId
                );

                if (relatedNotifications.Any())
                {
                    // Delete all related notifications
                    var deleteResult = await _notificationRepo.DeleteNotificationsBatchAsync(
                        relatedNotifications.Select(n => n.NotificationId).ToList()
                    );

                    _logger.LogInformation("Admin {AdminUserId} globally deleted {Count} related notifications for notification {NotificationId}",
                        adminUserId, relatedNotifications.Count, notificationId);

                    // Send real-time notifications to affected users
                    foreach (var relatedNotification in relatedNotifications)
                    {
                        await _hubContext.Clients.Group($"User_{relatedNotification.UserId}")
                            .SendAsync("NotificationDeleted", new { id = relatedNotification.NotificationId });
                    }

                    return deleteResult;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in admin global delete for notification {NotificationId}", notificationId);
                return false;
            }
        }

        /// <summary>
        /// Admin function to update notification globally (affects all users who received this notification)
        /// </summary>
        public async Task<bool> AdminUpdateNotificationGloballyAsync(string notificationId, string adminUserId, string newTitle, string newContent, string? newType = null)
        {
            try
            {
                // First get the notification to identify related notifications
                var notification = await _notificationRepo.GetNotificationByIdAsync(notificationId);
                if (notification == null)
                {
                    _logger.LogWarning("Notification {NotificationId} not found for global update", notificationId);
                    return false;
                }

                // Find all notifications with same title, content, and created time (batch notifications)
                var relatedNotifications = await _notificationRepo.GetRelatedNotificationsAsync(
                    notification.NotificationTitle,
                    notification.NotificationContent,
                    notification.NotificationCreatedAt,
                    notification.CourseId
                );

                if (relatedNotifications.Any())
                {
                    // Update all related notifications
                    var updateResult = await _notificationRepo.UpdateNotificationsBatchAsync(
                        relatedNotifications.Select(n => n.NotificationId).ToList(),
                        newTitle,
                        newContent,
                        newType
                    );

                    _logger.LogInformation("Admin {AdminUserId} globally updated {Count} related notifications for notification {NotificationId}",
                        adminUserId, relatedNotifications.Count, notificationId);

                    // Send real-time notifications to affected users
                    foreach (var relatedNotification in relatedNotifications)
                    {
                        await _hubContext.Clients.Group($"User_{relatedNotification.UserId}")
                            .SendAsync("NotificationUpdated", new
                            {
                                id = relatedNotification.NotificationId,
                                title = newTitle,
                                content = newContent,
                                type = newType ?? relatedNotification.NotificationType
                            });
                    }

                    return updateResult;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in admin global update for notification {NotificationId}", notificationId);
                return false;
            }
        }

        public async Task<bool> SendToUserAsync(string userId, string title, string content, string? type = null, string? courseId = null, string? createdBy = null)
        {
            try
            {
                // Create notification in database
                var notification = await CreateNotificationAsync(userId, title, content, type, courseId, createdBy);

                // Send real-time notification via SignalR
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", new
                {
                    id = notification.NotificationId,
                    title = notification.NotificationTitle,
                    content = notification.NotificationContent,
                    type = notification.NotificationType,
                    courseId = notification.CourseId,
                    createdAt = notification.NotificationCreatedAt,
                    isRead = notification.IsRead
                });

                // Update unread count
                var unreadCount = await GetUnreadNotificationCountAsync(userId);
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateUnreadCount", unreadCount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending notification to user {userId}");
                return false;
            }
        }

        public async Task<bool> SendToCourseAsync(string courseId, string title, string content, string? type = null, string? excludeUserId = null, string? createdBy = null)
        {
            try
            {
                // Get all enrolled users in the course
                var enrolledUsers = await _courseRepo.GetEnrolledUserIdsAsync(courseId);

                if (excludeUserId != null)
                {
                    enrolledUsers = enrolledUsers.Where(u => u != excludeUserId).ToList();
                }

                if (!enrolledUsers.Any())
                {
                    _logger.LogWarning("No enrolled users found for course {CourseId}", courseId);
                    return true; // Not an error, just no users to notify
                }

                // Create notifications in bulk (single transaction)
                var notifications = await CreateBulkNotificationsAsync(enrolledUsers, title, content, type, courseId, createdBy);

                // Send real-time notifications via SignalR
                await _hubContext.Clients.Group($"Course_{courseId}").SendAsync("ReceiveNotification", new
                {
                    title = title,
                    content = content,
                    type = type,
                    courseId = courseId,
                    createdAt = DateTime.Now
                });

                // Update unread counts for affected users
                foreach (var notification in notifications)
                {
                    var unreadCount = await GetUnreadNotificationCountAsync(notification.UserId);
                    await _hubContext.Clients.Group($"User_{notification.UserId}").SendAsync("UpdateUnreadCount", unreadCount);
                }

                _logger.LogInformation("Successfully sent notification to {Count} users in course {CourseId}", enrolledUsers.Count, courseId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to course {CourseId}", courseId);
                return false;
            }
        }

        public async Task<bool> SendToMultipleUsersAsync(List<string> userIds, string title, string content, string? type = null, string? courseId = null, string? createdBy = null)
        {
            try
            {
                if (userIds == null || !userIds.Any())
                {
                    _logger.LogWarning("No user IDs provided for multiple user notification");
                    return false;
                }

                // Create notifications in bulk (single transaction)
                var notifications = await CreateBulkNotificationsAsync(userIds, title, content, type, courseId, createdBy);

                // Send real-time notifications via SignalR to each user
                foreach (var notification in notifications)
                {
                    await _hubContext.Clients.Group($"User_{notification.UserId}").SendAsync("ReceiveNotification", new
                    {
                        id = notification.NotificationId,
                        title = notification.NotificationTitle,
                        content = notification.NotificationContent,
                        type = notification.NotificationType,
                        courseId = notification.CourseId,
                        createdAt = notification.NotificationCreatedAt,
                        isRead = notification.IsRead
                    });

                    // Update unread count for each user
                    var unreadCount = await GetUnreadNotificationCountAsync(notification.UserId);
                    await _hubContext.Clients.Group($"User_{notification.UserId}").SendAsync("UpdateUnreadCount", unreadCount);
                }

                _logger.LogInformation("Successfully sent notification to {Count} users", userIds.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to multiple users");
                return false;
            }
        }

        public async Task<bool> SendToRoleAsync(string role, string title, string content, string? type = null, string? createdBy = null)
        {
            try
            {
                // Get all users with the specified role
                var users = await _userRepo.GetUserIdsByRoleAsync(role);

                if (!users.Any())
                {
                    _logger.LogWarning("No users found with role {Role}", role);
                    return true; // Not an error, just no users to notify
                }

                // Create notifications in bulk (single transaction)
                var notifications = await CreateBulkNotificationsAsync(users, title, content, type, null, createdBy);

                // Send real-time notifications via SignalR
                await _hubContext.Clients.Group($"Role_{role}").SendAsync("ReceiveNotification", new
                {
                    title = title,
                    content = content,
                    type = type,
                    createdAt = DateTime.Now
                });

                // Update unread counts for affected users
                foreach (var notification in notifications)
                {
                    var unreadCount = await GetUnreadNotificationCountAsync(notification.UserId);
                    await _hubContext.Clients.Group($"User_{notification.UserId}").SendAsync("UpdateUnreadCount", unreadCount);
                }

                _logger.LogInformation("Successfully sent notification to {Count} users with role {Role}", users.Count, role);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to role {Role}", role);
                return false;
            }
        }

        public async Task<bool> SendToAllAsync(string title, string content, string? type = null, string? createdBy = null)
        {
            try
            {
                // Get all active users (not banned)
                var users = await _userRepo.GetAllActiveUserIdsAsync();

                if (!users.Any())
                {
                    _logger.LogWarning("No active users found to send notification to");
                    return true; // Not an error, just no users to notify
                }

                // Create notifications in bulk (single transaction)
                var notifications = await CreateBulkNotificationsAsync(users, title, content, type, null, createdBy);

                // Send real-time notifications via SignalR to all connected clients
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    title = title,
                    content = content,
                    type = type,
                    createdAt = DateTime.Now
                });

                // Update unread counts for all users
                foreach (var notification in notifications)
                {
                    var unreadCount = await GetUnreadNotificationCountAsync(notification.UserId);
                    await _hubContext.Clients.Group($"User_{notification.UserId}").SendAsync("UpdateUnreadCount", unreadCount);
                }

                _logger.LogInformation("Successfully sent notification to all {Count} active users", users.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to all users");
                return false;
            }
        }

        public async Task<Notification?> GetNotificationForEditAsync(string notificationId, string userId)
        {
            // Only allow editing notifications created by the current user
            return await _notificationRepo.GetNotificationForEditAsync(notificationId, userId);
        }

        public async Task<bool> UpdateNotificationAsync(string notificationId, string userId, string title, string content, string? type)
        {
            try
            {
                var result = await _notificationRepo.UpdateNotificationContentAsync(notificationId, userId, title, content, type);

                if (!result) return false;

                // Get the notification to send update via SignalR
                var notification = await _notificationRepo.GetNotificationForEditAsync(notificationId, userId);
                if (notification != null)
                {
                    // Send updated notification via SignalR to the recipient
                    await _hubContext.Clients.Group($"User_{notification.UserId}").SendAsync("NotificationUpdated", new
                    {
                        id = notification.NotificationId,
                        title = notification.NotificationTitle,
                        content = notification.NotificationContent,
                        type = notification.NotificationType
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating notification {notificationId}");
                return false;
            }
        }

        public async Task<bool> RestoreNotificationAsync(string notificationId, string userId)
        {
            return await _notificationRepo.RestoreNotificationAsync(notificationId, userId);
        }

        public async Task<List<Notification>> GetDeletedNotificationsAsync(string userId, int page = 1, int pageSize = 20)
        {
            return await _notificationRepo.GetDeletedNotificationsAsync(userId, page, pageSize);
        }

        public async Task<List<Account>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    // Return top 20 users if no search term
                    return await _userRepo.GetTopUsersAsync(20);
                }

                // Search users by name, email, or username
                return await _userRepo.SearchUsersAsync(searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term: {SearchTerm}", searchTerm);
                return new List<Account>();
            }
        }

        public async Task<List<Account>> GetEnrolledUsersAsync(string instructorId, string? courseId = null, string? searchTerm = null)
        {
            try
            {
                // Get users enrolled in the instructor's courses
                return await _userRepo.GetUsersWithEnrollmentsAsync(instructorId, courseId, searchTerm, null, 1, 1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrolled users for instructor: {InstructorId}", instructorId);
                return new List<Account>();
            }
        }

        #endregion

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

                var notifications = await GetAllNotificationsForUserAsync(userId, page, pageSize);
                var unreadCount = await GetUnreadNotificationCountAsync(userId);

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

                await MarkAsReadAsync(notificationId, userId);

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

                await MarkAllAsReadAsync(userId);

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
                            success = await SendToUserAsync(
                                model.TargetUserId, model.Title, model.Content, model.Type, model.CourseId, userId);
                        }
                        break;

                    case NotificationTargetType.MultipleUsers:
                        if (model.TargetUserIds != null && model.TargetUserIds.Any())
                        {
                            success = await SendToMultipleUsersAsync(
                                model.TargetUserIds, model.Title, model.Content, model.Type, model.CourseId, userId);
                        }
                        break;

                    case NotificationTargetType.Course:
                        if (!string.IsNullOrEmpty(model.CourseId))
                        {
                            success = await SendToCourseAsync(
                                model.CourseId, model.Title, model.Content, model.Type, userId, userId);
                        }
                        break;

                    case NotificationTargetType.Role:
                        if (!string.IsNullOrEmpty(model.TargetRole))
                        {
                            success = await SendToRoleAsync(
                                model.TargetRole, model.Title, model.Content, model.Type, userId);
                        }
                        break;

                    case NotificationTargetType.All:
                        success = await SendToAllAsync(
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

                var notification = await GetNotificationForEditAsync(notificationId, userId);
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
                var existingNotification = await GetNotificationForEditAsync(notificationId, userId);
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

                // Check if user is admin - if so, perform global update
                if (userRole == "admin" || userRole == "instructor")
                {
                    var globalUpdateResult = await AdminUpdateNotificationGloballyAsync(notificationId, userId, model.Title, model.Content, model.Type);

                    if (globalUpdateResult)
                    {
                        return new EditNotificationResult
                        {
                            Success = true,
                            SuccessMessage = "Notification updated globally for all users successfully!",
                            RedirectAction = "Index",
                            RedirectController = "Notification"
                        };
                    }
                    else
                    {
                        // Fallback to single user update
                        var success = await UpdateNotificationAsync(notificationId, userId, model.Title, model.Content, model.Type);

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
                }
                else
                {
                    // Regular user - only update their copy
                    var success = await UpdateNotificationAsync(notificationId, userId, model.Title, model.Content, model.Type);

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
                // Try different ways to get user ID - same as in controller
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }

                _logger.LogInformation("DeleteNotificationAsync - NotificationId: {NotificationId}, UserId: {UserId}", notificationId, userId);

                if (string.IsNullOrEmpty(userId))
                {
                    return new DeleteNotificationResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                // Get existing notification to check authorization
                var existingNotification = await GetNotificationForEditAsync(notificationId, userId);

                _logger.LogInformation("DeleteNotificationAsync - Notification found: {Found}", existingNotification != null);

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
                _logger.LogInformation("DeleteNotificationAsync - UserRole: {UserRole}", userRole);

                if (!IsAuthorizedToDeleteNotification(existingNotification, userId, userRole))
                {
                    return new DeleteNotificationResult
                    {
                        Success = false,
                        Message = "You are not authorized to delete this notification."
                    };
                }

                // Check if user is admin - if so, perform global delete
                if (userRole == "admin" || userRole == "instructor")
                {
                    var globalDeleteResult = await AdminDeleteNotificationGloballyAsync(notificationId, userId);

                    if (globalDeleteResult)
                    {
                        return new DeleteNotificationResult
                        {
                            Success = true,
                            Message = "Notification deleted globally for all users successfully!"
                        };
                    }
                    else
                    {
                        // Fallback to single user delete
                        await DeleteNotificationAsync(notificationId, userId);
                        return new DeleteNotificationResult
                        {
                            Success = true,
                            Message = "Notification removed from your inbox successfully!"
                        };
                    }
                }
                else
                {
                    // Regular user - only delete from their inbox
                    await DeleteNotificationAsync(notificationId, userId);
                    return new DeleteNotificationResult
                    {
                        Success = true,
                        Message = "Notification removed from your inbox successfully!"
                    };
                }
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
                        else if (model.TargetUserId == userId)
                        {
                            AddValidationError(errors, nameof(model.TargetUserId), "You cannot send notification to yourself.");
                        }
                        break;

                    case NotificationTargetType.MultipleUsers:
                        if (model.TargetUserIds == null || !model.TargetUserIds.Any())
                        {
                            AddValidationError(errors, nameof(model.TargetUserIds), "At least one target user is required when sending to multiple users.");
                        }
                        else if (model.TargetUserIds.Count > 50)
                        {
                            AddValidationError(errors, nameof(model.TargetUserIds), "Cannot send to more than 50 users at once.");
                        }
                        else
                        {
                            // Check if user is trying to send to themselves
                            if (model.TargetUserIds.Contains(userId))
                            {
                                AddValidationError(errors, nameof(model.TargetUserIds), "You cannot send notification to yourself.");
                            }

                            // Check for duplicates
                            var duplicates = model.TargetUserIds.GroupBy(x => x)
                                .Where(g => g.Count() > 1)
                                .Select(g => g.Key)
                                .ToList();

                            if (duplicates.Any())
                            {
                                AddValidationError(errors, nameof(model.TargetUserIds),
                                    "Duplicate users detected in the selection.");
                            }

                            // Validate no empty user IDs
                            if (model.TargetUserIds.Any(id => string.IsNullOrWhiteSpace(id)))
                            {
                                AddValidationError(errors, nameof(model.TargetUserIds),
                                    "Invalid user selection detected. Please refresh and try again.");
                            }
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
            if (string.IsNullOrEmpty(userRole)) return false;

            return userRole.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                   userRole.Equals("instructor", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if user is authorized to edit a specific notification
        /// </summary>
        private static bool IsAuthorizedToEditNotification(Notification notification, string userId, string? userRole)
        {
            // Admin can edit all notifications
            if (!string.IsNullOrEmpty(userRole) && userRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
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
            if (!string.IsNullOrEmpty(userRole) && userRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
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
            var validRoles = new[] { "admin", "instructor", "learner" };
            return validRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validate if the notification type is valid
        /// </summary>
        private static bool IsValidNotificationType(string type)
        {
            var validTypes = new[] { "General", "Course", "System", "Achievement", "Payment", "info", "warning", "success", "urgent", "course", "announcement" };
            return validTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region User Search Operations

        /// <summary>
        /// Search users for notification targeting
        /// </summary>
        public async Task<UserSearchResult> SearchUsersAsync(ClaimsPrincipal user, string searchTerm)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new UserSearchResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!IsAuthorizedToCreateNotifications(userRole))
                {
                    return new UserSearchResult
                    {
                        Success = false,
                        ErrorMessage = "You are not authorized to search users"
                    };
                }

                // Get users based on search term
                var users = await SearchUsersAsync(searchTerm?.Trim() ?? "");

                // Filter out the current user (creator) to prevent sending notification to themselves
                var filteredUsers = users.Where(u => u.UserId != userId).ToList();

                return new UserSearchResult
                {
                    Success = true,
                    Users = filteredUsers.Select(u => new UserSearchItem
                    {
                        UserId = u.UserId,
                        UserName = u.Username,
                        Email = u.UserEmail,
                        FullName = u.FullName ?? "",
                        Role = u.UserRole
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching users");
                return new UserSearchResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while searching users"
                };
            }
        }

        /// <summary>
        /// Get enrolled users for notification targeting (instructor only)
        /// </summary>
        public async Task<UserSearchResult> GetEnrolledUsersAsync(ClaimsPrincipal user, string? courseId = null, string? searchTerm = null)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new UserSearchResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!IsAuthorizedToCreateNotifications(userRole))
                {
                    return new UserSearchResult
                    {
                        Success = false,
                        ErrorMessage = "You are not authorized to get enrolled users"
                    };
                }

                // Get enrolled users for the instructor
                var users = await GetEnrolledUsersAsync(userId, courseId, searchTerm?.Trim());

                // Filter out the current user (creator) to prevent sending notification to themselves
                var filteredUsers = users.Where(u => u.UserId != userId).ToList();

                return new UserSearchResult
                {
                    Success = true,
                    Users = filteredUsers.Select(u => new UserSearchItem
                    {
                        UserId = u.UserId,
                        UserName = u.Username,
                        Email = u.UserEmail,
                        FullName = u.FullName ?? "",
                        Role = u.UserRole
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting enrolled users");
                return new UserSearchResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while getting enrolled users"
                };
            }
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

    public class UserSearchResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<UserSearchItem> Users { get; set; } = new List<UserSearchItem>();
    }

    public class UserSearchItem
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    #endregion
}








