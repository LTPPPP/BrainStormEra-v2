using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class Lesson
{
    public string LessonId { get; set; } = null!;

    public string ChapterId { get; set; } = null!;

    public string LessonName { get; set; } = null!;

    public string? LessonDescription { get; set; }

    public string LessonContent { get; set; } = null!;

    public int LessonOrder { get; set; }

    public int? LessonTypeId { get; set; }

    public int? LessonStatus { get; set; }

    public bool? IsLocked { get; set; }

    public string? UnlockAfterLessonId { get; set; }

    public decimal? MinCompletionPercentage { get; set; }

    public int? MinTimeSpent { get; set; }

    public DateTime? AccessTime { get; set; }

    public bool? IsMandatory { get; set; }

    public bool? RequiresQuizPass { get; set; }

    public decimal? MinQuizScore { get; set; }

    public DateTime LessonCreatedAt { get; set; }

    public DateTime LessonUpdatedAt { get; set; }

    public virtual Chapter Chapter { get; set; } = null!;

    public virtual ICollection<Enrollment> EnrollmentCurrentLessons { get; set; } = new List<Enrollment>();

    public virtual ICollection<Enrollment> EnrollmentLastAccessedLessons { get; set; } = new List<Enrollment>();

    public virtual ICollection<Lesson> InverseUnlockAfterLesson { get; set; } = new List<Lesson>();

    public virtual ICollection<LessonPrerequisite> LessonPrerequisiteLessons { get; set; } = new List<LessonPrerequisite>();

    public virtual ICollection<LessonPrerequisite> LessonPrerequisitePrerequisiteLessons { get; set; } = new List<LessonPrerequisite>();

    public virtual Status? LessonStatusNavigation { get; set; }

    public virtual LessonType? LessonType { get; set; }

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual Lesson? UnlockAfterLesson { get; set; }

    public virtual ICollection<UserProgress> UserProgresses { get; set; } = new List<UserProgress>();
}
