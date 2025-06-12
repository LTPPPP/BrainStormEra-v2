using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    public class CreateChapterViewModel
    {
        [Required(ErrorMessage = "Course ID is required")]
        public string CourseId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chapter name is required")]
        [StringLength(200, ErrorMessage = "Chapter name must be between 3 and 200 characters", MinimumLength = 3)]
        [Display(Name = "Chapter Name")]
        public string ChapterName { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Chapter description must not exceed 1000 characters")]
        [Display(Name = "Chapter Description")]
        public string ChapterDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chapter order is required")]
        [Range(1, 999, ErrorMessage = "Chapter order must be between 1 and 999")]
        [Display(Name = "Chapter Order")]
        public int ChapterOrder { get; set; } = 1;

        [Display(Name = "Lock Chapter")]
        public bool IsLocked { get; set; } = false;

        [Display(Name = "Unlock After Chapter")]
        public string? UnlockAfterChapterId { get; set; }

        // For displaying course information
        public string CourseName { get; set; } = string.Empty;
        public string CourseDescription { get; set; } = string.Empty;
        public List<ChapterViewModel> ExistingChapters { get; set; } = new List<ChapterViewModel>();

        // Additional properties for better user experience
        public int TotalExistingChapters => ExistingChapters.Count;
        public int TotalLessonsInCourse => ExistingChapters.Sum(c => c.Lessons?.Count ?? 0);
        public bool HasPrerequisites => ExistingChapters.Any();

        // Validation helper properties
        public bool IsChapterOrderValid => ChapterOrder > 0 && ChapterOrder <= 999;
        public bool IsFirstChapter => !ExistingChapters.Any();
    }
}