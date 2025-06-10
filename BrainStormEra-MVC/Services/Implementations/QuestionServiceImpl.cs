using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BrainStormEra_MVC.Services.Implementations
{
    /// <summary>
    /// Implementation class that handles complex business logic for Question operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// authorization, validation, and error handling.
    /// </summary>
    public class QuestionServiceImpl
    {
        private readonly IQuestionService _questionService;
        private readonly BrainStormEraContext _context;
        private readonly ILogger<QuestionServiceImpl> _logger;

        public QuestionServiceImpl(
            IQuestionService questionService,
            BrainStormEraContext context,
            ILogger<QuestionServiceImpl> logger)
        {
            _questionService = questionService;
            _context = context;
            _logger = logger;
        }

        #region Question Creation Operations

        /// <summary>
        /// Get create question view model with authorization and validation
        /// </summary>
        public async Task<CreateQuestionResult> GetCreateQuestionViewModelAsync(ClaimsPrincipal user, string quizId)
        {
            try
            {
                _logger.LogInformation("Getting create question view model for quiz {QuizId}", quizId);

                if (string.IsNullOrEmpty(quizId))
                {
                    _logger.LogWarning("Quiz ID is null or empty");
                    return new CreateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Quiz ID is required"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return new CreateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required",
                        RedirectToLogin = true
                    };
                }

                // Check authorization
                var quiz = await _questionService.GetQuizWithAuthorizationAsync(quizId, userId);
                if (quiz == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found or user {UserId} not authorized", quizId, userId);
                    return new CreateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Quiz not found or you are not authorized to create questions for this quiz"
                    };
                }

                var viewModel = await _questionService.GetCreateQuestionViewModelAsync(quizId);

                return new CreateQuestionResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting create question view model for quiz {QuizId}", quizId);
                return new CreateQuestionResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the question creation form"
                };
            }
        }

        /// <summary>
        /// Create a new question with validation and authorization
        /// </summary>
        public async Task<CreateQuestionResult> CreateQuestionAsync(
            ClaimsPrincipal user,
            CreateQuestionViewModel model,
            ModelStateDictionary modelState)
        {
            try
            {
                _logger.LogInformation("Creating question for quiz {QuizId}", model?.QuizId);

                if (model == null)
                {
                    return new CreateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid question data"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new CreateQuestionResult
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
                    var viewModel = await _questionService.GetCreateQuestionViewModelAsync(model.QuizId);
                    // Copy the submitted data back to the view model
                    CopyModelData(model, viewModel);

                    return new CreateQuestionResult
                    {
                        Success = false,
                        ViewModel = viewModel,
                        ValidationErrors = validationErrors,
                        ReturnView = true
                    };
                }

                // Check authorization
                var quiz = await _questionService.GetQuizWithAuthorizationAsync(model.QuizId, userId);
                if (quiz == null)
                {
                    return new CreateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Quiz not found or you are not authorized to create questions for this quiz"
                    };
                }

                // Perform additional business validation
                var businessValidationResult = await ValidateQuestionBusinessRules(model);
                if (!businessValidationResult.IsValid)
                {
                    var viewModel = await _questionService.GetCreateQuestionViewModelAsync(model.QuizId);
                    CopyModelData(model, viewModel);

                    return new CreateQuestionResult
                    {
                        Success = false,
                        ViewModel = viewModel,
                        ValidationErrors = businessValidationResult.Errors,
                        ReturnView = true
                    };
                }

                // Create the question
                var question = await _questionService.CreateQuestionAsync(model);

                _logger.LogInformation("Question {QuestionId} created successfully for quiz {QuizId}",
                    question.QuestionId, model.QuizId);

                return new CreateQuestionResult
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
                return new CreateQuestionResult
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
        public async Task<EditQuestionResult> GetEditQuestionViewModelAsync(ClaimsPrincipal user, string questionId)
        {
            try
            {
                _logger.LogInformation("Getting edit question view model for question {QuestionId}", questionId);

                if (string.IsNullOrEmpty(questionId))
                {
                    return new EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question ID is required"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required",
                        RedirectToLogin = true
                    };
                }

                // Check authorization
                var question = await _questionService.GetQuestionWithAuthorizationAsync(questionId, userId);
                if (question == null)
                {
                    return new EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question not found or you are not authorized to edit this question"
                    };
                }

                var viewModel = await _questionService.GetEditQuestionViewModelAsync(questionId);
                if (viewModel == null)
                {
                    return new EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question not found"
                    };
                }

                return new EditQuestionResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting edit question view model for question {QuestionId}", questionId);
                return new EditQuestionResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the question for editing"
                };
            }
        }

        /// <summary>
        /// Update question with validation and authorization
        /// </summary>
        public async Task<EditQuestionResult> UpdateQuestionAsync(
            ClaimsPrincipal user,
            CreateQuestionViewModel model,
            ModelStateDictionary modelState)
        {
            try
            {
                _logger.LogInformation("Updating question {QuestionId}", model?.QuestionId);

                if (model?.QuestionId == null)
                {
                    return new EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question ID is required"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditQuestionResult
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
                    return new EditQuestionResult
                    {
                        Success = false,
                        ViewModel = model,
                        ValidationErrors = validationErrors,
                        ReturnView = true
                    };
                }

                // Check authorization
                var question = await _questionService.GetQuestionWithAuthorizationAsync(model.QuestionId, userId);
                if (question == null)
                {
                    return new EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question not found or you are not authorized to edit this question"
                    };
                }

                // Perform business validation
                var businessValidationResult = await ValidateQuestionBusinessRules(model);
                if (!businessValidationResult.IsValid)
                {
                    return new EditQuestionResult
                    {
                        Success = false,
                        ViewModel = model,
                        ValidationErrors = businessValidationResult.Errors,
                        ReturnView = true
                    };
                }

                // Update the question
                var success = await _questionService.UpdateQuestionAsync(model);
                if (!success)
                {
                    return new EditQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to update question"
                    };
                }

                _logger.LogInformation("Question {QuestionId} updated successfully", model.QuestionId);

                return new EditQuestionResult
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
                return new EditQuestionResult
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
        public async Task<DeleteQuestionResult> DeleteQuestionAsync(ClaimsPrincipal user, string questionId)
        {
            try
            {
                _logger.LogInformation("Deleting question {QuestionId}", questionId);

                if (string.IsNullOrEmpty(questionId))
                {
                    return new DeleteQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question ID is required"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new DeleteQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required",
                        RedirectToLogin = true
                    };
                }

                // Get question with authorization check
                var question = await _questionService.GetQuestionWithAuthorizationAsync(questionId, userId);
                if (question == null)
                {
                    return new DeleteQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question not found or you are not authorized to delete this question"
                    };
                }

                var quizId = question.QuizId;

                // Delete the question
                var success = await _questionService.DeleteQuestionAsync(questionId);
                if (!success)
                {
                    return new DeleteQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to delete question"
                    };
                }

                _logger.LogInformation("Question {QuestionId} deleted successfully", questionId);

                return new DeleteQuestionResult
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
                return new DeleteQuestionResult
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
        public async Task<DuplicateQuestionResult> DuplicateQuestionAsync(ClaimsPrincipal user, string questionId)
        {
            try
            {
                _logger.LogInformation("Duplicating question {QuestionId}", questionId);

                if (string.IsNullOrEmpty(questionId))
                {
                    return new DuplicateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question ID is required"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new DuplicateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required"
                    };
                }

                // Get question with authorization check
                var question = await _questionService.GetQuestionWithAuthorizationAsync(questionId, userId);
                if (question == null)
                {
                    return new DuplicateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Question not found or you are not authorized to duplicate this question"
                    };
                }

                var quizId = question.QuizId;

                // Duplicate the question
                var success = await _questionService.DuplicateQuestionAsync(questionId);
                if (!success)
                {
                    return new DuplicateQuestionResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to duplicate question"
                    };
                }

                _logger.LogInformation("Question {QuestionId} duplicated successfully", questionId);

                return new DuplicateQuestionResult
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
                return new DuplicateQuestionResult
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
        public async Task<ReorderQuestionsResult> ReorderQuestionsAsync(
            ClaimsPrincipal user,
            string quizId,
            List<string> questionIds)
        {
            try
            {
                _logger.LogInformation("Reordering questions for quiz {QuizId}", quizId);

                if (string.IsNullOrEmpty(quizId) || questionIds == null || !questionIds.Any())
                {
                    return new ReorderQuestionsResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid data provided"
                    };
                }

                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new ReorderQuestionsResult
                    {
                        Success = false,
                        ErrorMessage = "User authentication required"
                    };
                }

                // Check authorization
                var quiz = await _questionService.GetQuizWithAuthorizationAsync(quizId, userId);
                if (quiz == null)
                {
                    return new ReorderQuestionsResult
                    {
                        Success = false,
                        ErrorMessage = "Quiz not found or you are not authorized to reorder questions for this quiz"
                    };
                }

                // Reorder the questions
                var success = await _questionService.ReorderQuestionsAsync(quizId, questionIds);
                if (!success)
                {
                    return new ReorderQuestionsResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to reorder questions"
                    };
                }

                _logger.LogInformation("Questions reordered successfully for quiz {QuizId}", quizId);

                return new ReorderQuestionsResult
                {
                    Success = true,
                    SuccessMessage = "Question order updated successfully!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering questions for quiz {QuizId}", quizId);
                return new ReorderQuestionsResult
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
        private async Task<BusinessValidationResult> ValidateQuestionBusinessRules(CreateQuestionViewModel model)
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

            return new BusinessValidationResult
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
