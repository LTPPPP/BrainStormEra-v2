using System.Collections.Generic;

namespace BrainStormEra_MVC.Models.ViewModels
{
    public class HomePageGuestViewModel
    {
        public List<CourseViewModel> RecommendedCourses { get; set; } = new List<CourseViewModel>();

        // Additional properties can be added here for other sections of the homepage
    }
    public class CourseViewModel
    {
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public required string CoursePicture { get; set; }
        public decimal Price { get; set; }
        public required string CreatedBy { get; set; }
        public int StarRating { get; set; }
        public List<string> CourseCategories { get; set; } = new List<string>();
    }
}
