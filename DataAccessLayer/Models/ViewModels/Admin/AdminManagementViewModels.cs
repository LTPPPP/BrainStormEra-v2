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
        public string EncodedUserId { get; set; } = string.Empty;
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

        // Profile additional fields from Account model
        public string? Bio { get; set; }
        public string? PhoneNumber { get; set; }
        public string? UserAddress { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public short? Gender { get; set; }
        public decimal? PaymentPoint { get; set; }

        // Bank account information
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? AccountHolderName { get; set; }

        // Additional profile fields
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

        public string GenderText => Gender switch
        {
            1 => "Male",
            2 => "Female",
            3 => "Other",
            _ => "Not specified"
        };

        public string PaymentPointText => PaymentPoint?.ToString("N0") ?? "0";
        public string BankAccountMasked => !string.IsNullOrEmpty(BankAccountNumber) && BankAccountNumber.Length > 4
            ? $"****{BankAccountNumber.Substring(BankAccountNumber.Length - 4)}"
            : "Not set";
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
        public string? ApprovalStatus { get; set; }
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
        public string StatusText => ApprovalStatus?.ToLower() switch
        {
            "approved" => "Approved",
            "pending" => "Pending",
            "rejected" => "Rejected",
            "banned" => "Banned",
            "draft" => "Draft",
            _ => IsApproved ? "Approved" : "Pending"
        };

        public string StatusBadgeClass => ApprovalStatus?.ToLower() switch
        {
            "approved" => "status-approved",
            "pending" => "status-pending",
            "rejected" => "status-rejected",
            "banned" => "status-banned",
            "draft" => "status-draft",
            _ => IsApproved ? "status-approved" : "status-pending"
        };

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

    public class AdminCourseDetailsViewModel
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
        public string? ApprovalStatus { get; set; }
        public bool IsApproved { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;
        public bool EnforceSequentialAccess { get; set; }
        public bool AllowLessonPreview { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Instructor information
        public required string InstructorId { get; set; }
        public required string InstructorName { get; set; }
        public string InstructorEmail { get; set; } = "";
        public string InstructorBio { get; set; } = "";
        public string InstructorImage { get; set; } = "/SharedMedia/defaults/default-avatar.svg";

        // Course structure
        public List<CourseChapterSummary> Chapters { get; set; } = new List<CourseChapterSummary>();
        public List<string> Categories { get; set; } = new List<string>();

        // Course statistics
        public int EnrollmentCount { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public decimal Revenue { get; set; }
        public int TotalLessons { get; set; }
        public int TotalQuizzes { get; set; }
        public int CompletionRate { get; set; }

        // Recent activity
        public List<CourseReviewSummary> RecentReviews { get; set; } = new List<CourseReviewSummary>();
        public List<CourseEnrollmentSummary> RecentEnrollments { get; set; } = new List<CourseEnrollmentSummary>();

        // Computed properties
        public string StatusText => ApprovalStatus ?? (IsApproved ? "Approved" : "Pending");
        public string StatusBadgeClass => ApprovalStatus?.ToLower() switch
        {
            "approved" => "bg-success",
            "pending" => "bg-warning",
            "rejected" => "bg-danger",
            "banned" => "bg-dark",
            _ => IsApproved ? "bg-success" : "bg-warning"
        };
        public string PriceText => Price > 0 ? $"${Price:N2}" : "Free";
        public string DifficultyText => DifficultyLevel switch
        {
            "1" => "Beginner",
            "2" => "Intermediate",
            "3" => "Advanced",
            "4" => "Expert",
            _ => "Unknown"
        };
        public string DurationText => EstimatedDuration.HasValue ? $"{EstimatedDuration} hours" : "Not specified";
        public string CategoriesText => Categories.Any() ? string.Join(", ", Categories) : "No categories";
        public string CompletionRateText => $"{CompletionRate}%";
        public string ApprovalStatusText => ApprovalStatus ?? "Unknown";
    }

    public class CourseChapterSummary
    {
        public required string ChapterId { get; set; }
        public required string ChapterName { get; set; }
        public int ChapterOrder { get; set; }
        public int LessonCount { get; set; }
        public bool IsLocked { get; set; }
    }

    public class CourseReviewSummary
    {
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public string UserImage { get; set; } = "/SharedMedia/defaults/default-avatar.svg";
        public decimal Rating { get; set; }
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class CourseEnrollmentSummary
    {
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public string UserImage { get; set; } = "/SharedMedia/defaults/default-avatar.svg";
        public DateTime EnrolledAt { get; set; }
        public int ProgressPercentage { get; set; }
        public string PaymentStatus { get; set; } = "";
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
        public string? SortBy { get; set; } = "date_desc";
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
            "course_completion" => "bg-success",
            "first_course" => "bg-success",
            "quiz_master" => "bg-warning",
            "instructor" => "bg-danger",
            "student_engagement" => "bg-info",
            "streak" => "bg-primary",
            _ => "bg-secondary"
        };

        public string TypeDisplayName => AchievementType.ToLower() switch
        {
            "course_completion" => "Course Completion",
            "first_course" => "First Course",
            "quiz_master" => "Quiz Master",
            "instructor" => "Instructor Achievement",
            "student_engagement" => "Student Engagement",
            "streak" => "Streak Achievement",
            _ => "General"
        };


    }

    public class CreateAchievementRequest
    {
        [Required]
        [StringLength(100)]
        public required string AchievementName { get; set; }

        [StringLength(500)]
        public string? AchievementDescription { get; set; }

        [StringLength(255)]
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

        [StringLength(255)]
        public string AchievementIcon { get; set; } = "fas fa-trophy";

        [Required]
        [StringLength(50)]
        public required string AchievementType { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // Profile Management ViewModels
    public class UpdateProfileRequest
    {
        [Required]
        public required string UserId { get; set; }

        [Required]
        [StringLength(100)]
        public required string FullName { get; set; }

        [Required]
        [StringLength(50)]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public required string UserEmail { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? UserAddress { get; set; }

        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        [Range(1, 3)]
        public short? Gender { get; set; }

        // Bank account information
        [StringLength(50)]
        public string? BankAccountNumber { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(100)]
        public string? AccountHolderName { get; set; }

        // Additional fields
        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(50)]
        public string? Timezone { get; set; }

        [StringLength(10)]
        public string? PreferredLanguage { get; set; }
    }

    public class VNPayQRRequest
    {
        [Required]
        public required string BankAccountNumber { get; set; }

        [Required]
        public required string BankName { get; set; }

        [Required]
        public required string AccountHolderName { get; set; }

        [Range(1000, 50000000)]
        public decimal Amount { get; set; } = 10000; // Default 10,000 VND

        [StringLength(200)]
        public string? Description { get; set; }
    }

    // Shared utility classes for admin operations
    public class IconUploadResult
    {
        public bool Success { get; set; }
        public string? IconPath { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
