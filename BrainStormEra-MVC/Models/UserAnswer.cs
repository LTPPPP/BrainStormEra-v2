using System;
using System.Collections.Generic;

namespace BrainStormEra_MVC.Models;

public partial class UserAnswer
{
    public string UserId { get; set; } = null!;

    public string QuestionId { get; set; } = null!;

    public string AttemptId { get; set; } = null!;

    public string? SelectedOptionId { get; set; }

    public string? AnswerText { get; set; }

    public bool? IsCorrect { get; set; }

    public decimal? PointsEarned { get; set; }

    public virtual QuizAttempt Attempt { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;

    public virtual AnswerOption? SelectedOption { get; set; }

    public virtual Account User { get; set; } = null!;
}
