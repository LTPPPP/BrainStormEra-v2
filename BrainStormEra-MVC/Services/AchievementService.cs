using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BrainStormEra_MVC.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly IAchievementRepository _achievementRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(
            IAchievementRepository achievementRepository,
            IMemoryCache cache,
            ILogger<AchievementService> logger)
        {
            _achievementRepository = achievementRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<UserAchievement>> GetUserAchievementsAsync(string userId)
        {
            try
            {
                await AssignAchievementsAsync(userId);
                return await _achievementRepository.GetUserAchievementsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievements for user {UserId}", userId);
                return new List<UserAchievement>();
            }
        }

        public async Task<object?> GetAchievementDetailAsync(string achievementId, string userId)
        {
            try
            {
                var userAchievement = await _achievementRepository.GetUserAchievementAsync(userId, achievementId);
                if (userAchievement == null)
                    return null;

                string? relatedCourseName = null;
                if (!string.IsNullOrEmpty(userAchievement.RelatedCourseId))
                {
                    relatedCourseName = await _achievementRepository.GetCourseNameAsync(userAchievement.RelatedCourseId);
                }

                return new
                {
                    userAchievement.Achievement.AchievementName,
                    userAchievement.Achievement.AchievementDescription,
                    userAchievement.Achievement.AchievementIcon,
                    userAchievement.ReceivedDate,
                    userAchievement.PointsEarned,
                    RelatedCourseName = relatedCourseName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievement detail {AchievementId} for user {UserId}", achievementId, userId);
                return null;
            }
        }

        public async Task AssignAchievementsAsync(string userId)
        {
            try
            {
                var achievements = await _achievementRepository.GetAllAchievementsAsync();
                var userCompletedCourses = await _achievementRepository.GetUserCompletedCoursesCountAsync();

                if (!userCompletedCourses.TryGetValue(userId, out int completedCount))
                    return;

                var tasks = achievements
                    .Where(a => ShouldAssignAchievement(a, completedCount))
                    .Select(async a =>
                    {
                        if (!await _achievementRepository.HasUserAchievementAsync(userId, a.AchievementId))
                        {
                            var userAchievement = new UserAchievement
                            {
                                UserId = userId,
                                AchievementId = a.AchievementId,
                                ReceivedDate = DateOnly.FromDateTime(DateTime.Today),
                                PointsEarned = a.PointsReward
                            };

                            await _achievementRepository.AddUserAchievementAsync(userAchievement);
                        }
                    });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning achievements for user {UserId}", userId);
            }
        }

        public async Task BulkAssignAchievementsAsync()
        {
            try
            {
                var achievements = await _achievementRepository.GetAllAchievementsAsync();
                var userCompletedCourses = await _achievementRepository.GetUserCompletedCoursesCountAsync();

                var tasks = userCompletedCourses.Select(async user =>
                {
                    foreach (var achievement in achievements.Where(a => ShouldAssignAchievement(a, user.Value)))
                    {
                        if (!await _achievementRepository.HasUserAchievementAsync(user.Key, achievement.AchievementId))
                        {
                            var userAchievement = new UserAchievement
                            {
                                UserId = user.Key,
                                AchievementId = achievement.AchievementId,
                                ReceivedDate = DateOnly.FromDateTime(DateTime.Today),
                                PointsEarned = achievement.PointsReward
                            };

                            await _achievementRepository.AddUserAchievementAsync(userAchievement);
                        }
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk assigning achievements");
            }
        }

        private static bool ShouldAssignAchievement(Achievement achievement, int completedCourses)
        {
            return int.TryParse(achievement.AchievementDescription, out int required) && completedCourses >= required;
        }
    }
}
