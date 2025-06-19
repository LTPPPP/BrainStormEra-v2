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
        public bool IsLocked { get; set; }
        public List<LearnLessonViewModel> Lessons { get; set; } = new List<LearnLessonViewModel>();
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
    }

    public class LearnManagementResult
    {
        public bool Success { get; set; }
        public bool IsNotFound { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public LearnManagementViewModel? ViewModel { get; set; }
    }
}