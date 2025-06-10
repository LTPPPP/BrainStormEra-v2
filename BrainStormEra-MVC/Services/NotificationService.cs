using DataAccessLayer.Data;
using DataAccessLayer.Models;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BrainStormEra_MVC.Hubs;

namespace BrainStormEra_MVC.Services
{
    public class NotificationService : INotificationService
    {
        private readonly BrainStormEraContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(BrainStormEraContext context, IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<List<Notification>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.NotificationCreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(n => n.Course)
                .Include(n => n.CreatedByNavigation)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetAllNotificationsForUserAsync(string userId, int page = 1, int pageSize = 10)
        {
            // Get notifications that the user received OR created
            return await _context.Notifications
                .Where(n => n.UserId == userId || n.CreatedBy == userId)
                .OrderByDescending(n => n.NotificationCreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(n => n.Course)
                .Include(n => n.CreatedByNavigation)
                .Include(n => n.User) // Include the recipient user info
                .ToListAsync();
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && n.IsRead == false);
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

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task MarkAsReadAsync(string notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification != null && notification.IsRead == false)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead == false)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(string notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
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
            {                // Get all enrolled users in the course
                var enrolledUsers = await _context.Enrollments
                    .Where(e => e.CourseId == courseId && e.EnrollmentStatus == 1) // 1 for Active
                    .Select(e => e.UserId)
                    .ToListAsync();

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
            {                // Get all users with the specified role
                var users = await _context.Accounts
                    .Where(a => a.UserRole == role)
                    .Select(a => a.UserId)
                    .ToListAsync();

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
            {                // Get all active users (not banned)
                var users = await _context.Accounts
                    .Where(a => a.IsBanned != true)
                    .Select(a => a.UserId)
                    .ToListAsync();

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
            return await _context.Notifications
                .Include(n => n.Course)
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.CreatedBy == userId);
        }

        public async Task<bool> UpdateNotificationAsync(string notificationId, string userId, string title, string content, string? type)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.CreatedBy == userId);

                if (notification == null) return false;

                notification.NotificationTitle = title;
                notification.NotificationContent = content;
                notification.NotificationType = type ?? "General";

                await _context.SaveChangesAsync();

                // Send updated notification via SignalR to the recipient
                await _hubContext.Clients.Group($"User_{notification.UserId}").SendAsync("NotificationUpdated", new
                {
                    id = notification.NotificationId,
                    title = notification.NotificationTitle,
                    content = notification.NotificationContent,
                    type = notification.NotificationType
                });

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
