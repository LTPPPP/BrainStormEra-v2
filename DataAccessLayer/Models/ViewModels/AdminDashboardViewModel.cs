using System;
using System.Collections.Generic;
using DataAccessLayer.Models.ViewModels;
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

        // Chatbot Analytics
        public Dictionary<string, object> ChatbotStatistics { get; set; } = new Dictionary<string, object>();
        public List<DailyConversationStats> ChatbotDailyUsage { get; set; } = new List<DailyConversationStats>();
        public List<FeedbackRatingStats> ChatbotFeedback { get; set; } = new List<FeedbackRatingStats>();
        public List<HourlyUsageStats> ChatbotHourlyUsage { get; set; } = new List<HourlyUsageStats>();
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
        public DateTime AccountCreatedAt { get; set; }
        public bool IsBanned { get; set; }
    }
}
