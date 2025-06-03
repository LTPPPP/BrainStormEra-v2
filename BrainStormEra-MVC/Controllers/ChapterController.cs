using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BrainStormEra_MVC.Services.Interfaces;
using BrainStormEra_MVC.Models.ViewModels;
using System.Linq;

namespace BrainStormEra_MVC.Controllers
{
    public class ChapterController : BaseController
    {
        private readonly IChapterService _chapterService;
        private readonly ICourseService _courseService;
        private readonly ILogger<ChapterController> _logger;

        public ChapterController(
            IChapterService chapterService,
            ICourseService courseService,
            ILogger<ChapterController> logger)
        {
            _chapterService = chapterService;
            _courseService = courseService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Instructor,instructor")]
        public async Task<IActionResult> CreateChapter(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated";
                    return RedirectToAction("Index", "Login");
                }

                var model = await _chapterService.GetCreateChapterViewModelAsync(courseId, userId);
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Course not found or you are not authorized to add chapters to this course.";
                    return RedirectToAction("Details", "Course", new { id = courseId });
                }

                return View("~/Views/Chapters/CreateChapter.cshtml", model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the create chapter page.";
                return RedirectToAction("Details", "Course", new { id = courseId });
            }
        }
        [HttpPost]
        [Authorize(Roles = "Instructor,instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChapter(CreateChapterViewModel model)
        {
            var userId = User.FindFirst("UserId")?.Value;

            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated";
                    return RedirectToAction("Index", "Login");
                }

                // Additional server-side validation
                await ValidateChapterModel(model, userId);

                if (ModelState.IsValid)
                {
                    var chapterId = await _chapterService.CreateChapterAsync(model, userId);

                    TempData["SuccessMessage"] = $"Chapter '{model.ChapterName}' has been successfully created! You can now add lessons and content to it.";
                    _logger.LogInformation("Chapter created successfully: {ChapterId} by user {UserId}", chapterId, userId);

                    return RedirectToAction("Details", "Course", new { id = model.CourseId });
                }

                // If model state is invalid, reload the form with existing chapters
                await ReloadCreateChapterViewModel(model, userId);

                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                TempData["ErrorMessage"] = "Please correct the following errors: " + string.Join("; ", errorMessages);
                return View("~/Views/Chapters/CreateChapter.cshtml", model);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access attempt: {Message} by user {UserId}", ex.Message, userId);
                TempData["ErrorMessage"] = "You are not authorized to add chapters to this course.";
                return RedirectToAction("Details", "Course", new { id = model.CourseId });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Validation error creating chapter: {Message}", ex.Message);
                if (!string.IsNullOrEmpty(userId))
                {
                    await ReloadCreateChapterViewModel(model, userId);
                }
                TempData["ErrorMessage"] = ex.Message;
                return View("~/Views/Chapters/CreateChapter.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chapter for course {CourseId} by user {UserId}", model.CourseId, userId);
                TempData["ErrorMessage"] = "An unexpected error occurred while creating the chapter. Please try again.";
                return RedirectToAction("Details", "Course", new { id = model.CourseId });
            }
        }

        /// <summary>
        /// Validates chapter model with additional business rules
        /// </summary>
        private async Task ValidateChapterModel(CreateChapterViewModel model, string userId)
        {
            // Check if chapter name already exists in the course
            var existingChapter = await _chapterService.GetChaptersByCourseIdAsync(model.CourseId);
            if (existingChapter.Any(c => string.Equals(c.ChapterName.Trim(), model.ChapterName.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.ChapterName), "A chapter with this name already exists in the course.");
            }

            // Check if chapter order already exists
            if (existingChapter.Any(c => c.ChapterOrder == model.ChapterOrder))
            {
                ModelState.AddModelError(nameof(model.ChapterOrder), $"Chapter order {model.ChapterOrder} is already taken. Please choose a different order.");
            }

            // Validate unlock prerequisite
            if (model.IsLocked && !string.IsNullOrEmpty(model.UnlockAfterChapterId))
            {
                var prerequisiteChapter = existingChapter.FirstOrDefault(c => c.ChapterId == model.UnlockAfterChapterId);
                if (prerequisiteChapter == null)
                {
                    ModelState.AddModelError(nameof(model.UnlockAfterChapterId), "Selected prerequisite chapter not found.");
                }
                else if (prerequisiteChapter.ChapterOrder >= model.ChapterOrder)
                {
                    ModelState.AddModelError(nameof(model.UnlockAfterChapterId), "Prerequisite chapter must come before this chapter in the course sequence.");
                }
            }

            // Validate chapter name doesn't contain inappropriate content
            if (ContainsInappropriateContent(model.ChapterName))
            {
                ModelState.AddModelError(nameof(model.ChapterName), "Chapter name contains inappropriate content.");
            }
        }

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
        /// Simple content filter for inappropriate content
        /// </summary>
        private static bool ContainsInappropriateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            var inappropriateWords = new[] { "test", "dummy", "placeholder", "xxx", "delete", "remove" };
            return inappropriateWords.Any(word => content.ToLower().Contains(word.ToLower()));
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChapter(string id, string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var success = await _chapterService.DeleteChapterAsync(id, userId);
                if (success)
                {
                    return Json(new { success = true, message = "Chapter deleted successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Chapter not found or you are not authorized to delete this chapter." });
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while deleting the chapter." });
            }
        }
    }
}