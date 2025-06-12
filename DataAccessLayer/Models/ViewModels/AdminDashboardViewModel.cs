using System;
using System.Collections.Generic;
using DataAccessLayer.Models.ViewModels;
namespace DataAccessLayer.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public required string AdminName { get; set; }
        public required string AdminImage { get; set; }
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<UserViewModel> RecentUsers { get; set; } = new List<UserViewModel>();
        public List<CourseViewModel> RecentCourses { get; set; } = new List<CourseViewModel>();
    }

    public class UserViewModel
    {
        public required string UserId { get; set; }
        public required string Username { get; set; }
        public required string FullName { get; set; }
        public required string UserEmail { get; set; }
        public required string UserRole { get; set; }
        public DateTime AccountCreatedAt { get; set; }
        public bool IsBanned { get; set; }
    }
}
