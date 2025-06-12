using Microsoft.AspNetCore.Mvc;
using BrainStormEra_MVC.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using System.Security.Claims;

namespace BrainStormEra_MVC.Services.Implementations
{
    /// <summary>
    /// Implementation class that handles complex business logic for Chapter operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// authorization, validation, and error handling.
    /// </summary>
    public class ChapterServiceImpl
    {
        private readonly IChapterService _chapterService;
        private readonly ILogger<ChapterServiceImpl> _logger;

        public ChapterServiceImpl(
            IChapterService chapterService,
            ILogger<ChapterServiceImpl> logger)
        {
            _chapterService = chapterService;
            _logger = logger;
        }

        #region Chapter Creation Operations

        /// <summary>
        /// Get create chapter view model with authorization check
        /// </summary>
        public async Task<CreateChapterResult> GetCreateChapterViewModelAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new CreateChapterResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var viewModel = await _chapterService.GetCreateChapterViewModelAsync(courseId, userId);
                if (viewModel == null)
                {
                    return new CreateChapterResult
                    {
                        Success = false,
                        ErrorMessage = "Course not found or you are not authorized to add chapters to this course.",
                        RedirectAction = "Index",
                        RedirectController = "Course"
                    };
                }

                return new CreateChapterResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading create chapter page for course {CourseId}", courseId);
                return new CreateChapterResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the create chapter page.",
                    RedirectAction = "Index",
                    RedirectController = "Course"
                };
            }
        }

        /// <summary>
        /// Create a new chapter with comprehensive validation
        /// </summary>
        public async Task<CreateChapterResult> CreateChapterAsync(ClaimsPrincipal user, CreateChapterViewModel model)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new CreateChapterResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                // Perform comprehensive validation
                var validationResult = await ValidateChapterModelAsync(model, userId);
                if (!validationResult.IsValid)
                {
                    // Reload the view model with existing data
                    await ReloadCreateChapterViewModel(model, userId);

                    return new CreateChapterResult
                    {
                        Success = false,
                        ErrorMessage = "Please correct the validation errors.",
                        ValidationErrors = validationResult.Errors,
                        ViewModel = model,
                        ReturnView = true
                    };
                }

                var chapterId = await _chapterService.CreateChapterAsync(model, userId);

                return new CreateChapterResult
                {
                    Success = true,
                    SuccessMessage = $"Chapter '{model.ChapterName}' has been successfully created!",
                    RedirectAction = "Details",
                    RedirectController = "Course",
                    RouteValues = new { id = model.CourseId }
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access attempt: {Message} by user {UserId}", ex.Message, user.FindFirst("UserId")?.Value);
                return new CreateChapterResult
                {
                    Success = false,
                    ErrorMessage = "You are not authorized to add chapters to this course.",
                    RedirectAction = "Index",
                    RedirectController = "Course"
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Validation error creating chapter: {Message}", ex.Message);

                // Reload the view model with existing data
                var userId = user.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await ReloadCreateChapterViewModel(model, userId);
                }

                return new CreateChapterResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ViewModel = model,
                    ReturnView = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chapter for course {CourseId} by user {UserId}", model.CourseId, user.FindFirst("UserId")?.Value);
                return new CreateChapterResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while creating the chapter. Please try again.",
                    RedirectAction = "Details",
                    RedirectController = "Course",
                    RouteValues = new { id = model.CourseId }
                };
            }
        }

        #endregion

        #region Chapter Edit Operations

        /// <summary>
        /// Get chapter for editing with authorization check
        /// </summary>
        public async Task<EditChapterResult> GetChapterForEditAsync(ClaimsPrincipal user, string chapterId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditChapterResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var chapter = await _chapterService.GetChapterForEditAsync(chapterId, userId);
                if (chapter == null)
                {
                    return new EditChapterResult
                    {
                        Success = false,
                        ErrorMessage = "Chapter not found or you are not authorized to edit this chapter.",
                        RedirectAction = "Index",
                        RedirectController = "Course"
                    };
                }

                return new EditChapterResult
                {
                    Success = true,
                    ViewModel = chapter,
                    ChapterId = chapterId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapter for edit: {ChapterId}", chapterId);
                return new EditChapterResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the edit chapter page.",
                    RedirectAction = "Index",
                    RedirectController = "Course"
                };
            }
        }

        /// <summary>
        /// Update chapter with comprehensive validation
        /// </summary>
        public async Task<EditChapterResult> UpdateChapterAsync(ClaimsPrincipal user, string chapterId, CreateChapterViewModel model)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditChapterResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                // Perform comprehensive validation for edit
                var validationResult = await ValidateChapterModelForEditAsync(model, userId, chapterId);
                if (!validationResult.IsValid)
                {
                    // Reload the view model with existing data
                    await ReloadEditChapterViewModel(model, userId, chapterId);

                    return new EditChapterResult
                    {
                        Success = false,
                        ErrorMessage = "Please correct the following errors: " + string.Join("; ", validationResult.Errors.SelectMany(e => e.Value)),
                        ValidationErrors = validationResult.Errors,
                        ViewModel = model,
                        ChapterId = chapterId,
                        ReturnView = true
                    };
                }

                var success = await _chapterService.UpdateChapterAsync(chapterId, model, userId);
                if (success)
                {
                    return new EditChapterResult
                    {
                        Success = true,
                        SuccessMessage = $"Chapter '{model.ChapterName}' has been successfully updated!",
                        RedirectAction = "Details",
                        RedirectController = "Course",
                        RouteValues = new { id = model.CourseId }
                    };
                }
                else
                {
                    await ReloadEditChapterViewModel(model, userId, chapterId);
                    return new EditChapterResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to update chapter. Please try again.",
                        ViewModel = model,
                        ChapterId = chapterId,
                        ReturnView = true
                    };
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access attempt: {Message} by user {UserId}", ex.Message, user.FindFirst("UserId")?.Value);
                return new EditChapterResult
                {
                    Success = false,
                    ErrorMessage = "You are not authorized to edit this chapter.",
                    RedirectAction = "Details",
                    RedirectController = "Course",
                    RouteValues = new { id = model.CourseId }
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Validation error updating chapter: {Message}", ex.Message);

                var userId = user.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await ReloadEditChapterViewModel(model, userId, chapterId);
                }

                return new EditChapterResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ViewModel = model,
                    ChapterId = chapterId,
                    ReturnView = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chapter {ChapterId} by user {UserId}", chapterId, user.FindFirst("UserId")?.Value);
                return new EditChapterResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while updating the chapter. Please try again.",
                    RedirectAction = "Details",
                    RedirectController = "Course",
                    RouteValues = new { id = model.CourseId }
                };
            }
        }

        #endregion

        #region Chapter Delete Operations

        /// <summary>
        /// Delete chapter with authorization check
        /// </summary>
        public async Task<DeleteChapterResult> DeleteChapterAsync(ClaimsPrincipal user, string chapterId, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new DeleteChapterResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                var success = await _chapterService.DeleteChapterAsync(chapterId, userId);
                if (success)
                {
                    return new DeleteChapterResult
                    {
                        Success = true,
                        Message = "Chapter deleted successfully!"
                    };
                }
                else
                {
                    return new DeleteChapterResult
                    {
                        Success = false,
                        Message = "Chapter not found or you are not authorized to delete this chapter."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chapter {ChapterId} by user {UserId}", chapterId, user.FindFirst("UserId")?.Value);
                return new DeleteChapterResult
                {
                    Success = false,
                    Message = "An error occurred while deleting the chapter."
                };
            }
        }

        #endregion

        #region Private Validation Methods

        /// <summary>
        /// Validates chapter model with comprehensive business rules
        /// </summary>
        private async Task<ChapterValidationResult> ValidateChapterModelAsync(CreateChapterViewModel model, string userId)
        {
            var errors = new Dictionary<string, List<string>>();

            try
            {
                // Check if chapter name already exists in the course
                var existingChapters = await _chapterService.GetChaptersByCourseIdAsync(model.CourseId);

                if (existingChapters.Any(c => string.Equals(c.ChapterName.Trim(), model.ChapterName.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    AddValidationError(errors, nameof(model.ChapterName), "A chapter with this name already exists in the course.");
                }

                // Check if chapter order already exists
                if (existingChapters.Any(c => c.ChapterOrder == model.ChapterOrder))
                {
                    AddValidationError(errors, nameof(model.ChapterOrder), $"Chapter order {model.ChapterOrder} is already taken. Please choose a different order.");
                }

                // Validate unlock prerequisite
                if (model.IsLocked && !string.IsNullOrEmpty(model.UnlockAfterChapterId))
                {
                    var prerequisiteChapter = existingChapters.FirstOrDefault(c => c.ChapterId == model.UnlockAfterChapterId);
                    if (prerequisiteChapter == null)
                    {
                        AddValidationError(errors, nameof(model.UnlockAfterChapterId), "Selected prerequisite chapter not found.");
                    }
                    else if (prerequisiteChapter.ChapterOrder >= model.ChapterOrder)
                    {
                        AddValidationError(errors, nameof(model.UnlockAfterChapterId), "Prerequisite chapter must come before this chapter in the course sequence.");
                    }
                }

                // Validate chapter name doesn't contain inappropriate content
                if (ContainsInappropriateContent(model.ChapterName))
                {
                    AddValidationError(errors, nameof(model.ChapterName), "Chapter name contains inappropriate content.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during chapter validation for course {CourseId}", model.CourseId);
                AddValidationError(errors, "", "An error occurred during validation. Please try again.");
            }

            return new ChapterValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// Validates chapter model for editing with additional business rules
        /// </summary>
        private async Task<ChapterValidationResult> ValidateChapterModelForEditAsync(CreateChapterViewModel model, string userId, string chapterId)
        {
            var errors = new Dictionary<string, List<string>>();

            try
            {
                // Check if chapter name already exists in the course (excluding current chapter)
                var existingChapters = await _chapterService.GetChaptersByCourseIdAsync(model.CourseId);

                if (existingChapters.Any(c => c.ChapterId != chapterId && string.Equals(c.ChapterName.Trim(), model.ChapterName.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    AddValidationError(errors, nameof(model.ChapterName), "A chapter with this name already exists in the course.");
                }

                // Check if chapter order already exists (excluding current chapter)
                if (existingChapters.Any(c => c.ChapterId != chapterId && c.ChapterOrder == model.ChapterOrder))
                {
                    AddValidationError(errors, nameof(model.ChapterOrder), $"Chapter order {model.ChapterOrder} is already taken. Please choose a different order.");
                }

                // Validate unlock prerequisite
                if (model.IsLocked && !string.IsNullOrEmpty(model.UnlockAfterChapterId))
                {
                    var prerequisiteChapter = existingChapters.FirstOrDefault(c => c.ChapterId == model.UnlockAfterChapterId);
                    if (prerequisiteChapter == null)
                    {
                        AddValidationError(errors, nameof(model.UnlockAfterChapterId), "Selected prerequisite chapter not found.");
                    }
                    else if (prerequisiteChapter.ChapterOrder >= model.ChapterOrder)
                    {
                        AddValidationError(errors, nameof(model.UnlockAfterChapterId), "Prerequisite chapter must come before this chapter in the course sequence.");
                    }
                }

                // Validate chapter name doesn't contain inappropriate content
                if (ContainsInappropriateContent(model.ChapterName))
                {
                    AddValidationError(errors, nameof(model.ChapterName), "Chapter name contains inappropriate content.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during chapter validation for edit. Chapter {ChapterId}", chapterId);
                AddValidationError(errors, "", "An error occurred during validation. Please try again.");
            }

            return new ChapterValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// Helper method to add validation errors
        /// </summary>
        private static void AddValidationError(Dictionary<string, List<string>> errors, string key, string message)
        {
            if (!errors.ContainsKey(key))
            {
                errors[key] = new List<string>();
            }
            errors[key].Add(message);
        }

        /// <summary>
        /// Simple content filter for inappropriate content
        /// </summary>
        private static bool ContainsInappropriateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            var inappropriateWords = new[] { "test", "dummy", "placeholder", "xxx", "delete", "remove" };
            return inappropriateWords.Any(word => content.ToLower().Contains(word.ToLower()));
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Reloads the create chapter view model with existing data
        /// </summary>
        private async Task ReloadCreateChapterViewModel(CreateChapterViewModel model, string userId)
        {
            try
            {
                var viewModel = await _chapterService.GetCreateChapterViewModelAsync(model.CourseId, userId);
                if (viewModel != null)
                {
                    model.CourseName = viewModel.CourseName;
                    model.CourseDescription = viewModel.CourseDescription;
                    model.ExistingChapters = viewModel.ExistingChapters;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading create chapter view model for course {CourseId}", model.CourseId);
            }
        }

        /// <summary>
        /// Reloads the edit chapter view model with existing data
        /// </summary>
        private async Task ReloadEditChapterViewModel(CreateChapterViewModel model, string userId, string chapterId)
        {
            try
            {
                var viewModel = await _chapterService.GetChapterForEditAsync(chapterId, userId);
                if (viewModel != null)
                {
                    model.CourseName = viewModel.CourseName;
                    model.CourseDescription = viewModel.CourseDescription;
                    model.ExistingChapters = viewModel.ExistingChapters;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading edit chapter view model for chapter {ChapterId}", chapterId);
            }
        }

        #endregion
    }

    #region Result Classes

    public class CreateChapterResult
    {
        public bool Success { get; set; }
        public CreateChapterViewModel? ViewModel { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RouteValues { get; set; }
        public Dictionary<string, List<string>>? ValidationErrors { get; set; }
        public bool ReturnView { get; set; }
    }

    public class EditChapterResult
    {
        public bool Success { get; set; }
        public CreateChapterViewModel? ViewModel { get; set; }
        public string? ChapterId { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RouteValues { get; set; }
        public Dictionary<string, List<string>>? ValidationErrors { get; set; }
        public bool ReturnView { get; set; }
    }

    public class DeleteChapterResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ChapterValidationResult
    {
        public bool IsValid { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; } = new();
    }

    #endregion
}
