using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BrainStormEra_MVC.Services.Interfaces;
using BrainStormEra_MVC.Models.ViewModels;

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
            try
            {
                if (ModelState.IsValid)
                {
                    var userId = User.FindFirst("UserId")?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        TempData["ErrorMessage"] = "User not authenticated";
                        return RedirectToAction("Index", "Login");
                    }

                    var chapterId = await _chapterService.CreateChapterAsync(model, userId);
                    TempData["SuccessMessage"] = "Chapter created successfully!";
                    return RedirectToAction("Details", "Course", new { id = model.CourseId });
                }

                // If model state is invalid, reload the form with existing chapters
                var userId2 = User.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(userId2))
                {
                    var viewModel = await _chapterService.GetCreateChapterViewModelAsync(model.CourseId, userId2);
                    if (viewModel != null)
                    {
                        model.CourseName = viewModel.CourseName;
                        model.ExistingChapters = viewModel.ExistingChapters;
                    }
                }

                TempData["ErrorMessage"] = "Please correct the errors below and try again.";
                return View("~/Views/Chapters/CreateChapter.cshtml", model);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "You are not authorized to add chapters to this course.";
                return RedirectToAction("Details", "Course", new { id = model.CourseId });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the chapter. Please try again.";
                return RedirectToAction("Details", "Course", new { id = model.CourseId });
            }
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