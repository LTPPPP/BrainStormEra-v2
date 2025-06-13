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
        Task<AdminCoursesViewModel> GetAllCoursesAsync(string? search = null, string? categoryFilter = null, int page = 1, int pageSize = 10);
        Task<bool> UpdateUserStatusAsync(string userId, bool isBanned);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> UpdateCourseStatusAsync(string courseId, bool isApproved, string? adminId = null);
        Task<bool> DeleteCourseAsync(string courseId);
    }
}
