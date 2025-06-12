using DataAccessLayer.Models.ViewModels;
using System.Security.Claims;

namespace BrainStormEra_MVC.Services.Interfaces
{
    /// <summary>
    /// Interface for Home Service operations
    /// This interface defines the contract for HomeService that handles 
    /// core data access operations for home page functionality
    /// </summary>
    public interface IHomeService
    {
        Task<HomePageGuestViewModel> GetGuestHomePageAsync();
        Task<LearnerDashboardViewModel> GetLearnerDashboardAsync(string userId);
        Task<InstructorDashboardViewModel> GetInstructorDashboardAsync(string userId);
        Task<List<dynamic>> GetRecommendedCoursesAsync();
        Task<List<dynamic>> GetIncomeDataAsync(string userId, int days = 30);
        Task<bool> IsDatabaseConnectedAsync();
    }
}
