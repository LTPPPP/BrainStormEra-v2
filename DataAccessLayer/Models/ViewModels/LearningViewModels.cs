using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    public class CoursePlayerViewModel
    {
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public required string CourseDescription { get; set; }
        public required string CourseImage { get; set; }
        public required string AuthorName { get; set; }
        public required string AuthorImage { get; set; }
        public decimal ProgressPercentage { get; set; }
        public string? CurrentLessonId { get; set; }
        public string? LastAccessedLessonId { get; set; }
        public List<ChapterLearningViewModel> Chapters { get; set; } = new List<ChapterLearningViewModel>();
        public List<string> Categories { get; set; } = new List<string>();
        public DateTime? EnrollmentDate { get; set; }
        public DateTime? LastAccessedDate { get; set; }
        public bool IsCompleted => ProgressPercentage >= 100;
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }

    public class ChapterLearningViewModel
    {
        public required string ChapterId { get; set; }
        public required string ChapterName { get; set; }
        public string? ChapterDescription { get; set; }
        public int ChapterOrder { get; set; }
        public decimal CompletionPercentage { get; set; }
        public bool IsCompleted => CompletionPercentage >= 100;
        public bool IsLocked { get; set; }
        public List<LessonLearningViewModel> Lessons { get; set; } = new List<LessonLearningViewModel>();
        public int TotalLessons => Lessons.Count;
        public int CompletedLessons => Lessons.Count(l => l.IsCompleted);
    }

    public class LessonLearningViewModel
    {
        public required string LessonId { get; set; }
        public required string LessonName { get; set; }
        public string? LessonDescription { get; set; }
        public int LessonOrder { get; set; }
        public required string LessonTypeId { get; set; }
        public required string LessonTypeName { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsLocked { get; set; }
        public bool IsCurrent { get; set; }
        public decimal CompletionPercentage { get; set; }
        public TimeSpan? TimeSpent { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public bool RequiresQuizPass { get; set; }
        public decimal? MinQuizScore { get; set; }
        public bool HasQuiz { get; set; }
        public bool HasPassedQuiz { get; set; }
    }

    public class LessonDetailViewModel
    {
        public required string LessonId { get; set; }
        public required string LessonName { get; set; }
        public string? LessonDescription { get; set; }
        public required string LessonContent { get; set; }
        public required string LessonTypeName { get; set; }
        public int LessonTypeId { get; set; }
        public int LessonOrder { get; set; }
        public required string ChapterId { get; set; }
        public required string ChapterName { get; set; }
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public bool IsCompleted { get; set; }
        public bool RequiresQuizPass { get; set; }
        public decimal? MinQuizScore { get; set; }
        public decimal CompletionPercentage { get; set; }
        public TimeSpan? TimeSpent { get; set; }

        // Navigation
        public string? PreviousLessonId { get; set; }
        public string? NextLessonId { get; set; }
        public bool HasPrevious => !string.IsNullOrEmpty(PreviousLessonId);
        public bool HasNext => !string.IsNullOrEmpty(NextLessonId);

        // Quiz info
        public List<QuizLearningViewModel> Quizzes { get; set; } = new List<QuizLearningViewModel>();
        public bool HasQuiz => Quizzes.Any();
        public bool HasPassedQuiz { get; set; }

        // Progress tracking
        public DateTime? StartedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public int MinTimeSpent { get; set; }
        public decimal MinCompletionPercentage { get; set; }
    }

    public class QuizLearningViewModel
    {
        public required string QuizId { get; set; }
        public required string QuizTitle { get; set; }
        public string? QuizDescription { get; set; }
        public int TotalQuestions { get; set; }
        public decimal PassingScore { get; set; }
        public int TimeLimit { get; set; }
        public int MaxAttempts { get; set; }
        public int AttemptsUsed { get; set; }
        public bool HasPassed { get; set; }
        public decimal? BestScore { get; set; }
        public bool CanRetake => AttemptsUsed < MaxAttempts || MaxAttempts == 0;
    }

    public class LearningProgressUpdateRequest
    {
        [Required]
        public required string LessonId { get; set; }
        [Range(0, 100)]
        public decimal CompletionPercentage { get; set; }
        public int TimeSpentSeconds { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class CourseNavigationViewModel
    {
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public List<ChapterNavigationViewModel> Chapters { get; set; } = new List<ChapterNavigationViewModel>();
        public string? CurrentLessonId { get; set; }
        public decimal OverallProgress { get; set; }
    }

    public class ChapterNavigationViewModel
    {
        public required string ChapterId { get; set; }
        public required string ChapterName { get; set; }
        public int ChapterOrder { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsLocked { get; set; }
        public decimal CompletionPercentage { get; set; }
        public List<LessonNavigationViewModel> Lessons { get; set; } = new List<LessonNavigationViewModel>();
    }

    public class LessonNavigationViewModel
    {
        public required string LessonId { get; set; }
        public required string LessonName { get; set; }
        public int LessonOrder { get; set; }
        public required string LessonTypeName { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsLocked { get; set; }
        public bool IsCurrent { get; set; }
        public bool HasQuiz { get; set; }
        public bool RequiresQuizPass { get; set; }
    }

    public class LearningDashboardViewModel
    {
        public List<CourseProgressViewModel> EnrolledCourses { get; set; } = new List<CourseProgressViewModel>();
        public List<LessonNavigationViewModel> RecentLessons { get; set; } = new List<LessonNavigationViewModel>();
        public int TotalCoursesEnrolled { get; set; }
        public int TotalCoursesCompleted { get; set; }
        public int TotalLessonsCompleted { get; set; }
        public decimal OverallProgress { get; set; }
    }

    public class CourseProgressViewModel
    {
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public required string CourseImage { get; set; }
        public required string AuthorName { get; set; }
        public decimal ProgressPercentage { get; set; }
        public string? CurrentLessonName { get; set; }
        public DateTime? LastAccessedDate { get; set; }
        public bool IsCompleted => ProgressPercentage >= 100;
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public DateTime EnrollmentDate { get; set; }
    }
}