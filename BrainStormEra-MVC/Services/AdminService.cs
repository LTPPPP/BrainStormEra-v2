using DataAccessLayer.Data;
using DataAccessLayer.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrainStormEra_MVC.Services
{    /// <summary>
     /// Service class that handles data access operations for Admin functionality.
     /// This class implements the core data access layer for admin operations.
     /// </summary>
    public class AdminService : IAdminService
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<AdminService> _logger;

        public AdminService(BrainStormEraContext context, ILogger<AdminService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get admin dashboard data
        /// </summary>
        public async Task<AdminDashboardViewModel> GetAdminDashboardAsync(string userId)
        {
            try
            {
                var admin = await _context.Accounts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .FirstOrDefaultAsync();

                if (admin == null)
                    throw new ArgumentException("Admin not found", nameof(userId));

                // Get statistics
                var statistics = await GetAdminStatisticsAsync();
                var recentUsers = await GetRecentUsersAsync(5);
                var recentCourses = await GetRecentCoursesAsync(5);
                var totalRevenue = await GetTotalRevenueAsync();

                return new AdminDashboardViewModel
                {
                    AdminName = admin.FullName ?? admin.Username,
                    AdminImage = admin.UserImage ?? "/img/defaults/default-avatar.svg",
                    TotalUsers = (int)statistics["TotalUsers"],
                    TotalCourses = (int)statistics["TotalCourses"],
                    TotalEnrollments = (int)statistics["TotalEnrollments"],
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
                var totalUsers = await _context.Accounts.CountAsync();
                var totalCourses = await _context.Courses.CountAsync();
                var totalEnrollments = await _context.Enrollments.CountAsync();

                return new Dictionary<string, object>
                {
                    { "TotalUsers", totalUsers },
                    { "TotalCourses", totalCourses },
                    { "TotalEnrollments", totalEnrollments }
                };
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
                return await _context.Accounts
                    .AsNoTracking()
                    .OrderByDescending(a => a.AccountCreatedAt)
                    .Take(count)
                    .Select(u => new UserViewModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName ?? u.Username,
                        UserEmail = u.UserEmail,
                        UserRole = u.UserRole,
                        AccountCreatedAt = u.AccountCreatedAt,
                        IsBanned = u.IsBanned ?? false
                    })
                    .ToListAsync();
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
                return await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.Author)
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Take(count)
                    .Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/img/defaults/default-course.svg",
                        Price = c.Price,
                        CreatedBy = c.Author.FullName ?? c.Author.Username,
                        Description = c.CourseDescription
                    })
                    .ToListAsync();
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
                return await _context.PaymentTransactions
                    .AsNoTracking()
                    .Where(pt => pt.TransactionStatus == "Success")
                    .SumAsync(pt => pt.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total revenue");
                throw;
            }
        }
    }
}
