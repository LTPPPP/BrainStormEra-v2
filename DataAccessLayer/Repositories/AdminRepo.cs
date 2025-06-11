using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
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
                _logger?.LogError(ex, "Error retrieving admin statistics");
                throw;
            }
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            try
            {
                return await _context.PaymentTransactions
                    .Where(pt => pt.TransactionStatus == "Success")
                    .SumAsync(pt => pt.Amount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving total revenue");
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
                return await _context.Courses.CountAsync();
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
                return await _context.Courses.Where(c => c.CourseStatus == 1).CountAsync(); // Active status
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
                return await _context.Courses.Where(c => c.ApprovalStatus == "Pending").CountAsync();
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
                    .Include(c => c.Author)
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
                course.ApprovedBy = adminId;
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
                course.ApprovedBy = adminId;
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

        public async Task<List<Course>> SearchCoursesAsync(string searchTerm, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Courses
                    .Include(c => c.Author)
                    .Where(c => c.CourseName.Contains(searchTerm) ||
                               (c.CourseDescription != null && c.CourseDescription.Contains(searchTerm)))
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
                    .Include(pt => pt.Course)
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
                    .Include(pt => pt.Course)
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
                    .Include(pt => pt.Course)
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
                    .Include(pt => pt.Course)
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
                    .Include(pt => pt.Course)
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

        // Missing methods implementation
        public async Task<object> GetDashboardDataAsync(string userId)
        {
            try
            {
                var stats = await GetAdminStatisticsAsync();
                var recentUsers = await GetRecentUsersAsync(5);
                var recentCourses = await GetRecentCoursesAsync(5);
                var recentEnrollments = await GetRecentEnrollmentsAsync(10);
                var recentPayments = await GetRecentPaymentsAsync(10);

                return new
                {
                    Statistics = stats,
                    RecentUsers = recentUsers,
                    RecentCourses = recentCourses,
                    RecentEnrollments = recentEnrollments,
                    RecentPayments = recentPayments
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

        // Basic implementations for remaining interface methods (to be expanded based on specific needs)
        public async Task<Dictionary<string, int>> GetUserRegistrationsByMonthAsync(int year) => new();
        public async Task<Dictionary<string, int>> GetCourseCreationsByMonthAsync(int year) => new();
        public async Task<Dictionary<string, int>> GetEnrollmentsByMonthAsync(int year) => new();
        public async Task<List<object>> GetTopCoursesAsync(int count = 10) => new();
        public async Task<List<object>> GetTopInstructorsAsync(int count = 10) => new();
        public async Task<Dictionary<string, object>> GetUserDemographicsAsync() => new();
        public async Task<Dictionary<string, object>> GetCourseAnalyticsAsync() => new();
        public async Task<List<Notification>> GetSystemNotificationsAsync(int page = 1, int pageSize = 20) => new();
        public async Task<bool> SendBulkNotificationAsync(List<string> userIds, string title, string content, string notificationType = "System") => true;
        public async Task<bool> CreateSystemAnnouncementAsync(string title, string content, string? targetRole = null) => true;
        public async Task<List<object>> GetSystemLogsAsync(int page = 1, int pageSize = 20) => new();
        public async Task<Dictionary<string, object>> GetSystemHealthAsync() => new();
        public async Task<List<Feedback>> GetReportedFeedbackAsync(int page = 1, int pageSize = 20) => new();
        public async Task<bool> ModerateFeedbackAsync(string feedbackId, bool isApproved, string? moderatorNote = null) => true;
        public async Task<List<object>> GetContentModerationQueueAsync(int page = 1, int pageSize = 20) => new();
        public async Task<bool> BackupDatabaseAsync() => true;
        public async Task<bool> CleanupExpiredDataAsync() => true;
        public async Task<bool> UpdateSystemSettingsAsync(Dictionary<string, object> settings) => true;
        public async Task<Dictionary<string, object>> GetSystemSettingsAsync() => new();
        public async Task<bool> GenerateReportAsync(string reportType, DateTime startDate, DateTime endDate) => true;
        public async Task<List<object>> GetSecurityLogsAsync(int page = 1, int pageSize = 20) => new();
        public async Task<List<object>> GetSuspiciousActivitiesAsync(int page = 1, int pageSize = 20) => new();
        public async Task<bool> LogAdminActionAsync(string adminId, string action, string details, string? targetId = null) => true;
        public async Task<List<object>> GetAdminActionLogsAsync(string? adminId = null, int page = 1, int pageSize = 20) => new();
    }
}
