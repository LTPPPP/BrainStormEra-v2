using DataAccessLayer.Models.ViewModels;
using System.Security.Claims;

namespace BusinessLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Interface for Admin Service operations
    /// This interface defines the contract for AdminService that handles 
    /// core data access operations for admin functionality
    /// </summary>
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetAdminDashboardAsync(string userId);
        Task<Dictionary<string, object>> GetAdminStatisticsAsync();
        Task<List<UserViewModel>> GetRecentUsersAsync(int count = 5);
        Task<List<CourseViewModel>> GetRecentCoursesAsync(int count = 5);
        Task<decimal> GetTotalRevenueAsync();
    }
}







