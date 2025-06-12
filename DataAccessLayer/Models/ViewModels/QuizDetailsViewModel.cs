namespace DataAccessLayer.Models.ViewModels
{
    public class QuizDetailsViewModel
    {
        public string QuizId { get; set; } = string.Empty;
        public string QuizName { get; set; } = string.Empty;
        public string? QuizDescription { get; set; }
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string? LessonId { get; set; }
        public string? LessonName { get; set; }
        public int? TimeLimit { get; set; }
        public decimal? PassingScore { get; set; }
        public DateTime QuizCreatedAt { get; set; }
        public DateTime QuizUpdatedAt { get; set; }
        public List<QuestionSummaryViewModel> Questions { get; set; } = new List<QuestionSummaryViewModel>();
        public int TotalQuestions => Questions.Count;
        public int TotalPoints => Questions.Sum(q => q.Points);
    }

    public class QuizPreviewViewModel
    {
        public string QuizId { get; set; } = string.Empty;
        public string QuizName { get; set; } = string.Empty;
        public string? QuizDescription { get; set; }
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int? TimeLimit { get; set; }
        public decimal? PassingScore { get; set; }
        public List<QuestionPreviewViewModel> Questions { get; set; } = new List<QuestionPreviewViewModel>();
        public int TotalQuestions => Questions.Count;
        public int TotalPoints => Questions.Sum(q => q.Points);
    }

    public class QuestionPreviewViewModel
    {
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int Points { get; set; }
        public int QuestionOrder { get; set; }
        public string? Explanation { get; set; }
        public List<AnswerOptionPreviewViewModel> AnswerOptions { get; set; } = new List<AnswerOptionPreviewViewModel>();
    }

    public class AnswerOptionPreviewViewModel
    {
        public string OptionId { get; set; } = string.Empty;
        public string OptionText { get; set; } = string.Empty;
        public int OptionOrder { get; set; }
    }
}
