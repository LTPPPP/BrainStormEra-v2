using Microsoft.Extensions.Logging;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Implementation class that handles complex business logic for Question operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// authorization, validation, and error handling.
    /// </summary>
    public class QuestionService : IQuestionService
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<QuestionService> _logger;

        public QuestionService(
            BrainStormEraContext context,
            ILogger<QuestionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region IQuestionService Implementation

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

        #endregion

        #region Question Creation Operations

        /// <summary>
        /// Get create question view model with authorization and validation
        /// </summary>
        public async Task<BusinessLogicLayer.Services.Interfaces.CreateQuestionResult> GetCreateQuestionViewModelAsync(ClaimsPrincipal user, string quizId)
        {
            try
            {
                _logger.LogInformation("Getting create question view model for quiz {QuizId}", quizId);

                if (string.IsNullOrEmpty(quizId))
                {
                    _logger.LogWarning("Quiz ID is null or empty");
                    return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Quiz ID is required"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required",
                        RedirectToLogin = true
                    };
                }

                // Check authorization
                var quiz = await GetQuizWithAuthorizationAsync(quizId, userId);
                if (quiz == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found or user {UserId} not authorized", quizId, userId);
                    return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Quiz not found or you are not authorized to create questions for this quiz"
                    };
                }

                var viewModel = await GetCreateQuestionViewModelAsync(quizId);

                return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting create question view model for quiz {QuizId}", quizId);
                return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the question creation form"
                };
            }
        }

        /// <summary>
        /// Create a new question with validation and authorization
        /// </summary>
        public async Task<BusinessLogicLayer.Services.Interfaces.CreateQuestionResult> CreateQuestionAsync(
            ClaimsPrincipal user,
            CreateQuestionViewModel model,
            ModelStateDictionary modelState)
        {
            try
            {
                _logger.LogInformation("Creating question for quiz {QuizId}", model?.QuizId);

                if (model == null)
                {
                    return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid question data"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required",
                        RedirectToLogin = true
                    };
                }

                // Validate model state
                if (!modelState.IsValid)
                {
                    _logger.LogWarning("Model validation failed for question creation");
                    var validationErrors = GetValidationErrors(modelState);

                    // Reload view model for display
                    var viewModel = await GetCreateQuestionViewModelAsync(model.QuizId);
                    // Copy the submitted data back to the view model
                    CopyModelData(model, viewModel);

                    return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                    {
                        Success = false,
                        ViewModel = viewModel,
                        ValidationErrors = validationErrors,
                        ReturnView = true
                    };
                }

                // Check authorization
                var quiz = await GetQuizWithAuthorizationAsync(model.QuizId, userId);
                if (quiz == null)
                {
                    return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Quiz not found or you are not authorized to create questions for this quiz"
                    };
                }

                // Perform additional business validation
                var businessValidationResult = await ValidateQuestionBusinessRules(model);
                if (!businessValidationResult.IsValid)
                {
                    var viewModel = await GetCreateQuestionViewModelAsync(model.QuizId);
                    CopyModelData(model, viewModel);

                    return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                    {
                        Success = false,
                        ViewModel = viewModel,
                        ValidationErrors = businessValidationResult.Errors,
                        ReturnView = true
                    };
                }

                // Create the question
                var question = await CreateQuestionAsync(model);

                _logger.LogInformation("Question {QuestionId} created successfully for quiz {QuizId}",
                    question.QuestionId, model.QuizId);

                return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                {
                    Success = true,
                    SuccessMessage = "Question created successfully!",
                    RedirectAction = "Details",
                    RedirectController = "Quiz",
                    RedirectValues = new { id = model.QuizId }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question for quiz {QuizId}", model?.QuizId);
                return new BusinessLogicLayer.Services.Interfaces.CreateQuestionResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while creating the question. Please try again."
                };
            }
        }

        #endregion

        #region Question Edit Operations

        /// <summary>
        /// Get edit question view model with authorization and validation
        /// </summary>
        public async Task<BusinessLogicLayer.Services.Interfaces.EditQuestionResult> GetEditQuestionViewModelAsync(ClaimsPrincipal user, string questionId)
        {
            try
            {
                _logger.LogInformation("Getting edit question view model for question {QuestionId}", questionId);

                if (string.IsNullOrEmpty(questionId))
                {
                    return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question ID is required"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required",
                        RedirectToLogin = true
                    };
                }

                // Check authorization
                var question = await GetQuestionWithAuthorizationAsync(questionId, userId);
                if (question == null)
                {
                    return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question not found or you are not authorized to edit this question"
                    };
                }

                var viewModel = await GetEditQuestionViewModelAsync(questionId);
                if (viewModel == null)
                {
                    return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question not found"
                    };
                }

                return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting edit question view model for question {QuestionId}", questionId);
                return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the question for editing"
                };
            }
        }

        /// <summary>
        /// Update question with validation and authorization
        /// </summary>
        public async Task<BusinessLogicLayer.Services.Interfaces.EditQuestionResult> UpdateQuestionAsync(
            ClaimsPrincipal user,
            CreateQuestionViewModel model,
            ModelStateDictionary modelState)
        {
            try
            {
                _logger.LogInformation("Updating question {QuestionId}", model?.QuestionId);

                if (model?.QuestionId == null)
                {
                    return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question ID is required"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required",
                        RedirectToLogin = true
                    };
                }

                // Validate model state
                if (!modelState.IsValid)
                {
                    var validationErrors = GetValidationErrors(modelState);
                    return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                    {
                        Success = false,
                        ViewModel = model,
                        ValidationErrors = validationErrors,
                        ReturnView = true
                    };
                }

                // Check authorization
                var question = await GetQuestionWithAuthorizationAsync(model.QuestionId, userId);
                if (question == null)
                {
                    return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question not found or you are not authorized to edit this question"
                    };
                }

                // Perform business validation
                var businessValidationResult = await ValidateQuestionBusinessRules(model);
                if (!businessValidationResult.IsValid)
                {
                    return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                    {
                        Success = false,
                        ViewModel = model,
                        ValidationErrors = businessValidationResult.Errors,
                        ReturnView = true
                    };
                }

                // Update the question
                var success = await UpdateQuestionAsync(model);
                if (!success)
                {
                    return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to update question"
                    };
                }

                _logger.LogInformation("Question {QuestionId} updated successfully", model.QuestionId);

                return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                {
                    Success = true,
                    SuccessMessage = "Question updated successfully!",
                    RedirectAction = "Details",
                    RedirectController = "Quiz",
                    RedirectValues = new { id = model.QuizId }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question {QuestionId}", model?.QuestionId);
                return new BusinessLogicLayer.Services.Interfaces.EditQuestionResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while updating the question. Please try again."
                };
            }
        }

        #endregion

        #region Question Delete Operations

        /// <summary>
        /// Delete question with authorization
        /// </summary>
        public async Task<BusinessLogicLayer.Services.Interfaces.DeleteQuestionResult> DeleteQuestionAsync(ClaimsPrincipal user, string questionId)
        {
            try
            {
                _logger.LogInformation("Deleting question {QuestionId}", questionId);

                if (string.IsNullOrEmpty(questionId))
                {
                    return new BusinessLogicLayer.Services.Interfaces.DeleteQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question ID is required"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new BusinessLogicLayer.Services.Interfaces.DeleteQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required",
                        RedirectToLogin = true
                    };
                }

                // Get question with authorization check
                var question = await GetQuestionWithAuthorizationAsync(questionId, userId);
                if (question == null)
                {
                    return new BusinessLogicLayer.Services.Interfaces.DeleteQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question not found or you are not authorized to delete this question"
                    };
                }

                var quizId = question.QuizId;

                // Delete the question
                var success = await DeleteQuestionAsync(questionId);
                if (!success)
                {
                    return new BusinessLogicLayer.Services.Interfaces.DeleteQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to delete question"
                    };
                }

                _logger.LogInformation("Question {QuestionId} deleted successfully", questionId);

                return new BusinessLogicLayer.Services.Interfaces.DeleteQuestionResult
                {
                    Success = true,
                    SuccessMessage = "Question deleted successfully!",
                    RedirectAction = "Details",
                    RedirectController = "Quiz",
                    RedirectValues = new { id = quizId }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question {QuestionId}", questionId);
                return new BusinessLogicLayer.Services.Interfaces.DeleteQuestionResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while deleting the question. Please try again."
                };
            }
        }

        #endregion

        #region Question Duplicate Operations

        /// <summary>
        /// Duplicate question with authorization
        /// </summary>
        public async Task<BusinessLogicLayer.Services.Interfaces.DuplicateQuestionResult> DuplicateQuestionAsync(ClaimsPrincipal user, string questionId)
        {
            try
            {
                _logger.LogInformation("Duplicating question {QuestionId}", questionId);

                if (string.IsNullOrEmpty(questionId))
                {
                    return new BusinessLogicLayer.Services.Interfaces.DuplicateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question ID is required"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new BusinessLogicLayer.Services.Interfaces.DuplicateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required"
                    };
                }

                // Get question with authorization check
                var question = await GetQuestionWithAuthorizationAsync(questionId, userId);
                if (question == null)
                {
                    return new BusinessLogicLayer.Services.Interfaces.DuplicateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question not found or you are not authorized to duplicate this question"
                    };
                }

                var quizId = question.QuizId;

                // Duplicate the question
                var success = await DuplicateQuestionAsync(questionId);
                if (!success)
                {
                    return new BusinessLogicLayer.Services.Interfaces.DuplicateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to duplicate question"
                    };
                }

                _logger.LogInformation("Question {QuestionId} duplicated successfully", questionId);

                return new BusinessLogicLayer.Services.Interfaces.DuplicateQuestionResult
                {
                    Success = true,
                    SuccessMessage = "Question duplicated successfully!",
                    RedirectAction = "Details",
                    RedirectController = "Quiz",
                    RedirectValues = new { id = quizId }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error duplicating question {QuestionId}", questionId);
                return new BusinessLogicLayer.Services.Interfaces.DuplicateQuestionResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while duplicating the question. Please try again."
                };
            }
        }

        #endregion

        #region Question Reorder Operations

        /// <summary>
        /// Reorder questions with authorization
        /// </summary>
        public async Task<BusinessLogicLayer.Services.Interfaces.ReorderQuestionsResult> ReorderQuestionsAsync(
            ClaimsPrincipal user,
            string quizId,
            List<string> questionIds)
        {
            try
            {
                _logger.LogInformation("Reordering questions for quiz {QuizId}", quizId);

                if (string.IsNullOrEmpty(quizId) || questionIds == null || !questionIds.Any())
                {
                    return new BusinessLogicLayer.Services.Interfaces.ReorderQuestionsResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid data provided"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new BusinessLogicLayer.Services.Interfaces.ReorderQuestionsResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required"
                    };
                }

                // Check authorization
                var quiz = await GetQuizWithAuthorizationAsync(quizId, userId);
                if (quiz == null)
                {
                    return new BusinessLogicLayer.Services.Interfaces.ReorderQuestionsResult
                    {
                        Success = false,
                        ErrorMessage = "Quiz not found or you are not authorized to reorder questions for this quiz"
                    };
                }

                // Reorder the questions
                var success = await ReorderQuestionsAsync(quizId, questionIds);
                if (!success)
                {
                    return new BusinessLogicLayer.Services.Interfaces.ReorderQuestionsResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to reorder questions"
                    };
                }

                _logger.LogInformation("Questions reordered successfully for quiz {QuizId}", quizId);

                return new BusinessLogicLayer.Services.Interfaces.ReorderQuestionsResult
                {
                    Success = true,
                    SuccessMessage = "Question order updated successfully!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering questions for quiz {QuizId}", quizId);
                return new BusinessLogicLayer.Services.Interfaces.ReorderQuestionsResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while reordering questions. Please try again."
                };
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Validate business rules for question creation/editing
        /// </summary>
        private async Task<BusinessLogicLayer.Services.Interfaces.BusinessValidationResult> ValidateQuestionBusinessRules(CreateQuestionViewModel model)
        {
            var errors = new Dictionary<string, List<string>>();

            try
            {
                // Validate question type specific rules
                if (model.QuestionType == "multiple_choice")
                {
                    if (model.AnswerOptions == null || !model.AnswerOptions.Any())
                    {
                        errors.Add("AnswerOptions", new List<string> { "Multiple choice questions must have answer options" });
                    }
                    else
                    {
                        var validOptions = model.AnswerOptions.Where(o => !string.IsNullOrWhiteSpace(o.OptionText)).ToList();
                        var correctOptions = validOptions.Where(o => o.IsCorrect).ToList();

                        if (validOptions.Count < 2)
                        {
                            errors.Add("AnswerOptions", new List<string> { "Multiple choice questions must have at least 2 answer options" });
                        }

                        if (correctOptions.Count == 0)
                        {
                            errors.Add("AnswerOptions", new List<string> { "Multiple choice questions must have at least one correct answer" });
                        }

                        if (validOptions.Count > 10)
                        {
                            errors.Add("AnswerOptions", new List<string> { "Multiple choice questions cannot have more than 10 answer options" });
                        }
                    }
                }
                else if (model.QuestionType == "true_false")
                {
                    if (!model.TrueFalseAnswer.HasValue)
                    {
                        errors.Add("TrueFalseAnswer", new List<string> { "True/False questions must have a correct answer selected" });
                    }
                }

                // Validate question order
                if (model.QuestionOrder <= 0)
                {
                    errors.Add("QuestionOrder", new List<string> { "Question order must be a positive number" });
                }

                // Validate points
                if (model.Points <= 0 || model.Points > 100)
                {
                    errors.Add("Points", new List<string> { "Points must be between 1 and 100" });
                }

                // Check for duplicate question order within the quiz (only for new questions)
                if (string.IsNullOrEmpty(model.QuestionId))
                {
                    var existingQuestionWithOrder = await _context.Questions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(q => q.QuizId == model.QuizId && q.QuestionOrder == model.QuestionOrder);

                    if (existingQuestionWithOrder != null)
                    {
                        errors.Add("QuestionOrder", new List<string> { "A question with this order already exists in the quiz" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating question business rules");
                errors.Add("General", new List<string> { "An error occurred during validation" });
            }

            return new BusinessLogicLayer.Services.Interfaces.BusinessValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// Extract validation errors from ModelState
        /// </summary>
        private Dictionary<string, List<string>> GetValidationErrors(ModelStateDictionary modelState)
        {
            var errors = new Dictionary<string, List<string>>();

            foreach (var kvp in modelState)
            {
                var key = kvp.Key;
                var state = kvp.Value;

                if (state.Errors.Any())
                {
                    var errorMessages = state.Errors.Select(e => e.ErrorMessage).ToList();
                    errors[key] = errorMessages;
                }
            }

            return errors;
        }

        /// <summary>
        /// Copy model data for redisplay on validation errors
        /// </summary>
        private void CopyModelData(CreateQuestionViewModel source, CreateQuestionViewModel target)
        {
            if (source == null || target == null) return;

            target.QuestionId = source.QuestionId;
            target.QuestionText = source.QuestionText;
            target.QuestionType = source.QuestionType;
            target.Points = source.Points;
            target.QuestionOrder = source.QuestionOrder;
            target.Explanation = source.Explanation;
            target.TrueFalseAnswer = source.TrueFalseAnswer;

            if (source.AnswerOptions != null)
            {
                target.AnswerOptions = source.AnswerOptions;
            }
        }

        #endregion
    }

    #region Result Classes

    /// <summary>
    /// Result class for create question operations
    /// </summary>
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

    /// <summary>
    /// Result class for edit question operations
    /// </summary>
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

    /// <summary>
    /// Result class for delete question operations
    /// </summary>
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

    /// <summary>
    /// Result class for duplicate question operations
    /// </summary>
    public class DuplicateQuestionResult
    {
        public bool Success { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RedirectValues { get; set; }
    }

    /// <summary>
    /// Result class for reorder questions operations
    /// </summary>
    public class ReorderQuestionsResult
    {
        public bool Success { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Business validation result helper class
    /// </summary>
    public class BusinessValidationResult
    {
        public bool IsValid { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; } = new();
    }

    #endregion
}








