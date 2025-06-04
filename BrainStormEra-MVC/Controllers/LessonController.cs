using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class LessonController : Controller
    {
        private readonly ILessonService _lessonService;
        private readonly ILogger<LessonController> _logger;

        public LessonController(ILessonService lessonService, ILogger<LessonController> logger)
        {
            _lessonService = lessonService;
            _logger = logger;
        }        // Step 1: Select Lesson Type
        [HttpGet]
        public async Task<IActionResult> SelectLessonType(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                return BadRequest("Chapter ID is required");
            }

            var chapter = await _lessonService.GetChapterByIdAsync(chapterId);
            if (chapter == null)
            {
                return NotFound("Chapter not found");
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

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectLessonType(SelectLessonTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload lesson types for the view
                model.LessonTypes = await _lessonService.GetLessonTypesAsync();
                return View(model);
            }

            // Redirect to appropriate create lesson page based on lesson type
            switch (model.SelectedLessonTypeId)
            {
                case 1: // Video
                    return RedirectToAction("CreateVideoLesson", new { chapterId = model.ChapterId });
                case 2: // Text
                    return RedirectToAction("CreateTextLesson", new { chapterId = model.ChapterId });
                case 3: // Interactive/Document
                    return RedirectToAction("CreateInteractiveLesson", new { chapterId = model.ChapterId });
                default:
                    TempData["ErrorMessage"] = "Invalid lesson type selected.";
                    model.LessonTypes = await _lessonService.GetLessonTypesAsync();
                    return View(model);
            }
        }

        // Step 2a: Create Video Lesson
        [HttpGet]
        public async Task<IActionResult> CreateVideoLesson(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                return BadRequest("Chapter ID is required");
            }

            var chapter = await _lessonService.GetChapterByIdAsync(chapterId);
            if (chapter == null)
            {
                return NotFound("Chapter not found");
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
                LessonTypeId = 1, // Video type
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

            return View(viewModel);
        }

        // Step 2b: Create Text Lesson
        [HttpGet]
        public async Task<IActionResult> CreateTextLesson(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                return BadRequest("Chapter ID is required");
            }

            var chapter = await _lessonService.GetChapterByIdAsync(chapterId);
            if (chapter == null)
            {
                return NotFound("Chapter not found");
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
                LessonTypeId = 2, // Text type
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

            return View(viewModel);
        }

        // Step 2c: Create Interactive/Document Lesson
        [HttpGet]
        public async Task<IActionResult> CreateInteractiveLesson(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                return BadRequest("Chapter ID is required");
            }

            var chapter = await _lessonService.GetChapterByIdAsync(chapterId);
            if (chapter == null)
            {
                return NotFound("Chapter not found");
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
                LessonTypeId = 3, // Interactive type
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

            return View(viewModel);
        }        // Legacy method - for backward compatibility
        [HttpGet]
        public IActionResult CreateLesson(string chapterId)
        {
            // Redirect to new flow
            return RedirectToAction("SelectLessonType", new { chapterId = chapterId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(CreateLessonViewModel model)
        {
            Console.WriteLine("=== CreateLesson POST METHOD CALLED ===");
            Console.WriteLine($"LessonName: {model?.LessonName}");
            Console.WriteLine($"ChapterId: {model?.ChapterId}");
            Console.WriteLine($"Content: {model?.Content}");
            Console.WriteLine($"LessonTypeId: {model?.LessonTypeId}");

            _logger.LogInformation("CreateLesson POST called with LessonName: {LessonName}, ChapterId: {ChapterId}",
                model?.LessonName, model?.ChapterId);

            if (model == null)
            {
                Console.WriteLine("ERROR: Model is null!");
                _logger.LogError("Model is null");
                TempData["ErrorMessage"] = "Invalid request data.";
                return RedirectToAction("Index", "Courses");
            }
            try
            {
                Console.WriteLine("=== CHECKING MODEL STATE ===");
                Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState Errors:");
                    foreach (var error in ModelState)
                    {
                        if (error.Value.Errors.Count > 0)
                        {
                            Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    Console.WriteLine("=== MODEL STATE IS VALID ===");
                    _logger.LogInformation("ModelState is valid. Starting additional validations...");                    // Additional validations
                    var validationErrors = await ValidateCreateLessonModel(model);
                    Console.WriteLine($"=== VALIDATION COMPLETED ===");
                    Console.WriteLine($"Validation errors count: {validationErrors.Count}");

                    _logger.LogInformation("Validation completed. Error count: {ErrorCount}", validationErrors.Count);

                    if (validationErrors.Count > 0)
                    {
                        Console.WriteLine("Validation Errors:");
                        foreach (var error in validationErrors)
                        {
                            Console.WriteLine($"  {error.Key}: {error.Value}");
                            _logger.LogWarning("Validation error - {Key}: {Value}", error.Key, error.Value);
                        }
                    }
                    if (validationErrors.Count == 0)
                    {
                        Console.WriteLine("=== NO VALIDATION ERRORS, CREATING LESSON ===");
                        _logger.LogInformation("Creating lesson object...");

                        // Determine content based on lesson type
                        string finalContent = await ProcessLessonContentByType(model);

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

                        Console.WriteLine($"=== LESSON OBJECT CREATED ===");
                        Console.WriteLine($"LessonId: {lesson.LessonId}");
                        Console.WriteLine($"LessonName: {lesson.LessonName}");
                        Console.WriteLine($"ChapterId: {lesson.ChapterId}");
                        Console.WriteLine($"LessonTypeId: {lesson.LessonTypeId}");
                        Console.WriteLine($"LessonOrder: {lesson.LessonOrder}");

                        _logger.LogInformation("Calling lesson service to create lesson with ID: {LessonId}", lesson.LessonId);

                        var result = await _lessonService.CreateLessonAsync(lesson);
                        Console.WriteLine($"=== LESSON SERVICE RESULT: {result} ===");
                        _logger.LogInformation("Lesson service result: {Result}", result); if (result)
                        {
                            Console.WriteLine("=== LESSON CREATED SUCCESSFULLY ===");
                            _logger.LogInformation("Lesson created successfully. Setting success message and redirecting...");
                            TempData["SuccessMessage"] = $"Lesson '{model.LessonName}' has been created successfully!";

                            var chapter = await _lessonService.GetChapterByIdAsync(model.ChapterId);
                            Console.WriteLine($"Retrieved chapter for redirect. CourseId: {chapter?.CourseId}");
                            _logger.LogInformation("Retrieved chapter for redirect. CourseId: {CourseId}", chapter?.CourseId);

                            return RedirectToAction("Details", "Course", new { id = chapter?.CourseId, tab = "curriculum" });
                        }
                        else
                        {
                            Console.WriteLine("=== LESSON SERVICE RETURNED FALSE ===");
                            _logger.LogError("Lesson service returned false when creating lesson");
                            TempData["ErrorMessage"] = "An error occurred while creating the lesson. Please try again.";
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Validation errors found, adding to ModelState");
                        // Add validation errors to ModelState
                        foreach (var error in validationErrors)
                        {
                            ModelState.AddModelError(error.Key, error.Value);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid. Errors:");
                    foreach (var error in ModelState)
                    {
                        if (error.Value.Errors.Count > 0)
                        {
                            _logger.LogWarning("ModelState error - {Key}: {Errors}",
                                error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION OCCURRED ===");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Exception Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                _logger.LogError(ex, "Exception occurred while creating lesson");
                TempData["ErrorMessage"] = "An unexpected error occurred while creating the lesson. Please try again.";
            }

            // Reload data for the view if validation fails
            Console.WriteLine("=== RELOADING VIEW MODEL DATA AND RETURNING VIEW ===");
            _logger.LogInformation("Reloading view model data and returning view");
            await ReloadViewModelData(model);
            return View(model);
        }

        // POST methods for specific lesson types
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVideoLesson(CreateLessonViewModel model)
        {
            model.LessonTypeId = 1; // Ensure video type
            return await ProcessCreateLesson(model, "CreateVideoLesson");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTextLesson(CreateLessonViewModel model)
        {
            model.LessonTypeId = 2; // Ensure text type
            return await ProcessCreateLesson(model, "CreateTextLesson");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInteractiveLesson(CreateLessonViewModel model)
        {
            model.LessonTypeId = 3; // Ensure interactive type
            return await ProcessCreateLesson(model, "CreateInteractiveLesson");
        }

        // Unified lesson processing method
        private async Task<IActionResult> ProcessCreateLesson(CreateLessonViewModel model, string viewName)
        {
            Console.WriteLine($"=== {viewName} POST METHOD CALLED ===");
            Console.WriteLine($"LessonName: {model?.LessonName}");
            Console.WriteLine($"ChapterId: {model?.ChapterId}");
            Console.WriteLine($"LessonTypeId: {model?.LessonTypeId}");

            _logger.LogInformation("{ViewName} POST called with LessonName: {LessonName}, ChapterId: {ChapterId}",
                viewName, model?.LessonName, model?.ChapterId);

            if (model == null)
            {
                Console.WriteLine("ERROR: Model is null!");
                _logger.LogError("Model is null");
                TempData["ErrorMessage"] = "Invalid request data.";
                return RedirectToAction("Index", "Courses");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var validationErrors = await ValidateCreateLessonModel(model);

                    if (validationErrors.Count == 0)
                    {
                        Console.WriteLine("=== NO VALIDATION ERRORS, CREATING LESSON ===");

                        string finalContent = await ProcessLessonContentByType(model);

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

                        var result = await _lessonService.CreateLessonAsync(lesson);

                        if (result)
                        {
                            Console.WriteLine("=== LESSON CREATED SUCCESSFULLY ===");
                            TempData["SuccessMessage"] = $"Lesson '{model.LessonName}' has been created successfully!";

                            var chapter = await _lessonService.GetChapterByIdAsync(model.ChapterId);
                            return RedirectToAction("Details", "Course", new { id = chapter?.CourseId, tab = "curriculum" });
                        }
                        else
                        {
                            Console.WriteLine("=== LESSON SERVICE RETURNED FALSE ===");
                            TempData["ErrorMessage"] = "An error occurred while creating the lesson. Please try again.";
                        }
                    }
                    else
                    {
                        foreach (var error in validationErrors)
                        {
                            ModelState.AddModelError(error.Key, error.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION OCCURRED IN {viewName} ===");
                Console.WriteLine($"Exception: {ex.Message}");
                _logger.LogError(ex, "Exception occurred while creating lesson in {ViewName}", viewName);
                TempData["ErrorMessage"] = "An unexpected error occurred while creating the lesson. Please try again.";
            }

            // Reload data and return view
            await ReloadViewModelData(model);
            return View(viewName, model);
        }

        private async Task<Dictionary<string, string>> ValidateCreateLessonModel(CreateLessonViewModel model)
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

        private async Task<string> ProcessLessonContentByType(CreateLessonViewModel model)
        {
            try
            {
                switch (model.LessonTypeId)
                {
                    case 1: // Video lesson type
                        return await ProcessVideoContent(model);
                    case 2: // Text lesson type
                        return ProcessTextContent(model);
                    case 3: // Document lesson type
                        return await ProcessDocumentContent(model);
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

        private async Task<string> ProcessVideoContent(CreateLessonViewModel model)
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

        private async Task<string> ProcessDocumentContent(CreateLessonViewModel model)
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

        private async Task ReloadViewModelData(CreateLessonViewModel model)
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
