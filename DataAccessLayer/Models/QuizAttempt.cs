using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class QuizAttempt
{
    public string AttemptId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string QuizId { get; set; } = null!;

    public decimal? Score { get; set; }

    public decimal? TotalPoints { get; set; }

    public decimal? PercentageScore { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public bool? IsPassed { get; set; }

    public int? AttemptNumber { get; set; }

    public int? TimeSpent { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual Account User { get; set; } = null!;

    public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
