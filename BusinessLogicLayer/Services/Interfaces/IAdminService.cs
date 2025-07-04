using DataAccessLayer.Models.ViewModels;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetAdminDashboardAsync(string userId);
        Task<Dictionary<string, object>> GetAdminStatisticsAsync();
        Task<List<UserViewModel>> GetRecentUsersAsync(int count);
        Task<List<CourseViewModel>> GetRecentCoursesAsync(int count);
        Task<decimal> GetTotalRevenueAsync();
        Task<AdminUsersViewModel> GetAllUsersAsync(string? search = null, string? roleFilter = null, int page = 1, int pageSize = 10);
        Task<AdminUserViewModel?> GetUserDetailAsync(string userId);
        Task<AdminCoursesViewModel> GetAllCoursesAsync(string? search = null, string? categoryFilter = null, string? statusFilter = null, string? priceFilter = null, string? difficultyFilter = null, string? instructorFilter = null, string? sortBy = null, int page = 1, int pageSize = 12);
        Task<AdminCourseDetailsViewModel?> GetCourseDetailsAsync(string courseId);
        Task<bool> UpdateUserStatusAsync(string userId, bool isBanned);
        Task<bool> UpdateUserPointsAsync(string userId, decimal pointsChange);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> UpdateCourseStatusAsync(string courseId, bool isApproved, string? adminId = null);
        Task<bool> RejectCourseAsync(string courseId, string reason, string? adminId = null);
        Task<bool> BanCourseAsync(string courseId, string reason, string? adminId = null);
        Task<bool> DeleteCourseAsync(string courseId);

        // Achievement Management
        Task<AdminAchievementsViewModel> GetAllAchievementsAsync(string? search = null, string? typeFilter = null, string? pointsFilter = null, int page = 1, int pageSize = 12, string? sortBy = "date_desc");
        Task<AdminAchievementViewModel?> GetAchievementByIdAsync(string achievementId);
        Task<(bool Success, string? AchievementId)> CreateAchievementAsync(CreateAchievementRequest request, string? adminId = null);
        Task<bool> UpdateAchievementAsync(UpdateAchievementRequest request, string? adminId = null);
        Task<bool> DeleteAchievementAsync(string achievementId, string? adminId = null);
        Task<(bool Success, string? IconPath, string? ErrorMessage)> UploadAchievementIconAsync(Microsoft.AspNetCore.Http.IFormFile file, string achievementId, string? adminId = null);
    }
}
