using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class Enrollment
{
    public string EnrollmentId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string CourseId { get; set; } = null!;

    public int? EnrollmentStatus { get; set; }

    public bool? Approved { get; set; }

    public decimal? ProgressPercentage { get; set; }

    public DateOnly? CertificateIssuedDate { get; set; }

    public string? CurrentLessonId { get; set; }

    public string? LastAccessedLessonId { get; set; }

    public DateTime EnrollmentCreatedAt { get; set; }

    public DateTime EnrollmentUpdatedAt { get; set; }

    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public virtual Course Course { get; set; } = null!;

    public virtual Lesson? CurrentLesson { get; set; }

    public virtual Status? EnrollmentStatusNavigation { get; set; }

    public virtual Lesson? LastAccessedLesson { get; set; }

    public virtual Account User { get; set; } = null!;

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
