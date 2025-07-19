using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    public class UserRankingViewModel
    {
        public List<UserRankingItem> Users { get; set; } = new List<UserRankingItem>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalUsers { get; set; } = 0;
        public int TotalCompletedLessons { get; set; } = 0;
        public double AverageCompletedLessons { get; set; } = 0;
    }

    public class UserRankingItem
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserImage { get; set; } = "/SharedMedia/defaults/default-avatar.svg";
        public string UserRole { get; set; } = string.Empty;
        public DateTime AccountCreatedAt { get; set; }
        public DateTime? LastLoginDate { get; set; }

        // Ranking statistics
        public int CompletedLessonsCount { get; set; } = 0;
        public int TotalEnrolledCourses { get; set; } = 0;
        public int CompletedCourses { get; set; } = 0;
        public int Rank { get; set; } = 0;
        public double AverageProgress { get; set; } = 0;
        public int TotalTimeSpent { get; set; } = 0; // in minutes
        public int CertificatesEarned { get; set; } = 0;
        public int AchievementsEarned { get; set; } = 0;

        // Recent activity
        public DateTime? LastActivityDate { get; set; }
        public string? LastAccessedCourse { get; set; }
        public string? CurrentCourse { get; set; }
    }
}