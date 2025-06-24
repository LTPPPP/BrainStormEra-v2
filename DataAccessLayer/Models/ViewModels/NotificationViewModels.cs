using DataAccessLayer.Data;
using DataAccessLayer.Models;

namespace DataAccessLayer.Models.ViewModels
{
    public class NotificationIndexViewModel
    {
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public int UnreadCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public string CurrentUserId { get; set; } = string.Empty;
        public bool HasNextPage => Notifications.Count == PageSize;
    }

    public class NotificationCreateViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? CourseId { get; set; }
        public string? TargetUserId { get; set; }
        public List<string> TargetUserIds { get; set; } = new List<string>();
        public string? TargetRole { get; set; }
        public NotificationTargetType TargetType { get; set; }
    }

    public class NotificationEditViewModel
    {
        public string NotificationId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? CourseId { get; set; }
        public string RecipientUserName { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum NotificationTargetType
    {
        User,
        MultipleUsers,
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
