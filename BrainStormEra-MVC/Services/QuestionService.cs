using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Services
{
    /// <summary>
    /// Service class that handles data access operations for Questions.
    /// This class implements the IQuestionService interface and contains 
    /// the core data manipulation logic.
    /// </summary>
    public class QuestionService : IQuestionService
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<QuestionService> _logger;

        public QuestionService(BrainStormEraContext context, ILogger<QuestionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Create Question Operations

        /// <summary>
        /// Get view model for creating a new question
        /// </summary>
        public async Task<CreateQuestionViewModel> GetCreateQuestionViewModelAsync(string quizId)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l!.Chapter)
                            .ThenInclude(c => c!.Course)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    throw new ArgumentException($"Quiz with ID {quizId} not found");
                }

                var nextOrder = await GetNextQuestionOrderAsync(quizId);

                var viewModel = new CreateQuestionViewModel
                {
                    QuizId = quizId,
                    QuizName = quiz.QuizName,
                    QuestionType = "multiple_choice",
                    Points = 1,
                    QuestionOrder = nextOrder,
                    AnswerOptions = new List<CreateAnswerOptionViewModel>
                    {
                        new CreateAnswerOptionViewModel { OptionOrder = 1, IsCorrect = false },
                        new CreateAnswerOptionViewModel { OptionOrder = 2, IsCorrect = false },
                        new CreateAnswerOptionViewModel { OptionOrder = 3, IsCorrect = false },
                        new CreateAnswerOptionViewModel { OptionOrder = 4, IsCorrect = false }
                    }
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting create question view model for quiz {QuizId}", quizId);
                throw;
            }
        }

        /// <summary>
        /// Create a new question in the database
        /// </summary>
        public async Task<Question> CreateQuestionAsync(CreateQuestionViewModel model)
        {
            try
            {
                var questionId = Guid.NewGuid().ToString();

                var question = new Question
                {
                    QuestionId = questionId,
                    QuizId = model.QuizId,
                    QuestionText = model.QuestionText,
                    QuestionType = model.QuestionType,
                    Points = model.Points,
                    QuestionOrder = model.QuestionOrder,
                    Explanation = model.Explanation,
                    QuestionCreatedAt = DateTime.UtcNow
                };

                _context.Questions.Add(question);

                // Add answer options based on question type
                if (model.QuestionType == "multiple_choice" && model.AnswerOptions != null)
                {
                    var validOptions = model.AnswerOptions
                        .Where(o => !string.IsNullOrWhiteSpace(o.OptionText))
                        .ToList();

                    foreach (var option in validOptions)
                    {
                        var answerOption = new AnswerOption
                        {
                            OptionId = Guid.NewGuid().ToString(),
                            QuestionId = questionId,
                            OptionText = option.OptionText,
                            IsCorrect = option.IsCorrect,
                            OptionOrder = option.OptionOrder
                        };
                        _context.AnswerOptions.Add(answerOption);
                    }
                }
                else if (model.QuestionType == "true_false")
                {
                    var trueOption = new AnswerOption
                    {
                        OptionId = Guid.NewGuid().ToString(),
                        QuestionId = questionId,
                        OptionText = "True",
                        IsCorrect = model.TrueFalseAnswer == true,
                        OptionOrder = 1
                    };

                    var falseOption = new AnswerOption
                    {
                        OptionId = Guid.NewGuid().ToString(),
                        QuestionId = questionId,
                        OptionText = "False",
                        IsCorrect = model.TrueFalseAnswer == false,
                        OptionOrder = 2
                    };

                    _context.AnswerOptions.Add(trueOption);
                    _context.AnswerOptions.Add(falseOption);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Question {QuestionId} created successfully in quiz {QuizId}",
                    questionId, model.QuizId);

                return question;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question for quiz {QuizId}", model.QuizId);
                throw;
            }
        }

        #endregion

        #region Edit Question Operations

        /// <summary>
        /// Get view model for editing an existing question
        /// </summary>
        public async Task<CreateQuestionViewModel?> GetEditQuestionViewModelAsync(string questionId)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.Quiz)
                    .Include(q => q.AnswerOptions)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (question == null)
                {
                    return null;
                }

                var viewModel = new CreateQuestionViewModel
                {
                    QuestionId = question.QuestionId,
                    QuizId = question.QuizId,
                    QuizName = question.Quiz?.QuizName ?? "",
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType ?? "multiple_choice",
                    Points = (int)(question.Points ?? 1),
                    QuestionOrder = question.QuestionOrder ?? 1,
                    Explanation = question.Explanation,
                    AnswerOptions = new List<CreateAnswerOptionViewModel>()
                };

                // Handle answer options based on question type
                if (question.QuestionType == "multiple_choice")
                {
                    viewModel.AnswerOptions = question.AnswerOptions?
                        .OrderBy(o => o.OptionOrder)
                        .Select(o => new CreateAnswerOptionViewModel
                        {
                            OptionId = o.OptionId,
                            OptionText = o.OptionText,
                            IsCorrect = o.IsCorrect ?? false,
                            OptionOrder = o.OptionOrder ?? 1
                        }).ToList() ?? new List<CreateAnswerOptionViewModel>();

                    // Ensure we have at least 4 options for editing
                    while (viewModel.AnswerOptions.Count < 4)
                    {
                        viewModel.AnswerOptions.Add(new CreateAnswerOptionViewModel
                        {
                            OptionOrder = viewModel.AnswerOptions.Count + 1,
                            IsCorrect = false
                        });
                    }
                }
                else if (question.QuestionType == "true_false")
                {
                    var trueOption = question.AnswerOptions?.FirstOrDefault(o => o.OptionText == "True");
                    viewModel.TrueFalseAnswer = trueOption?.IsCorrect ?? false;
                }

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting edit question view model for question {QuestionId}", questionId);
                throw;
            }
        }

        /// <summary>
        /// Update an existing question in the database
        /// </summary>
        public async Task<bool> UpdateQuestionAsync(CreateQuestionViewModel model)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.AnswerOptions)
                    .FirstOrDefaultAsync(q => q.QuestionId == model.QuestionId);

                if (question == null)
                {
                    return false;
                }

                // Update question properties
                question.QuestionText = model.QuestionText;
                question.QuestionType = model.QuestionType;
                question.Points = model.Points;
                question.QuestionOrder = model.QuestionOrder;
                question.Explanation = model.Explanation;

                // Remove existing answer options
                if (question.AnswerOptions != null)
                {
                    _context.AnswerOptions.RemoveRange(question.AnswerOptions);
                }

                // Add new answer options based on question type
                if (model.QuestionType == "multiple_choice" && model.AnswerOptions != null)
                {
                    var validOptions = model.AnswerOptions
                        .Where(o => !string.IsNullOrWhiteSpace(o.OptionText))
                        .ToList();

                    foreach (var option in validOptions)
                    {
                        var answerOption = new AnswerOption
                        {
                            OptionId = Guid.NewGuid().ToString(),
                            QuestionId = model.QuestionId!,
                            OptionText = option.OptionText,
                            IsCorrect = option.IsCorrect,
                            OptionOrder = option.OptionOrder
                        };
                        _context.AnswerOptions.Add(answerOption);
                    }
                }
                else if (model.QuestionType == "true_false")
                {
                    var trueOption = new AnswerOption
                    {
                        OptionId = Guid.NewGuid().ToString(),
                        QuestionId = model.QuestionId!,
                        OptionText = "True",
                        IsCorrect = model.TrueFalseAnswer == true,
                        OptionOrder = 1
                    };

                    var falseOption = new AnswerOption
                    {
                        OptionId = Guid.NewGuid().ToString(),
                        QuestionId = model.QuestionId!,
                        OptionText = "False",
                        IsCorrect = model.TrueFalseAnswer == false,
                        OptionOrder = 2
                    };

                    _context.AnswerOptions.Add(trueOption);
                    _context.AnswerOptions.Add(falseOption);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Question {QuestionId} updated successfully", model.QuestionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question {QuestionId}", model.QuestionId);
                throw;
            }
        }

        #endregion

        #region Delete Question Operations

        /// <summary>
        /// Delete a question from the database
        /// </summary>
        public async Task<bool> DeleteQuestionAsync(string questionId)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.AnswerOptions)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (question == null)
                {
                    return false;
                }

                // Remove answer options first
                if (question.AnswerOptions != null)
                {
                    _context.AnswerOptions.RemoveRange(question.AnswerOptions);
                }

                // Remove the question
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Question {QuestionId} deleted successfully", questionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question {QuestionId}", questionId);
                throw;
            }
        }

        #endregion

        #region Duplicate Question Operations

        /// <summary>
        /// Duplicate an existing question
        /// </summary>
        public async Task<bool> DuplicateQuestionAsync(string questionId)
        {
            try
            {
                var originalQuestion = await _context.Questions
                    .Include(q => q.AnswerOptions)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (originalQuestion == null)
                {
                    return false;
                }

                var newQuestionId = Guid.NewGuid().ToString();
                var nextOrder = await GetNextQuestionOrderAsync(originalQuestion.QuizId);

                var duplicatedQuestion = new Question
                {
                    QuestionId = newQuestionId,
                    QuizId = originalQuestion.QuizId,
                    QuestionText = $"Copy of {originalQuestion.QuestionText}",
                    QuestionType = originalQuestion.QuestionType,
                    Points = originalQuestion.Points,
                    QuestionOrder = nextOrder,
                    Explanation = originalQuestion.Explanation,
                    QuestionCreatedAt = DateTime.UtcNow
                };

                _context.Questions.Add(duplicatedQuestion);

                // Duplicate answer options
                if (originalQuestion.AnswerOptions != null)
                {
                    foreach (var originalOption in originalQuestion.AnswerOptions)
                    {
                        var duplicatedOption = new AnswerOption
                        {
                            OptionId = Guid.NewGuid().ToString(),
                            QuestionId = newQuestionId,
                            OptionText = originalOption.OptionText,
                            IsCorrect = originalOption.IsCorrect,
                            OptionOrder = originalOption.OptionOrder
                        };
                        _context.AnswerOptions.Add(duplicatedOption);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Question {QuestionId} duplicated successfully as {NewQuestionId}",
                    questionId, newQuestionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error duplicating question {QuestionId}", questionId);
                throw;
            }
        }

        #endregion

        #region Reorder Questions Operations

        /// <summary>
        /// Reorder questions within a quiz
        /// </summary>
        public async Task<bool> ReorderQuestionsAsync(string quizId, List<string> questionIds)
        {
            try
            {
                var questions = await _context.Questions
                    .Where(q => q.QuizId == quizId && questionIds.Contains(q.QuestionId))
                    .ToListAsync();

                for (int i = 0; i < questionIds.Count; i++)
                {
                    var question = questions.FirstOrDefault(q => q.QuestionId == questionIds[i]);
                    if (question != null)
                    {
                        question.QuestionOrder = i + 1;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Questions reordered successfully for quiz {QuizId}", quizId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering questions for quiz {QuizId}", quizId);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get the next question order number for a quiz
        /// </summary>
        public async Task<int> GetNextQuestionOrderAsync(string quizId)
        {
            try
            {
                var maxOrder = await _context.Questions
                    .Where(q => q.QuizId == quizId)
                    .MaxAsync(q => (int?)q.QuestionOrder) ?? 0;

                return maxOrder + 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next question order for quiz {QuizId}", quizId);
                return 1; // Default to 1 if error occurs
            }
        }

        /// <summary>
        /// Get quiz with authorization check
        /// </summary>
        public async Task<Quiz?> GetQuizWithAuthorizationAsync(string quizId, string userId)
        {
            try
            {
                return await _context.Quizzes
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l!.Chapter)
                            .ThenInclude(c => c!.Course)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.QuizId == quizId &&
                                            q.Lesson!.Chapter!.Course!.AuthorId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz with authorization for quiz {QuizId} and user {UserId}",
                    quizId, userId);
                return null;
            }
        }

        /// <summary>
        /// Get question with authorization check
        /// </summary>
        public async Task<Question?> GetQuestionWithAuthorizationAsync(string questionId, string userId)
        {
            try
            {
                return await _context.Questions
                    .Include(q => q.Quiz)
                        .ThenInclude(quiz => quiz!.Lesson)
                            .ThenInclude(l => l!.Chapter)
                                .ThenInclude(c => c!.Course)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId &&
                                            q.Quiz!.Lesson!.Chapter!.Course!.AuthorId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question with authorization for question {QuestionId} and user {UserId}",
                    questionId, userId);
                return null;
            }
        }

        #endregion
    }
}
