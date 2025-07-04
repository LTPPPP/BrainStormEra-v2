using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface INotificationRepo : IBaseRepo<Notification>
    {        // Notification query methods
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20);
        Task<List<Notification>> GetAllNotificationsForUserAsync(string userId, int page = 1, int pageSize = 10);
        Task<List<Notification>> GetUnreadNotificationsAsync(string userId);
        Task<int> GetUnreadNotificationCountAsync(string userId);
        Task<List<Notification>> GetNotificationsByTypeAsync(string userId, string notificationType);
        Task<List<Notification>> GetRecentNotificationsAsync(string userId, int count = 10);

        // Notification management methods
        Task<string> CreateNotificationAsync(Notification notification);
        Task<bool> CreateBulkNotificationsAsync(List<Notification> notifications);
        Task<bool> UpdateNotificationAsync(Notification notification);
        Task<bool> DeleteNotificationAsync(string notificationId, string userId);
        Task<bool> DeleteExpiredNotificationsAsync();

        // Notification status methods
        Task<bool> MarkAsReadAsync(string notificationId, string userId);
        Task<bool> MarkAsUnreadAsync(string notificationId, string userId);
        Task<bool> MarkAllAsReadAsync(string userId);
        Task<bool> ToggleNotificationStatusAsync(string notificationId, string userId);

        // Notification types and categories
        Task<List<string>> GetNotificationTypesAsync();
        Task<List<Notification>> GetSystemNotificationsAsync(int page = 1, int pageSize = 20);
        Task<List<Notification>> GetCourseNotificationsAsync(string courseId, string userId);
        Task<List<Notification>> GetAchievementNotificationsAsync(string userId);

        // Notification settings and preferences
        Task<bool> CreateUserNotificationAsync(string userId, string title, string message, string type = "info");
        Task<bool> CreateCourseNotificationAsync(string courseId, string title, string message, string type = "course");
        Task<bool> CreateSystemNotificationAsync(string title, string message, List<string>? userIds = null);
        Task<bool> CreateAchievementNotificationAsync(string userId, string achievementId, string achievementName);

        // Notification cleanup and maintenance
        Task<bool> DeleteOldNotificationsAsync(int daysOld = 30);
        Task<bool> DeleteReadNotificationsAsync(string userId, int daysOld = 7);
        Task<int> GetNotificationCountByTypeAsync(string userId, string notificationType);

        // Real-time notification support
        Task<List<Notification>> GetLatestNotificationsAsync(string userId, DateTime since);
        Task<bool> UpdateNotificationDeliveryStatusAsync(string notificationId, bool delivered);

        // Additional notification methods for service support
        Task<Notification?> GetNotificationForEditAsync(string notificationId, string userId);
        Task<bool> UpdateNotificationContentAsync(string notificationId, string userId, string title, string content, string? type);

        // Soft delete support methods
        Task<bool> RestoreNotificationAsync(string notificationId, string userId);
        Task<List<Notification>> GetDeletedNotificationsAsync(string userId, int page = 1, int pageSize = 20);

        // Admin global operations for notifications
        Task<Notification?> GetNotificationByIdAsync(string notificationId);
        Task<List<Notification>> GetRelatedNotificationsAsync(string title, string content, DateTime createdAt, string? courseId = null);
        Task<bool> DeleteNotificationsBatchAsync(List<string> notificationIds);
        Task<bool> UpdateNotificationsBatchAsync(List<string> notificationIds, string newTitle, string newContent, string? newType = null);
    }
}
