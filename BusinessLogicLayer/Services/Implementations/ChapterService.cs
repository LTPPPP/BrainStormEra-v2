using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using System.Security.Claims;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using System;
using System.Linq;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Implementation class that handles complex business logic for Chapter operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// authorization, validation, and error handling.
    /// </summary>
    public class ChapterService : IChapterService
    {
        private readonly IChapterRepo _chapterRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly ILogger<ChapterService> _logger;

        public ChapterService(
            IChapterRepo chapterRepo,
            ICourseRepo courseRepo,
            ILogger<ChapterService> logger)
        {
            _chapterRepo = chapterRepo;
            _courseRepo = courseRepo;
            _logger = logger;
        }

        // IChapterService Implementation Methods
        public async Task<string> CreateChapterAsync(CreateChapterViewModel model, string authorId)
        {
            try
            {
                // Verify that the user is the author of the course
                var course = await _courseRepo.GetCourseWithChaptersAsync(model.CourseId, authorId);

                if (course == null)
                {
                    _logger.LogWarning("Course not found or user not authorized to add chapters to course {CourseId}", model.CourseId);
                    throw new UnauthorizedAccessException("You are not authorized to add chapters to this course.");
                }

                // Additional validation for chapter order uniqueness
                if (course.Chapters.Any(c => c.ChapterOrder == model.ChapterOrder))
                {
                    throw new ArgumentException($"Chapter order {model.ChapterOrder} is already taken in this course.");
                }

                // Validate chapter name uniqueness
                if (course.Chapters.Any(c => string.Equals(c.ChapterName.Trim(), model.ChapterName.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"A chapter with the name '{model.ChapterName}' already exists in this course.");
                }

                // Validate prerequisite chapter exists and is before this chapter
                if (model.IsLocked && !string.IsNullOrEmpty(model.UnlockAfterChapterId))
                {
                    var prerequisiteChapter = course.Chapters.FirstOrDefault(c => c.ChapterId == model.UnlockAfterChapterId);
                    if (prerequisiteChapter == null)
                    {
                        throw new ArgumentException("Selected prerequisite chapter does not exist.");
                    }
                    if (prerequisiteChapter.ChapterOrder >= model.ChapterOrder)
                    {
                        throw new ArgumentException("Prerequisite chapter must come before this chapter in the sequence.");
                    }
                }

                var chapterId = Guid.NewGuid().ToString();

                var chapter = new Chapter
                {
                    ChapterId = chapterId,
                    CourseId = model.CourseId,
                    ChapterName = model.ChapterName.Trim(),
                    ChapterDescription = string.IsNullOrEmpty(model.ChapterDescription) ? null : model.ChapterDescription.Trim(),
                    ChapterOrder = model.ChapterOrder,
                    ChapterStatus = 1, // Active status
                    IsLocked = model.IsLocked,
                    UnlockAfterChapterId = string.IsNullOrEmpty(model.UnlockAfterChapterId) ? null : model.UnlockAfterChapterId,
                    ChapterCreatedAt = DateTime.UtcNow,
                    ChapterUpdatedAt = DateTime.UtcNow
                };

                var result = await _chapterRepo.CreateChapterAsync(chapter);

                _logger.LogInformation("Chapter created successfully: {ChapterId} for course {CourseId} by author {AuthorId}", chapterId, model.CourseId, authorId);
                return result;
            }
            catch (ArgumentException)
            {
                // Re-throw argument exceptions for controller to handle
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                // Re-throw unauthorized exceptions for controller to handle
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chapter for course {CourseId} by author {AuthorId}", model.CourseId, authorId);
                throw new InvalidOperationException("An error occurred while creating the chapter. Please try again.", ex);
            }
        }

        public async Task<CreateChapterViewModel?> GetCreateChapterViewModelAsync(string courseId, string authorId)
        {
            try
            {
                var course = await _courseRepo.GetCourseWithChaptersAsync(courseId, authorId);

                if (course == null)
                {
                    _logger.LogWarning("Course not found or user not authorized: {CourseId} for user {AuthorId}", courseId, authorId);
                    return null;
                }

                var existingChapters = course.Chapters
                    .OrderBy(c => c.ChapterOrder)
                    .Select(c => new ChapterViewModel
                    {
                        ChapterId = c.ChapterId,
                        ChapterName = c.ChapterName,
                        ChapterDescription = c.ChapterDescription ?? "",
                        ChapterOrder = c.ChapterOrder ?? 0,
                        Lessons = c.Lessons?.Select(l => new LessonViewModel
                        {
                            LessonId = l.LessonId,
                            LessonName = l.LessonName,
                            LessonDescription = l.LessonDescription ?? "",
                            LessonOrder = l.LessonOrder,
                            LessonType = l.LessonType?.LessonTypeName ?? "Content",
                            EstimatedDuration = 0, // Calculate if needed
                            IsLocked = l.IsLocked ?? false
                        }).ToList() ?? new List<LessonViewModel>()
                    }).ToList();

                var nextChapterOrder = existingChapters.Any() ? existingChapters.Max(c => c.ChapterOrder) + 1 : 1;

                return new CreateChapterViewModel
                {
                    CourseId = courseId,
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription ?? "",
                    ChapterOrder = nextChapterOrder,
                    ExistingChapters = existingChapters
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting create chapter view model for course {CourseId}", courseId);
                return null;
            }
        }

        public async Task<bool> UpdateChapterAsync(string chapterId, CreateChapterViewModel model, string authorId)
        {
            try
            {
                var chapter = await _chapterRepo.GetChapterWithCourseAsync(chapterId, authorId);

                if (chapter == null)
                {
                    _logger.LogWarning("Chapter not found or user not authorized to update chapter {ChapterId}", chapterId);
                    return false;
                }

                chapter.ChapterName = model.ChapterName;
                chapter.ChapterDescription = model.ChapterDescription;
                chapter.ChapterOrder = model.ChapterOrder;
                chapter.IsLocked = model.IsLocked;
                chapter.UnlockAfterChapterId = model.UnlockAfterChapterId;
                chapter.ChapterUpdatedAt = DateTime.UtcNow;

                var result = await _chapterRepo.UpdateChapterAsync(chapter);

                _logger.LogInformation("Chapter updated successfully: {ChapterId}", chapterId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chapter {ChapterId}", chapterId);
                return false;
            }
        }

        /// <summary>
        /// Delete chapter - performs soft delete by setting status to Archived
        /// </summary>
        /// <param name="chapterId">Chapter ID to delete</param>
        /// <param name="authorId">Author ID for authorization</param>
        /// <returns>Success result</returns>
        public async Task<bool> DeleteChapterAsync(string chapterId, string authorId)
        {
            try
            {
                var result = await _chapterRepo.DeleteChapterAsync(chapterId, authorId);

                if (result)
                {
                    _logger.LogInformation("Chapter deleted successfully: {ChapterId}", chapterId);
                }
                else
                {
                    _logger.LogWarning("Chapter not found or user not authorized to delete chapter {ChapterId}", chapterId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chapter {ChapterId}", chapterId);
                return false;
            }
        }

        public async Task<List<ChapterViewModel>> GetChaptersByCourseIdAsync(string courseId)
        {
            try
            {
                var chapters = await _chapterRepo.GetChaptersByCourseAsync(courseId);

                return chapters.Select(c => new ChapterViewModel
                {
                    ChapterId = c.ChapterId,
                    ChapterName = c.ChapterName,
                    ChapterDescription = c.ChapterDescription ?? "",
                    ChapterOrder = c.ChapterOrder ?? 0
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapters for course {CourseId}", courseId);
                return new List<ChapterViewModel>();
            }
        }

        public async Task<CreateChapterViewModel?> GetChapterForEditAsync(string chapterId, string authorId)
        {
            try
            {
                var chapter = await _chapterRepo.GetChapterWithCourseAsync(chapterId, authorId);

                if (chapter == null)
                {
                    _logger.LogWarning("Chapter not found or user not authorized: {ChapterId} for user {AuthorId}", chapterId, authorId);
                    return null;
                }

                // Get all chapters for the course
                var allChapters = await _chapterRepo.GetChaptersByCourseAsync(chapter.CourseId);

                var existingChapters = allChapters
                    .Where(c => c.ChapterId != chapterId) // Exclude current chapter
                    .OrderBy(c => c.ChapterOrder)
                    .Select(c => new ChapterViewModel
                    {
                        ChapterId = c.ChapterId,
                        ChapterName = c.ChapterName,
                        ChapterDescription = c.ChapterDescription ?? "",
                        ChapterOrder = c.ChapterOrder ?? 0,
                        Lessons = c.Lessons?.Select(l => new LessonViewModel
                        {
                            LessonId = l.LessonId,
                            LessonName = l.LessonName,
                            LessonDescription = l.LessonDescription ?? "",
                            LessonOrder = l.LessonOrder,
                            LessonType = l.LessonType?.LessonTypeName ?? "Content",
                            EstimatedDuration = 0,
                            IsLocked = l.IsLocked ?? false
                        }).ToList() ?? new List<LessonViewModel>()
                    }).ToList();

                return new CreateChapterViewModel
                {
                    CourseId = chapter.CourseId,
                    CourseName = chapter.Course.CourseName,
                    CourseDescription = chapter.Course.CourseDescription ?? "",
                    ChapterName = chapter.ChapterName,
                    ChapterDescription = chapter.ChapterDescription ?? "",
                    ChapterOrder = chapter.ChapterOrder ?? 1,
                    IsLocked = chapter.IsLocked ?? false,
                    UnlockAfterChapterId = chapter.UnlockAfterChapterId,
                    ExistingChapters = existingChapters
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapter for edit: {ChapterId}", chapterId);
                return null;
            }
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

                var viewModel = await GetCreateChapterViewModelAsync(courseId, userId);
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

                var chapterId = await CreateChapterAsync(model, userId);

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

                var chapter = await GetChapterForEditAsync(chapterId, userId);
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
                        ErrorMessage = "Please correct the validation errors.",
                        ValidationErrors = validationResult.Errors,
                        ViewModel = model,
                        ChapterId = chapterId,
                        ReturnView = true
                    };
                }

                var success = await UpdateChapterAsync(chapterId, model, userId);

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
                    return new EditChapterResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to update the chapter. Please try again.",
                        ViewModel = model,
                        ChapterId = chapterId,
                        ReturnView = true
                    };
                }
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

                var success = await DeleteChapterAsync(chapterId, userId);

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
                    Message = "An error occurred while deleting the chapter. Please try again."
                };
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task<ChapterValidationResult> ValidateChapterModelAsync(CreateChapterViewModel model, string userId)
        {
            var errors = new Dictionary<string, List<string>>();

            // Basic validation
            if (string.IsNullOrWhiteSpace(model.ChapterName))
            {
                AddValidationError(errors, "ChapterName", "Chapter name is required.");
            }
            else if (model.ChapterName.Length < 3)
            {
                AddValidationError(errors, "ChapterName", "Chapter name must be at least 3 characters long.");
            }
            else if (model.ChapterName.Length > 100)
            {
                AddValidationError(errors, "ChapterName", "Chapter name cannot exceed 100 characters.");
            }
            else if (ContainsInappropriateContent(model.ChapterName))
            {
                AddValidationError(errors, "ChapterName", "Chapter name contains inappropriate content.");
            }

            if (!string.IsNullOrWhiteSpace(model.ChapterDescription) && model.ChapterDescription.Length > 500)
            {
                AddValidationError(errors, "ChapterDescription", "Chapter description cannot exceed 500 characters.");
            }

            if (model.ChapterOrder <= 0)
            {
                AddValidationError(errors, "ChapterOrder", "Chapter order must be a positive number.");
            }

            // Business logic validation
            if (!string.IsNullOrEmpty(model.CourseId))
            {
                var course = await _courseRepo.GetCourseWithChaptersAsync(model.CourseId, userId);
                if (course == null)
                {
                    AddValidationError(errors, "", "Course not found or you are not authorized to add chapters to this course.");
                }
                else
                {
                    // Check for duplicate chapter order
                    if (course.Chapters.Any(c => c.ChapterOrder == model.ChapterOrder))
                    {
                        AddValidationError(errors, "ChapterOrder", $"Chapter order {model.ChapterOrder} is already taken in this course.");
                    }

                    // Check for duplicate chapter name
                    if (course.Chapters.Any(c => string.Equals(c.ChapterName.Trim(), model.ChapterName?.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        AddValidationError(errors, "ChapterName", $"A chapter with the name '{model.ChapterName}' already exists in this course.");
                    }
                }
            }

            return new ChapterValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        private async Task<ChapterValidationResult> ValidateChapterModelForEditAsync(CreateChapterViewModel model, string userId, string chapterId)
        {
            var errors = new Dictionary<string, List<string>>();

            // Basic validation (same as create)
            if (string.IsNullOrWhiteSpace(model.ChapterName))
            {
                AddValidationError(errors, "ChapterName", "Chapter name is required.");
            }
            else if (model.ChapterName.Length < 3)
            {
                AddValidationError(errors, "ChapterName", "Chapter name must be at least 3 characters long.");
            }
            else if (model.ChapterName.Length > 100)
            {
                AddValidationError(errors, "ChapterName", "Chapter name cannot exceed 100 characters.");
            }
            else if (ContainsInappropriateContent(model.ChapterName))
            {
                AddValidationError(errors, "ChapterName", "Chapter name contains inappropriate content.");
            }

            if (!string.IsNullOrWhiteSpace(model.ChapterDescription) && model.ChapterDescription.Length > 500)
            {
                AddValidationError(errors, "ChapterDescription", "Chapter description cannot exceed 500 characters.");
            }

            if (model.ChapterOrder <= 0)
            {
                AddValidationError(errors, "ChapterOrder", "Chapter order must be a positive number.");
            }

            // Business logic validation for edit
            if (!string.IsNullOrEmpty(model.CourseId))
            {
                var course = await _courseRepo.GetCourseWithChaptersAsync(model.CourseId, userId);
                if (course == null)
                {
                    AddValidationError(errors, "", "Course not found or you are not authorized to edit chapters in this course.");
                }
                else
                {
                    // Check for duplicate chapter order (excluding current chapter)
                    if (course.Chapters.Any(c => c.ChapterId != chapterId && c.ChapterOrder == model.ChapterOrder))
                    {
                        AddValidationError(errors, "ChapterOrder", $"Chapter order {model.ChapterOrder} is already taken in this course.");
                    }

                    // Check for duplicate chapter name (excluding current chapter)
                    if (course.Chapters.Any(c => c.ChapterId != chapterId && string.Equals(c.ChapterName.Trim(), model.ChapterName?.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        AddValidationError(errors, "ChapterName", $"A chapter with the name '{model.ChapterName}' already exists in this course.");
                    }
                }
            }

            return new ChapterValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        private static void AddValidationError(Dictionary<string, List<string>> errors, string key, string message)
        {
            if (!errors.ContainsKey(key))
            {
                errors[key] = new List<string>();
            }
            errors[key].Add(message);
        }

        private static bool ContainsInappropriateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            var inappropriateWords = new[] { "spam", "scam", "hack", "crack", "illegal", "porn", "sex" };
            var contentLower = content.ToLowerInvariant();

            return inappropriateWords.Any(word => contentLower.Contains(word));
        }

        private async Task ReloadCreateChapterViewModel(CreateChapterViewModel model, string userId)
        {
            try
            {
                var viewModel = await GetCreateChapterViewModelAsync(model.CourseId, userId);
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

        private async Task ReloadEditChapterViewModel(CreateChapterViewModel model, string userId, string chapterId)
        {
            try
            {
                var viewModel = await GetChapterForEditAsync(chapterId, userId);
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








