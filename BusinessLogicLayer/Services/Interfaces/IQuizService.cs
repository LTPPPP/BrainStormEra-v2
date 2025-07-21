using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.DTOs.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using BusinessLogicLayer.Services.Implementations;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IQuizService
    {
        Task<ServiceResult<CreateQuizViewModel>> GetCreateQuizViewModelAsync(string chapterId, string userId);
        Task<ServiceResult<string>> CreateQuizAsync(CreateQuizViewModel model, string userId);
        Task<ServiceResult<CreateQuizViewModel>> GetEditQuizViewModelAsync(string quizId, string userId);
        Task<ServiceResult<bool>> UpdateQuizAsync(CreateQuizViewModel model, string userId);
        Task<ServiceResult<string>> DeleteQuizAsync(string quizId, string userId);
        Task<ServiceResult<QuizDetailsViewModel>> GetQuizDetailsAsync(string quizId, string userId);
        Task<ServiceResult<QuizPreviewViewModel>> GetQuizPreviewAsync(string quizId, string userId);
        Task<ServiceResult<bool>> UpdateQuizStatusAsync(string quizId, string userId, int newStatus);
        Task<ServiceResult<QuizStatisticsViewModel>> GetQuizStatisticsAsync(string quizId, string userId);
        Task CleanupAbandonedAttemptsAsync();
        // Controller-facing methods
        Task<CreateQuizResult> GetCreateQuizAsync(ClaimsPrincipal user, string chapterId);
        Task<CreateQuizResult> CreateQuizAsync(ClaimsPrincipal user, CreateQuizViewModel model, ModelStateDictionary modelState);
        Task<EditQuizResult> GetEditQuizAsync(ClaimsPrincipal user, string quizId);
        Task<EditQuizResult> UpdateQuizAsync(ClaimsPrincipal user, CreateQuizViewModel model, ModelStateDictionary modelState);
        Task<DeleteQuizResult> DeleteQuizAsync(ClaimsPrincipal user, string quizId);
        Task<QuizDetailsResult> GetQuizDetailsAsync(ClaimsPrincipal user, string quizId);
        Task<QuizPreviewResult> GetQuizPreviewAsync(ClaimsPrincipal user, string quizId);
        Task<QuizTakeResult> GetQuizTakeAsync(ClaimsPrincipal user, string quizId);
        Task<QuizSubmitResult> SubmitQuizAsync(ClaimsPrincipal user, QuizTakeSubmitViewModel model, ModelStateDictionary modelState);
        Task<QuizResultResult> GetQuizResultAsync(ClaimsPrincipal user, string attemptId);
        Task<GetQuizQuestionsResult> GetQuizQuestionsAsync(ClaimsPrincipal user, string quizId);
    }
}







