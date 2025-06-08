using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BrainStormEra_MVC.Models.ViewModels
{
    public class CreateQuizViewModel
    {
        public string? QuizId { get; set; }

        [Required(ErrorMessage = "Quiz title is required")]
        [StringLength(255, ErrorMessage = "Quiz title cannot exceed 255 characters")]
        [Display(Name = "Quiz Title")]
        public string QuizTitle { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Quiz description cannot exceed 1000 characters")]
        [Display(Name = "Quiz Description")]
        public string? QuizDescription { get; set; }

        [Required]
        public string ChapterId { get; set; } = string.Empty;

        public string CourseId { get; set; } = string.Empty;

        public string CourseName { get; set; } = string.Empty;

        public string ChapterName { get; set; } = string.Empty;

        [Display(Name = "Associated Lesson")]
        public string? LessonId { get; set; }

        public string? LessonName { get; set; }        // Available lessons in the chapter for selection
        public List<QuizLessonViewModel> AvailableLessons { get; set; } = new List<QuizLessonViewModel>();

        [Range(1, 300, ErrorMessage = "Time limit must be between 1 and 300 minutes")]
        [Display(Name = "Time Limit (minutes)")]
        public int? TimeLimit { get; set; }

        [Range(0, 100, ErrorMessage = "Passing score must be between 0 and 100")]
        [Display(Name = "Passing Score (%)")]
        public decimal? PassingScore { get; set; }

        [Display(Name = "Maximum Attempts")]
        [Range(1, 10, ErrorMessage = "Maximum attempts must be between 1 and 10")]
        public int? MaxAttempts { get; set; }

        [Display(Name = "Is Final Quiz")]
        public bool IsFinalQuiz { get; set; }

        [Display(Name = "Is Prerequisite Quiz")]
        public bool IsPrerequisiteQuiz { get; set; }

        [Display(Name = "Blocks Lesson Completion")]
        public bool BlocksLessonCompletion { get; set; }
    }

    public class QuizLessonViewModel
    {
        public string LessonId { get; set; } = string.Empty;
        public string LessonName { get; set; } = string.Empty;
        public string LessonDescription { get; set; } = string.Empty;
        public int LessonOrder { get; set; }
        public string LessonType { get; set; } = string.Empty;
        public int EstimatedDuration { get; set; }
        public bool IsLocked { get; set; }
    }
}
