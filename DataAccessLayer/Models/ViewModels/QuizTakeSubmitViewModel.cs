using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    public class QuizTakeSubmitViewModel
    {
        [Required]
        public string QuizId { get; set; } = null!;

        [Required]
        public string AttemptId { get; set; } = null!;

        public DateTime StartTime { get; set; }
        public DateTime SubmissionTime { get; set; }

        public List<UserAnswerSubmissionViewModel> UserAnswers { get; set; } = new List<UserAnswerSubmissionViewModel>();
    }

    public class UserAnswerSubmissionViewModel
    {
        [Required]
        public string QuestionId { get; set; } = null!;

        public string? SelectedOptionId { get; set; }

        public string? AnswerText { get; set; }
    }
}
