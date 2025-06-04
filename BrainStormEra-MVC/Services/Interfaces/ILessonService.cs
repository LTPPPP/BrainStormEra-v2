using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;

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
        Task<CreateLessonViewModel?> GetLessonForEditAsync(string lessonId, string authorId);
        Task<bool> UpdateLessonAsync(string lessonId, CreateLessonViewModel model, string authorId);
        Task<bool> IsDuplicateLessonNameForEditAsync(string lessonName, string chapterId, string currentLessonId);
    }
}
