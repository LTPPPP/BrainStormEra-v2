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
    public class QuizRepo : BaseRepo<Quiz>, IQuizRepo
    {
        private readonly ILogger<QuizRepo>? _logger;

        public QuizRepo(BrainStormEraContext context, ILogger<QuizRepo>? logger = null)
            : base(context)
        {
            _logger = logger;
        }

        // Quiz query methods
        public async Task<Quiz?> GetQuizWithQuestionsAsync(string quizId)
        {
            try
            {
                return await _dbSet
                    .Include(q => q.Questions.OrderBy(qu => qu.QuestionOrder))
                        .ThenInclude(qu => qu.AnswerOptions.OrderBy(ao => ao.OptionOrder))
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l.Chapter)
                            .ThenInclude(c => c.Course)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving quiz with questions: {QuizId}", quizId);
                throw;
            }
        }

        public async Task<Quiz?> GetQuizWithDetailsAsync(string quizId)
        {
            try
            {
                return await _dbSet
                    .Include(q => q.Lesson!)
                        .ThenInclude(l => l.Chapter!)
                            .ThenInclude(c => c.Course)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving quiz with details: {QuizId}", quizId);
                throw;
            }
        }

        public async Task<List<Quiz>> GetQuizzesByLessonAsync(string lessonId)
        {
            try
            {
                return await _dbSet
                    .Where(q => q.LessonId == lessonId)
                    .Include(q => q.Questions)
                    .OrderBy(q => q.QuizCreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting quizzes by lesson: {LessonId}", lessonId);
                throw;
            }
        }

        public async Task<List<Quiz>> GetQuizzesByCourseAsync(string courseId)
        {
            try
            {
                return await _dbSet
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l.Chapter)
                    .Where(q => q.Lesson.Chapter.CourseId == courseId)
                    .OrderBy(q => q.Lesson.Chapter.ChapterOrder)
                    .ThenBy(q => q.Lesson.LessonOrder)
                    .ThenBy(q => q.QuizCreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting quizzes by course: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<Quiz?> GetQuizWithAttemptsAsync(string quizId, string? userId = null)
        {
            try
            {
                var query = _dbSet
                    .Include(q => q.QuizAttempts)
                        .ThenInclude(qa => qa.User)
                    .Include(q => q.Questions)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Include(q => q.QuizAttempts.Where(qa => qa.UserId == userId));
                }

                return await query.FirstOrDefaultAsync(q => q.QuizId == quizId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving quiz with attempts: {QuizId}", quizId);
                throw;
            }
        }

        // Quiz management methods
        public async Task<string> CreateQuizAsync(Quiz quiz)
        {
            try
            {
                await AddAsync(quiz);
                await SaveChangesAsync();
                return quiz.QuizId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating quiz");
                throw;
            }
        }

        public async Task<bool> UpdateQuizAsync(Quiz quiz)
        {
            try
            {
                await UpdateAsync(quiz);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating quiz");
                throw;
            }
        }

        public async Task<bool> DeleteQuizAsync(string quizId, string authorId)
        {
            try
            {
                var quiz = await _dbSet
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l.Chapter)
                            .ThenInclude(c => c.Course)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId && q.Lesson.Chapter.Course.AuthorId == authorId);

                if (quiz == null)
                    return false;

                await DeleteAsync(quiz);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting quiz");
                throw;
            }
        }

        public async Task<bool> UpdateQuizSettingsAsync(string quizId, int timeLimit, decimal passingScore, int maxAttempts)
        {
            try
            {
                var quiz = await GetByIdAsync(quizId);
                if (quiz == null)
                    return false;

                quiz.TimeLimit = timeLimit;
                quiz.PassingScore = passingScore;
                quiz.MaxAttempts = maxAttempts;
                quiz.QuizUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating quiz settings");
                throw;
            }
        }

        // Quiz attempt methods
        public async Task<QuizAttempt?> StartQuizAttemptAsync(string userId, string quizId)
        {
            try
            {
                // Check if user can take the quiz
                if (!await CanUserRetakeQuizAsync(userId, quizId))
                    return null;

                var attempt = new QuizAttempt
                {
                    AttemptId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    QuizId = quizId,
                    StartTime = DateTime.UtcNow
                };

                _context.QuizAttempts.Add(attempt);
                await SaveChangesAsync();
                return attempt;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error starting quiz attempt");
                throw;
            }
        }

        public async Task<bool> SubmitQuizAttemptAsync(string attemptId, List<UserAnswer> answers)
        {
            try
            {
                var attempt = await _context.QuizAttempts
                    .Include(qa => qa.Quiz)
                    .FirstOrDefaultAsync(qa => qa.AttemptId == attemptId);

                if (attempt == null)
                    return false;

                // Add user answers
                foreach (var answer in answers)
                {
                    answer.AttemptId = attemptId;
                    _context.UserAnswers.Add(answer);
                }

                // Complete the attempt
                attempt.EndTime = DateTime.UtcNow;

                // Calculate score
                var score = await CalculateQuizScoreAsync(attemptId);
                attempt.Score = score;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error submitting quiz attempt");
                throw;
            }
        }

        public async Task<QuizAttempt?> GetActiveAttemptAsync(string userId, string quizId)
        {
            try
            {
                return await _context.QuizAttempts
                    .FirstOrDefaultAsync(qa => qa.UserId == userId &&
                                              qa.QuizId == quizId &&
                                              qa.EndTime == null);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting active attempt");
                throw;
            }
        }

        public async Task<List<QuizAttempt>> GetUserAttemptsAsync(string userId, string quizId)
        {
            try
            {
                return await _context.QuizAttempts
                    .Where(qa => qa.UserId == userId && qa.QuizId == quizId)
                    .OrderByDescending(qa => qa.StartTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user attempts");
                throw;
            }
        }

        public async Task<QuizAttempt?> GetBestAttemptAsync(string userId, string quizId)
        {
            try
            {
                return await _context.QuizAttempts
                    .Where(qa => qa.UserId == userId &&
                                qa.QuizId == quizId &&
                                qa.EndTime != null &&
                                qa.Score.HasValue)
                    .OrderByDescending(qa => qa.Score)
                    .ThenByDescending(qa => qa.EndTime)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting best attempt");
                throw;
            }
        }

        // Quiz statistics
        public async Task<decimal> GetQuizAverageScoreAsync(string quizId)
        {
            try
            {
                var scores = await _context.QuizAttempts
                    .Where(qa => qa.QuizId == quizId &&
                                qa.EndTime != null &&
                                qa.Score.HasValue)
                    .Select(qa => qa.Score!.Value)
                    .ToListAsync();

                return scores.Any() ? scores.Average() : 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting quiz average score");
                throw;
            }
        }

        public async Task<int> GetQuizAttemptsCountAsync(string quizId)
        {
            try
            {
                return await _context.QuizAttempts
                    .CountAsync(qa => qa.QuizId == quizId && qa.EndTime != null);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting quiz attempts count");
                throw;
            }
        }

        public async Task<int> GetQuizPassedCountAsync(string quizId)
        {
            try
            {
                var quiz = await GetByIdAsync(quizId);
                if (quiz == null) return 0;

                return await _context.QuizAttempts
                    .CountAsync(qa => qa.QuizId == quizId &&
                                     qa.EndTime != null &&
                                     qa.Score >= quiz.PassingScore);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting quiz passed count");
                throw;
            }
        }

        public async Task<decimal> GetQuizPassRateAsync(string quizId)
        {
            try
            {
                var totalAttempts = await GetQuizAttemptsCountAsync(quizId);
                if (totalAttempts == 0) return 0;

                var passedAttempts = await GetQuizPassedCountAsync(quizId);
                return Math.Round((decimal)passedAttempts / totalAttempts * 100, 2);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting quiz pass rate");
                throw;
            }
        }

        public async Task<TimeSpan> GetQuizAverageCompletionTimeAsync(string quizId)
        {
            try
            {
                var completionTimes = await _context.QuizAttempts
                    .Where(qa => qa.QuizId == quizId &&
                                qa.StartTime != null &&
                                qa.EndTime != null)
                    .Select(qa => qa.EndTime!.Value - qa.StartTime!.Value)
                    .ToListAsync();

                if (!completionTimes.Any()) return TimeSpan.Zero;

                var averageTicks = (long)completionTimes.Average(t => t.Ticks);
                return new TimeSpan(averageTicks);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting quiz average completion time");
                throw;
            }
        }

        // Quiz validation
        public async Task<bool> IsQuizAccessibleAsync(string userId, string quizId)
        {
            try
            {
                var quiz = await _dbSet
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l.Chapter)
                            .ThenInclude(c => c.Course)
                                .ThenInclude(c => c.Enrollments)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null) return false;

                // Check if user is enrolled in the course
                var isEnrolled = quiz.Lesson.Chapter.Course.Enrollments
                    .Any(e => e.UserId == userId);

                return isEnrolled;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking quiz accessibility");
                throw;
            }
        }

        public async Task<bool> HasUserPassedQuizAsync(string userId, string quizId)
        {
            try
            {
                var quiz = await GetByIdAsync(quizId);
                if (quiz == null) return false;

                var bestAttempt = await GetBestAttemptAsync(userId, quizId);
                return bestAttempt?.Score >= quiz.PassingScore;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user passed quiz");
                throw;
            }
        }

        public async Task<bool> CanUserRetakeQuizAsync(string userId, string quizId)
        {
            try
            {
                var quiz = await GetByIdAsync(quizId);
                if (quiz == null) return false;

                var attemptCount = await GetUserAttemptCountAsync(userId, quizId);
                return attemptCount < quiz.MaxAttempts;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user can retake quiz");
                throw;
            }
        }

        public async Task<int> GetUserAttemptCountAsync(string userId, string quizId)
        {
            try
            {
                return await _context.QuizAttempts
                    .CountAsync(qa => qa.UserId == userId && qa.QuizId == quizId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user attempt count");
                throw;
            }
        }

        // Quiz grading
        public async Task<decimal> CalculateQuizScoreAsync(string attemptId)
        {
            try
            {
                var attempt = await _context.QuizAttempts
                    .Include(qa => qa.Quiz)
                        .ThenInclude(q => q.Questions)
                            .ThenInclude(qu => qu.AnswerOptions)
                    .Include(qa => qa.UserAnswers)
                    .FirstOrDefaultAsync(qa => qa.AttemptId == attemptId);

                if (attempt == null) return 0;

                decimal totalPoints = 0;
                decimal earnedPoints = 0;

                foreach (var question in attempt.Quiz.Questions)
                {
                    totalPoints += question.Points ?? 1;

                    var userAnswer = attempt.UserAnswers
                        .FirstOrDefault(ua => ua.QuestionId == question.QuestionId);

                    if (userAnswer != null)
                    {
                        var correctOption = question.AnswerOptions
                            .FirstOrDefault(ao => ao.IsCorrect == true);

                        if (correctOption != null && userAnswer.SelectedOptionId == correctOption.OptionId)
                        {
                            earnedPoints += question.Points ?? 1;
                        }
                    }
                }

                return totalPoints > 0 ? Math.Round((earnedPoints / totalPoints) * 100, 2) : 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating quiz score");
                throw;
            }
        }

        public async Task<bool> GradeQuizAttemptAsync(string attemptId)
        {
            try
            {
                var attempt = await _context.QuizAttempts
                    .FirstOrDefaultAsync(qa => qa.AttemptId == attemptId);

                if (attempt == null)
                    return false;

                var score = await CalculateQuizScoreAsync(attemptId);
                attempt.Score = score;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error grading quiz attempt");
                throw;
            }
        }

        public async Task<List<QuizAttempt>> GetUnGradedAttemptsAsync(string quizId)
        {
            try
            {
                return await _context.QuizAttempts
                    .Where(qa => qa.QuizId == quizId &&
                                qa.EndTime != null &&
                                qa.Score == null)
                    .Include(qa => qa.User)
                    .OrderBy(qa => qa.EndTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting ungraded attempts");
                throw;
            }
        }
    }
}
