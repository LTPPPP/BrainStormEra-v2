using BrainStormEra_MVC.Models.ViewModels;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface IChapterService
    {
        Task<string> CreateChapterAsync(CreateChapterViewModel model, string authorId);
        Task<CreateChapterViewModel?> GetCreateChapterViewModelAsync(string courseId, string authorId);
        Task<CreateChapterViewModel?> GetChapterForEditAsync(string chapterId, string authorId);
        Task<bool> UpdateChapterAsync(string chapterId, CreateChapterViewModel model, string authorId);
        Task<bool> DeleteChapterAsync(string chapterId, string authorId);
        Task<List<ChapterViewModel>> GetChaptersByCourseIdAsync(string courseId);
    }
}