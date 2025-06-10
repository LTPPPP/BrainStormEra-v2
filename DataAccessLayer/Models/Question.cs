using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class Question
{
    public string QuestionId { get; set; } = null!;

    public string QuizId { get; set; } = null!;

    public string QuestionText { get; set; } = null!;

    public string? QuestionType { get; set; }

    public decimal? Points { get; set; }

    public int? QuestionOrder { get; set; }

    public string? Explanation { get; set; }

    public DateTime QuestionCreatedAt { get; set; }

    public virtual ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
