using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataAccessLayer.Repositories
{
    public class NotificationRepo : BaseRepo<Notification>, INotificationRepo
    {
        private readonly ILogger<NotificationRepo>? _logger;

        public NotificationRepo(BrainStormEraContext context, ILogger<NotificationRepo>? logger = null)
            : base(context)
        {
            _logger = logger;
        }

        // Notification query methods
        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _dbSet
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.NotificationCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user notifications: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Notification>> GetAllNotificationsForUserAsync(string userId, int page = 1, int pageSize = 10)
        {
            try
            {
                return await _dbSet
                    .Where(n => n.UserId == userId || n.CreatedBy == userId)
                    .OrderByDescending(n => n.NotificationCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(n => n.Course)
                    .Include(n => n.CreatedByNavigation)
                    .Include(n => n.User)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting all notifications for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        {
            try
            {
                return await _dbSet
                    .Where(n => n.UserId == userId && n.IsRead != true)
                    .OrderByDescending(n => n.NotificationCreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting unread notifications: {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            try
            {
                return await _dbSet
                    .CountAsync(n => n.UserId == userId && n.IsRead != true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting unread notification count: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Notification>> GetNotificationsByTypeAsync(string userId, string notificationType)
        {
            try
            {
                return await _dbSet
                    .Where(n => n.UserId == userId && n.NotificationType == notificationType)
                    .OrderByDescending(n => n.NotificationCreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting notifications by type: {UserId}, {Type}", userId, notificationType);
                throw;
            }
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(string userId, int count = 10)
        {
            try
            {
                return await _dbSet
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.NotificationCreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting recent notifications: {UserId}", userId);
                throw;
            }
        }

        // Notification management methods
        public async Task<string> CreateNotificationAsync(Notification notification)
        {
            try
            {
                await AddAsync(notification);
                await SaveChangesAsync();
                return notification.NotificationId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating notification");
                throw;
            }
        }

        public async Task<bool> CreateBulkNotificationsAsync(List<Notification> notifications)
        {
            try
            {
                await AddRangeAsync(notifications);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating bulk notifications");
                throw;
            }
        }

        public async Task<bool> UpdateNotificationAsync(Notification notification)
        {
            try
            {
                await UpdateAsync(notification);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating notification");
                throw;
            }
        }

        public async Task<bool> DeleteNotificationAsync(string notificationId, string userId)
        {
            try
            {
                var notification = await _dbSet
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

                if (notification == null)
                    return false;

                await DeleteAsync(notification);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting notification");
                throw;
            }
        }

        public async Task<bool> DeleteExpiredNotificationsAsync()
        {
            try
            {
                var expiredDate = DateTime.UtcNow.AddDays(-30);
                var expiredNotifications = await _dbSet
                    .Where(n => n.NotificationCreatedAt < expiredDate)
                    .ToListAsync();

                if (expiredNotifications.Any())
                {
                    await DeleteRangeAsync(expiredNotifications);
                    var result = await SaveChangesAsync();
                    return result > 0;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting expired notifications");
                throw;
            }
        }

        // Notification status methods
        public async Task<bool> MarkAsReadAsync(string notificationId, string userId)
        {
            try
            {
                var notification = await _dbSet
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

                if (notification == null)
                    return false;

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error marking notification as read");
                throw;
            }
        }

        public async Task<bool> MarkAsUnreadAsync(string notificationId, string userId)
        {
            try
            {
                var notification = await _dbSet
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

                if (notification == null)
                    return false;

                notification.IsRead = false;
                notification.ReadAt = null;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error marking notification as unread");
                throw;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            try
            {
                var unreadNotifications = await _dbSet
                    .Where(n => n.UserId == userId && n.IsRead != true)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error marking all notifications as read");
                throw;
            }
        }

        public async Task<bool> ToggleNotificationStatusAsync(string notificationId, string userId)
        {
            try
            {
                var notification = await _dbSet
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

                if (notification == null)
                    return false;

                notification.IsRead = !notification.IsRead;
                notification.ReadAt = notification.IsRead == true ? DateTime.UtcNow : null;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error toggling notification status");
                throw;
            }
        }

        // Basic implementations for remaining interface methods
        public async Task<List<string>> GetNotificationTypesAsync() =>
            await _dbSet.Select(n => n.NotificationType).Distinct().ToListAsync();

        public async Task<List<Notification>> GetSystemNotificationsAsync(int page = 1, int pageSize = 20) =>
            await _dbSet.Where(n => n.NotificationType == "system")
                       .OrderByDescending(n => n.NotificationCreatedAt)
                       .Skip((page - 1) * pageSize)
                       .Take(pageSize)
                       .ToListAsync();

        public async Task<List<Notification>> GetCourseNotificationsAsync(string courseId, string userId) =>
            await _dbSet.Where(n => n.UserId == userId && n.CourseId == courseId && n.NotificationType == "course")
                       .OrderByDescending(n => n.NotificationCreatedAt)
                       .ToListAsync();

        public async Task<List<Notification>> GetAchievementNotificationsAsync(string userId) =>
            await _dbSet.Where(n => n.UserId == userId && n.NotificationType == "achievement")
                       .OrderByDescending(n => n.NotificationCreatedAt)
                       .ToListAsync();

        // Helper methods for creating specific notification types
        public async Task<bool> CreateUserNotificationAsync(string userId, string title, string message, string type = "info")
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid().ToString(),
                UserId = userId,
                NotificationTitle = title,
                NotificationContent = message,
                NotificationType = type,
                IsRead = false,
                NotificationCreatedAt = DateTime.UtcNow
            };

            return await CreateNotificationAsync(notification) != null;
        }

        public async Task<bool> CreateCourseNotificationAsync(string courseId, string title, string message, string type = "course")
        {
            // This would need to get all enrolled users for the course
            // For now, returning a basic implementation
            return true;
        }

        public async Task<bool> CreateSystemNotificationAsync(string title, string message, List<string>? userIds = null)
        {
            // Basic implementation - would need to create notifications for all users or specified users
            return true;
        }

        public async Task<bool> CreateAchievementNotificationAsync(string userId, string achievementId, string achievementName)
        {
            return await CreateUserNotificationAsync(userId, "Achievement Unlocked!", $"You've unlocked the '{achievementName}' achievement!", "achievement");
        }

        // Basic implementations for cleanup and statistics methods
        public async Task<bool> DeleteOldNotificationsAsync(int daysOld = 30) => await DeleteExpiredNotificationsAsync();
        public async Task<bool> DeleteReadNotificationsAsync(string userId, int daysOld = 7) => true;
        public async Task<int> GetNotificationCountByTypeAsync(string userId, string notificationType) =>
            await _dbSet.CountAsync(n => n.UserId == userId && n.NotificationType == notificationType);

        public async Task<List<Notification>> GetLatestNotificationsAsync(string userId, DateTime since) =>
            await _dbSet.Where(n => n.UserId == userId && n.NotificationCreatedAt > since)
                       .OrderByDescending(n => n.NotificationCreatedAt)
                       .ToListAsync();

        public async Task<bool> UpdateNotificationDeliveryStatusAsync(string notificationId, bool delivered) => true;

        public async Task<Dictionary<string, int>> GetNotificationStatsByTypeAsync(string userId) =>
            await _dbSet.Where(n => n.UserId == userId)
                       .GroupBy(n => n.NotificationType)
                       .ToDictionaryAsync(g => g.Key, g => g.Count());

        public async Task<int> GetTotalNotificationCountAsync(string userId) =>
            await _dbSet.CountAsync(n => n.UserId == userId);

        public async Task<decimal> GetNotificationReadRateAsync(string userId)
        {
            var total = await GetTotalNotificationCountAsync(userId);
            if (total == 0) return 0;

            var read = await _dbSet.CountAsync(n => n.UserId == userId && n.IsRead == true);
            return Math.Round((decimal)read / total * 100, 2);
        }

        // Additional notification methods for service support
        public async Task<Notification?> GetNotificationForEditAsync(string notificationId, string userId)
        {
            try
            {
                return await _dbSet
                    .Include(n => n.Course)
                    .Include(n => n.User)
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.CreatedBy == userId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting notification for edit: {NotificationId}", notificationId);
                throw;
            }
        }

        public async Task<bool> UpdateNotificationContentAsync(string notificationId, string userId, string title, string content, string? type)
        {
            try
            {
                var notification = await _dbSet
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.CreatedBy == userId);

                if (notification == null)
                    return false;

                notification.NotificationTitle = title;
                notification.NotificationContent = content;
                notification.NotificationType = type ?? "General";

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating notification content: {NotificationId}", notificationId);
                throw;
            }
        }
    }
}
