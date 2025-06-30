using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    public class QuizTakeViewModel
    {
        public string QuizId { get; set; } = null!;
        public string QuizName { get; set; } = null!;
        public string? QuizDescription { get; set; }
        public int? TimeLimit { get; set; }
        public decimal? PassingScore { get; set; }
        public int? MaxAttempts { get; set; }
        public int? CurrentAttemptNumber { get; set; }
        public int? RemainingAttempts { get; set; }
        public bool CanAttempt { get; set; }
        public string? CourseId { get; set; }
        public string? CourseName { get; set; }
        public string? LessonId { get; set; }
        public string? LessonTitle { get; set; }
        public List<QuizQuestionViewModel> Questions { get; set; } = new List<QuizQuestionViewModel>();
        public DateTime? StartTime { get; set; }
        public string? AttemptId { get; set; }
        public bool IsPrerequisiteQuiz { get; set; }
        public bool BlocksLessonCompletion { get; set; }
    }

    public class QuizQuestionViewModel
    {
        public string QuestionId { get; set; } = null!;
        public string QuestionText { get; set; } = null!;
        public string? QuestionType { get; set; }
        public decimal? Points { get; set; }
        public int? QuestionOrder { get; set; }
        public string? Explanation { get; set; }
        public List<QuizAnswerOptionViewModel> AnswerOptions { get; set; } = new List<QuizAnswerOptionViewModel>();
        public string? SelectedOptionId { get; set; } // For single choice (true/false)
        public List<string> SelectedOptionIds { get; set; } = new List<string>(); // For multiple choice
        public string? AnswerText { get; set; }
    }

    public class QuizAnswerOptionViewModel
    {
        public string OptionId { get; set; } = null!;
        public string OptionText { get; set; } = null!;
        public int? OptionOrder { get; set; }
        public bool IsSelected { get; set; }
    }
}
