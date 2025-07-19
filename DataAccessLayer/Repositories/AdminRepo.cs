using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataAccessLayer.Repositories
{
    public class AdminRepo : BaseRepo<Account>, IAdminRepo
    {
        private readonly ILogger<AdminRepo>? _logger;

        public AdminRepo(BrainStormEraContext context, ILogger<AdminRepo>? logger = null)
            : base(context)
        {
            _logger = logger;
        }

        // Dashboard Statistics
        public async Task<Dictionary<string, object>> GetAdminStatisticsAsync()
        {
            try
            {
                var totalUsers = await _context.Accounts.CountAsync();
                var totalCourses = await _context.Courses
                    .Where(c => c.CourseStatus != 4 && // Exclude archived/soft deleted courses
                               !string.IsNullOrEmpty(c.ApprovalStatus) && // Must have approval status
                               c.ApprovalStatus.ToLower() != "draft") // Exclude draft courses
                    .CountAsync();
                var totalEnrollments = await _context.Enrollments.CountAsync();
                var totalRevenue = await _context.PaymentTransactions
                    .Where(pt => pt.TransactionStatus == "completed")
                    .SumAsync(pt => pt.Amount);

                // User role distribution
                var totalLearners = await _context.Accounts.CountAsync(a => a.UserRole == "learner");
                var totalInstructors = await _context.Accounts.CountAsync(a => a.UserRole == "instructor");
                var totalAdmins = await _context.Accounts.CountAsync(a => a.UserRole == "admin");

                // Course status distribution (exclude drafts)
                var approvedCourses = await _context.Courses
                    .CountAsync(c => c.ApprovalStatus == "approved" && c.CourseStatus != 4);
                var pendingCourses = await _context.Courses
                    .CountAsync(c => c.ApprovalStatus == "pending" && c.CourseStatus != 4);
                var rejectedCourses = await _context.Courses
                    .CountAsync(c => c.ApprovalStatus == "rejected" && c.CourseStatus != 4);

                // Certificate statistics
                var totalCertificates = await _context.Certificates.CountAsync();
                var validCertificates = await _context.Certificates.CountAsync(c => c.IsValid == true);
                var expiredCertificates = totalCertificates - validCertificates;

                // Point statistics
                var totalPointsInSystem = await _context.Accounts
                    .Where(a => a.PaymentPoint.HasValue)
                    .SumAsync(a => a.PaymentPoint ?? 0);
                var averageUserPoints = totalUsers > 0 ? totalPointsInSystem / totalUsers : 0;

                return new Dictionary<string, object>
                {
                    { "TotalUsers", totalUsers },
                    { "TotalCourses", totalCourses },
                    { "TotalEnrollments", totalEnrollments },
                    { "TotalRevenue", totalRevenue },
                    { "TotalLearners", totalLearners },
                    { "TotalInstructors", totalInstructors },
                    { "TotalAdmins", totalAdmins },
                    { "ApprovedCourses", approvedCourses },
                    { "PendingCourses", pendingCourses },
                    { "RejectedCourses", rejectedCourses },
                    { "TotalCertificates", totalCertificates },
                    { "ValidCertificates", validCertificates },
                    { "ExpiredCertificates", expiredCertificates },
                    { "TotalPointsInSystem", totalPointsInSystem },
                    { "AverageUserPoints", averageUserPoints }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving admin statistics");
                throw;
            }
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            try
            {
                return await _context.PaymentTransactions
                    .Where(pt => pt.TransactionStatus == "completed")
                    .SumAsync(pt => pt.Amount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving total revenue");
                throw;
            }
        }

        // Real time-series data methods for charts
        public async Task<List<MonthlyUserGrowth>> GetUserGrowthDataAsync()
        {
            try
            {
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);

                // First, get the grouped data from database
                var userGrowthData = await _context.Accounts
                    .Where(a => a.AccountCreatedAt >= sixMonthsAgo)
                    .GroupBy(a => new
                    {
                        Year = a.AccountCreatedAt.Year,
                        Month = a.AccountCreatedAt.Month
                    })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        NewUsers = g.Count()
                    })
                    .ToListAsync();

                // Then convert to MonthlyUserGrowth objects in memory
                var convertedData = userGrowthData.Select(x => new DataAccessLayer.Models.ViewModels.MonthlyUserGrowth
                {
                    Month = new DateTime(x.Year, x.Month, 1).ToString("MMMM"),
                    NewUsers = x.NewUsers,
                    Date = new DateTime(x.Year, x.Month, 1)
                }).OrderBy(x => x.Date).ToList();

                // Fill in missing months with zero values
                var result = new List<MonthlyUserGrowth>();
                for (int i = 5; i >= 0; i--)
                {
                    var monthDate = DateTime.Now.AddMonths(-i);
                    var existingData = convertedData.FirstOrDefault(x =>
                        x.Date.Year == monthDate.Year && x.Date.Month == monthDate.Month);

                    result.Add(existingData ?? new DataAccessLayer.Models.ViewModels.MonthlyUserGrowth
                    {
                        Month = monthDate.ToString("MMMM"),
                        NewUsers = 0,
                        Date = new DateTime(monthDate.Year, monthDate.Month, 1)
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user growth data");
                throw;
            }
        }

        public async Task<List<MonthlyRevenue>> GetRevenueDataAsync()
        {
            try
            {
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);

                // First, get the grouped data from database
                var revenueData = await _context.PaymentTransactions
                    .Where(pt => pt.TransactionStatus == "completed" && pt.PaymentDate != null && pt.PaymentDate >= sixMonthsAgo)
                    .GroupBy(pt => new
                    {
                        Year = pt.PaymentDate!.Value.Year,
                        Month = pt.PaymentDate!.Value.Month
                    })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(pt => pt.Amount)
                    })
                    .ToListAsync();

                // Then convert to MonthlyRevenue objects in memory
                var convertedData = revenueData.Select(x => new DataAccessLayer.Models.ViewModels.MonthlyRevenue
                {
                    Month = new DateTime(x.Year, x.Month, 1).ToString("MMMM"),
                    Revenue = x.Revenue,
                    Date = new DateTime(x.Year, x.Month, 1)
                }).OrderBy(x => x.Date).ToList();

                // Fill in missing months with zero values
                var result = new List<MonthlyRevenue>();
                for (int i = 5; i >= 0; i--)
                {
                    var monthDate = DateTime.Now.AddMonths(-i);
                    var existingData = convertedData.FirstOrDefault(x =>
                        x.Date.Year == monthDate.Year && x.Date.Month == monthDate.Month);

                    result.Add(existingData ?? new DataAccessLayer.Models.ViewModels.MonthlyRevenue
                    {
                        Month = monthDate.ToString("MMMM"),
                        Revenue = 0,
                        Date = new DateTime(monthDate.Year, monthDate.Month, 1)
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving revenue data");
                throw;
            }
        }

        public async Task<List<WeeklyEnrollment>> GetWeeklyEnrollmentDataAsync()
        {
            try
            {
                var sevenWeeksAgo = DateTime.Now.AddDays(-49); // 7 weeks

                var enrollmentData = await _context.Enrollments
                    .Where(e => e.EnrollmentCreatedAt >= sevenWeeksAgo)
                    .ToListAsync();

                var completionData = await _context.UserProgresses
                    .Where(up => up.IsCompleted == true && up.CompletedAt != null && up.CompletedAt >= sevenWeeksAgo)
                    .ToListAsync();

                var result = new List<WeeklyEnrollment>();

                for (int i = 6; i >= 0; i--)
                {
                    var weekStart = DateTime.Now.AddDays(-7 * i).Date;
                    var weekEnd = weekStart.AddDays(7);

                    var weeklyEnrollments = enrollmentData
                        .Where(e => e.EnrollmentCreatedAt >= weekStart && e.EnrollmentCreatedAt < weekEnd)
                        .Count();

                    var weeklyCompletions = completionData
                        .Where(up => up.CompletedAt.HasValue && up.CompletedAt >= weekStart && up.CompletedAt < weekEnd)
                        .Count();

                    result.Add(new DataAccessLayer.Models.ViewModels.WeeklyEnrollment
                    {
                        Week = $"Week {7 - i}",
                        NewEnrollments = weeklyEnrollments,
                        CompletedCourses = weeklyCompletions,
                        Date = weekStart
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving weekly enrollment data");
                throw;
            }
        }



        public async Task<int> GetTotalUsersCountAsync()
        {
            try
            {
                return await _context.Accounts.CountAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving total users count");
                throw;
            }
        }

        public async Task<int> GetTotalCoursesCountAsync()
        {
            try
            {
                return await _context.Courses
                    .Where(c => c.CourseStatus != 4 && // Exclude archived/soft deleted courses
                               !string.IsNullOrEmpty(c.ApprovalStatus) && // Must have approval status
                               c.ApprovalStatus.ToLower() != "draft") // Exclude draft courses
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving total courses count");
                throw;
            }
        }

        public async Task<int> GetTotalEnrollmentsCountAsync()
        {
            try
            {
                return await _context.Enrollments.CountAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving total enrollments count");
                throw;
            }
        }

        public async Task<int> GetActiveUsersCountAsync()
        {
            try
            {
                return await _context.Accounts.Where(a => a.IsBanned != true).CountAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving active users count");
                throw;
            }
        }

        public async Task<int> GetActiveCoursesCountAsync()
        {
            try
            {
                return await _context.Courses.Where(c => c.CourseStatus == 1).CountAsync(); // Active status only
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving active courses count");
                throw;
            }
        }

        public async Task<int> GetPendingCoursesCountAsync()
        {
            try
            {
                return await _context.Courses.Where(c => c.ApprovalStatus == "Pending" && c.CourseStatus != 4).CountAsync(); // Exclude archived/soft deleted courses
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving pending courses count");
                throw;
            }
        }

        // User Management
        public async Task<List<Account>> GetRecentUsersAsync(int count = 5)
        {
            try
            {
                return await _context.Accounts
                    .OrderByDescending(a => a.AccountCreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving recent users");
                throw;
            }
        }

        public async Task<List<Account>> GetAllUsersAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Accounts
                    .OrderByDescending(a => a.AccountCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving all users");
                throw;
            }
        }

        public async Task<List<Account>> GetUsersByRoleAsync(string role, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Accounts
                    .Where(a => a.UserRole == role)
                    .OrderByDescending(a => a.AccountCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving users by role: {Role}", role);
                throw;
            }
        }

        public async Task<List<Account>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Accounts
                    .Where(a => a.Username.Contains(searchTerm) ||
                               a.UserEmail.Contains(searchTerm) ||
                               (a.FullName != null && a.FullName.Contains(searchTerm)))
                    .OrderByDescending(a => a.AccountCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching users with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<Account?> GetUserWithDetailsAsync(string userId)
        {
            try
            {
                return await _context.Accounts
                    .Include(a => a.Enrollments)
                        .ThenInclude(e => e.Course)
                    .Include(a => a.CourseAuthors)
                    .Include(a => a.PaymentTransactionUsers)
                    .FirstOrDefaultAsync(a => a.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user details: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> BanUserAsync(string userId, bool isBanned, string reason = "")
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.IsBanned = isBanned;
                user.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error banning/unbanning user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserPointsAsync(string userId, decimal pointsChange)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                // Add or subtract points (pointsChange can be positive or negative)
                user.PaymentPoint = (user.PaymentPoint ?? 0) + pointsChange;

                // Ensure points don't go below 0
                if (user.PaymentPoint < 0)
                    user.PaymentPoint = 0;

                user.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user points: {UserId}, pointsChange: {PointsChange}", userId, pointsChange);
                throw;
            }
        }

        public async Task<bool> ChangeUserRoleAsync(string userId, string newRole)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.UserRole = newRole;
                user.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error changing user role: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Account>> GetBannedUsersAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Accounts
                    .Where(a => a.IsBanned == true)
                    .OrderByDescending(a => a.AccountUpdatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving banned users");
                throw;
            }
        }

        // Course Management
        public async Task<List<Course>> GetRecentCoursesAsync(int count = 5)
        {
            try
            {
                return await _context.Courses
                    .Include(c => c.Author)
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving recent courses");
                throw;
            }
        }

        public async Task<List<Course>> GetAllCoursesAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Courses
                    .Where(c => c.CourseStatus != 4 && // Exclude archived/soft deleted courses
                               !string.IsNullOrEmpty(c.ApprovalStatus) && // Must have approval status
                               c.ApprovalStatus.ToLower() != "draft") // Exclude draft courses
                    .Include(c => c.Author)
                    .Include(c => c.Enrollments)
                    .Include(c => c.Feedbacks)
                    .Include(c => c.CourseCategories)
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving all courses");
                throw;
            }
        }

        public async Task<List<Course>> GetCoursesByStatusAsync(string status, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Courses
                    .Where(c => c.CourseStatus != 4) // Exclude archived/soft deleted courses
                    .Include(c => c.Author)
                    .Where(c => c.ApprovalStatus == status)
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving courses by status: {Status}", status);
                throw;
            }
        }

        public async Task<List<Course>> GetPendingCoursesAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await GetCoursesByStatusAsync("Pending", page, pageSize);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving pending courses");
                throw;
            }
        }

        public async Task<Course?> GetCourseWithDetailsAsync(string courseId)
        {
            try
            {
                return await _context.Courses
                    .Include(c => c.Author)
                    .Include(c => c.Chapters)
                        .ThenInclude(ch => ch.Lessons)
                    .Include(c => c.Enrollments)
                    .Include(c => c.Feedbacks)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving course details: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<bool> ApproveCourseAsync(string courseId, string adminId)
        {
            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
                if (course == null)
                    return false;

                course.ApprovalStatus = "Approved";

                // Only set ApprovedBy if adminId exists in account table
                if (!string.IsNullOrEmpty(adminId) && adminId != "system")
                {
                    var adminExists = await _context.Accounts.AnyAsync(a => a.UserId == adminId);
                    if (adminExists)
                    {
                        course.ApprovedBy = adminId;
                    }
                    // If admin doesn't exist, leave ApprovedBy as null
                }

                course.ApprovedAt = DateTime.UtcNow;
                course.CourseUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error approving course: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<bool> RejectCourseAsync(string courseId, string adminId, string reason)
        {
            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
                if (course == null)
                    return false;

                course.ApprovalStatus = "Rejected";

                // Only set ApprovedBy if adminId exists in account table
                if (!string.IsNullOrEmpty(adminId) && adminId != "system")
                {
                    var adminExists = await _context.Accounts.AnyAsync(a => a.UserId == adminId);
                    if (adminExists)
                    {
                        course.ApprovedBy = adminId;
                    }
                    // If admin doesn't exist, leave ApprovedBy as null
                }

                course.ApprovedAt = DateTime.UtcNow;
                course.CourseUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error rejecting course: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<bool> FeatureCourseAsync(string courseId, bool isFeatured)
        {
            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
                if (course == null)
                    return false;

                course.IsFeatured = isFeatured;
                course.CourseUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error featuring/unfeaturing course: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<bool> BanCourseAsync(string courseId, string adminId, string reason = "")
        {
            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
                if (course == null)
                    return false;

                course.ApprovalStatus = "Banned";
                course.CourseStatus = 0; // Set to inactive status

                // Only set ApprovedBy if adminId exists in account table
                if (!string.IsNullOrEmpty(adminId) && adminId != "system")
                {
                    var adminExists = await _context.Accounts.AnyAsync(a => a.UserId == adminId);
                    if (adminExists)
                    {
                        course.ApprovedBy = adminId;
                    }
                }

                course.ApprovedAt = DateTime.UtcNow;
                course.CourseUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error banning course: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<List<Course>> SearchCoursesAsync(string searchTerm, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Courses
                    .Where(c => c.CourseStatus != 4 && // Exclude archived/soft deleted courses
                               !string.IsNullOrEmpty(c.ApprovalStatus) && // Must have approval status
                               c.ApprovalStatus.ToLower() != "draft") // Exclude draft courses
                    .Include(c => c.Author)
                    .Include(c => c.Enrollments)
                    .Include(c => c.Feedbacks)
                    .Include(c => c.CourseCategories) // Include categories for search
                    .Where(c => c.CourseName.Contains(searchTerm) ||
                               (c.CourseDescription != null && c.CourseDescription.Contains(searchTerm)) ||
                               c.CourseCategories.Any(cc => cc.CourseCategoryName!.Contains(searchTerm)))
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching courses with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        // Enrollment Management
        public async Task<List<Enrollment>> GetRecentEnrollmentsAsync(int count = 10)
        {
            try
            {
                return await _context.Enrollments
                    .Include(e => e.User)
                    .Include(e => e.Course)
                    .OrderByDescending(e => e.EnrollmentCreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving recent enrollments");
                throw;
            }
        }

        public async Task<List<Enrollment>> GetAllEnrollmentsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Enrollments
                    .Include(e => e.User)
                    .Include(e => e.Course)
                    .OrderByDescending(e => e.EnrollmentCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving all enrollments");
                throw;
            }
        }

        public async Task<List<Enrollment>> GetEnrollmentsByStatusAsync(int status, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Enrollments
                    .Include(e => e.User)
                    .Include(e => e.Course)
                    .Where(e => e.EnrollmentStatus == status)
                    .OrderByDescending(e => e.EnrollmentCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving enrollments by status: {Status}", status);
                throw;
            }
        }

        public async Task<Enrollment?> GetEnrollmentWithDetailsAsync(string enrollmentId)
        {
            try
            {
                return await _context.Enrollments
                    .Include(e => e.User)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Author)
                    .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving enrollment details: {EnrollmentId}", enrollmentId);
                throw;
            }
        }

        public async Task<bool> UpdateEnrollmentStatusAsync(string enrollmentId, int newStatus)
        {
            try
            {
                var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);
                if (enrollment == null)
                    return false;

                enrollment.EnrollmentStatus = newStatus;
                enrollment.EnrollmentUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating enrollment status: {EnrollmentId}", enrollmentId);
                throw;
            }
        }

        public async Task<List<Enrollment>> GetRefundRequestsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Enrollments
                    .Include(e => e.User)
                    .Include(e => e.Course)
                    .Where(e => e.EnrollmentStatus == 5) // Refund requested status
                    .OrderByDescending(e => e.EnrollmentUpdatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving refund requests");
                throw;
            }
        }

        // Payment Management
        public async Task<List<PaymentTransaction>> GetRecentPaymentsAsync(int count = 10)
        {
            try
            {
                return await _context.PaymentTransactions
                    .Include(pt => pt.User)
                    .OrderByDescending(pt => pt.TransactionCreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving recent payments");
                throw;
            }
        }

        public async Task<List<PaymentTransaction>> GetAllPaymentsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.PaymentTransactions
                    .Include(pt => pt.User)
                    .OrderByDescending(pt => pt.TransactionCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving all payments");
                throw;
            }
        }

        public async Task<List<PaymentTransaction>> GetPaymentsByStatusAsync(string status, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.PaymentTransactions
                    .Include(pt => pt.User)
                    .Where(pt => pt.TransactionStatus == status)
                    .OrderByDescending(pt => pt.TransactionCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving payments by status: {Status}", status);
                throw;
            }
        }

        public async Task<PaymentTransaction?> GetPaymentWithDetailsAsync(string transactionId)
        {
            try
            {
                return await _context.PaymentTransactions
                    .Include(pt => pt.User)
                    .Include(pt => pt.Recipient)
                    .FirstOrDefaultAsync(pt => pt.TransactionId == transactionId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving payment details: {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<decimal> GetRevenueByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.PaymentTransactions
                    .Where(pt => pt.TransactionStatus == "Success" &&
                                pt.TransactionCreatedAt >= startDate &&
                                pt.TransactionCreatedAt <= endDate)
                    .SumAsync(pt => pt.Amount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving revenue by date range");
                throw;
            }
        }

        public async Task<Dictionary<string, decimal>> GetRevenueByMonthAsync(int year)
        {
            try
            {
                var result = new Dictionary<string, decimal>();

                for (int month = 1; month <= 12; month++)
                {
                    var startDate = new DateTime(year, month, 1);
                    var endDate = startDate.AddMonths(1).AddDays(-1);

                    var revenue = await GetRevenueByDateRangeAsync(startDate, endDate);
                    result[startDate.ToString("MMM")] = revenue;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving revenue by month for year: {Year}", year);
                throw;
            }
        }

        public async Task<List<PaymentTransaction>> GetRefundedPaymentsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.PaymentTransactions
                    .Include(pt => pt.User)
                    .Where(pt => pt.TransactionStatus == "Refunded")
                    .OrderByDescending(pt => pt.TransactionUpdatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving refunded payments");
                throw;
            }
        }

        // Chatbot Analytics
        public async Task<Dictionary<string, object>> GetChatbotStatisticsAsync()
        {
            try
            {
                var totalConversations = await _context.ChatbotConversations.CountAsync();
                var totalUsers = await _context.ChatbotConversations
                    .Select(c => c.UserId)
                    .Distinct()
                    .CountAsync();

                var averageRating = await _context.ChatbotConversations
                    .Where(c => c.FeedbackRating.HasValue)
                    .AverageAsync(c => (double?)c.FeedbackRating) ?? 0;

                var conversationsWithFeedback = await _context.ChatbotConversations
                    .CountAsync(c => c.FeedbackRating.HasValue);

                var today = DateTime.Today;
                var todayConversations = await _context.ChatbotConversations
                    .CountAsync(c => c.ConversationTime >= today);

                var thisWeek = DateTime.Today.AddDays(-7);
                var weeklyConversations = await _context.ChatbotConversations
                    .CountAsync(c => c.ConversationTime >= thisWeek);

                return new Dictionary<string, object>
                {
                    { "TotalConversations", totalConversations },
                    { "TotalUsers", totalUsers },
                    { "AverageRating", Math.Round(averageRating, 2) },
                    { "ConversationsWithFeedback", conversationsWithFeedback },
                    { "TodayConversations", todayConversations },
                    { "WeeklyConversations", weeklyConversations }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving chatbot statistics");
                throw;
            }
        }

        public async Task<List<DailyConversationStats>> GetDailyChatbotUsageAsync(int days = 7)
        {
            try
            {
                var startDate = DateTime.Today.AddDays(-days);

                var conversationData = await _context.ChatbotConversations
                    .Where(c => c.ConversationTime >= startDate)
                    .GroupBy(c => c.ConversationTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        ConversationCount = g.Count(),
                        UniqueUsers = g.Select(x => x.UserId).Distinct().Count()
                    })
                    .ToListAsync();

                var result = new List<DailyConversationStats>();
                for (int i = days - 1; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i);
                    var existingData = conversationData.FirstOrDefault(x => x.Date == date);

                    result.Add(new DataAccessLayer.Models.ViewModels.DailyConversationStats
                    {
                        Date = date,
                        ConversationCount = existingData?.ConversationCount ?? 0,
                        UniqueUsers = existingData?.UniqueUsers ?? 0,
                        DateLabel = date.ToString("MMM dd")
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving daily chatbot usage");
                throw;
            }
        }

        public async Task<List<FeedbackRatingStats>> GetChatbotFeedbackStatsAsync()
        {
            try
            {
                var feedbackStats = await _context.ChatbotConversations
                    .Where(c => c.FeedbackRating.HasValue)
                    .GroupBy(c => c.FeedbackRating!.Value)
                    .Select(g => new DataAccessLayer.Models.ViewModels.FeedbackRatingStats
                    {
                        Rating = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Rating)
                    .ToListAsync();

                // Ensure all ratings from 1-5 are represented
                var result = new List<FeedbackRatingStats>();
                for (byte i = 1; i <= 5; i++)
                {
                    var existing = feedbackStats.FirstOrDefault(x => x.Rating == i);
                    result.Add(new DataAccessLayer.Models.ViewModels.FeedbackRatingStats
                    {
                        Rating = i,
                        Count = existing?.Count ?? 0
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving chatbot feedback stats");
                throw;
            }
        }

        public async Task<List<HourlyUsageStats>> GetChatbotHourlyUsageAsync()
        {
            try
            {
                var last24Hours = DateTime.Now.AddHours(-24);

                var usageData = await _context.ChatbotConversations
                    .Where(c => c.ConversationTime >= last24Hours)
                    .GroupBy(c => c.ConversationTime.Hour)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        ConversationCount = g.Count()
                    })
                    .ToListAsync();

                var result = new List<HourlyUsageStats>();
                for (int hour = 0; hour < 24; hour++)
                {
                    var existing = usageData.FirstOrDefault(x => x.Hour == hour);
                    result.Add(new DataAccessLayer.Models.ViewModels.HourlyUsageStats
                    {
                        Hour = hour,
                        ConversationCount = existing?.ConversationCount ?? 0,
                        HourLabel = $"{hour:00}:00"
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving chatbot hourly usage");
                throw;
            }
        }

        // Missing methods implementation
        public async Task<object> GetDashboardDataAsync(string userId)
        {
            try
            {
                var stats = await GetAdminStatisticsAsync();
                var chatbotStats = await GetChatbotStatisticsAsync();
                var dailyChatbotUsage = await GetDailyChatbotUsageAsync();
                var chatbotFeedback = await GetChatbotFeedbackStatsAsync();
                var chatbotHourlyUsage = await GetChatbotHourlyUsageAsync();

                return new
                {
                    Statistics = stats,
                    ChatbotStatistics = chatbotStats,
                    ChatbotDailyUsage = dailyChatbotUsage,
                    ChatbotFeedback = chatbotFeedback,
                    ChatbotHourlyUsage = chatbotHourlyUsage
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting dashboard data for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetSystemStatisticsAsync()
        {
            try
            {
                return await GetAdminStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting system statistics");
                throw;
            }
        }

        // Certificate Analytics Methods
        public async Task<List<MonthlyCertificateIssued>> GetMonthlyCertificateDataAsync()
        {
            try
            {
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);

                var certificateData = await _context.Certificates
                    .Where(c => c.CertificateCreatedAt >= sixMonthsAgo)
                    .GroupBy(c => new
                    {
                        Year = c.CertificateCreatedAt.Year,
                        Month = c.CertificateCreatedAt.Month
                    })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        CertificatesIssued = g.Count()
                    })
                    .ToListAsync();

                var convertedData = certificateData.Select(x => new DataAccessLayer.Models.ViewModels.MonthlyCertificateIssued
                {
                    Month = new DateTime(x.Year, x.Month, 1).ToString("MMMM"),
                    CertificatesIssued = x.CertificatesIssued,
                    Date = new DateTime(x.Year, x.Month, 1)
                }).OrderBy(x => x.Date).ToList();

                // Fill in missing months with zero values
                var result = new List<MonthlyCertificateIssued>();
                for (int i = 5; i >= 0; i--)
                {
                    var monthDate = DateTime.Now.AddMonths(-i);
                    var existingData = convertedData.FirstOrDefault(x =>
                        x.Date.Year == monthDate.Year && x.Date.Month == monthDate.Month);

                    result.Add(existingData ?? new DataAccessLayer.Models.ViewModels.MonthlyCertificateIssued
                    {
                        Month = monthDate.ToString("MMMM"),
                        CertificatesIssued = 0,
                        Date = new DateTime(monthDate.Year, monthDate.Month, 1)
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving monthly certificate data");
                throw;
            }
        }

        public async Task<List<CourseCompletionRate>> GetCourseCompletionRatesAsync()
        {
            try
            {
                var courseCompletionData = await _context.Courses
                    .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Certificates)
                    .Where(c => c.ApprovalStatus == "approved")
                    .Select(c => new DataAccessLayer.Models.ViewModels.CourseCompletionRate
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName ?? "Unknown Course",
                        TotalEnrollments = c.Enrollments!.Count(),
                        CompletedCount = c.Enrollments!.Count(e => e.Certificates!.Any()),
                        CompletionRate = c.Enrollments!.Count() > 0 ?
                            (decimal)c.Enrollments!.Count(e => e.Certificates!.Any()) / c.Enrollments!.Count() * 100 : 0
                    })
                    .OrderByDescending(c => c.CompletionRate)
                    .Take(10)
                    .ToListAsync();

                return courseCompletionData;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving course completion rates");
                throw;
            }
        }

        // Point Analytics Methods
        public async Task<List<PointDistribution>> GetPointDistributionDataAsync()
        {
            try
            {
                var pointRanges = new[]
                {
                    new { Min = 0, Max = 100, Label = "0-100" },
                    new { Min = 101, Max = 500, Label = "101-500" },
                    new { Min = 501, Max = 1000, Label = "501-1000" },
                    new { Min = 1001, Max = 5000, Label = "1001-5000" },
                    new { Min = 5001, Max = int.MaxValue, Label = "5000+" }
                };

                var result = new List<PointDistribution>();

                foreach (var range in pointRanges)
                {
                    int userCount;
                    if (range.Max == int.MaxValue)
                    {
                        userCount = await _context.Accounts
                            .Where(a => a.PaymentPoint >= range.Min)
                            .CountAsync();
                    }
                    else
                    {
                        userCount = await _context.Accounts
                            .Where(a => a.PaymentPoint >= range.Min && a.PaymentPoint <= range.Max)
                            .CountAsync();
                    }

                    result.Add(new DataAccessLayer.Models.ViewModels.PointDistribution
                    {
                        PointRange = range.Label,
                        UserCount = userCount
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving point distribution data");
                throw;
            }
        }

        public async Task<List<MonthlyPointsEarned>> GetMonthlyPointsDataAsync()
        {
            try
            {
                var sixMonthsAgo = DateOnly.FromDateTime(DateTime.Now.AddMonths(-6));

                // Get points earned from achievements
                var pointsData = await _context.UserAchievements
                    .Include(ua => ua.Achievement)
                    .Where(ua => ua.ReceivedDate >= sixMonthsAgo)
                    .GroupBy(ua => new
                    {
                        Year = ua.ReceivedDate.Year,
                        Month = ua.ReceivedDate.Month
                    })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        TotalPointsEarned = g.Sum(ua => ua.Achievement!.PointsReward ?? 0)
                    })
                    .ToListAsync();

                var convertedData = pointsData.Select(x => new DataAccessLayer.Models.ViewModels.MonthlyPointsEarned
                {
                    Month = new DateTime(x.Year, x.Month, 1).ToString("MMMM"),
                    TotalPointsEarned = x.TotalPointsEarned,
                    Date = new DateTime(x.Year, x.Month, 1)
                }).OrderBy(x => x.Date).ToList();

                // Fill in missing months with zero values
                var result = new List<MonthlyPointsEarned>();
                for (int i = 5; i >= 0; i--)
                {
                    var monthDate = DateTime.Now.AddMonths(-i);
                    var existingData = convertedData.FirstOrDefault(x =>
                        x.Date.Year == monthDate.Year && x.Date.Month == monthDate.Month);

                    result.Add(existingData ?? new DataAccessLayer.Models.ViewModels.MonthlyPointsEarned
                    {
                        Month = monthDate.ToString("MMMM"),
                        TotalPointsEarned = 0,
                        Date = new DateTime(monthDate.Year, monthDate.Month, 1)
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving monthly points data");
                throw;
            }
        }

        // Basic implementations for remaining interface methods (to be expanded based on specific needs)
        public Task<Dictionary<string, int>> GetUserRegistrationsByMonthAsync(int year) => Task.FromResult(new Dictionary<string, int>());
        public Task<Dictionary<string, int>> GetCourseCreationsByMonthAsync(int year) => Task.FromResult(new Dictionary<string, int>());
        public Task<Dictionary<string, int>> GetEnrollmentsByMonthAsync(int year) => Task.FromResult(new Dictionary<string, int>());
        public Task<List<object>> GetTopCoursesAsync(int count = 10) => Task.FromResult(new List<object>());
        public Task<List<object>> GetTopInstructorsAsync(int count = 10) => Task.FromResult(new List<object>());
        public Task<Dictionary<string, object>> GetUserDemographicsAsync() => Task.FromResult(new Dictionary<string, object>());
        public Task<Dictionary<string, object>> GetCourseAnalyticsAsync() => Task.FromResult(new Dictionary<string, object>());
        public Task<List<Notification>> GetSystemNotificationsAsync(int page = 1, int pageSize = 20) => Task.FromResult(new List<Notification>());
        public Task<bool> SendBulkNotificationAsync(List<string> userIds, string title, string content, string notificationType = "System") => Task.FromResult(true);
        public Task<bool> CreateSystemAnnouncementAsync(string title, string content, string? targetRole = null) => Task.FromResult(true);
        public Task<List<object>> GetSystemLogsAsync(int page = 1, int pageSize = 20) => Task.FromResult(new List<object>());

        public Task<List<Feedback>> GetReportedFeedbackAsync(int page = 1, int pageSize = 20) => Task.FromResult(new List<Feedback>());
        public Task<bool> ModerateFeedbackAsync(string feedbackId, bool isApproved, string? moderatorNote = null) => Task.FromResult(true);
        public Task<List<object>> GetContentModerationQueueAsync(int page = 1, int pageSize = 20) => Task.FromResult(new List<object>());
        public Task<bool> BackupDatabaseAsync() => Task.FromResult(true);
        public Task<bool> CleanupExpiredDataAsync() => Task.FromResult(true);
        public Task<bool> UpdateSystemSettingsAsync(Dictionary<string, object> settings) => Task.FromResult(true);
        public Task<Dictionary<string, object>> GetSystemSettingsAsync() => Task.FromResult(new Dictionary<string, object>());
        public Task<bool> GenerateReportAsync(string reportType, DateTime startDate, DateTime endDate) => Task.FromResult(true);
        public Task<List<object>> GetSecurityLogsAsync(int page = 1, int pageSize = 20) => Task.FromResult(new List<object>());
        public Task<List<object>> GetSuspiciousActivitiesAsync(int page = 1, int pageSize = 20) => Task.FromResult(new List<object>());
        public Task<bool> LogAdminActionAsync(string adminId, string action, string details, string? targetId = null) => Task.FromResult(true);
        public Task<List<object>> GetAdminActionLogsAsync(string? adminId = null, int page = 1, int pageSize = 20) => Task.FromResult(new List<object>());

        // User Ranking Methods
        public async Task<List<UserRankingData>> GetUserRankingAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                var userRankingQuery = await _context.Accounts
                    .Where(a => a.UserRole == "learner") // Only include learners
                    .Select(a => new UserRankingData
                    {
                        UserId = a.UserId,
                        Username = a.Username ?? "",
                        FullName = a.FullName ?? "",
                        Email = a.UserEmail ?? "",
                        UserImage = a.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",
                        UserRole = a.UserRole ?? "",
                        AccountCreatedAt = a.AccountCreatedAt,
                        LastLoginDate = a.LastLogin,
                        CompletedLessonsCount = a.UserProgresses.Count(up => up.IsCompleted == true),
                        TotalEnrolledCourses = a.Enrollments.Count(),
                        CompletedCourses = a.Enrollments.Count(e => e.ProgressPercentage >= 100),
                        AverageProgress = a.Enrollments.Any() ? a.Enrollments.Average(e => (double)(e.ProgressPercentage ?? 0)) : 0,
                        TotalTimeSpent = a.UserProgresses.Sum(up => up.TimeSpent ?? 0) / 60, // Convert to minutes
                        CertificatesEarned = a.Certificates.Count(),
                        AchievementsEarned = a.UserAchievements.Count(),
                        LastActivityDate = a.UserProgresses.Any() ? a.UserProgresses.Max(up => up.LastAccessedAt) : null,
                        LastAccessedCourse = null, // Will be populated after query
                        CurrentCourse = null // Will be populated after query
                    })
                    .OrderByDescending(u => u.CompletedLessonsCount)
                    .ThenByDescending(u => u.CompletedCourses)
                    .ThenByDescending(u => u.AverageProgress)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Populate the course names after the query
                foreach (var user in userRankingQuery)
                {
                    var account = await _context.Accounts
                        .Include(a => a.Enrollments)
                        .ThenInclude(e => e.Course)
                        .FirstOrDefaultAsync(a => a.UserId == user.UserId);

                    if (account != null)
                    {
                        var lastEnrollment = account.Enrollments.OrderByDescending(e => e.EnrollmentUpdatedAt).FirstOrDefault();
                        user.LastAccessedCourse = lastEnrollment?.Course?.CourseName;

                        var currentEnrollment = account.Enrollments
                            .Where(e => e.ProgressPercentage > 0 && e.ProgressPercentage < 100)
                            .OrderByDescending(e => e.EnrollmentUpdatedAt)
                            .FirstOrDefault();
                        user.CurrentCourse = currentEnrollment?.Course?.CourseName;
                    }
                }

                // Add rank numbers
                var startRank = (page - 1) * pageSize + 1;
                for (int i = 0; i < userRankingQuery.Count; i++)
                {
                    userRankingQuery[i].Rank = startRank + i;
                }

                return userRankingQuery;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user ranking data");
                throw;
            }
        }

        public async Task<int> GetUserRankingTotalCountAsync()
        {
            try
            {
                return await _context.Accounts
                    .Where(a => a.UserRole == "learner")
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user ranking total count");
                throw;
            }
        }

        public async Task<double> GetAverageCompletedLessonsAsync()
        {
            try
            {
                var average = await _context.Accounts
                    .Where(a => a.UserRole == "learner")
                    .Select(a => a.UserProgresses.Count(up => up.IsCompleted == true))
                    .AverageAsync();

                return Math.Round(average, 2);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating average completed lessons");
                return 0;
            }
        }

        // Chatbot History Methods
        public async Task<List<ChatbotConversation>> GetChatbotHistoryAsync(string? search = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.ChatbotConversations
                    .Include(c => c.User)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c =>
                        c.UserMessage.Contains(search) ||
                        c.BotResponse.Contains(search) ||
                        (c.User != null && c.User.Username != null && c.User.Username.Contains(search)) ||
                        (c.User != null && c.User.FullName != null && c.User.FullName.Contains(search)));
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(c => c.UserId == userId);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(c => c.ConversationTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(c => c.ConversationTime <= toDate.Value);
                }

                return await query
                    .OrderByDescending(c => c.ConversationTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving chatbot history");
                throw;
            }
        }

        public async Task<int> GetChatbotHistoryTotalCountAsync(string? search = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.ChatbotConversations
                    .Include(c => c.User)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c =>
                        c.UserMessage.Contains(search) ||
                        c.BotResponse.Contains(search) ||
                        (c.User != null && c.User.Username != null && c.User.Username.Contains(search)) ||
                        (c.User != null && c.User.FullName != null && c.User.FullName.Contains(search)));
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(c => c.UserId == userId);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(c => c.ConversationTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(c => c.ConversationTime <= toDate.Value);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting chatbot history total count");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetChatbotHistoryStatisticsAsync()
        {
            try
            {
                var totalConversations = await _context.ChatbotConversations.CountAsync();
                var totalUsers = await _context.ChatbotConversations.Select(c => c.UserId).Distinct().CountAsync();

                var ratingStats = await _context.ChatbotConversations
                    .Where(c => c.FeedbackRating.HasValue)
                    .GroupBy(c => c.FeedbackRating)
                    .Select(g => new { Rating = g.Key, Count = g.Count() })
                    .ToListAsync();

                var averageRating = ratingStats.Any() ? ratingStats.Average(r => r.Rating!.Value) : 0;
                var totalRatings = ratingStats.Sum(r => r.Count);

                var ratingDistribution = new Dictionary<int, int>();
                for (int i = 1; i <= 5; i++)
                {
                    ratingDistribution[i] = ratingStats.FirstOrDefault(r => r.Rating == i)?.Count ?? 0;
                }

                return new Dictionary<string, object>
                {
                    ["TotalConversations"] = totalConversations,
                    ["TotalUsers"] = totalUsers,
                    ["AverageRating"] = Math.Round(averageRating, 2),
                    ["TotalRatings"] = totalRatings,
                    ["RatingDistribution"] = ratingDistribution
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting chatbot history statistics");
                throw;
            }
        }
    }
}
