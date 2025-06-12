using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;

namespace BusinessLogicLayer.Services.Interfaces
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







