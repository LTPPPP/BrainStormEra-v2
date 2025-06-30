using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models.ViewModels
{
    public class QuizResultViewModel
    {
        public string AttemptId { get; set; } = null!;
        public string QuizId { get; set; } = null!;
        public string QuizName { get; set; } = null!;
        public string? QuizDescription { get; set; }
        public decimal? Score { get; set; }
        public decimal? TotalPoints { get; set; }
        public decimal? PercentageScore { get; set; }
        public decimal? PassingScore { get; set; }
        public bool? IsPassed { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? TimeSpent { get; set; }
        public int? AttemptNumber { get; set; }
        public int? MaxAttempts { get; set; }
        public int? RemainingAttempts { get; set; }
        public string? CourseId { get; set; }
        public string? CourseName { get; set; }
        public string? LessonId { get; set; }
        public string? LessonTitle { get; set; }
        public bool CanRetake { get; set; }
        public bool IsPrerequisiteQuiz { get; set; }
        public bool BlocksLessonCompletion { get; set; }
        public List<QuestionResultViewModel> QuestionResults { get; set; } = new List<QuestionResultViewModel>();
    }

    public class QuestionResultViewModel
    {
        public string QuestionId { get; set; } = null!;
        public string QuestionText { get; set; } = null!;
        public string? QuestionType { get; set; }
        public decimal? Points { get; set; }
        public decimal? PointsEarned { get; set; }
        public bool? IsCorrect { get; set; }
        public string? UserAnswer { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        public List<AnswerOptionResultViewModel> AnswerOptions { get; set; } = new List<AnswerOptionResultViewModel>();
    }

    public class AnswerOptionResultViewModel
    {
        public string OptionId { get; set; } = null!;
        public string OptionText { get; set; } = null!;
        public bool IsCorrect { get; set; }
        public bool IsSelected { get; set; }
    }
}
