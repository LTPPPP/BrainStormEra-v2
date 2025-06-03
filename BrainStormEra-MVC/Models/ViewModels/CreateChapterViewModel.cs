using System.ComponentModel.DataAnnotations;

namespace BrainStormEra_MVC.Models.ViewModels
{
    public class CreateChapterViewModel
    {
        [Required(ErrorMessage = "Course ID is required")]
        public string CourseId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chapter name is required")]
        [StringLength(200, ErrorMessage = "Chapter name must be between 3 and 200 characters", MinimumLength = 3)]
        public string ChapterName { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Chapter description must not exceed 1000 characters")]
        public string ChapterDescription { get; set; } = string.Empty;

        [Range(1, 999, ErrorMessage = "Chapter order must be between 1 and 999")]
        public int ChapterOrder { get; set; } = 1;

        public bool IsLocked { get; set; } = false;

        public string? UnlockAfterChapterId { get; set; }

        // For displaying course information
        public string CourseName { get; set; } = string.Empty;
        public List<ChapterViewModel> ExistingChapters { get; set; } = new List<ChapterViewModel>();
    }
}