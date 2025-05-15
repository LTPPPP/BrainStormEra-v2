using System;
using System.Collections.Generic;

namespace BrainStormEra.Models;

public partial class Transaction
{
    public string TransactionId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? CourseId { get; set; }

    public string? TransactionType { get; set; }

    public decimal Amount { get; set; }

    public DateTime? TransactionTime { get; set; }

    public virtual Course? Course { get; set; }

    public virtual Account? User { get; set; }
}
