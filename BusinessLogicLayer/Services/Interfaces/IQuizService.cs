using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.DTOs.Common;

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
    }
}







