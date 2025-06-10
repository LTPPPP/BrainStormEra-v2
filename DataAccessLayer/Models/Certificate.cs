using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class Certificate
{
    public string CertificateId { get; set; } = null!;

    public string EnrollmentId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string CourseId { get; set; } = null!;

    public string CertificateCode { get; set; } = null!;

    public string CertificateName { get; set; } = null!;

    public string? CertificateTemplate { get; set; }

    public string? CertificateUrl { get; set; }

    public DateOnly IssueDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public bool? IsValid { get; set; }

    public string? VerificationUrl { get; set; }

    public decimal? FinalScore { get; set; }

    public DateTime CertificateCreatedAt { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual Account User { get; set; } = null!;
}
