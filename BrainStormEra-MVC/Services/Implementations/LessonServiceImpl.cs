using DataAccessLayer.Data;
using DataAccessLayer.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BrainStormEra_MVC.Services.Implementations
{
    public class LessonServiceImpl
    {
        private readonly ILessonService _lessonService;
        private readonly ILogger<LessonServiceImpl> _logger;

        public LessonServiceImpl(ILessonService lessonService, ILogger<LessonServiceImpl> logger)
        {
            _lessonService = lessonService;
            _logger = logger;
        }

        // Result classes for structured returns
        public class SelectLessonTypeResult
        {
            public bool Success { get; set; }
            public SelectLessonTypeViewModel? ViewModel { get; set; }
            public string? ErrorMessage { get; set; }
            public string? RedirectAction { get; set; }
            public object? RedirectValues { get; set; }
        }

        public class CreateLessonViewResult
        {
            public bool Success { get; set; }
            public CreateLessonViewModel? ViewModel { get; set; }
            public string? ErrorMessage { get; set; }
        }

        public class CreateLessonResult
        {
            public bool Success { get; set; }
            public string? SuccessMessage { get; set; }
            public string? ErrorMessage { get; set; }
            public string? RedirectAction { get; set; }
            public string? RedirectController { get; set; }
            public object? RedirectValues { get; set; }
            public Dictionary<string, string>? ValidationErrors { get; set; }
            public CreateLessonViewModel? ViewModel { get; set; }
        }

        public class EditLessonResult
        {
            public bool Success { get; set; }
            public CreateLessonViewModel? ViewModel { get; set; }
            public string? ErrorMessage { get; set; }
        }

        public class UpdateLessonResult
        {
            public bool Success { get; set; }
            public string? SuccessMessage { get; set; }
            public string? ErrorMessage { get; set; }
            public string? RedirectAction { get; set; }
            public string? RedirectController { get; set; }
            public object? RedirectValues { get; set; }
            public Dictionary<string, string>? ValidationErrors { get; set; }
            public CreateLessonViewModel? ViewModel { get; set; }
        }

        public class DeleteLessonResult
        {
            public bool Success { get; set; }
            public string? SuccessMessage { get; set; }
            public string? ErrorMessage { get; set; }
            public string? RedirectAction { get; set; }
            public string? RedirectController { get; set; }
            public object? RedirectValues { get; set; }
        }

        // Business logic methods
        public async Task<SelectLessonTypeResult> GetSelectLessonTypeViewModelAsync(string chapterId)
        {
            try
            {
                if (string.IsNullOrEmpty(chapterId))
                {
                    return new SelectLessonTypeResult
                    {
                        Success = false,
                        ErrorMessage = "Chapter ID is required"
                    };
                }

                var chapter = await _lessonService.GetChapterByIdAsync(chapterId);
                if (chapter == null)
                {
                    return new SelectLessonTypeResult
                    {
                        Success = false,
                        ErrorMessage = "Chapter not found"
                    };
                }

                var viewModel = new SelectLessonTypeViewModel
                {
                    ChapterId = chapterId,
                    CourseId = chapter.CourseId,
                    ChapterName = chapter.ChapterName,
                    CourseName = chapter.Course.CourseName,
                    ChapterOrder = chapter.ChapterOrder ?? 1,
                    LessonTypes = await _lessonService.GetLessonTypesAsync()
                };

                return new SelectLessonTypeResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting select lesson type view model for chapter {ChapterId}", chapterId);
                return new SelectLessonTypeResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the lesson type selection page."
                };
            }
        }

        public async Task<SelectLessonTypeResult> ProcessSelectLessonTypeAsync(SelectLessonTypeViewModel model, ModelStateDictionary modelState)
        {
            try
            {
                if (!modelState.IsValid)
                {
                    // Reload lesson types for the view
                    model.LessonTypes = await _lessonService.GetLessonTypesAsync();
                    return new SelectLessonTypeResult
                    {
                        Success = false,
                        ViewModel = model
                    };
                }

                // Redirect to appropriate create lesson page based on lesson type
                switch (model.SelectedLessonTypeId)
                {
                    case 1: // Video
                        return new SelectLessonTypeResult
                        {
                            Success = true,
                            RedirectAction = "CreateVideoLesson",
                            RedirectValues = new { chapterId = model.ChapterId }
                        };
                    case 2: // Text
                        return new SelectLessonTypeResult
                        {
                            Success = true,
                            RedirectAction = "CreateTextLesson",
                            RedirectValues = new { chapterId = model.ChapterId }
                        };
                    case 3: // Interactive/Document
                        return new SelectLessonTypeResult
                        {
                            Success = true,
                            RedirectAction = "CreateInteractiveLesson",
                            RedirectValues = new { chapterId = model.ChapterId }
                        };
                    default:
                        model.LessonTypes = await _lessonService.GetLessonTypesAsync();
                        return new SelectLessonTypeResult
                        {
                            Success = false,
                            ViewModel = model,
                            ErrorMessage = "Invalid lesson type selected."
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing select lesson type for chapter {ChapterId}", model?.ChapterId);
                return new SelectLessonTypeResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while processing the lesson type selection."
                };
            }
        }

        public async Task<CreateLessonViewResult> GetCreateLessonViewModelAsync(string chapterId, int lessonTypeId)
        {
            try
            {
                if (string.IsNullOrEmpty(chapterId))
                {
                    return new CreateLessonViewResult
                    {
                        Success = false,
                        ErrorMessage = "Chapter ID is required"
                    };
                }

                var chapter = await _lessonService.GetChapterByIdAsync(chapterId);
                if (chapter == null)
                {
                    return new CreateLessonViewResult
                    {
                        Success = false,
                        ErrorMessage = "Chapter not found"
                    };
                }

                var existingLessons = await _lessonService.GetLessonsInChapterAsync(chapterId);

                var viewModel = new CreateLessonViewModel
                {
                    ChapterId = chapterId,
                    CourseId = chapter.CourseId,
                    ChapterName = chapter.ChapterName,
                    CourseName = chapter.Course.CourseName,
                    ChapterOrder = chapter.ChapterOrder ?? 1,
                    Order = await _lessonService.GetNextLessonOrderAsync(chapterId),
                    LessonTypeId = lessonTypeId,
                    LessonTypes = await _lessonService.GetLessonTypesAsync(),
                    ExistingLessons = existingLessons.ToList(),
                    // Set default values
                    IsLocked = false,
                    IsMandatory = true,
                    RequiresQuizPass = false,
                    MinQuizScore = 70,
                    MinCompletionPercentage = 100,
                    MinTimeSpent = 0
                };

                return new CreateLessonViewResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting create lesson view model for chapter {ChapterId}, type {LessonTypeId}", chapterId, lessonTypeId);
                return new CreateLessonViewResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the lesson creation page."
                };
            }
        }

        public async Task<CreateLessonResult> ProcessCreateLessonAsync(CreateLessonViewModel model, ModelStateDictionary modelState)
        {
            try
            {
                _logger.LogInformation("ProcessCreateLesson called with LessonName: {LessonName}, ChapterId: {ChapterId}", model?.LessonName, model?.ChapterId);

                if (model == null)
                {
                    _logger.LogError("Model is null");
                    return new CreateLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid request data.",
                        RedirectAction = "Index",
                        RedirectController = "Courses"
                    };
                }

                if (modelState.IsValid)
                {
                    _logger.LogInformation("ModelState is valid. Starting additional validations...");

                    // Additional validations
                    var validationErrors = await ValidateCreateLessonModelAsync(model);
                    _logger.LogInformation("Validation completed. Error count: {ErrorCount}", validationErrors.Count);

                    if (validationErrors.Count == 0)
                    {
                        _logger.LogInformation("Creating lesson object...");

                        // Determine content based on lesson type
                        string finalContent = await ProcessLessonContentByTypeAsync(model);

                        var lesson = new Lesson
                        {
                            LessonId = Guid.NewGuid().ToString(),
                            LessonName = model.LessonName.Trim(),
                            LessonDescription = model.Description?.Trim(),
                            LessonContent = finalContent,
                            LessonTypeId = model.LessonTypeId,
                            LessonOrder = model.Order,
                            ChapterId = model.ChapterId,
                            IsLocked = model.IsLocked,
                            UnlockAfterLessonId = string.IsNullOrEmpty(model.UnlockAfterLessonId) ? null : model.UnlockAfterLessonId,
                            IsMandatory = model.IsMandatory,
                            RequiresQuizPass = model.RequiresQuizPass,
                            MinQuizScore = model.MinQuizScore ?? 70.00m,
                            MinCompletionPercentage = model.MinCompletionPercentage ?? 100.00m,
                            MinTimeSpent = model.MinTimeSpent ?? 0,
                            LessonStatus = 1 // Active
                        };

                        _logger.LogInformation("Calling lesson service to create lesson with ID: {LessonId}", lesson.LessonId);

                        var result = await _lessonService.CreateLessonAsync(lesson);
                        _logger.LogInformation("Lesson service result: {Result}", result);

                        if (result)
                        {
                            _logger.LogInformation("Lesson created successfully. Setting success message and redirecting...");

                            var chapter = await _lessonService.GetChapterByIdAsync(model.ChapterId);
                            _logger.LogInformation("Retrieved chapter for redirect. CourseId: {CourseId}", chapter?.CourseId);

                            return new CreateLessonResult
                            {
                                Success = true,
                                SuccessMessage = $"Lesson '{model.LessonName}' has been created successfully!",
                                RedirectAction = "Details",
                                RedirectController = "Course",
                                RedirectValues = new { id = chapter?.CourseId, tab = "curriculum" }
                            };
                        }
                        else
                        {
                            _logger.LogError("Lesson service returned false when creating lesson");
                            await ReloadViewModelDataAsync(model);
                            return new CreateLessonResult
                            {
                                Success = false,
                                ErrorMessage = "An error occurred while creating the lesson. Please try again.",
                                ViewModel = model
                            };
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Validation errors found");
                        await ReloadViewModelDataAsync(model);
                        return new CreateLessonResult
                        {
                            Success = false,
                            ValidationErrors = validationErrors,
                            ViewModel = model
                        };
                    }
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid");
                    await ReloadViewModelDataAsync(model);
                    return new CreateLessonResult
                    {
                        Success = false,
                        ViewModel = model
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating lesson");
                await ReloadViewModelDataAsync(model);
                return new CreateLessonResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while creating the lesson. Please try again.",
                    ViewModel = model
                };
            }
        }

        public async Task<EditLessonResult> GetEditLessonViewModelAsync(string lessonId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(lessonId))
                {
                    return new EditLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Lesson ID is required"
                    };
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return new EditLessonResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                var viewModel = await _lessonService.GetLessonForEditAsync(lessonId, userId);
                if (viewModel == null)
                {
                    return new EditLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Lesson not found or you don't have permission to edit it"
                    };
                }

                return new EditLessonResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting edit lesson view model for lesson {LessonId}, user {UserId}", lessonId, userId);
                return new EditLessonResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the lesson for editing."
                };
            }
        }

        public async Task<UpdateLessonResult> ProcessUpdateLessonAsync(string lessonId, CreateLessonViewModel model, ModelStateDictionary modelState, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(lessonId))
                {
                    return new UpdateLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Lesson ID is required"
                    };
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return new UpdateLessonResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                if (model == null)
                {
                    return new UpdateLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid request data.",
                        RedirectAction = "EditLesson",
                        RedirectValues = new { id = lessonId }
                    };
                }

                if (modelState.IsValid)
                {
                    // Additional validations
                    var validationErrors = await ValidateEditLessonModelAsync(model, lessonId);

                    if (validationErrors.Count == 0)
                    {
                        // Update lesson content based on lesson type
                        await ProcessEditLessonContentAsync(model);

                        var result = await _lessonService.UpdateLessonAsync(lessonId, model, userId);

                        if (result)
                        {
                            var chapter = await _lessonService.GetChapterByIdAsync(model.ChapterId);
                            return new UpdateLessonResult
                            {
                                Success = true,
                                SuccessMessage = $"Lesson '{model.LessonName}' has been updated successfully!",
                                RedirectAction = "Details",
                                RedirectController = "Course",
                                RedirectValues = new { id = chapter?.CourseId, tab = "curriculum" }
                            };
                        }
                        else
                        {
                            await ReloadViewModelDataAsync(model);
                            return new UpdateLessonResult
                            {
                                Success = false,
                                ErrorMessage = "An error occurred while updating the lesson. Please try again.",
                                ViewModel = model
                            };
                        }
                    }
                    else
                    {
                        await ReloadViewModelDataAsync(model);
                        return new UpdateLessonResult
                        {
                            Success = false,
                            ValidationErrors = validationErrors,
                            ViewModel = model
                        };
                    }
                }

                await ReloadViewModelDataAsync(model);
                return new UpdateLessonResult
                {
                    Success = false,
                    ViewModel = model
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating lesson {LessonId}", lessonId);
                await ReloadViewModelDataAsync(model);
                return new UpdateLessonResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while updating the lesson. Please try again.",
                    ViewModel = model
                };
            }
        }

        public async Task<DeleteLessonResult> ProcessDeleteLessonAsync(string lessonId, string userId, string courseId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return new DeleteLessonResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Details",
                        RedirectController = "Course",
                        RedirectValues = new { id = courseId }
                    };
                }

                var success = await _lessonService.DeleteLessonAsync(lessonId, userId);
                if (success)
                {
                    return new DeleteLessonResult
                    {
                        Success = true,
                        SuccessMessage = "Lesson deleted successfully!",
                        RedirectAction = "Details",
                        RedirectController = "Course",
                        RedirectValues = new { id = courseId }
                    };
                }
                else
                {
                    return new DeleteLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Lesson not found or you are not authorized to delete this lesson.",
                        RedirectAction = "Details",
                        RedirectController = "Course",
                        RedirectValues = new { id = courseId }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lesson {LessonId} by user {UserId}", lessonId, userId);
                return new DeleteLessonResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while deleting the lesson.",
                    RedirectAction = "Details",
                    RedirectController = "Course",
                    RedirectValues = new { id = courseId }
                };
            }
        }

        // Private helper methods
        private async Task<Dictionary<string, string>> ValidateCreateLessonModelAsync(CreateLessonViewModel model)
        {
            var errors = new Dictionary<string, string>();

            // Check for duplicate lesson name
            if (await _lessonService.IsDuplicateLessonNameAsync(model.LessonName, model.ChapterId))
            {
                errors.Add("LessonName", "A lesson with this name already exists in this chapter.");
            }

            // Validate content based on lesson type
            switch (model.LessonTypeId)
            {
                case 1: // Video lesson type
                    if (string.IsNullOrEmpty(model.VideoUrl) && (model.VideoFile == null || model.VideoFile.Length == 0))
                    {
                        errors.Add("Video", "Please provide either a video URL or upload a video file.");
                    }
                    if (!string.IsNullOrEmpty(model.VideoUrl) && !IsValidVideoUrl(model.VideoUrl))
                    {
                        errors.Add("VideoUrl", "Please enter a valid video URL (YouTube, Vimeo, etc.).");
                    }
                    if (model.VideoFile != null && model.VideoFile.Length > 100 * 1024 * 1024) // 100MB
                    {
                        errors.Add("VideoFile", "Video file size must not exceed 100MB.");
                    }
                    break;
                case 2: // Text lesson type
                    if (string.IsNullOrEmpty(model.TextContent))
                    {
                        errors.Add("TextContent", "Text content is required for text lessons.");
                    }
                    break;
                case 3: // Document lesson type
                    if (model.DocumentFiles == null || model.DocumentFiles.Count == 0 || model.DocumentFiles.All(f => f.Length == 0))
                    {
                        errors.Add("DocumentFiles", "Please upload at least one document file.");
                    }
                    if (model.DocumentFiles != null)
                    {
                        foreach (var file in model.DocumentFiles)
                        {
                            if (file.Length > 10 * 1024 * 1024) // 10MB
                            {
                                errors.Add("DocumentFiles", $"Document file '{file.FileName}' exceeds 10MB size limit.");
                                break;
                            }
                        }
                    }
                    break;
            }

            // Validate unlock after lesson if specified
            if (!string.IsNullOrEmpty(model.UnlockAfterLessonId))
            {
                if (!await _lessonService.ValidateUnlockAfterLessonAsync(model.ChapterId, model.UnlockAfterLessonId))
                {
                    errors.Add("UnlockAfterLessonId", "The specified unlock lesson does not exist in this chapter.");
                }
            }

            // Validate quiz requirements
            if (model.RequiresQuizPass && (!model.MinQuizScore.HasValue || model.MinQuizScore < 0 || model.MinQuizScore > 100))
            {
                errors.Add("MinQuizScore", "Minimum quiz score must be between 0 and 100 when quiz pass is required.");
            }

            // Validate completion percentage
            if (model.MinCompletionPercentage.HasValue && (model.MinCompletionPercentage < 0 || model.MinCompletionPercentage > 100))
            {
                errors.Add("MinCompletionPercentage", "Minimum completion percentage must be between 0 and 100.");
            }

            // Validate lesson order
            if (model.Order <= 0)
            {
                errors.Add("Order", "Lesson order must be a positive number.");
            }

            return errors;
        }

        private async Task<Dictionary<string, string>> ValidateEditLessonModelAsync(CreateLessonViewModel model, string currentLessonId)
        {
            var errors = new Dictionary<string, string>();

            // Check for duplicate lesson name (excluding current lesson)
            if (await _lessonService.IsDuplicateLessonNameForEditAsync(model.LessonName, model.ChapterId, currentLessonId))
            {
                errors.Add("LessonName", "A lesson with this name already exists in this chapter.");
            }

            // Validate content based on lesson type (reuse existing validation logic)
            switch (model.LessonTypeId)
            {
                case 1: // Video lesson type
                    if (string.IsNullOrEmpty(model.VideoUrl) && (model.VideoFile == null || model.VideoFile.Length == 0))
                    {
                        errors.Add("Video", "Please provide either a video URL or upload a video file.");
                    }
                    if (!string.IsNullOrEmpty(model.VideoUrl) && !IsValidVideoUrl(model.VideoUrl))
                    {
                        errors.Add("VideoUrl", "Please enter a valid video URL (YouTube, Vimeo, etc.).");
                    }
                    if (model.VideoFile != null && model.VideoFile.Length > 100 * 1024 * 1024) // 100MB
                    {
                        errors.Add("VideoFile", "Video file size must not exceed 100MB.");
                    }
                    break;
                case 2: // Text lesson type
                    if (string.IsNullOrEmpty(model.TextContent))
                    {
                        errors.Add("TextContent", "Text content is required for text lessons.");
                    }
                    break;
                case 3: // Document lesson type
                    // For edit, documents are optional (existing ones might be kept)
                    if (model.DocumentFiles != null)
                    {
                        foreach (var file in model.DocumentFiles)
                        {
                            if (file.Length > 10 * 1024 * 1024) // 10MB
                            {
                                errors.Add("DocumentFiles", $"Document file '{file.FileName}' exceeds 10MB size limit.");
                                break;
                            }
                        }
                    }
                    break;
            }

            // Validate unlock after lesson if specified
            if (!string.IsNullOrEmpty(model.UnlockAfterLessonId))
            {
                if (!await _lessonService.ValidateUnlockAfterLessonAsync(model.ChapterId, model.UnlockAfterLessonId))
                {
                    errors.Add("UnlockAfterLessonId", "The specified unlock lesson does not exist in this chapter.");
                }
            }

            // Validate quiz requirements
            if (model.RequiresQuizPass && (!model.MinQuizScore.HasValue || model.MinQuizScore < 0 || model.MinQuizScore > 100))
            {
                errors.Add("MinQuizScore", "Minimum quiz score must be between 0 and 100 when quiz pass is required.");
            }

            // Validate completion percentage
            if (model.MinCompletionPercentage.HasValue && (model.MinCompletionPercentage < 0 || model.MinCompletionPercentage > 100))
            {
                errors.Add("MinCompletionPercentage", "Minimum completion percentage must be between 0 and 100.");
            }

            // Validate lesson order
            if (model.Order <= 0)
            {
                errors.Add("Order", "Lesson order must be a positive number.");
            }

            return errors;
        }

        private bool IsValidVideoUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            try
            {
                var uri = new Uri(url);
                var host = uri.Host.ToLower();
                return host.Contains("youtube.com") || host.Contains("youtu.be") ||
                       host.Contains("vimeo.com") || host.Contains("dailymotion.com") ||
                       host.Contains("twitch.tv") || host.Contains("facebook.com");
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> ProcessLessonContentByTypeAsync(CreateLessonViewModel model)
        {
            try
            {
                switch (model.LessonTypeId)
                {
                    case 1: // Video lesson type
                        return await ProcessVideoContentAsync(model);
                    case 2: // Text lesson type
                        return ProcessTextContent(model);
                    case 3: // Document lesson type
                        return await ProcessDocumentContentAsync(model);
                    default:
                        return model.Content?.Trim() ?? "";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing lesson content for type {LessonTypeId}", model.LessonTypeId);
                return model.Content?.Trim() ?? "";
            }
        }

        private async Task<string> ProcessVideoContentAsync(CreateLessonViewModel model)
        {
            var content = model.Content?.Trim() ?? "";

            // Add video URL if provided
            if (!string.IsNullOrEmpty(model.VideoUrl))
            {
                content += $"\n\n[VIDEO_URL]{model.VideoUrl}[/VIDEO_URL]";
            }

            // Handle video file upload if provided
            if (model.VideoFile != null && model.VideoFile.Length > 0)
            {
                try
                {
                    // Create uploads directory if it doesn't exist
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "videos");
                    Directory.CreateDirectory(uploadsDir);

                    // Generate unique filename
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.VideoFile.FileName)}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.VideoFile.CopyToAsync(stream);
                    }

                    // Add file reference to content
                    content += $"\n\n[VIDEO_FILE]/uploads/videos/{fileName}[/VIDEO_FILE]";

                    _logger.LogInformation("Video file uploaded: {FileName}", fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading video file");
                    throw new InvalidOperationException("Failed to upload video file");
                }
            }

            return content;
        }

        private string ProcessTextContent(CreateLessonViewModel model)
        {
            var content = model.Content?.Trim() ?? "";

            // Add the rich text content
            if (!string.IsNullOrEmpty(model.TextContent))
            {
                content += $"\n\n[TEXT_CONTENT]{model.TextContent.Trim()}[/TEXT_CONTENT]";
            }

            return content;
        }

        private async Task<string> ProcessDocumentContentAsync(CreateLessonViewModel model)
        {
            var content = model.Content?.Trim() ?? "";

            // Add document description if provided
            if (!string.IsNullOrEmpty(model.DocumentDescription))
            {
                content += $"\n\n[DOCUMENT_DESCRIPTION]{model.DocumentDescription.Trim()}[/DOCUMENT_DESCRIPTION]";
            }

            // Handle document file uploads
            if (model.DocumentFiles != null && model.DocumentFiles.Count > 0)
            {
                try
                {
                    // Create uploads directory if it doesn't exist
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                    Directory.CreateDirectory(uploadsDir);

                    var uploadedFiles = new List<string>();

                    foreach (var file in model.DocumentFiles)
                    {
                        if (file.Length > 0)
                        {
                            // Generate unique filename
                            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                            var filePath = Path.Combine(uploadsDir, fileName);

                            // Save file
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            uploadedFiles.Add($"/uploads/documents/{fileName}|{Path.GetFileName(file.FileName)}");
                            _logger.LogInformation("Document file uploaded: {FileName}", fileName);
                        }
                    }

                    if (uploadedFiles.Count > 0)
                    {
                        content += $"\n\n[DOCUMENT_FILES]{string.Join(";", uploadedFiles)}[/DOCUMENT_FILES]";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading document files");
                    throw new InvalidOperationException("Failed to upload document files");
                }
            }

            return content;
        }

        private async Task ProcessEditLessonContentAsync(CreateLessonViewModel model)
        {
            // Process content based on lesson type, similar to create but for editing
            switch (model.LessonTypeId)
            {
                case 1: // Video lesson
                    if (model.VideoFile != null && model.VideoFile.Length > 0)
                    {
                        // Handle new video file upload
                        await ProcessVideoContentAsync(model);
                    }
                    break;
                case 3: // Document lesson
                    if (model.DocumentFiles != null && model.DocumentFiles.Any(f => f.Length > 0))
                    {
                        // Handle new document uploads
                        await ProcessDocumentContentAsync(model);
                    }
                    break;
                    // Text lessons don't need special file processing
            }
        }

        private async Task ReloadViewModelDataAsync(CreateLessonViewModel model)
        {
            var chapter = await _lessonService.GetChapterByIdAsync(model.ChapterId);
            if (chapter != null)
            {
                model.ChapterName = chapter.ChapterName;
                model.CourseName = chapter.Course.CourseName;
                model.CourseId = chapter.CourseId;
                model.ChapterOrder = chapter.ChapterOrder ?? 1;
            }

            model.LessonTypes = await _lessonService.GetLessonTypesAsync();
            model.ExistingLessons = (await _lessonService.GetLessonsInChapterAsync(model.ChapterId)).ToList();
        }
    }
}
