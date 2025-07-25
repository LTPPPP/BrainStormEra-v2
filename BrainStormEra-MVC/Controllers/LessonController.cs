using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Implementations;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BrainStormEra_MVC.Filters;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class LessonController : BaseController
    {
        private readonly ILessonService _lessonService;
        private readonly ILogger<LessonController> _logger;

        public LessonController(ILessonService lessonService, ILogger<LessonController> logger) : base()
        {
            _lessonService = lessonService;
            _logger = logger;
        }

        // Step 1: Select Lesson Type
        [HttpGet]
        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> SelectLessonType(string chapterId)
        {
            // Use chapter ID directly without decoding
            var realChapterId = chapterId;

            if (string.IsNullOrEmpty(realChapterId))
            {
                return NotFound();
            }

            var result = await _lessonService.GetSelectLessonTypeViewModelAsync(realChapterId);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController);
            }

            ViewBag.ChapterId = realChapterId;
            return View(result.ViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectLessonType(SelectLessonTypeViewModel model)
        {
            var result = await _lessonService.ProcessSelectLessonTypeAsync(model, ModelState);

            if (result.Success && result.RedirectAction != null)
            {
                return RedirectToAction(result.RedirectAction, result.RedirectValues);
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return View(result.ViewModel);
        }

        // Step 2a: Create Video Lesson
        [HttpGet]
        public async Task<IActionResult> CreateVideoLesson(string chapterId)
        {
            // Decode hash ID to real ID
            var realChapterId = chapterId;
            var result = await _lessonService.GetCreateLessonViewModelAsync(realChapterId, 1); // Video type

            if (!result.Success)
            {
                if (result.ErrorMessage?.Contains("required") == true)
                    return BadRequest(result.ErrorMessage);
                else
                    return NotFound(result.ErrorMessage);
            }

            return View(result.ViewModel);
        }

        // Step 2b: Create Text Lesson
        [HttpGet]
        public async Task<IActionResult> CreateTextLesson(string chapterId)
        {
            // Decode hash ID to real ID
            var realChapterId = chapterId;
            var result = await _lessonService.GetCreateLessonViewModelAsync(realChapterId, 2); // Text type

            if (!result.Success)
            {
                if (result.ErrorMessage?.Contains("required") == true)
                    return BadRequest(result.ErrorMessage);
                else
                    return NotFound(result.ErrorMessage);
            }

            return View(result.ViewModel);
        }

        // Step 2c: Create Interactive/Document Lesson
        [HttpGet]
        public async Task<IActionResult> CreateInteractiveLesson(string chapterId)
        {
            // Decode hash ID to real ID
            var realChapterId = chapterId;
            var result = await _lessonService.GetCreateLessonViewModelAsync(realChapterId, 3); // Interactive type

            if (!result.Success)
            {
                if (result.ErrorMessage?.Contains("required") == true)
                    return BadRequest(result.ErrorMessage);
                else
                    return NotFound(result.ErrorMessage);
            }

            return View(result.ViewModel);
        }

        // Legacy method - for backward compatibility
        [HttpGet]
        public IActionResult CreateLesson(string chapterId)
        {
            // Redirect to new flow
            return RedirectToAction("SelectLessonType", chapterId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(CreateLessonViewModel model)
        {
            var result = await _lessonService.ProcessCreateLessonAsync(model, ModelState);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
                if (result.RedirectAction != null && result.RedirectController != null)
                {
                    return RedirectToAction(result.RedirectAction, result.RedirectController, result.RedirectValues);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }

                if (result.ValidationErrors != null)
                {
                    foreach (var error in result.ValidationErrors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }
                }

                if (result.RedirectAction != null && result.RedirectController != null)
                {
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }
            }

            return View(result.ViewModel);
        }

        // POST methods for specific lesson types
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVideoLesson(CreateLessonViewModel model)
        {
            model.LessonTypeId = 1; // Ensure video type
            var result = await _lessonService.ProcessCreateLessonAsync(model, ModelState);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController, result.RedirectValues);
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            if (result.ValidationErrors != null)
            {
                foreach (var error in result.ValidationErrors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
            }

            return View("CreateVideoLesson", result.ViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTextLesson(CreateLessonViewModel model)
        {
            model.LessonTypeId = 2; // Ensure text type
            var result = await _lessonService.ProcessCreateLessonAsync(model, ModelState);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController, result.RedirectValues);
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            if (result.ValidationErrors != null)
            {
                foreach (var error in result.ValidationErrors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
            }

            return View("CreateTextLesson", result.ViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInteractiveLesson(CreateLessonViewModel model)
        {
            model.LessonTypeId = 3; // Ensure interactive type
            var result = await _lessonService.ProcessCreateLessonAsync(model, ModelState);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController, result.RedirectValues);
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            if (result.ValidationErrors != null)
            {
                foreach (var error in result.ValidationErrors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
            }

            return View("CreateInteractiveLesson", result.ViewModel);
        }

        // Edit Lesson Methods        [HttpGet]
        public async Task<IActionResult> EditLesson(string id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Decode hash ID to real ID
            var realId = id;
            var result = await _lessonService.GetEditLessonViewModelAsync(realId, userId);

            if (!result.Success)
            {
                if (result.ErrorMessage?.Contains("required") == true)
                    return BadRequest(result.ErrorMessage);
                else if (result.ErrorMessage?.Contains("authenticated") == true)
                    return Unauthorized();
                else
                    return NotFound(result.ErrorMessage);
            }

            return View(result.ViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(string id, CreateLessonViewModel model)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Decode hash ID to real ID
            var realId = id;
            var result = await _lessonService.ProcessUpdateLessonAsync(realId, model, ModelState, userId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController, result.RedirectValues);
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            if (result.ValidationErrors != null)
            {
                foreach (var error in result.ValidationErrors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
            }

            if (result.RedirectAction != null)
            {
                return RedirectToAction(result.RedirectAction, result.RedirectValues);
            }

            return View(result.ViewModel);
        }
        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(string id, string courseId)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User not authenticated";
                return RedirectToAction("Details", "Course", courseId);
            }

            // Decode hash IDs to real IDs
            var realId = id;
            var realCourseId = courseId;
            var result = await _lessonService.ProcessDeleteLessonAsync(realId, userId, realCourseId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction(result.RedirectAction, result.RedirectController, result.RedirectValues);
        }

        // New Learn action for displaying lesson content
        [RequireAuthentication("You need to login to access lesson content. Please login to continue.")]
        public async Task<IActionResult> Learn(string id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Use ID directly without hash checks
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var result = await _lessonService.GetLessonLearningDataAsync(id, userId);

            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }
                if (result.IsUnauthorized)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Course");
                }

                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("Index", "Course");
            }

            // Pass additional data for sidebar
            ViewBag.Chapters = result.ViewModel?.Chapters;
            ViewBag.CourseDescription = result.ViewModel?.CourseDescription;

            return View(result.ViewModel);
        }

        // AJAX endpoint to mark lesson as completed
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAuthentication("You need to login to complete lessons.")]
        public async Task<IActionResult> MarkAsComplete(string lessonId)
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            if (string.IsNullOrEmpty(lessonId))
            {
                return Json(new { success = false, message = "Lesson ID is required" });
            }

            try
            {
                var result = await _lessonService.MarkLessonAsCompletedAsync(userId, lessonId);

                if (result)
                {
                    // Also update course progress in enrollment table
                    try
                    {
                        // Get the course ID from the lesson
                        var lesson = await _lessonService.GetLessonWithDetailsAsync(lessonId);
                        if (lesson?.Chapter?.CourseId != null)
                        {
                            var courseId = lesson.Chapter.CourseId;
                            var progressPercentage = await _lessonService.GetLessonCompletionPercentageAsync(userId, courseId);
                            await _lessonService.UpdateEnrollmentProgressAsync(userId, courseId, progressPercentage, lessonId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update course progress after completing lesson {LessonId} for user {UserId}", lessonId, userId);
                        // Don't fail the entire operation if progress update fails
                    }

                    return Json(new { success = true, message = "Lesson completed successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to mark lesson as completed. Please try again." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking lesson {LessonId} as completed for user {UserId}", lessonId, userId);
                return Json(new { success = false, message = "An error occurred while completing the lesson" });
            }
        }

        [HttpGet]
        [RequireAuthentication("You need to login to access lesson data.")]
        public async Task<IActionResult> GetLessonData(string lessonId)
        {
            if (string.IsNullOrEmpty(lessonId) || string.IsNullOrEmpty(CurrentUserId))
            {
                return Json(new { success = false, message = "Invalid request" });
            }

            try
            {
                var result = await _lessonService.GetLessonLearningDataAsync(lessonId, CurrentUserId);

                if (!result.Success || result.ViewModel == null)
                {
                    return Json(new { success = false, message = result.ErrorMessage ?? "Lesson data not found" });
                }

                // Get course progress
                var courseProgress = await _lessonService.GetLessonCompletionPercentageAsync(CurrentUserId, result.ViewModel.CourseId);

                return Json(new
                {
                    success = true,
                    chapters = result.ViewModel.Chapters,
                    courseProgress = courseProgress,
                    message = "Lesson data retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lesson data for lessonId: {LessonId}, userId: {UserId}", lessonId, CurrentUserId);
                return Json(new { success = false, message = "An error occurred while retrieving lesson data" });
            }
        }
    }
}

