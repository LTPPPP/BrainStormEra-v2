using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services
{    /// <summary>
     /// Service class that handles data access operations for Admin functionality.
     /// This class implements the core data access layer for admin operations.
     /// </summary>
    public class AdminService : IAdminService
    {
        private readonly IAdminRepo _adminRepo;
        private readonly IUserRepo _userRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            IAdminRepo adminRepo,
            IUserRepo userRepo,
            ICourseRepo courseRepo,
            ILogger<AdminService> logger)
        {
            _adminRepo = adminRepo;
            _userRepo = userRepo;
            _courseRepo = courseRepo;
            _logger = logger;
        }

        /// <summary>
        /// Get admin dashboard data
        /// </summary>
        public async Task<AdminDashboardViewModel> GetAdminDashboardAsync(string userId)
        {
            try
            {
                // Get admin user info using repository
                var adminUser = await _userRepo.GetUserBasicInfoAsync(userId);

                if (adminUser == null)
                    throw new InvalidOperationException("Admin user not found");

                // Get statistics
                var statistics = await _adminRepo.GetSystemStatisticsAsync();
                var totalRevenue = await GetTotalRevenueAsync();
                var recentUsers = await GetRecentUsersAsync(5);
                var recentCourses = await GetRecentCoursesAsync(5);

                return new AdminDashboardViewModel
                {
                    AdminName = adminUser.FullName ?? adminUser.Username,
                    AdminImage = adminUser.UserImage ?? "/img/defaults/default-avatar.svg",
                    TotalUsers = (int)statistics.GetValueOrDefault("TotalUsers", 0),
                    TotalCourses = (int)statistics.GetValueOrDefault("TotalCourses", 0),
                    TotalEnrollments = (int)statistics.GetValueOrDefault("TotalEnrollments", 0),
                    TotalRevenue = totalRevenue,
                    RecentUsers = recentUsers,
                    RecentCourses = recentCourses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard data for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get admin statistics
        /// </summary>
        public async Task<Dictionary<string, object>> GetAdminStatisticsAsync()
        {
            try
            {
                return await _adminRepo.GetSystemStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin statistics");
                throw;
            }
        }

        /// <summary>
        /// Get recent users for admin dashboard
        /// </summary>
        public async Task<List<UserViewModel>> GetRecentUsersAsync(int count = 5)
        {
            try
            {
                var users = await _userRepo.GetRecentUsersAsync(count);
                return users.Select(u => new UserViewModel
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName ?? u.Username,
                    UserEmail = u.UserEmail,
                    UserRole = u.UserRole,
                    AccountCreatedAt = u.AccountCreatedAt,
                    IsBanned = u.IsBanned ?? false
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent users");
                throw;
            }
        }

        /// <summary>
        /// Get recent courses for admin dashboard
        /// </summary>
        public async Task<List<CourseViewModel>> GetRecentCoursesAsync(int count = 5)
        {
            try
            {
                var courses = await _courseRepo.GetRecentCoursesAsync(count);
                return courses.Select(c => new CourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CoursePicture = c.CourseImage ?? "/img/defaults/default-course.svg",
                    Price = c.Price,
                    CreatedBy = c.Author?.FullName ?? c.Author?.Username ?? "Unknown",
                    Description = c.CourseDescription
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent courses");
                throw;
            }
        }

        /// <summary>
        /// Get total revenue for admin dashboard
        /// </summary>
        public async Task<decimal> GetTotalRevenueAsync()
        {
            try
            {
                return await _adminRepo.GetTotalRevenueAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total revenue");
                throw;
            }
        }
    }
}







