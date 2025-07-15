using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IAchievementService
    {
        // User achievement methods
        Task<List<UserAchievement>> GetUserAchievementsAsync(string userId);
        Task<AchievementListViewModel> GetUserAchievementsAsync(string userId, string? search, int page, int pageSize);
        Task<object?> GetAchievementDetailAsync(string achievementId, string userId);
        Task AssignAchievementsAsync(string userId);
        Task BulkAssignAchievementsAsync();

        // Admin achievement management methods
        Task<AdminAchievementsViewModel> GetAllAchievementsAsync(string? search = null, string? typeFilter = null, string? pointsFilter = null, int page = 1, int pageSize = 12, string? sortBy = "date_desc");
        Task<AdminAchievementViewModel?> GetAchievementByIdAsync(string achievementId);
        Task<(bool Success, string? AchievementId)> CreateAchievementAsync(CreateAchievementRequest request, string? adminId = null);
        Task<bool> UpdateAchievementAsync(UpdateAchievementRequest request, string? adminId = null);
        Task<bool> DeleteAchievementAsync(string achievementId, string? adminId = null);
        Task<(bool Success, string? IconPath, string? ErrorMessage)> UploadAchievementIconAsync(IFormFile file, string achievementId, string? adminId = null);
    }
}







