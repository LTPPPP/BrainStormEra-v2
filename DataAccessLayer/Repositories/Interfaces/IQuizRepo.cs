using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IQuizRepo : IBaseRepo<Quiz>
    {
        // Quiz query methods
        Task<Quiz?> GetQuizWithQuestionsAsync(string quizId);
        Task<Quiz?> GetQuizWithDetailsAsync(string quizId);
        Task<List<Quiz>> GetQuizzesByLessonAsync(string lessonId);
        Task<List<Quiz>> GetQuizzesByCourseAsync(string courseId);
        Task<Quiz?> GetQuizWithAttemptsAsync(string quizId, string? userId = null);

        // Quiz management methods
        Task<string> CreateQuizAsync(Quiz quiz);
        Task<bool> UpdateQuizAsync(Quiz quiz);
        Task<bool> DeleteQuizAsync(string quizId, string authorId);
        Task<bool> UpdateQuizSettingsAsync(string quizId, int timeLimit, decimal passingScore, int maxAttempts);

        // Quiz attempt methods
        Task<QuizAttempt?> StartQuizAttemptAsync(string userId, string quizId);
        Task<bool> SubmitQuizAttemptAsync(string attemptId, List<UserAnswer> answers);
        Task<QuizAttempt?> GetActiveAttemptAsync(string userId, string quizId);
        Task<List<QuizAttempt>> GetUserAttemptsAsync(string userId, string quizId);
        Task<QuizAttempt?> GetBestAttemptAsync(string userId, string quizId);

        // Quiz statistics
        Task<decimal> GetQuizAverageScoreAsync(string quizId);
        Task<int> GetQuizAttemptsCountAsync(string quizId);
        Task<int> GetQuizPassedCountAsync(string quizId);
        Task<decimal> GetQuizPassRateAsync(string quizId);
        Task<TimeSpan> GetQuizAverageCompletionTimeAsync(string quizId);

        // Quiz validation
        Task<bool> IsQuizAccessibleAsync(string userId, string quizId);
        Task<bool> HasUserPassedQuizAsync(string userId, string quizId);
        Task<bool> CanUserRetakeQuizAsync(string userId, string quizId);
        Task<int> GetUserAttemptCountAsync(string userId, string quizId);

        // Quiz grading
        Task<decimal> CalculateQuizScoreAsync(string attemptId);
        Task<bool> GradeQuizAttemptAsync(string attemptId);
        Task<List<QuizAttempt>> GetUnGradedAttemptsAsync(string quizId);
    }
}
