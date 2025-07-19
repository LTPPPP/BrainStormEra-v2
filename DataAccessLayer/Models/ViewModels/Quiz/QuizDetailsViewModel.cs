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

    public class QuizStatisticsViewModel
    {
        public string QuizId { get; set; } = string.Empty;
        public string QuizName { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public int CompletedAttempts { get; set; }
        public int PassedAttempts { get; set; }
        public decimal PassRate { get; set; }
        public decimal AverageScore { get; set; }
        public int TotalQuestions { get; set; }
        public int? QuizStatus { get; set; }
        public bool IsPrerequisiteQuiz { get; set; }
        public bool BlocksLessonCompletion { get; set; }

        // Computed properties
        public string PassRateText => $"{PassRate:F1}%";
        public string AverageScoreText => $"{AverageScore:F1}%";
        public string StatusText => QuizStatus switch
        {
            0 => "Draft",
            1 => "Published",
            2 => "Active",
            3 => "Inactive",
            4 => "Archived",
            5 => "Suspended",
            6 => "Completed",
            7 => "In Progress",
            _ => "Unknown"
        };
    }
}
