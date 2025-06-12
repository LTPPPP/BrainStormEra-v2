using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace BrainStormEra_MVC.Models.ViewModels
{
    public class CreateQuestionViewModel
    {
        public string? QuestionId { get; set; }

        [Required(ErrorMessage = "Quiz ID is required")]
        public string QuizId { get; set; } = string.Empty;

        public string QuizName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Question text is required")]
        [StringLength(1000, ErrorMessage = "Question text cannot exceed 1000 characters")]
        [Display(Name = "Question Text")]
        public string QuestionText { get; set; } = string.Empty;

        [Required(ErrorMessage = "Question type is required")]
        [Display(Name = "Question Type")]
        public string QuestionType { get; set; } = "multiple_choice";

        [Range(1, 100, ErrorMessage = "Points must be between 1 and 100")]
        [Display(Name = "Points")]
        public int Points { get; set; } = 1;

        [Range(1, 1000, ErrorMessage = "Question order must be between 1 and 1000")]
        [Display(Name = "Question Order")]
        public int QuestionOrder { get; set; } = 1;

        [StringLength(500, ErrorMessage = "Explanation cannot exceed 500 characters")]
        [Display(Name = "Explanation (Optional)")]
        public string? Explanation { get; set; }

        // For True/False questions
        [Display(Name = "Correct Answer")]
        public bool? TrueFalseAnswer { get; set; }

        // For Multiple Choice questions
        public List<CreateAnswerOptionViewModel> AnswerOptions { get; set; } = new List<CreateAnswerOptionViewModel>();

        // For validation
        public bool IsMultipleChoice => QuestionType == "multiple_choice";
        public bool IsTrueFalse => QuestionType == "true_false";
        public bool IsEssay => QuestionType == "essay";
        public bool IsFillBlank => QuestionType == "fill_blank";
    }

    public class CreateAnswerOptionViewModel
    {
        public string? OptionId { get; set; }

        // Remove [Required] attribute since we'll validate conditionally
        [StringLength(500, ErrorMessage = "Option text cannot exceed 500 characters")]
        [Display(Name = "Option Text")]
        public string OptionText { get; set; } = string.Empty;

        [Display(Name = "Is Correct Answer")]
        public bool IsCorrect { get; set; }

        [Range(1, 100, ErrorMessage = "Option order must be between 1 and 100")]
        [Display(Name = "Option Order")]
        public int OptionOrder { get; set; } = 1;
    }

    public class QuestionListViewModel
    {
        public string QuizId { get; set; } = string.Empty;
        public string QuizName { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public List<QuestionSummaryViewModel> Questions { get; set; } = new List<QuestionSummaryViewModel>();
    }

    public class QuestionSummaryViewModel
    {
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int Points { get; set; }
        public int QuestionOrder { get; set; }
        public int AnswerOptionsCount { get; set; }
        public DateTime QuestionCreatedAt { get; set; }
        public DateTime QuestionUpdatedAt { get; set; }
    }
}
