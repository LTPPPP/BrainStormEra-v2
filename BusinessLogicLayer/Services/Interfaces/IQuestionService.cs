using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BusinessLogicLayer.Services.Implementations;

namespace BusinessLogicLayer.Services.Interfaces
{
    // Move result types to the interface namespace for direct reference
    public class CreateQuestionResult
    {
        public bool Success { get; set; }
        public CreateQuestionViewModel? ViewModel { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RedirectValues { get; set; }
        public Dictionary<string, List<string>>? ValidationErrors { get; set; }
        public bool ReturnView { get; set; }
        public bool RedirectToLogin { get; set; }
    }

    public class EditQuestionResult
    {
        public bool Success { get; set; }
        public CreateQuestionViewModel? ViewModel { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RedirectValues { get; set; }
        public Dictionary<string, List<string>>? ValidationErrors { get; set; }
        public bool ReturnView { get; set; }
        public bool RedirectToLogin { get; set; }
    }

    public class DeleteQuestionResult
    {
        public bool Success { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RedirectValues { get; set; }
        public bool RedirectToLogin { get; set; }
    }

    public class DuplicateQuestionResult
    {
        public bool Success { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RedirectValues { get; set; }
    }

    public class ReorderQuestionsResult
    {
        public bool Success { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BusinessValidationResult
    {
        public bool IsValid { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; } = new();
    }

    public interface IQuestionService
    {
        // Controller-facing methods
        Task<CreateQuestionResult> GetCreateQuestionViewModelAsync(ClaimsPrincipal user, string quizId);
        Task<CreateQuestionResult> CreateQuestionAsync(ClaimsPrincipal user, CreateQuestionViewModel model, ModelStateDictionary modelState);
        Task<EditQuestionResult> GetEditQuestionViewModelAsync(ClaimsPrincipal user, string questionId);
        Task<EditQuestionResult> UpdateQuestionAsync(ClaimsPrincipal user, CreateQuestionViewModel model, ModelStateDictionary modelState);
        Task<DeleteQuestionResult> DeleteQuestionAsync(ClaimsPrincipal user, string questionId);
        Task<DuplicateQuestionResult> DuplicateQuestionAsync(ClaimsPrincipal user, string questionId);
        Task<ReorderQuestionsResult> ReorderQuestionsAsync(ClaimsPrincipal user, string quizId, List<string> questionIds);
        Task<int> GetNextQuestionOrderAsync(string quizId);
        Task<Quiz?> GetQuizWithAuthorizationAsync(string quizId, string userId);
        Task<Question?> GetQuestionWithAuthorizationAsync(string questionId, string userId);
    }
}







