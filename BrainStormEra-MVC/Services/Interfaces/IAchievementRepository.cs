using BrainStormEra_MVC.Models;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface IAchievementRepository
    {
        Task<List<Achievement>> GetAllAchievementsAsync();
        Task<Achievement?> GetAchievementByIdAsync(string achievementId);
        Task<List<UserAchievement>> GetUserAchievementsAsync(string userId);
        Task<UserAchievement?> GetUserAchievementAsync(string userId, string achievementId);
        Task<bool> HasUserAchievementAsync(string userId, string achievementId);
        Task AddUserAchievementAsync(UserAchievement userAchievement);
        Task<Dictionary<string, int>> GetUserCompletedCoursesCountAsync();
        Task<string?> GetCourseNameAsync(string courseId);

        // Paginated methods
        Task<List<UserAchievement>> GetUserAchievementsAsync(string userId, string? search, int page, int pageSize);
        Task<int> GetUserAchievementsCountAsync(string userId, string? search);
    }
}
