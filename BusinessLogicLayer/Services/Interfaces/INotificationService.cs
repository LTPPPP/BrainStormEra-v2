using DataAccessLayer.Data;
using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface INotificationService
    {
        Task<List<Notification>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 10);
        Task<List<Notification>> GetAllNotificationsForUserAsync(string userId, int page = 1, int pageSize = 10);
        Task<int> GetUnreadNotificationCountAsync(string userId);
        Task<Notification> CreateNotificationAsync(string userId, string title, string content, string? type = null, string? courseId = null, string? createdBy = null);
        Task<Notification?> GetNotificationForEditAsync(string notificationId, string userId);
        Task<bool> UpdateNotificationAsync(string notificationId, string userId, string title, string content, string? type);
        Task MarkAsReadAsync(string notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(string notificationId, string userId);
        Task<bool> SendToUserAsync(string userId, string title, string content, string? type = null, string? courseId = null, string? createdBy = null);
        Task<bool> SendToCourseAsync(string courseId, string title, string content, string? type = null, string? excludeUserId = null, string? createdBy = null);
        Task<bool> SendToRoleAsync(string role, string title, string content, string? type = null, string? createdBy = null);
        Task<bool> SendToAllAsync(string title, string content, string? type = null, string? createdBy = null);
    }
}







