using System;
using System.Collections.Generic;

namespace BrainStormEra_MVC.Models.ViewModels
{
    public class InstructorDashboardViewModel
    {
        public required string InstructorName { get; set; }
        public required string InstructorImage { get; set; }
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<CourseViewModel> RecentCourses { get; set; } = new List<CourseViewModel>();
    }
}
