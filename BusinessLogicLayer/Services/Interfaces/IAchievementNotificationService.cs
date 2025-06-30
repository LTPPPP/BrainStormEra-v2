using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for handling achievement notifications
    /// </summary>
    public interface IAchievementNotificationService
    {
        /// <summary>
        /// Send notification when user unlocks an achievement
        /// </summary>
        Task<bool> SendAchievementNotificationAsync(string userId, Achievement achievement, string? relatedCourseId = null);

        /// <summary>
        /// Send notification for multiple achievements
        /// </summary>
        Task<bool> SendMultipleAchievementNotificationsAsync(string userId, List<Achievement> achievements);

        /// <summary>
        /// Get achievement notification message
        /// </summary>
        string GetAchievementNotificationMessage(Achievement achievement, string? courseName = null);

        /// <summary>
        /// Check if user has enabled achievement notifications
        /// </summary>
        Task<bool> IsAchievementNotificationsEnabledAsync(string userId);
    }
}