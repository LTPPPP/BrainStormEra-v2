using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using BrainStormEra_MVC.Hubs;

namespace BrainStormEra_MVC.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepo _notificationRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly IUserRepo _userRepo;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepo notificationRepo,
            ICourseRepo courseRepo,
            IUserRepo userRepo,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _notificationRepo = notificationRepo;
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _hubContext = hubContext;
            _logger = logger;
        }

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

                // Create notifications for all enrolled users
                var tasks = enrolledUsers.Select(async userId =>
                {
                    await CreateNotificationAsync(userId, title, content, type, courseId, createdBy);
                });

                await Task.WhenAll(tasks);

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
                foreach (var userId in enrolledUsers)
                {
                    if (excludeUserId == null || userId != excludeUserId)
                    {
                        var unreadCount = await GetUnreadNotificationCountAsync(userId);
                        await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateUnreadCount", unreadCount);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending notification to course {courseId}");
                return false;
            }
        }

        public async Task<bool> SendToRoleAsync(string role, string title, string content, string? type = null, string? createdBy = null)
        {
            try
            {
                // Get all users with the specified role
                var users = await _userRepo.GetUserIdsByRoleAsync(role);

                // Create notifications for all users with the role
                var tasks = users.Select(async userId =>
                {
                    await CreateNotificationAsync(userId, title, content, type, null, createdBy);
                });

                await Task.WhenAll(tasks);

                // Send real-time notifications via SignalR
                await _hubContext.Clients.Group($"Role_{role}").SendAsync("ReceiveNotification", new
                {
                    title = title,
                    content = content,
                    type = type,
                    createdAt = DateTime.Now
                });

                // Update unread counts for affected users
                foreach (var userId in users)
                {
                    var unreadCount = await GetUnreadNotificationCountAsync(userId);
                    await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateUnreadCount", unreadCount);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending notification to role {role}");
                return false;
            }
        }

        public async Task<bool> SendToAllAsync(string title, string content, string? type = null, string? createdBy = null)
        {
            try
            {
                // Get all active users (not banned)
                var users = await _userRepo.GetAllActiveUserIdsAsync();

                // Create notifications for all users
                var tasks = users.Select(async userId =>
                {
                    await CreateNotificationAsync(userId, title, content, type, null, createdBy);
                });

                await Task.WhenAll(tasks);

                // Send real-time notifications via SignalR to all connected clients
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    title = title,
                    content = content,
                    type = type,
                    createdAt = DateTime.Now
                });

                // Update unread counts for all users
                foreach (var userId in users)
                {
                    var unreadCount = await GetUnreadNotificationCountAsync(userId);
                    await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateUnreadCount", unreadCount);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending notification to all users");
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
    }
}
