using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public required string AdminName { get; set; }
        public required string AdminImage { get; set; }
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public decimal TotalRevenue { get; set; }

        // Additional statistics for charts
        public int TotalLearners { get; set; }
        public int TotalInstructors { get; set; }
        public int TotalAdmins { get; set; }
        public int ApprovedCourses { get; set; }
        public int PendingCourses { get; set; }
        public int RejectedCourses { get; set; }

        // Time series data for charts
        public List<MonthlyUserGrowth> UserGrowthData { get; set; } = new List<MonthlyUserGrowth>();
        public List<MonthlyRevenue> RevenueData { get; set; } = new List<MonthlyRevenue>();
        public List<WeeklyEnrollment> EnrollmentData { get; set; } = new List<WeeklyEnrollment>();

        public List<UserViewModel> RecentUsers { get; set; } = new List<UserViewModel>();
        public List<CourseViewModel> RecentCourses { get; set; } = new List<CourseViewModel>();

        // Certificate Analytics
        public int TotalCertificates { get; set; }
        public int ValidCertificates { get; set; }
        public int ExpiredCertificates { get; set; }
        public List<MonthlyCertificateIssued> CertificateData { get; set; } = new List<MonthlyCertificateIssued>();
        public List<CourseCompletionRate> CourseCompletionRates { get; set; } = new List<CourseCompletionRate>();

        // Point Analytics
        public decimal TotalPointsInSystem { get; set; }
        public decimal AverageUserPoints { get; set; }
        public List<PointDistribution> PointDistributionData { get; set; } = new List<PointDistribution>();
        public List<MonthlyPointsEarned> MonthlyPointsData { get; set; } = new List<MonthlyPointsEarned>();

        // Chatbot Analytics
        public Dictionary<string, object> ChatbotStatistics { get; set; } = new Dictionary<string, object>();
        public List<DailyConversationStats> ChatbotDailyUsage { get; set; } = new List<DailyConversationStats>();
        public List<FeedbackRatingStats> ChatbotFeedback { get; set; } = new List<FeedbackRatingStats>();
        public List<HourlyUsageStats> ChatbotHourlyUsage { get; set; } = new List<HourlyUsageStats>();

        // Filter Options
        public List<string> AvailableYears { get; set; } = new List<string>();
        public List<string> AvailableMonths { get; set; } = new List<string>();
        public List<CourseCategory> CourseCategories { get; set; } = new List<CourseCategory>();
    }

    public class MonthlyUserGrowth
    {
        public string Month { get; set; } = string.Empty;
        public int NewUsers { get; set; }
        public DateTime Date { get; set; }
    }

    public class MonthlyRevenue
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public DateTime Date { get; set; }
    }

    public class WeeklyEnrollment
    {
        public string Week { get; set; } = string.Empty;
        public int NewEnrollments { get; set; }
        public int CompletedCourses { get; set; }
        public DateTime Date { get; set; }
    }

    public class UserViewModel
    {
        public required string UserId { get; set; }
        public required string Username { get; set; }
        public required string FullName { get; set; }
        public required string UserEmail { get; set; }
        public required string UserRole { get; set; }
        public string UserImage { get; set; } = "/SharedMedia/defaults/default-avatar.svg";
        public DateTime AccountCreatedAt { get; set; }
        public bool IsBanned { get; set; }
    }

    // Certificate related classes
    public class MonthlyCertificateIssued
    {
        public string Month { get; set; } = string.Empty;
        public int CertificatesIssued { get; set; }
        public DateTime Date { get; set; }
    }

    public class CourseCompletionRate
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int TotalEnrollments { get; set; }
        public int CompletedCount { get; set; }
        public decimal CompletionRate { get; set; }
    }

    // Point related classes
    public class PointDistribution
    {
        public string PointRange { get; set; } = string.Empty;
        public int UserCount { get; set; }
    }

    public class MonthlyPointsEarned
    {
        public string Month { get; set; } = string.Empty;
        public decimal TotalPointsEarned { get; set; }
        public DateTime Date { get; set; }
    }

    // Note: DailyConversationStats, FeedbackRatingStats, and HourlyUsageStats 
    // are already defined in AdminManagementViewModels.cs
}
