using Microsoft.Extensions.Logging;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogicLayer.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly IAchievementRepo _achievementRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly IAchievementUnlockService _achievementUnlockService;
        private readonly BrainStormEraContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(
            IAchievementRepo achievementRepo,
            ICourseRepo courseRepo,
            IAchievementUnlockService achievementUnlockService,
            BrainStormEraContext context,
            IMemoryCache cache,
            ILogger<AchievementService> logger)
        {
            _achievementRepo = achievementRepo;
            _courseRepo = courseRepo;
            _achievementUnlockService = achievementUnlockService;
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<UserAchievement>> GetUserAchievementsAsync(string userId)
        {
            try
            {
                // Check for new achievements before returning
                await _achievementUnlockService.CheckAllAchievementsAsync(userId);
                return await _achievementRepo.GetUserAchievementsAsync(userId);
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
                var userAchievement = await _achievementRepo.GetUserAchievementAsync(userId, achievementId);
                if (userAchievement == null)
                    return null;

                string? relatedCourseName = null;
                if (!string.IsNullOrEmpty(userAchievement.RelatedCourseId))
                {
                    relatedCourseName = await _achievementRepo.GetCourseNameAsync(userAchievement.RelatedCourseId);
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
                // Use the new achievement unlock service
                await _achievementUnlockService.CheckAllAchievementsAsync(userId);
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
                // Get all users who have enrollments
                var userIds = await _context.Enrollments
                    .Select(e => e.UserId)
                    .Distinct()
                    .ToListAsync();

                var tasks = userIds.Select(userId => _achievementUnlockService.CheckAllAchievementsAsync(userId));
                await Task.WhenAll(tasks);

                _logger.LogInformation("Bulk achievement assignment completed for {UserCount} users", userIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk assigning achievements");
            }
        }

        public async Task<AchievementListViewModel> GetUserAchievementsAsync(string userId, string? search, int page, int pageSize)
        {
            try
            {
                // Check for new achievements before getting the list
                await _achievementUnlockService.CheckAllAchievementsAsync(userId);

                var cacheKey = $"UserAchievementsList_{userId}_{search}_{page}_{pageSize}";
                if (_cache.TryGetValue(cacheKey, out AchievementListViewModel? cached))
                    return cached!;

                var userAchievements = await _achievementRepo.GetUserAchievementsAsync(userId, search, page, pageSize);
                var totalCount = await _achievementRepo.GetUserAchievementsCountAsync(userId, search);

                var achievements = userAchievements.Select(ua => new AchievementSummaryViewModel
                {
                    AchievementId = ua.AchievementId,
                    AchievementName = ua.Achievement.AchievementName,
                    AchievementDescription = ua.Achievement.AchievementDescription ?? "",
                    AchievementIcon = ua.Achievement.AchievementIcon ?? MediaConstants.Defaults.DefaultAchievementPath,
                    AchievementType = ua.Achievement.AchievementType ?? "",
                    PointsReward = ua.Achievement.PointsReward,
                    ReceivedDate = ua.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                    RelatedCourseName = ua.RelatedCourseId != null ? GetCourseNameFromCache(ua.RelatedCourseId) : null
                }).ToList();

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var result = new AchievementListViewModel
                {
                    Achievements = achievements,
                    SearchQuery = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalAchievements = totalCount,
                    TotalPages = totalPages
                };

                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated achievements for user {UserId}", userId);
                return new AchievementListViewModel
                {
                    Achievements = new List<AchievementSummaryViewModel>(),
                    SearchQuery = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalAchievements = 0,
                    TotalPages = 0
                };
            }
        }

        /// <summary>
        /// Check and unlock achievements for course completion
        /// </summary>
        public async Task<List<Achievement>> CheckCourseCompletionAchievementsAsync(string userId, string courseId)
        {
            return await _achievementUnlockService.CheckCourseCompletionAchievementsAsync(userId, courseId);
        }

        /// <summary>
        /// Check and unlock achievements for quiz performance
        /// </summary>
        public async Task<List<Achievement>> CheckQuizAchievementsAsync(string userId, string quizId, decimal score, bool isPassed)
        {
            return await _achievementUnlockService.CheckQuizAchievementsAsync(userId, quizId, score, isPassed);
        }

        /// <summary>
        /// Check and unlock achievements for learning streak
        /// </summary>
        public async Task<List<Achievement>> CheckStreakAchievementsAsync(string userId)
        {
            return await _achievementUnlockService.CheckStreakAchievementsAsync(userId);
        }

        /// <summary>
        /// Get newly unlocked achievements for a user
        /// </summary>
        public async Task<List<Achievement>> GetNewlyUnlockedAchievementsAsync(string userId, TimeSpan timeWindow)
        {
            return await _achievementUnlockService.GetNewlyUnlockedAchievementsAsync(userId, timeWindow);
        }

        private string? GetCourseNameFromCache(string courseId)
        {
            var cacheKey = $"CourseName_{courseId}";
            return _cache.TryGetValue(cacheKey, out string? courseName) ? courseName : null;
        }

        private static bool ShouldAssignAchievement(Achievement achievement, int completedCourses)
        {
            return int.TryParse(achievement.AchievementDescription, out int required) && completedCourses >= required;
        }
    }
}








