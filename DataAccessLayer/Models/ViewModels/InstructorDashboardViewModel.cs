namespace DataAccessLayer.Models.ViewModels
{
    public class InstructorDashboardViewModel
    {
        public required string InstructorName { get; set; }
        public required string InstructorImage { get; set; }
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public List<CourseViewModel> RecentCourses { get; set; } = new List<CourseViewModel>();
        public List<EnrollmentTrendViewModel> EnrollmentTrends { get; set; } = new List<EnrollmentTrendViewModel>();
        public List<NotificationViewModel> Notifications { get; set; } = new List<NotificationViewModel>();
        public int UnreadNotificationsCount => Notifications?.Count(n => !n.IsRead) ?? 0;
    }

    public class EnrollmentTrendViewModel
    {
        public string Month { get; set; } = "";
        public int EnrollmentCount { get; set; }
    }
}
