using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IQuestionRepo : IBaseRepo<Question>
    {
        // Question query methods
        Task<Question?> GetQuestionWithOptionsAsync(string questionId);
        Task<List<Question>> GetQuestionsByQuizAsync(string quizId);
        Task<List<Question>> GetQuestionsByCourseAsync(string courseId);
        Task<Question?> GetQuestionWithAnswersAsync(string questionId);

        // Question management methods
        Task<string> CreateQuestionAsync(Question question);
        Task<bool> UpdateQuestionAsync(Question question);
        Task<bool> DeleteQuestionAsync(string questionId, string authorId);
        Task<bool> UpdateQuestionOrderAsync(string questionId, int newOrder);
        Task<bool> ReorderQuestionsAsync(string quizId, List<(string questionId, int order)> questionOrders);

        // Answer options management
        Task<string> CreateAnswerOptionAsync(AnswerOption option);
        Task<bool> UpdateAnswerOptionAsync(AnswerOption option);
        Task<bool> DeleteAnswerOptionAsync(string optionId);
        Task<List<AnswerOption>> GetAnswerOptionsByQuestionAsync(string questionId);
        Task<AnswerOption?> GetCorrectAnswerAsync(string questionId);

        // Question content methods
        Task<bool> UpdateQuestionTextAsync(string questionId, string questionText);
        Task<bool> UpdateQuestionTypeAsync(string questionId, string questionType);
        Task<bool> UpdateQuestionPointsAsync(string questionId, int points);
        Task<bool> UpdateQuestionExplanationAsync(string questionId, string explanation);

        // Question statistics
        Task<decimal> GetQuestionAverageScoreAsync(string questionId);
        Task<int> GetQuestionAttemptsCountAsync(string questionId);
        Task<decimal> GetQuestionCorrectAnswerRateAsync(string questionId);
        Task<Dictionary<string, int>> GetAnswerOptionStatisticsAsync(string questionId);

        // Question validation
        Task<bool> HasValidAnswerOptionsAsync(string questionId);
        Task<bool> HasCorrectAnswerAsync(string questionId);
        Task<bool> IsQuestionCompleteAsync(string questionId);

        // Bulk operations
        Task<bool> BulkCreateQuestionsAsync(List<Question> questions);
        Task<bool> BulkUpdateQuestionOrdersAsync(List<(string questionId, int order)> questionOrders);
        Task<bool> BulkDeleteQuestionsAsync(List<string> questionIds, string authorId);
    }
}
