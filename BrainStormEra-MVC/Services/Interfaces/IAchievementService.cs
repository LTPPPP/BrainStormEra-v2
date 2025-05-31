using BrainStormEra_MVC.Models;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface IAchievementService
    {
        Task<List<UserAchievement>> GetUserAchievementsAsync(string userId);
        Task<object?> GetAchievementDetailAsync(string achievementId, string userId);
        Task AssignAchievementsAsync(string userId);
        Task BulkAssignAchievementsAsync();
    }
}
