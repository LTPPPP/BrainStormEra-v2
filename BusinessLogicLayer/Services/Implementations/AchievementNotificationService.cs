using Microsoft.Extensions.Logging;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.DTOs.Common;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Implementation for handling achievement notifications
    /// </summary>
    public class AchievementNotificationService : IAchievementNotificationService
    {
        private readonly IAchievementRepo _achievementRepo;
        private readonly INotificationRepo _notificationRepo;
        private readonly IUserRepo _userRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AchievementNotificationService> _logger;

        public AchievementNotificationService(
            IAchievementRepo achievementRepo,
            INotificationRepo notificationRepo,
            IUserRepo userRepo,
            ICourseRepo courseRepo,
            INotificationService notificationService,
            ILogger<AchievementNotificationService> logger)
        {
            _achievementRepo = achievementRepo;
            _notificationRepo = notificationRepo;
            _userRepo = userRepo;
            _courseRepo = courseRepo;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<ServiceResult<bool>> SendAchievementUnlockedNotificationAsync(string userId, string achievementId)
        {
            try
            {
                var achievement = await _achievementRepo.GetByIdAsync(achievementId);

                if (achievement == null)
                {
                    _logger.LogWarning("Achievement {AchievementId} not found for notification", achievementId);
                    return ServiceResult<bool>.Failure("Achievement not found");
                }

                var user = await _userRepo.GetByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for achievement notification", userId);
                    return ServiceResult<bool>.Failure("User not found");
                }

                // Create notification
                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    NotificationType = "achievement_unlocked",
                    NotificationTitle = "Achievement Unlocked!",
                    NotificationContent = $"Congratulations! You've unlocked the '{achievement.AchievementName}' achievement!",
                    IsRead = false,
                    NotificationCreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                };

                await _notificationRepo.CreateNotificationAsync(notification);

                // Send real-time notification via SignalR
                var notificationResult = await _notificationService.CreateNotificationAsync(
                    userId,
                    notification.NotificationTitle,
                    notification.NotificationContent,
                    "achievement_unlocked",
                    achievementId,
                    "achievement");

                if (notificationResult == null)
                {
                    _logger.LogWarning("Failed to send real-time notification for achievement {AchievementId} to user {UserId}",
                        achievementId, userId);
                }

                _logger.LogInformation("Achievement notification sent successfully for user {UserId}, achievement {AchievementId}",
                    userId, achievementId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending achievement notification for user {UserId}, achievement {AchievementId}",
                    userId, achievementId);
                return ServiceResult<bool>.Failure("Failed to send achievement notification");
            }
        }

        public async Task<ServiceResult<bool>> SendAchievementProgressNotificationAsync(string userId, string achievementId, int progressPercentage)
        {
            try
            {
                var achievement = await _achievementRepo.GetByIdAsync(achievementId);

                if (achievement == null)
                {
                    _logger.LogWarning("Achievement {AchievementId} not found for progress notification", achievementId);
                    return ServiceResult<bool>.Failure("Achievement not found");
                }

                var user = await _userRepo.GetByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for achievement progress notification", userId);
                    return ServiceResult<bool>.Failure("User not found");
                }

                // Create progress notification
                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    NotificationType = "achievement_progress",
                    NotificationTitle = "Achievement Progress",
                    NotificationContent = $"You're {progressPercentage}% of the way to unlocking '{achievement.AchievementName}'!",
                    IsRead = false,
                    NotificationCreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                };

                await _notificationRepo.CreateNotificationAsync(notification);

                // Send real-time notification via SignalR
                var notificationResult = await _notificationService.CreateNotificationAsync(
                    userId,
                    notification.NotificationTitle,
                    notification.NotificationContent,
                    "achievement_progress",
                    achievementId,
                    "achievement");

                if (notificationResult == null)
                {
                    _logger.LogWarning("Failed to send real-time progress notification for achievement {AchievementId} to user {UserId}",
                        achievementId, userId);
                }

                _logger.LogInformation("Achievement progress notification sent successfully for user {UserId}, achievement {AchievementId}, progress {Progress}%",
                    userId, achievementId, progressPercentage);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending achievement progress notification for user {UserId}, achievement {AchievementId}",
                    userId, achievementId);
                return ServiceResult<bool>.Failure("Failed to send achievement progress notification");
            }
        }

        public async Task<ServiceResult<bool>> SendAchievementMilestoneNotificationAsync(string userId, string achievementId, string milestone)
        {
            try
            {
                var achievement = await _achievementRepo.GetByIdAsync(achievementId);

                if (achievement == null)
                {
                    _logger.LogWarning("Achievement {AchievementId} not found for milestone notification", achievementId);
                    return ServiceResult<bool>.Failure("Achievement not found");
                }

                var user = await _userRepo.GetByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for achievement milestone notification", userId);
                    return ServiceResult<bool>.Failure("User not found");
                }

                // Create milestone notification
                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    NotificationType = "achievement_milestone",
                    NotificationTitle = "Achievement Milestone Reached!",
                    NotificationContent = $"You've reached a milestone in '{achievement.AchievementName}': {milestone}",
                    IsRead = false,
                    NotificationCreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                };

                await _notificationRepo.CreateNotificationAsync(notification);

                // Send real-time notification via SignalR
                var notificationResult = await _notificationService.CreateNotificationAsync(
                    userId,
                    notification.NotificationTitle,
                    notification.NotificationContent,
                    "achievement_milestone",
                    achievementId,
                    "achievement");

                if (notificationResult == null)
                {
                    _logger.LogWarning("Failed to send real-time milestone notification for achievement {AchievementId} to user {UserId}",
                        achievementId, userId);
                }

                _logger.LogInformation("Achievement milestone notification sent successfully for user {UserId}, achievement {AchievementId}, milestone {Milestone}",
                    userId, achievementId, milestone);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending achievement milestone notification for user {UserId}, achievement {AchievementId}",
                    userId, achievementId);
                return ServiceResult<bool>.Failure("Failed to send achievement milestone notification");
            }
        }

        public async Task<ServiceResult<bool>> SendAchievementStreakNotificationAsync(string userId, int streakDays)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for streak notification", userId);
                    return ServiceResult<bool>.Failure("User not found");
                }

                // Create streak notification
                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    NotificationType = "achievement_streak",
                    NotificationTitle = "Learning Streak!",
                    NotificationContent = $"Amazing! You've maintained a {streakDays}-day learning streak! Keep it up!",
                    IsRead = false,
                    NotificationCreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                };

                await _notificationRepo.CreateNotificationAsync(notification);

                // Send real-time notification via SignalR
                var notificationResult = await _notificationService.CreateNotificationAsync(
                    userId,
                    notification.NotificationTitle,
                    notification.NotificationContent,
                    "achievement_streak",
                    null,
                    "achievement");

                if (notificationResult == null)
                {
                    _logger.LogWarning("Failed to send real-time streak notification to user {UserId}", userId);
                }

                _logger.LogInformation("Achievement streak notification sent successfully for user {UserId}, streak {StreakDays} days",
                    userId, streakDays);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending achievement streak notification for user {UserId}", userId);
                return ServiceResult<bool>.Failure("Failed to send achievement streak notification");
            }
        }

        public async Task<bool> SendAchievementNotificationAsync(string userId, Achievement achievement, string? relatedCourseId = null)
        {
            try
            {
                // Check if user has enabled achievement notifications
                if (!await IsAchievementNotificationsEnabledAsync(userId))
                {
                    return true; // Not an error, just user preference
                }

                // Get course name if related
                string? courseName = null;
                if (!string.IsNullOrEmpty(relatedCourseId))
                {
                    var course = await _courseRepo.GetCourseByIdAsync(relatedCourseId);
                    courseName = course?.CourseName;
                }

                // Create notification message
                var message = GetAchievementNotificationMessage(achievement, courseName);

                // Create notification record
                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    NotificationTitle = "🎉 New Achievement Unlocked!",
                    NotificationContent = message,
                    NotificationType = "achievement",
                    IsRead = false,
                    NotificationCreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                };

                await _notificationRepo.CreateNotificationAsync(notification);

                // Send real-time notification via SignalR
                var notificationResult = await _notificationService.CreateNotificationAsync(
                    userId,
                    notification.NotificationTitle,
                    notification.NotificationContent,
                    "achievement",
                    achievement.AchievementId,
                    "achievement");

                if (notificationResult == null)
                {
                    _logger.LogWarning("Failed to send real-time achievement notification for user {UserId}", userId);
                }

                _logger.LogInformation("Achievement notification sent for user {UserId}, achievement {AchievementName}",
                    userId, achievement.AchievementName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending achievement notification for user {UserId}, achievement {AchievementId}",
                    userId, achievement.AchievementId);
                return false;
            }
        }

        public async Task<bool> SendMultipleAchievementNotificationsAsync(string userId, List<Achievement> achievements)
        {
            try
            {
                if (!achievements.Any())
                    return true;

                // Check if user has enabled achievement notifications
                if (!await IsAchievementNotificationsEnabledAsync(userId))
                {
                    return true; // Not an error, just user preference
                }

                var notifications = new List<Notification>();

                foreach (var achievement in achievements)
                {
                    var message = GetAchievementNotificationMessage(achievement);

                    var notification = new Notification
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        UserId = userId,
                        NotificationTitle = "🏆 Multiple Achievements Unlocked!",
                        NotificationContent = message,
                        NotificationType = "achievement",
                        IsRead = false,
                        NotificationCreatedAt = DateTime.UtcNow,
                        CreatedBy = "system"
                    };

                    notifications.Add(notification);
                }

                await _notificationRepo.CreateBulkNotificationsAsync(notifications);

                // Send real-time notifications
                foreach (var notification in notifications)
                {
                    var notificationResult = await _notificationService.CreateNotificationAsync(
                        userId,
                        notification.NotificationTitle,
                        notification.NotificationContent,
                        "achievement",
                        null,
                        "achievement");

                    if (notificationResult == null)
                    {
                        _logger.LogWarning("Failed to send real-time multiple achievement notification for user {UserId}", userId);
                    }
                }

                _logger.LogInformation("Multiple achievement notifications sent for user {UserId}, count: {Count}",
                    userId, achievements.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending multiple achievement notifications for user {UserId}", userId);
                return false;
            }
        }

        public string GetAchievementNotificationMessage(Achievement achievement, string? courseName = null)
        {
            var baseMessage = $"Congratulations! You've unlocked the '{achievement.AchievementName}' achievement!";

            if (!string.IsNullOrEmpty(achievement.AchievementDescription))
            {
                baseMessage += $" {achievement.AchievementDescription}";
            }

            if (achievement.PointsReward.HasValue && achievement.PointsReward > 0)
            {
                baseMessage += $" You earned {achievement.PointsReward} points!";
            }

            if (!string.IsNullOrEmpty(courseName))
            {
                baseMessage += $" This achievement was earned from the course '{courseName}'.";
            }

            return baseMessage;
        }

        public Task<bool> IsAchievementNotificationsEnabledAsync(string userId)
        {
            try
            {
                // For now, assume all users have notifications enabled
                // In the future, this could check user preferences
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking achievement notification settings for user {UserId}", userId);
                return Task.FromResult(true); // Default to enabled if there's an error
            }
        }
    }
}