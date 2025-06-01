using BrainStormEra_MVC.Models;

namespace BrainStormEra_MVC.Models.ViewModels
{
    public class NotificationIndexViewModel
    {
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public int UnreadCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage => Notifications.Count == PageSize;
    }

    public class NotificationCreateViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? CourseId { get; set; }
        public string? TargetUserId { get; set; }
        public string? TargetRole { get; set; }
        public NotificationTargetType TargetType { get; set; }
    }

    public enum NotificationTargetType
    {
        User,
        Course,
        Role,
        All
    }

    public class NotificationSummaryViewModel
    {
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public List<Notification> RecentNotifications { get; set; } = new List<Notification>();
    }
}
