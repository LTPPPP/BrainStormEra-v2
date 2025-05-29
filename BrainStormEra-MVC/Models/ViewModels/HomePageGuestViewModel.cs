using System.Collections.Generic;

namespace BrainStormEra_MVC.Models.ViewModels
{
    public class HomePageGuestViewModel
    {
        public List<CourseViewModel> RecommendedCourses { get; set; } = new List<CourseViewModel>();
        public List<string> CourseCategories { get; set; } = new List<string>();
        public List<CourseCategoryViewModel> Categories { get; set; } = new List<CourseCategoryViewModel>();

        // Additional properties can be added here for other sections of the homepage
    }
    public class CourseViewModel
    {
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public required string CoursePicture { get; set; }
        public string CourseImage => CoursePicture; // Alias for compatibility
        public decimal Price { get; set; }
        public required string CreatedBy { get; set; }
        public string AuthorName => CreatedBy; // Alias for compatibility
        public string? Description { get; set; }
        public int StarRating { get; set; }
        public int EnrollmentCount { get; set; }
        public List<string> CourseCategories { get; set; } = new List<string>();
    }

    public class CourseCategoryViewModel
    {
        public required string CategoryId { get; set; }
        public required string CategoryName { get; set; }
    }
}
