using System;
using System.Collections.Generic;

namespace BrainStormEra_MVC.Models;

public partial class Account
{
    public string UserId { get; set; } = null!;

    public string UserRole { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public string? FullName { get; set; }

    public decimal? PaymentPoint { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public short? Gender { get; set; }

    public string? PhoneNumber { get; set; }

    public string? UserAddress { get; set; }

    public string? UserImage { get; set; }

    public bool? IsBanned { get; set; }

    public DateTime? LastLogin { get; set; }

    public DateTime AccountCreatedAt { get; set; }

    public DateTime AccountUpdatedAt { get; set; }

    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public virtual ICollection<ChatbotConversation> ChatbotConversations { get; set; } = new List<ChatbotConversation>();

    public virtual ICollection<Course> CourseApprovedByNavigations { get; set; } = new List<Course>();

    public virtual ICollection<Course> CourseAuthors { get; set; } = new List<Course>();

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Notification> NotificationCreatedByNavigations { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationUsers { get; set; } = new List<Notification>();

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

    public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();

    public virtual ICollection<UserProgress> UserProgresses { get; set; } = new List<UserProgress>();
}
