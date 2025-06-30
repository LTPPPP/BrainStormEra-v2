using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    public class LearnManagementViewModel
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string CourseDescription { get; set; } = string.Empty;
        public string CourseImage { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public decimal ProgressPercentage { get; set; }
        public List<LearnChapterViewModel> Chapters { get; set; } = new List<LearnChapterViewModel>();
        public int TotalChapters => Chapters.Count;
        public int TotalLessons => Chapters.Sum(c => c.Lessons.Count);
        public int CompletedLessons => Chapters.Sum(c => c.Lessons.Count(l => l.IsCompleted));
        public bool IsEnrolled { get; set; }
    }

    public class LearnChapterViewModel
    {
        public string ChapterId { get; set; } = string.Empty;
        public string ChapterName { get; set; } = string.Empty;
        public string ChapterDescription { get; set; } = string.Empty;
        public int ChapterOrder { get; set; }
        public int ChapterNumber => ChapterOrder;
        public bool IsLocked { get; set; }
        public List<LearnLessonViewModel> Lessons { get; set; } = new List<LearnLessonViewModel>();
        public List<LearnQuizViewModel> Quizzes { get; set; } = new List<LearnQuizViewModel>();
        public bool IsCompleted => Lessons.All(l => l.IsCompleted);
        public decimal CompletionPercentage => Lessons.Count > 0 ? (Lessons.Count(l => l.IsCompleted) * 100m / Lessons.Count) : 0;
    }

    public class LearnLessonViewModel
    {
        public string LessonId { get; set; } = string.Empty;
        public string LessonName { get; set; } = string.Empty;
        public string LessonDescription { get; set; } = string.Empty;
        public int LessonOrder { get; set; }
        public string LessonType { get; set; } = string.Empty;
        public string LessonTypeIcon { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public bool IsMandatory { get; set; }
        public bool IsCompleted { get; set; }
        public int EstimatedDuration { get; set; } // in minutes
        public decimal ProgressPercentage { get; set; }

        // Prerequisite information
        public string? PrerequisiteLessonId { get; set; }
        public string? PrerequisiteLessonName { get; set; }
        public bool HasPrerequisite => !string.IsNullOrEmpty(PrerequisiteLessonId);
    }

    public class LearnQuizViewModel
    {
        public string QuizId { get; set; } = string.Empty;
        public string QuizName { get; set; } = string.Empty;
        public string QuizDescription { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public string LessonName { get; set; } = string.Empty;
        public int? TimeLimit { get; set; }
        public decimal? PassingScore { get; set; }
        public int? MaxAttempts { get; set; }
        public bool IsFinalQuiz { get; set; }
        public bool IsPrerequisiteQuiz { get; set; }
        public bool IsCompleted { get; set; }
        public int? AttemptsUsed { get; set; }
        public decimal? BestScore { get; set; }
        public string QuizTypeIcon => "fas fa-question-circle";
    }

    public class LearnManagementResult
    {
        public bool Success { get; set; }
        public bool IsNotFound { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public LearnManagementViewModel? ViewModel { get; set; }
    }

    // ViewModel for individual lesson learning
    public class LessonLearningViewModel
    {
        public string LessonId { get; set; } = string.Empty;
        public string LessonName { get; set; } = string.Empty;
        public string LessonDescription { get; set; } = string.Empty;
        public string LessonContent { get; set; } = string.Empty;
        public string LessonType { get; set; } = string.Empty;
        public int LessonTypeId { get; set; }
        public string LessonTypeIcon { get; set; } = string.Empty;
        public int EstimatedDuration { get; set; }

        // Course & Chapter info
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string CourseDescription { get; set; } = string.Empty;
        public string ChapterId { get; set; } = string.Empty;
        public string ChapterName { get; set; } = string.Empty;
        public int ChapterNumber { get; set; }

        // Navigation
        public string? PreviousLessonId { get; set; }
        public string? NextLessonId { get; set; }
        public string? PreviousLessonName { get; set; }
        public string? NextLessonName { get; set; }

        // Progress tracking
        public decimal? CurrentProgress { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsMandatory { get; set; }

        // Quiz specific (if lesson type is quiz)
        public bool HasQuiz { get; set; }
        public string? QuizId { get; set; }
        public decimal? MinQuizScore { get; set; }
        public bool? RequiresQuizPass { get; set; }

        // Sidebar data for course navigation
        public List<LearnChapterViewModel> Chapters { get; set; } = new List<LearnChapterViewModel>();
        public decimal ProgressPercentage => CurrentProgress ?? 0;
    }

    public class LessonLearningResult
    {
        public bool Success { get; set; }
        public bool IsNotFound { get; set; }
        public bool IsUnauthorized { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public LessonLearningViewModel? ViewModel { get; set; }
    }
}