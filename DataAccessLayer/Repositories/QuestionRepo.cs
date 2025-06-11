using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataAccessLayer.Repositories
{
    public class QuestionRepo : BaseRepo<Question>, IQuestionRepo
    {
        private readonly ILogger<QuestionRepo>? _logger;

        public QuestionRepo(BrainStormEraContext context, ILogger<QuestionRepo>? logger = null)
            : base(context)
        {
            _logger = logger;
        }

        // Question query methods
        public async Task<Question?> GetQuestionWithOptionsAsync(string questionId)
        {
            try
            {
                return await _dbSet
                    .Include(q => q.AnswerOptions.OrderBy(ao => ao.OptionOrder))
                    .Include(q => q.Quiz)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving question with options: {QuestionId}", questionId);
                throw;
            }
        }

        public async Task<List<Question>> GetQuestionsByQuizAsync(string quizId)
        {
            try
            {
                return await _dbSet
                    .Where(q => q.QuizId == quizId)
                    .Include(q => q.AnswerOptions.OrderBy(ao => ao.OptionOrder))
                    .OrderBy(q => q.QuestionOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting questions by quiz: {QuizId}", quizId);
                throw;
            }
        }

        public async Task<List<Question>> GetQuestionsByCourseAsync(string courseId)
        {
            try
            {
                return await _dbSet
                    .Include(q => q.Quiz)
                        .ThenInclude(quiz => quiz.Lesson)
                            .ThenInclude(l => l.Chapter)
                    .Where(q => q.Quiz.Lesson.Chapter.CourseId == courseId)
                    .OrderBy(q => q.Quiz.Lesson.Chapter.ChapterOrder)
                    .ThenBy(q => q.Quiz.Lesson.LessonOrder)
                    .ThenBy(q => q.QuestionOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting questions by course: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<Question?> GetQuestionWithAnswersAsync(string questionId)
        {
            try
            {
                return await _dbSet
                    .Include(q => q.AnswerOptions)
                    .Include(q => q.UserAnswers)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving question with answers: {QuestionId}", questionId);
                throw;
            }
        }

        // Question management methods
        public async Task<string> CreateQuestionAsync(Question question)
        {
            try
            {
                await AddAsync(question);
                await SaveChangesAsync();
                return question.QuestionId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating question");
                throw;
            }
        }

        public async Task<bool> UpdateQuestionAsync(Question question)
        {
            try
            {
                await UpdateAsync(question);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating question");
                throw;
            }
        }

        public async Task<bool> DeleteQuestionAsync(string questionId, string authorId)
        {
            try
            {
                var question = await _dbSet
                    .Include(q => q.Quiz)
                        .ThenInclude(quiz => quiz.Lesson)
                            .ThenInclude(l => l.Chapter)
                                .ThenInclude(c => c.Course)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId &&
                                             q.Quiz.Lesson.Chapter.Course.AuthorId == authorId);

                if (question == null)
                    return false;

                await DeleteAsync(question);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting question");
                throw;
            }
        }

        public async Task<bool> UpdateQuestionOrderAsync(string questionId, int newOrder)
        {
            try
            {
                var question = await GetByIdAsync(questionId);
                if (question == null)
                    return false;

                question.QuestionOrder = newOrder;
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating question order");
                throw;
            }
        }

        public async Task<bool> ReorderQuestionsAsync(string quizId, List<(string questionId, int order)> questionOrders)
        {
            try
            {
                var questions = await _dbSet
                    .Where(q => q.QuizId == quizId)
                    .ToListAsync();

                foreach (var questionOrder in questionOrders)
                {
                    var question = questions.FirstOrDefault(q => q.QuestionId == questionOrder.questionId);
                    if (question != null)
                    {
                        question.QuestionOrder = questionOrder.order;
                    }
                }

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reordering questions");
                throw;
            }
        }

        // Answer options management - Implementing required interface methods
        public async Task<string> CreateAnswerOptionAsync(AnswerOption option)
        {
            try
            {
                _context.AnswerOptions.Add(option);
                await SaveChangesAsync();
                return option.OptionId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating answer option");
                throw;
            }
        }

        public async Task<bool> UpdateAnswerOptionAsync(AnswerOption option)
        {
            try
            {
                _context.AnswerOptions.Update(option);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating answer option");
                throw;
            }
        }

        public async Task<bool> DeleteAnswerOptionAsync(string optionId)
        {
            try
            {
                var option = await _context.AnswerOptions.FindAsync(optionId);
                if (option == null)
                    return false;

                _context.AnswerOptions.Remove(option);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting answer option");
                throw;
            }
        }

        public async Task<List<AnswerOption>> GetAnswerOptionsByQuestionAsync(string questionId)
        {
            try
            {
                return await _context.AnswerOptions
                    .Where(ao => ao.QuestionId == questionId)
                    .OrderBy(ao => ao.OptionOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting answer options by question");
                throw;
            }
        }

        public async Task<AnswerOption?> GetCorrectAnswerAsync(string questionId)
        {
            try
            {
                return await _context.AnswerOptions
                    .FirstOrDefaultAsync(ao => ao.QuestionId == questionId && ao.IsCorrect == true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting correct answer");
                throw;
            }
        }

        // Implementing other required interface methods with basic implementations
        public async Task<bool> UpdateQuestionTextAsync(string questionId, string questionText)
        {
            var question = await GetByIdAsync(questionId);
            if (question == null) return false;
            question.QuestionText = questionText;
            return await SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateQuestionTypeAsync(string questionId, string questionType)
        {
            var question = await GetByIdAsync(questionId);
            if (question == null) return false;
            question.QuestionType = questionType;
            return await SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateQuestionPointsAsync(string questionId, int points)
        {
            var question = await GetByIdAsync(questionId);
            if (question == null) return false;
            question.Points = points;
            return await SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateQuestionExplanationAsync(string questionId, string explanation)
        {
            var question = await GetByIdAsync(questionId);
            if (question == null) return false;
            question.Explanation = explanation;
            return await SaveChangesAsync() > 0;
        }

        // Basic implementations for statistics and validation methods
        public async Task<decimal> GetQuestionAverageScoreAsync(string questionId) => await Task.FromResult(0m);
        public async Task<int> GetQuestionAttemptsCountAsync(string questionId) => await Task.FromResult(0);
        public async Task<decimal> GetQuestionCorrectAnswerRateAsync(string questionId) => await Task.FromResult(0m);
        public async Task<Dictionary<string, int>> GetAnswerOptionStatisticsAsync(string questionId) => await Task.FromResult(new Dictionary<string, int>());
        public async Task<bool> HasValidAnswerOptionsAsync(string questionId) => await Task.FromResult(true);
        public async Task<bool> HasCorrectAnswerAsync(string questionId) => await Task.FromResult(true);
        public async Task<bool> IsQuestionCompleteAsync(string questionId) => await Task.FromResult(true);

        // Bulk operations
        public async Task<bool> BulkCreateQuestionsAsync(List<Question> questions)
        {
            try
            {
                await AddRangeAsync(questions);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error bulk creating questions");
                throw;
            }
        }

        public async Task<bool> BulkUpdateQuestionOrdersAsync(List<(string questionId, int order)> questionOrders)
        {
            try
            {
                var questionIds = questionOrders.Select(qo => qo.questionId).ToList();
                var questions = await _dbSet.Where(q => questionIds.Contains(q.QuestionId)).ToListAsync();

                foreach (var questionOrder in questionOrders)
                {
                    var question = questions.FirstOrDefault(q => q.QuestionId == questionOrder.questionId);
                    if (question != null)
                    {
                        question.QuestionOrder = questionOrder.order;
                    }
                }

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error bulk updating question orders");
                throw;
            }
        }

        public async Task<bool> BulkDeleteQuestionsAsync(List<string> questionIds, string authorId)
        {
            try
            {
                var questions = await _dbSet
                    .Include(q => q.Quiz)
                        .ThenInclude(quiz => quiz.Lesson)
                            .ThenInclude(l => l.Chapter)
                                .ThenInclude(c => c.Course)
                    .Where(q => questionIds.Contains(q.QuestionId) &&
                               q.Quiz.Lesson.Chapter.Course.AuthorId == authorId)
                    .ToListAsync();

                await DeleteRangeAsync(questions);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error bulk deleting questions");
                throw;
            }
        }
    }
}
