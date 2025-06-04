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
        }

        [HttpGet]
        public async Task<IActionResult> CreateLesson(string chapterId)
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

                        var lesson = new Lesson
                        {
                            LessonId = Guid.NewGuid().ToString(),
                            LessonName = model.LessonName.Trim(),
                            LessonDescription = model.Description?.Trim(),
                            LessonContent = model.Content.Trim(),
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

        private async Task<Dictionary<string, string>> ValidateCreateLessonModel(CreateLessonViewModel model)
        {
            var errors = new Dictionary<string, string>();

            // Check for duplicate lesson name
            if (await _lessonService.IsDuplicateLessonNameAsync(model.LessonName, model.ChapterId))
            {
                errors.Add("LessonName", "A lesson with this name already exists in this chapter.");
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
