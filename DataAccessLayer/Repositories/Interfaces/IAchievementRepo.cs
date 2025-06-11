using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IAchievementRepo : IBaseRepo<Achievement>
    {
        // Achievement query methods
        Task<List<Achievement>> GetAllAchievementsAsync();
        Task<List<Achievement>> GetAchievementsByTypeAsync(string achievementType);
        Task<Achievement?> GetAchievementWithUsersAsync(string achievementId);
        Task<List<Achievement>> GetAchievementsByPointsRangeAsync(int minPoints, int maxPoints);

        // User achievement methods
        Task<List<UserAchievement>> GetUserAchievementsAsync(string userId);
        Task<List<UserAchievement>> GetUserAchievementsAsync(string userId, string? search, int page, int pageSize);
        Task<int> GetUserAchievementsCountAsync(string userId, string? search);
        Task<List<Achievement>> GetUnlockedAchievementsAsync(string userId);
        Task<List<Achievement>> GetLockedAchievementsAsync(string userId);
        Task<UserAchievement?> GetUserAchievementAsync(string userId, string achievementId);
        Task<string?> GetCourseNameAsync(string courseId);
        Task<Dictionary<string, int>> GetUserCompletedCoursesCountAsync();
        Task<bool> HasUserAchievementAsync(string userId, string achievementId);
        Task<bool> AddUserAchievementAsync(UserAchievement userAchievement);

        // Achievement management methods
        Task<string> CreateAchievementAsync(Achievement achievement);
        Task<bool> UpdateAchievementAsync(Achievement achievement);
        Task<bool> DeleteAchievementAsync(string achievementId);
        Task<bool> UpdateAchievementIconAsync(string achievementId, string iconPath);

        // Achievement unlocking methods
        Task<bool> UnlockAchievementAsync(string userId, string achievementId);
        Task<bool> CheckAndUnlockAchievementsAsync(string userId);
        Task<List<Achievement>> GetNewlyUnlockedAchievementsAsync(string userId);
        Task<bool> IsAchievementUnlockedAsync(string userId, string achievementId);

        // Achievement statistics
        Task<int> GetUserAchievementCountAsync(string userId);
        Task<int> GetTotalPointsEarnedAsync(string userId);
        Task<decimal> GetAchievementCompletionPercentageAsync(string userId);
        Task<Dictionary<string, int>> GetAchievementTypeStatsAsync(string userId);

        // Achievement leaderboard
        Task<List<(string userId, string userName, int points, int achievementCount)>> GetAchievementLeaderboardAsync(int top = 10);
        Task<int> GetUserRankingAsync(string userId);
        Task<List<(string userId, string userName, int points)>> GetTopUsersByPointsAsync(int top = 10);

        // Achievement validation and requirements
        Task<bool> ValidateAchievementRequirementsAsync(string userId, string achievementId);
        Task<List<Achievement>> GetEligibleAchievementsAsync(string userId);
        Task<bool> HasPrerequisiteAchievementsAsync(string userId, string achievementId);

        // Achievement categories and types
        Task<List<string>> GetAchievementTypesAsync();
        Task<List<Achievement>> GetCourseCompletionAchievementsAsync();
        Task<List<Achievement>> GetQuizMasteryAchievementsAsync();
        Task<List<Achievement>> GetStreakAchievementsAsync();
        Task<List<Achievement>> GetSocialAchievementsAsync();

        // Achievement progress tracking
        Task<bool> UpdateAchievementProgressAsync(string userId, string achievementType, int progress);
        Task<Dictionary<string, int>> GetAchievementProgressAsync(string userId);
        Task<bool> IncrementAchievementProgressAsync(string userId, string achievementType, int increment = 1);
    }
}
