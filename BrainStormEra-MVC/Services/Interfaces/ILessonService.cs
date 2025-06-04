using BrainStormEra_MVC.Models;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface ILessonService
    {
        Task<bool> CreateLessonAsync(Lesson lesson);
        Task<IEnumerable<LessonType>> GetLessonTypesAsync();
        Task<int> GetNextLessonOrderAsync(string chapterId);
        Task<bool> IsDuplicateLessonNameAsync(string lessonName, string chapterId);
        Task<Chapter?> GetChapterByIdAsync(string chapterId);
        Task<bool> IsLessonOrderTakenAsync(string chapterId, int order);
        Task<bool> UpdateLessonOrdersAsync(string chapterId, int insertOrder);
        Task<IEnumerable<Lesson>> GetLessonsInChapterAsync(string chapterId);
        Task<bool> ValidateUnlockAfterLessonAsync(string chapterId, string? unlockAfterLessonId);
    }
}
