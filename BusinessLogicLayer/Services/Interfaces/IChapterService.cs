using DataAccessLayer.Models.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BusinessLogicLayer.Services.Implementations;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IChapterService
    {
        // Controller-facing methods
        Task<CreateChapterResult> GetCreateChapterViewModelAsync(ClaimsPrincipal user, string courseId);
        Task<CreateChapterResult> CreateChapterAsync(ClaimsPrincipal user, CreateChapterViewModel model);
        Task<DeleteChapterResult> DeleteChapterAsync(ClaimsPrincipal user, string chapterId, string courseId);
        Task<EditChapterResult> GetChapterForEditAsync(ClaimsPrincipal user, string chapterId);
        Task<EditChapterResult> UpdateChapterAsync(ClaimsPrincipal user, string chapterId, CreateChapterViewModel model);
        Task<List<ChapterViewModel>> GetChaptersByCourseIdAsync(string courseId);
    }

    public class CreateChapterResult
    {
        public bool Success { get; set; }
        public CreateChapterViewModel? ViewModel { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RouteValues { get; set; }
        public bool ReturnView { get; set; }
        public Dictionary<string, List<string>>? ValidationErrors { get; set; }
    }

    public class EditChapterResult
    {
        public bool Success { get; set; }
        public CreateChapterViewModel? ViewModel { get; set; }
        public string? ChapterId { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RouteValues { get; set; }
        public bool ReturnView { get; set; }
        public Dictionary<string, List<string>>? ValidationErrors { get; set; }
    }

    public class DeleteChapterResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}







