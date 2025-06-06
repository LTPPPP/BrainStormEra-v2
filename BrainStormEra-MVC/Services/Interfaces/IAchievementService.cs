using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface IAchievementService
    {
        Task<List<UserAchievement>> GetUserAchievementsAsync(string userId);
        Task<AchievementListViewModel> GetUserAchievementsAsync(string userId, string? search, int page, int pageSize);
        Task<object?> GetAchievementDetailAsync(string achievementId, string userId);
        Task AssignAchievementsAsync(string userId);
        Task BulkAssignAchievementsAsync();
    }
}
