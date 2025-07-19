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
        Task<bool> ChangeUserRoleAsync(string userId, string newRole);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> UpdateCourseStatusAsync(string courseId, bool isApproved, string? adminId = null);
        Task<bool> RejectCourseAsync(string courseId, string reason, string? adminId = null);
        Task<bool> BanCourseAsync(string courseId, string reason, string? adminId = null);
        Task<bool> DeleteCourseAsync(string courseId);

        // New methods for User Ranking
        Task<UserRankingViewModel> GetUserRankingAsync(int page = 1, int pageSize = 20);

        // New methods for Chatbot History
        Task<ChatbotHistoryViewModel> GetChatbotHistoryAsync(string? search = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 20);
    }
}
