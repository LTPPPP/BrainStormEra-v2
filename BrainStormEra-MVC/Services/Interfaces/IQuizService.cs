using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_MVC.Services.Interfaces
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

    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string Error { get; set; } = "";

        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T> { IsSuccess = true, Data = data };
        }

        public static ServiceResult<T> Failure(string error)
        {
            return new ServiceResult<T> { IsSuccess = false, Error = error };
        }
    }
}
