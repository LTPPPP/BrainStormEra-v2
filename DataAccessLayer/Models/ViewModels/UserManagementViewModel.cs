using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    public class EnrolledUserViewModel
    {
        public required string UserId { get; set; }
        public required string Username { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public string UserImage { get; set; } = "/SharedMedia/defaults/default-avatar.svg";
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public DateTime LastAccessDate { get; set; }
        public decimal ProgressPercentage { get; set; }
        public int? EnrollmentStatus { get; set; }
        public string? CurrentLessonName { get; set; }
        public string? LastAccessedLessonName { get; set; }
        public bool IsCompleted => ProgressPercentage >= 100;
        public string StatusText => EnrollmentStatus switch
        {
            1 => "Active",
            2 => "Suspended",
            3 => "Completed",
            _ => "Unknown"
        };

        // Additional properties needed by views
        public string Status => StatusText; // Alias for compatibility
        public DateTime EnrolledDate => EnrollmentDate; // Alias for compatibility
        public DateTime? LastActivity => LastAccessDate; // Alias for compatibility
    }
    public class UserManagementViewModel
    {
        public required string InstructorId { get; set; }
        public required string InstructorName { get; set; }
        public List<EnrolledUserViewModel> EnrolledUsers { get; set; } = new List<EnrolledUserViewModel>();
        public List<CourseFilterOption> CourseFilters { get; set; } = new List<CourseFilterOption>();
        public string? SelectedCourseId { get; set; }
        public string? SearchQuery { get; set; }
        public string? StatusFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalUsers { get; set; }
        public int PageSize { get; set; } = 10;

        // Summary statistics
        public int TotalEnrolledUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int CompletedUsers { get; set; }
        public double AverageProgress { get; set; }

        // Additional properties needed by views
        public List<EnrolledUserViewModel> Users => EnrolledUsers; // Alias for compatibility
        public string? SearchTerm => SearchQuery; // Alias for compatibility
        public string? SelectedStatus => StatusFilter; // Alias for compatibility
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
    public class CourseFilterOption
    {
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public int EnrollmentCount { get; set; }

        // Additional properties needed by views
        public int EnrolledCount => EnrollmentCount; // Alias for compatibility
    }
    public class UserDetailViewModel
    {
        public required string UserId { get; set; }
        public required string Username { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string UserImage { get; set; } = "/SharedMedia/defaults/default-avatar.svg";
        public DateTime AccountCreatedAt { get; set; }
        public List<UserCourseEnrollment> Enrollments { get; set; } = new List<UserCourseEnrollment>();
        public List<UserAchievementSummary> Achievements { get; set; } = new List<UserAchievementSummary>();
        public int TotalCertificates { get; set; }
        public double OverallProgress { get; set; }

        // Course-specific properties (for single course view)
        public string? CourseId { get; set; }
        public string? CourseName { get; set; }
        public string? CourseDescription { get; set; }
        public string? CourseThumbnail { get; set; }
        public decimal ProgressPercentage { get; set; }
        public string Status { get; set; } = "Unknown";
        public DateTime EnrolledDate { get; set; }

        // Learning statistics
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public int TimeSpent { get; set; } // in minutes
        public DateTime? LastActivity { get; set; }
        public int StudyStreak { get; set; }
        public int AverageSessionTime { get; set; } // in minutes
        public int LoginCount { get; set; }

        // Activity and achievements
        public List<ActivityItem> RecentActivity { get; set; } = new List<ActivityItem>();
        public List<UserCourseEnrollment> OtherEnrollments { get; set; } = new List<UserCourseEnrollment>();
    }

    public class ActivityItem
    {
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Date { get; set; }
    }
    public class UserCourseEnrollment
    {
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public string CourseImage { get; set; } = "/SharedMedia/defaults/default-course.svg";
        public DateTime EnrollmentDate { get; set; }
        public DateTime LastAccessDate { get; set; }
        public decimal ProgressPercentage { get; set; }
        public string? CurrentLessonName { get; set; }
        public string? LastAccessedLessonName { get; set; }
        public int? EnrollmentStatus { get; set; }
        public bool IsCompleted => ProgressPercentage >= 100;
        public string StatusText => EnrollmentStatus switch
        {
            1 => "Active",
            2 => "Suspended",
            3 => "Completed",
            _ => "Unknown"
        };

        // Add Status property as alias for compatibility
        public string Status => StatusText;
    }
    public class UserAchievementSummary
    {
        public required string AchievementName { get; set; }
        public required string Description { get; set; }
        public DateTime EarnedDate { get; set; }
        public string AchievementIcon { get; set; } = "fas fa-trophy";

        // Additional properties needed by views
        public string Name => AchievementName; // Alias for compatibility
        public DateTime DateEarned => EarnedDate; // Alias for compatibility
        public string Icon => AchievementIcon; // Alias for compatibility
    }

    public class UpdateUserProgressRequest
    {
        [Required]
        public required string UserId { get; set; }

        [Required]
        public required string CourseId { get; set; }

        [Range(0, 100)]
        public decimal ProgressPercentage { get; set; }

        public string? CurrentLessonId { get; set; }

        public int? EnrollmentStatus { get; set; }
    }

    public class BulkUserActionRequest
    {
        [Required]
        public required List<string> UserIds { get; set; }

        [Required]
        public required string Action { get; set; } // "suspend", "activate", "unenroll", "sendNotification"

        public string? CourseId { get; set; }
        public string? Message { get; set; }
        public string? NotificationTitle { get; set; }
    }
}
