using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class Feedback
{
    public string FeedbackId { get; set; } = null!;

    public string CourseId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public byte? StarRating { get; set; }

    public string? Comment { get; set; }

    public DateOnly FeedbackDate { get; set; }

    public bool? HiddenStatus { get; set; }

    public bool? IsVerifiedPurchase { get; set; }

    public int? HelpfulCount { get; set; }

    public DateTime FeedbackCreatedAt { get; set; }

    public DateTime FeedbackUpdatedAt { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual Account User { get; set; } = null!;
}
