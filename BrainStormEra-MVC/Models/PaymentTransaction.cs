using System;
using System.Collections.Generic;

namespace BrainStormEra_MVC.Models;

public partial class PaymentTransaction
{
    public string TransactionId { get; set; } = null!;

    public string? RecipientId { get; set; }

    public string? PayoutNotes { get; set; }

    public string UserId { get; set; } = null!;

    public string CourseId { get; set; } = null!;

    public decimal Amount { get; set; }

    public int? PaymentMethodId { get; set; }

    public string? TransactionType { get; set; }

    public string? TransactionStatus { get; set; }

    public string? PaymentGateway { get; set; }

    public string? PaymentGatewayRef { get; set; }

    public string? GatewayTransactionId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public DateTime? RefundDate { get; set; }

    public decimal? RefundAmount { get; set; }

    public string? RefundReason { get; set; }

    public string? CurrencyCode { get; set; }

    public decimal? ExchangeRate { get; set; }

    public decimal? TransactionFee { get; set; }

    public decimal? NetAmount { get; set; }

    public DateTime TransactionCreatedAt { get; set; }

    public DateTime TransactionUpdatedAt { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual Account? Recipient { get; set; }

    public virtual Account User { get; set; } = null!;
}
