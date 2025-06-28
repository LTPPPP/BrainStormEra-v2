using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models.ViewModels
{
    public class LearnerDashboardViewModel
    {
        public required string UserName { get; set; }
        public required string FullName { get; set; }
        public required string UserImage { get; set; }
        public List<EnrolledCourseViewModel> EnrolledCourses { get; set; } = new List<EnrolledCourseViewModel>();
        public List<NotificationViewModel> Notifications { get; set; } = new List<NotificationViewModel>();
        public int UnreadNotificationsCount => Notifications?.Count(n => !n.IsRead) ?? 0;
    }

    public class EnrolledCourseViewModel
    {
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public required string CourseImage { get; set; }
        public required string AuthorName { get; set; }
        public DateTime? EnrolledDate { get; set; }
        public DateTime? LastAccessDate { get; set; }
        public int CompletionPercentage { get; set; }
    }

    public class NotificationViewModel
    {
        public required string NotificationId { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
