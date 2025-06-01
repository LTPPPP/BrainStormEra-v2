using BrainStormEra_MVC.Models;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface INotificationService
    {
        Task<List<Notification>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 10);
        Task<int> GetUnreadNotificationCountAsync(string userId);
        Task<Notification> CreateNotificationAsync(string userId, string title, string content, string? type = null, string? courseId = null, string? createdBy = null);
        Task MarkAsReadAsync(string notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(string notificationId, string userId);
        Task<bool> SendToUserAsync(string userId, string title, string content, string? type = null, string? courseId = null);
        Task<bool> SendToCourseAsync(string courseId, string title, string content, string? type = null, string? excludeUserId = null);
        Task<bool> SendToRoleAsync(string role, string title, string content, string? type = null);
        Task<bool> SendToAllAsync(string title, string content, string? type = null);
    }
}
