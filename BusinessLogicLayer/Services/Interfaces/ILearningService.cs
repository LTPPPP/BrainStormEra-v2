using DataAccessLayer.Models.ViewModels;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface ILearningService
    {
        // Course learning methods
        Task<CoursePlayerViewModel?> GetCoursePlayerAsync(string userId, string courseId);
        Task<bool> IsUserEnrolledInCourseAsync(string userId, string courseId);
        Task<bool> CanUserAccessCourseAsync(string userId, string courseId);

        // Lesson learning methods
        Task<LessonDetailViewModel?> GetLessonDetailAsync(string userId, string lessonId);
        Task<bool> CanUserAccessLessonAsync(string userId, string lessonId);
        Task<string?> GetNextLessonIdAsync(string userId, string currentLessonId);
        Task<string?> GetPreviousLessonIdAsync(string userId, string currentLessonId);

        // Progress tracking
        Task<bool> UpdateLessonProgressAsync(string userId, string lessonId, decimal completionPercentage, int timeSpentSeconds);
        Task<bool> MarkLessonAsCompletedAsync(string userId, string lessonId);
        Task<bool> UpdateCourseProgressAsync(string userId, string courseId);
        Task<decimal> CalculateCourseProgressAsync(string userId, string courseId);

        // Navigation
        Task<CourseNavigationViewModel?> GetCourseNavigationAsync(string userId, string courseId);
        Task<LearningDashboardViewModel> GetLearningDashboardAsync(string userId);

        // Lesson unlocking logic
        Task<bool> IsLessonUnlockedAsync(string userId, string lessonId);
        Task<List<string>> GetUnlockedLessonIdsAsync(string userId, string courseId);

        // Time tracking
        Task<bool> UpdateLessonTimeSpentAsync(string userId, string lessonId, int additionalSeconds);
        Task<TimeSpan> GetTotalTimeSpentInCourseAsync(string userId, string courseId);
        Task<TimeSpan> GetTimeSpentInLessonAsync(string userId, string lessonId);

        // Achievement and completion
        Task<bool> CheckCourseCompletionAsync(string userId, string courseId);
        Task<bool> CheckChapterCompletionAsync(string userId, string chapterId);
        Task<List<string>> GetCompletedLessonIdsAsync(string userId, string courseId);

        // Resume learning
        Task<string?> GetCurrentLessonIdAsync(string userId, string courseId);
        Task<bool> SetCurrentLessonAsync(string userId, string courseId, string lessonId);
    }
}