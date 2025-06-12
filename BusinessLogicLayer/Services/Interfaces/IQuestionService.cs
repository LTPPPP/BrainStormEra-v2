using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using System.Security.Claims;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IQuestionService
    {
        Task<CreateQuestionViewModel> GetCreateQuestionViewModelAsync(string quizId);
        Task<Question> CreateQuestionAsync(CreateQuestionViewModel model);
        Task<CreateQuestionViewModel?> GetEditQuestionViewModelAsync(string questionId);
        Task<bool> UpdateQuestionAsync(CreateQuestionViewModel model);
        Task<bool> DeleteQuestionAsync(string questionId);
        Task<bool> DuplicateQuestionAsync(string questionId);
        Task<bool> ReorderQuestionsAsync(string quizId, List<string> questionIds);
        Task<int> GetNextQuestionOrderAsync(string quizId);
        Task<Quiz?> GetQuizWithAuthorizationAsync(string quizId, string userId);
        Task<Question?> GetQuestionWithAuthorizationAsync(string questionId, string userId);
    }
}







