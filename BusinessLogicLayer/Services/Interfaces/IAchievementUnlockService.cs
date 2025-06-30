using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface for handling automatic achievement unlocking based on user activities
    /// </summary>
    public interface IAchievementUnlockService
    {
        /// <summary>
        /// Check and unlock achievements for course completion
        /// </summary>
        Task<List<Achievement>> CheckCourseCompletionAchievementsAsync(string userId, string courseId);

        /// <summary>
        /// Check and unlock achievements for quiz performance
        /// </summary>
        Task<List<Achievement>> CheckQuizAchievementsAsync(string userId, string quizId, decimal score, bool isPassed);

        /// <summary>
        /// Check and unlock achievements for learning streak
        /// </summary>
        Task<List<Achievement>> CheckStreakAchievementsAsync(string userId);

        /// <summary>
        /// Check and unlock achievements for instructor activities
        /// </summary>
        Task<List<Achievement>> CheckInstructorAchievementsAsync(string userId);

        /// <summary>
        /// Check and unlock achievements for student engagement
        /// </summary>
        Task<List<Achievement>> CheckEngagementAchievementsAsync(string userId);

        /// <summary>
        /// Comprehensive achievement check for all types
        /// </summary>
        Task<List<Achievement>> CheckAllAchievementsAsync(string userId);

        /// <summary>
        /// Get newly unlocked achievements for a user
        /// </summary>
        Task<List<Achievement>> GetNewlyUnlockedAchievementsAsync(string userId, TimeSpan timeWindow);

        /// <summary>
        /// Process achievement unlock with notification
        /// </summary>
        Task<bool> ProcessAchievementUnlockAsync(string userId, string achievementId, string? relatedCourseId = null, string? enrollmentId = null);
    }
}