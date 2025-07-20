using Microsoft.Extensions.Logging;
using DataAccessLayer.Models;
using BusinessLogicLayer.Services.Interfaces;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Mediator service implementation to break circular dependency between LessonService and AchievementUnlockService
    /// </summary>
    public class AchievementMediatorService : IAchievementMediatorService
    {
        private readonly IAchievementUnlockService _achievementUnlockService;
        private readonly ILogger<AchievementMediatorService> _logger;

        public AchievementMediatorService(
            IAchievementUnlockService achievementUnlockService,
            ILogger<AchievementMediatorService> logger)
        {
            _achievementUnlockService = achievementUnlockService;
            _logger = logger;
        }

        public async Task<List<Achievement>> CheckCourseCompletionAchievementsAsync(string userId, string courseId)
        {
            try
            {
                return await _achievementUnlockService.CheckCourseCompletionAchievementsAsync(userId, courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AchievementMediatorService.CheckCourseCompletionAchievementsAsync for user {UserId}, course {CourseId}", userId, courseId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> CheckQuizAchievementsAsync(string userId, string quizId, decimal score, bool isPassed)
        {
            try
            {
                return await _achievementUnlockService.CheckQuizAchievementsAsync(userId, quizId, score, isPassed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AchievementMediatorService.CheckQuizAchievementsAsync for user {UserId}, quiz {QuizId}", userId, quizId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> CheckStreakAchievementsAsync(string userId)
        {
            try
            {
                return await _achievementUnlockService.CheckStreakAchievementsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AchievementMediatorService.CheckStreakAchievementsAsync for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> CheckInstructorAchievementsAsync(string userId)
        {
            try
            {
                return await _achievementUnlockService.CheckInstructorAchievementsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AchievementMediatorService.CheckInstructorAchievementsAsync for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> CheckEngagementAchievementsAsync(string userId)
        {
            try
            {
                return await _achievementUnlockService.CheckEngagementAchievementsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AchievementMediatorService.CheckEngagementAchievementsAsync for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> CheckAllAchievementsAsync(string userId)
        {
            try
            {
                return await _achievementUnlockService.CheckAllAchievementsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AchievementMediatorService.CheckAllAchievementsAsync for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> GetNewlyUnlockedAchievementsAsync(string userId, TimeSpan timeWindow)
        {
            try
            {
                return await _achievementUnlockService.GetNewlyUnlockedAchievementsAsync(userId, timeWindow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AchievementMediatorService.GetNewlyUnlockedAchievementsAsync for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        public async Task<bool> ProcessAchievementUnlockAsync(string userId, string achievementId, string? relatedCourseId = null, string? enrollmentId = null)
        {
            try
            {
                return await _achievementUnlockService.ProcessAchievementUnlockAsync(userId, achievementId, relatedCourseId, enrollmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AchievementMediatorService.ProcessAchievementUnlockAsync for user {UserId}, achievement {AchievementId}", userId, achievementId);
                return false;
            }
        }
    }
}