using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace BrainStormEra_MVC.Models.ViewModels
{
    public class CreateCourseViewModel
    {
        [Required(ErrorMessage = "Course name is required")]
        [StringLength(200, ErrorMessage = "Course name must be between 3 and 200 characters", MinimumLength = 3)]
        public string CourseName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Course description is required")]
        [StringLength(2000, ErrorMessage = "Course description must be between 10 and 2000 characters", MinimumLength = 10)]
        public string CourseDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select at least one category")]
        public List<string> SelectedCategories { get; set; } = new List<string>();

        [Required(ErrorMessage = "Price is required")]
        [Range(0, 99999.99, ErrorMessage = "Price must be between 0 and 99999.99")]
        public decimal Price { get; set; }

        [Range(1, 1000, ErrorMessage = "Estimated duration must be between 1 and 1000 hours")]
        public int? EstimatedDuration { get; set; }

        [Required(ErrorMessage = "Difficulty level is required")]
        [Range(1, 4, ErrorMessage = "Please select a valid difficulty level")]
        public byte DifficultyLevel { get; set; }

        public IFormFile? CourseImage { get; set; }

        public bool IsFeatured { get; set; }

        public bool EnforceSequentialAccess { get; set; } = true;

        public bool AllowLessonPreview { get; set; } = false;

        // Available categories for the form
        public List<CourseCategoryViewModel> AvailableCategories { get; set; } = new List<CourseCategoryViewModel>();
    }

    public class CategoryAutocompleteItem
    {
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryIcon { get; set; }
    }
}
