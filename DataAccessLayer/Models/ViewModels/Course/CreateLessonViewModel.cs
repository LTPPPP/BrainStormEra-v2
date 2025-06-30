using System.ComponentModel.DataAnnotations;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Http;
namespace DataAccessLayer.Models.ViewModels
{
    public class CreateLessonViewModel
    {
        [Required]
        public string ChapterId { get; set; } = null!;

        [Required]
        public string CourseId { get; set; } = null!;

        [Required(ErrorMessage = "Lesson name is required")]
        [StringLength(200, ErrorMessage = "Lesson name cannot exceed 200 characters")]
        [Display(Name = "Lesson Name")]
        public string LessonName { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Lesson description cannot exceed 1000 characters")]
        [Display(Name = "Lesson Description")]
        public string? Description { get; set; }
        [Required(ErrorMessage = "Lesson content is required")]
        [Display(Name = "Lesson Content")]
        public string Content { get; set; } = null!;

        [Required(ErrorMessage = "Lesson type is required")]
        [Display(Name = "Lesson Type")]
        public int LessonTypeId { get; set; }

        // Fields for Video Lesson Type (Option 1)
        [Display(Name = "Video URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? VideoUrl { get; set; }

        [Display(Name = "Upload Video File")]
        public IFormFile? VideoFile { get; set; }

        // Fields for Text Lesson Type (Option 2)
        [Display(Name = "Text Content")]
        public string? TextContent { get; set; }

        // Fields for Document Lesson Type (Option 3)
        [Display(Name = "Upload Documents")]
        public List<IFormFile>? DocumentFiles { get; set; }

        [Display(Name = "Document Description")]
        public string? DocumentDescription { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Lesson order must be a positive number")]
        [Display(Name = "Lesson Order")]
        public int Order { get; set; } = 1;

        [Display(Name = "Is Locked")]
        public bool IsLocked { get; set; } = false;

        [Display(Name = "Unlock After Lesson")]
        public string? UnlockAfterLessonId { get; set; }

        [Display(Name = "Is Mandatory")]
        public bool IsMandatory { get; set; } = true;

        [Display(Name = "Requires Quiz Pass")]
        public bool RequiresQuizPass { get; set; } = false;

        [Range(0, 100, ErrorMessage = "Minimum quiz score must be between 0 and 100")]
        [Display(Name = "Minimum Quiz Score (%)")]
        public decimal? MinQuizScore { get; set; }
        [Range(0, 100, ErrorMessage = "Minimum completion percentage must be between 0 and 100")]
        [Display(Name = "Minimum Completion Percentage (%)")]
        public decimal? MinCompletionPercentage { get; set; }

        [Display(Name = "Minimum Time Spent (minutes)")]
        [Range(0, 9999, ErrorMessage = "Minimum time spent must be between 0 and 9999 minutes")]
        public int? MinTimeSpent { get; set; }        // Additional properties for the view
        public string CourseName { get; set; } = null!;
        public string ChapterName { get; set; } = null!;
        public int ChapterOrder { get; set; }
        public IEnumerable<LessonType> LessonTypes { get; set; } = new List<LessonType>();
        public List<Lesson> ExistingLessons { get; set; } = new List<Lesson>();

        // Helper properties for UI
        public bool HasExistingLessons => ExistingLessons.Any();
        public int TotalLessonsInChapter => ExistingLessons.Count;
        public bool IsFirstLesson => !ExistingLessons.Any();
    }
}
