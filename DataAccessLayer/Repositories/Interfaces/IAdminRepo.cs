using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IAdminRepo : IBaseRepo<Account>
    {
        // Dashboard Statistics
        Task<Dictionary<string, object>> GetAdminStatisticsAsync();
        Task<object> GetDashboardDataAsync(string userId);
        Task<Dictionary<string, object>> GetSystemStatisticsAsync();
        Task<decimal> GetTotalRevenueAsync();

        // Chart data methods
        Task<List<MonthlyUserGrowth>> GetUserGrowthDataAsync();
        Task<List<MonthlyRevenue>> GetRevenueDataAsync();
        Task<List<WeeklyEnrollment>> GetWeeklyEnrollmentDataAsync();

        Task<int> GetTotalUsersCountAsync();
        Task<int> GetTotalCoursesCountAsync();
        Task<int> GetTotalEnrollmentsCountAsync();
        Task<int> GetActiveUsersCountAsync();
        Task<int> GetActiveCoursesCountAsync();
        Task<int> GetPendingCoursesCountAsync();

        // User Management
        Task<List<Account>> GetRecentUsersAsync(int count = 5);
        Task<List<Account>> GetAllUsersAsync(int page = 1, int pageSize = 20);
        Task<List<Account>> GetUsersByRoleAsync(string role, int page = 1, int pageSize = 20);
        Task<List<Account>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 20);
        Task<Account?> GetUserWithDetailsAsync(string userId);
        Task<bool> BanUserAsync(string userId, bool isBanned, string reason = "");
        Task<bool> ChangeUserRoleAsync(string userId, string newRole);
        Task<List<Account>> GetBannedUsersAsync(int page = 1, int pageSize = 20);

        // Course Management  
        Task<List<Course>> GetRecentCoursesAsync(int count = 5);
        Task<List<Course>> GetAllCoursesAsync(int page = 1, int pageSize = 20);
        Task<List<Course>> GetCoursesByStatusAsync(string status, int page = 1, int pageSize = 20);
        Task<List<Course>> GetPendingCoursesAsync(int page = 1, int pageSize = 20);
        Task<Course?> GetCourseWithDetailsAsync(string courseId);
        Task<bool> ApproveCourseAsync(string courseId, string adminId);
        Task<bool> RejectCourseAsync(string courseId, string adminId, string reason);
        Task<bool> FeatureCourseAsync(string courseId, bool isFeatured);
        Task<List<Course>> SearchCoursesAsync(string searchTerm, int page = 1, int pageSize = 20);

        // Enrollment Management
        Task<List<Enrollment>> GetRecentEnrollmentsAsync(int count = 10);
        Task<List<Enrollment>> GetAllEnrollmentsAsync(int page = 1, int pageSize = 20);
        Task<List<Enrollment>> GetEnrollmentsByStatusAsync(int status, int page = 1, int pageSize = 20);
        Task<Enrollment?> GetEnrollmentWithDetailsAsync(string enrollmentId);
        Task<bool> UpdateEnrollmentStatusAsync(string enrollmentId, int newStatus);
        Task<List<Enrollment>> GetRefundRequestsAsync(int page = 1, int pageSize = 20);

        // Payment Management
        Task<List<PaymentTransaction>> GetRecentPaymentsAsync(int count = 10);
        Task<List<PaymentTransaction>> GetAllPaymentsAsync(int page = 1, int pageSize = 20);
        Task<List<PaymentTransaction>> GetPaymentsByStatusAsync(string status, int page = 1, int pageSize = 20);
        Task<PaymentTransaction?> GetPaymentWithDetailsAsync(string transactionId);
        Task<decimal> GetRevenueByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, decimal>> GetRevenueByMonthAsync(int year);
        Task<List<PaymentTransaction>> GetRefundedPaymentsAsync(int page = 1, int pageSize = 20);

        // Analytics and Reports
        Task<Dictionary<string, int>> GetUserRegistrationsByMonthAsync(int year);
        Task<Dictionary<string, int>> GetCourseCreationsByMonthAsync(int year);
        Task<Dictionary<string, int>> GetEnrollmentsByMonthAsync(int year);
        Task<List<object>> GetTopCoursesAsync(int count = 10);
        Task<List<object>> GetTopInstructorsAsync(int count = 10);
        Task<Dictionary<string, object>> GetUserDemographicsAsync();
        Task<Dictionary<string, object>> GetCourseAnalyticsAsync();

        // System Management
        Task<List<Notification>> GetSystemNotificationsAsync(int page = 1, int pageSize = 20);
        Task<bool> SendBulkNotificationAsync(List<string> userIds, string title, string content, string notificationType = "System");
        Task<bool> CreateSystemAnnouncementAsync(string title, string content, string? targetRole = null);
        Task<List<object>> GetSystemLogsAsync(int page = 1, int pageSize = 20);


        // Content Moderation
        Task<List<Feedback>> GetReportedFeedbackAsync(int page = 1, int pageSize = 20);
        Task<bool> ModerateFeedbackAsync(string feedbackId, bool isApproved, string? moderatorNote = null);
        Task<List<object>> GetContentModerationQueueAsync(int page = 1, int pageSize = 20);

        // Administrative Actions
        Task<bool> BackupDatabaseAsync();
        Task<bool> CleanupExpiredDataAsync();
        Task<bool> UpdateSystemSettingsAsync(Dictionary<string, object> settings);
        Task<Dictionary<string, object>> GetSystemSettingsAsync();
        Task<bool> GenerateReportAsync(string reportType, DateTime startDate, DateTime endDate);

        // Security and Audit
        Task<List<object>> GetSecurityLogsAsync(int page = 1, int pageSize = 20);
        Task<List<object>> GetSuspiciousActivitiesAsync(int page = 1, int pageSize = 20);
        Task<bool> LogAdminActionAsync(string adminId, string action, string details, string? targetId = null);
        Task<List<object>> GetAdminActionLogsAsync(string? adminId = null, int page = 1, int pageSize = 20);

        // Chatbot Analytics
        Task<Dictionary<string, object>> GetChatbotStatisticsAsync();
        Task<List<DailyConversationStats>> GetDailyChatbotUsageAsync(int days = 7);
        Task<List<FeedbackRatingStats>> GetChatbotFeedbackStatsAsync();
        Task<List<HourlyUsageStats>> GetChatbotHourlyUsageAsync();
    }

    // Supporting DTOs for Admin operations
    public class AdminDashboardStats
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalCourses { get; set; }
        public int PendingCourses { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OpenReports { get; set; }
        public int CriticalIssues { get; set; }
    }

    public class UserGrowthStats
    {
        public DateTime Date { get; set; }
        public int NewUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int ReturnedUsers { get; set; }
    }

    public class CourseStats
    {
        public int TotalCourses { get; set; }
        public int PublishedCourses { get; set; }
        public int DraftCourses { get; set; }
        public int PendingCourses { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalEnrollments { get; set; }
    }

    public class RevenueStats
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransactionValue { get; set; }
    }



    public enum ExportFormat
    {
        CSV,
        Excel,
        PDF,
        JSON
    }

    public enum ReportType
    {
        UserActivity,
        CoursePerformance,
        RevenueAnalysis,
        SystemUsage,
        SecurityAudit
    }
}
