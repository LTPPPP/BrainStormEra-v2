using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class UserProgress
{
    public string UserId { get; set; } = null!;

    public string LessonId { get; set; } = null!;

    public bool? IsCompleted { get; set; }

    public decimal? ProgressPercentage { get; set; }

    public int? TimeSpent { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? AccessCount { get; set; }

    public DateTime? FirstAccessedAt { get; set; }

    public bool? IsUnlocked { get; set; }

    public DateTime? UnlockedAt { get; set; }

    public bool? MeetsTimeRequirement { get; set; }

    public bool? MeetsPercentageRequirement { get; set; }

    public bool? MeetsQuizRequirement { get; set; }

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual Account User { get; set; } = null!;
}
