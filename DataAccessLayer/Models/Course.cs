using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class Course
{
    public string CourseId { get; set; } = null!;

    public string AuthorId { get; set; } = null!;

    public string CourseName { get; set; } = null!;

    public string? CourseDescription { get; set; }

    public int? CourseStatus { get; set; }

    public string? CourseImage { get; set; }

    public decimal Price { get; set; }

    public int? EstimatedDuration { get; set; }

    public byte? DifficultyLevel { get; set; }

    public bool? IsFeatured { get; set; }

    public string? ApprovalStatus { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public bool? EnforceSequentialAccess { get; set; }

    public bool? AllowLessonPreview { get; set; }

    public DateTime CourseCreatedAt { get; set; }

    public DateTime CourseUpdatedAt { get; set; }

    public virtual Account? ApprovedByNavigation { get; set; }

    public virtual Account Author { get; set; } = null!;

    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    public virtual Status? CourseStatusNavigation { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

    public virtual ICollection<CourseCategory> CourseCategories { get; set; } = new List<CourseCategory>();
}
