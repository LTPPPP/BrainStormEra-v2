using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    public class AdminUsersViewModel
    {
        public List<AdminUserViewModel> Users { get; set; } = new List<AdminUserViewModel>();
        public string? SearchQuery { get; set; }
        public string? RoleFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalUsers { get; set; }
        public int PageSize { get; set; } = 10;

        // Summary statistics
        public int TotalAdmins { get; set; }
        public int TotalInstructors { get; set; }
        public int TotalLearners { get; set; }
        public int BannedUsers { get; set; }

        // Additional properties for compatibility
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class AdminUserViewModel
    {
        public required string UserId { get; set; }
        public required string Username { get; set; }
        public required string FullName { get; set; }
        public required string UserEmail { get; set; }
        public required string UserRole { get; set; }
        public string UserImage { get; set; } = "/SharedMedia/defaults/default-avatar.svg";
        public DateTime AccountCreatedAt { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsBanned { get; set; }
        public bool IsActive { get; set; } = true;

        // Course related statistics
        public int EnrolledCoursesCount { get; set; }
        public int CreatedCoursesCount { get; set; }
        public decimal TotalSpent { get; set; }

        // Profile additional fields
        public string? Bio { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Location { get; set; }
        public string? Timezone { get; set; }
        public string? PreferredLanguage { get; set; }

        // Computed properties
        public string StatusText => IsBanned ? "Banned" : (IsActive ? "Active" : "Inactive");
        public string RoleBadgeClass => UserRole.ToLower() switch
        {
            "admin" => "bg-danger",
            "instructor" => "bg-warning",
            "learner" => "bg-info",
            _ => "bg-secondary"
        };
    }

    public class AdminCoursesViewModel
    {
        public List<AdminCourseViewModel> Courses { get; set; } = new List<AdminCourseViewModel>();
        public string? SearchQuery { get; set; }
        public string? CategoryFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCourses { get; set; }
        public int PageSize { get; set; } = 10;

        // Summary statistics
        public int ApprovedCourses { get; set; }
        public int PendingCourses { get; set; }
        public int RejectedCourses { get; set; }
        public int FreeCourses { get; set; }
        public int PaidCourses { get; set; }
        public decimal TotalRevenue { get; set; }

        // Additional properties for compatibility
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class AdminCourseViewModel
    {
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public string CourseDescription { get; set; } = "";
        public string CoursePicture { get; set; } = "/SharedMedia/defaults/default-course.svg";
        public decimal Price { get; set; }
        public string? DifficultyLevel { get; set; }
        public int? EstimatedDuration { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsApproved { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;

        // Instructor information
        public required string InstructorId { get; set; }
        public required string InstructorName { get; set; }
        public string InstructorEmail { get; set; } = "";

        // Course statistics
        public int EnrollmentCount { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public decimal Revenue { get; set; }

        // Course categories
        public List<string> Categories { get; set; } = new List<string>();

        // Computed properties
        public string StatusText => IsApproved ? "Approved" : "Pending";
        public string StatusBadgeClass => IsApproved ? "bg-success" : "bg-warning";
        public string PriceText => Price > 0 ? $"${Price:N2}" : "Free";
        public string DifficultyText => DifficultyLevel switch
        {
            "1" => "Beginner",
            "2" => "Intermediate",
            "3" => "Advanced",
            "4" => "Expert",
            _ => "Unknown"
        };
    }

    public class UpdateUserStatusRequest
    {
        [Required]
        public required string UserId { get; set; }

        [Required]
        public bool IsBanned { get; set; }
    }

    public class UpdateCourseStatusRequest
    {
        [Required]
        public required string CourseId { get; set; }

        [Required]
        public bool IsApproved { get; set; }
    }

    // Chatbot Analytics ViewModels
    public class DailyConversationStats
    {
        public DateTime Date { get; set; }
        public int ConversationCount { get; set; }
        public int UniqueUsers { get; set; }
        public string DateLabel { get; set; } = "";
    }

    public class FeedbackRatingStats
    {
        public byte Rating { get; set; }
        public int Count { get; set; }
    }

    public class HourlyUsageStats
    {
        public int Hour { get; set; }
        public int ConversationCount { get; set; }
        public string HourLabel { get; set; } = "";
    }

    public class ChatbotAnalyticsViewModel
    {
        public Dictionary<string, object> Statistics { get; set; } = new();
        public List<DailyConversationStats> DailyUsage { get; set; } = new();
        public List<FeedbackRatingStats> FeedbackStats { get; set; } = new();
        public List<HourlyUsageStats> HourlyUsage { get; set; } = new();
    }

    // Achievement Management ViewModels
    public class AdminAchievementsViewModel
    {
        public List<AdminAchievementViewModel> Achievements { get; set; } = new List<AdminAchievementViewModel>();
        public string? SearchQuery { get; set; }
        public string? TypeFilter { get; set; }
        public string? PointsFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalAchievements { get; set; }
        public int PageSize { get; set; } = 12;

        // Summary statistics
        public int CourseAchievements { get; set; }
        public int QuizAchievements { get; set; }
        public int SpecialAchievements { get; set; }
        public int MilestoneAchievements { get; set; }
        public int TotalAwarded { get; set; }

        // Additional properties for compatibility
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class AdminAchievementViewModel
    {
        public required string AchievementId { get; set; }
        public required string AchievementName { get; set; }
        public string AchievementDescription { get; set; } = "";
        public string AchievementIcon { get; set; } = "fas fa-trophy";
        public string AchievementType { get; set; } = "general";
        public int PointsReward { get; set; }
        public DateTime AchievementCreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Statistics
        public int TimesAwarded { get; set; }
        public int TotalPointsDistributed => TimesAwarded * PointsReward;

        // Computed properties
        public string TypeBadgeClass => AchievementType.ToLower() switch
        {
            "course" => "bg-success",
            "quiz" => "bg-warning",
            "special" => "bg-danger",
            "milestone" => "bg-info",
            _ => "bg-secondary"
        };

        public string TypeDisplayName => AchievementType.ToLower() switch
        {
            "course" => "Course Completion",
            "quiz" => "Quiz Achievement",
            "special" => "Special Achievement",
            "milestone" => "Milestone",
            _ => "General"
        };

        public string PointsText => PointsReward switch
        {
            0 => "No Points",
            1 => "1 Point",
            _ => $"{PointsReward} Points"
        };
    }

    public class CreateAchievementRequest
    {
        [Required]
        [StringLength(100)]
        public required string AchievementName { get; set; }

        [StringLength(500)]
        public string? AchievementDescription { get; set; }

        [StringLength(100)]
        public string AchievementIcon { get; set; } = "fas fa-trophy";

        [Required]
        [StringLength(50)]
        public required string AchievementType { get; set; }
    }

    public class UpdateAchievementRequest
    {
        [Required]
        public required string AchievementId { get; set; }

        [Required]
        [StringLength(100)]
        public required string AchievementName { get; set; }

        [StringLength(500)]
        public string? AchievementDescription { get; set; }

        [StringLength(100)]
        public string AchievementIcon { get; set; } = "fas fa-trophy";

        [Required]
        [StringLength(50)]
        public required string AchievementType { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
