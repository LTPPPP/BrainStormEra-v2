using System;
using System.Collections.Generic;

namespace BrainStormEra_MVC.Models;

public partial class Quiz
{
    public string QuizId { get; set; } = null!;

    public string? LessonId { get; set; }

    public string? CourseId { get; set; }

    public string QuizName { get; set; } = null!;

    public string? QuizDescription { get; set; }

    public int? TimeLimit { get; set; }

    public decimal? PassingScore { get; set; }

    public int? MaxAttempts { get; set; }

    public int? QuizStatus { get; set; }

    public bool? IsFinalQuiz { get; set; }

    public bool? IsPrerequisiteQuiz { get; set; }

    public bool? BlocksLessonCompletion { get; set; }

    public DateTime QuizCreatedAt { get; set; }

    public DateTime QuizUpdatedAt { get; set; }

    public virtual Course? Course { get; set; }

    public virtual Lesson? Lesson { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();

    public virtual Status? QuizStatusNavigation { get; set; }
}
