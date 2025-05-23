using System;
using System.Collections.Generic;

namespace BrainStormEra_MVC.Models;

public partial class Notification
{
    public string NotificationId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string? CourseId { get; set; }

    public string NotificationTitle { get; set; } = null!;

    public string NotificationContent { get; set; } = null!;

    public string? NotificationType { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime NotificationCreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public virtual Course? Course { get; set; }

    public virtual Account? CreatedByNavigation { get; set; }

    public virtual Account User { get; set; } = null!;
}
