using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Implementations;

namespace BusinessLogicLayer.Services.Interfaces
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
        Task<bool> DeleteLessonAsync(string lessonId, string authorId);
        Task<LessonLearningResult> GetLessonLearningDataAsync(string lessonId, string userId);
        Task<bool> MarkLessonAsCompletedAsync(string userId, string lessonId);
        Task<bool> IsLessonCompletedAsync(string userId, string lessonId);
        Task<Lesson?> GetLessonWithDetailsAsync(string lessonId);
        Task<decimal> GetLessonCompletionPercentageAsync(string userId, string courseId);
        Task<bool> UpdateEnrollmentProgressAsync(string userId, string courseId, decimal progressPercentage, string? currentLessonId = null);
        // Controller-facing methods
        Task<LessonService.SelectLessonTypeResult> GetSelectLessonTypeViewModelAsync(string chapterId);
        Task<LessonService.SelectLessonTypeResult> ProcessSelectLessonTypeAsync(SelectLessonTypeViewModel model, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState);
        Task<LessonService.CreateLessonViewResult> GetCreateLessonViewModelAsync(string chapterId, int lessonTypeId);
        Task<LessonService.CreateLessonResult> ProcessCreateLessonAsync(CreateLessonViewModel model, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState);
        Task<LessonService.EditLessonResult> GetEditLessonViewModelAsync(string lessonId, string userId);
        Task<LessonService.UpdateLessonResult> ProcessUpdateLessonAsync(string lessonId, CreateLessonViewModel model, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, string userId);
        Task<LessonService.DeleteLessonResult> ProcessDeleteLessonAsync(string lessonId, string userId, string courseId);
    }
}







