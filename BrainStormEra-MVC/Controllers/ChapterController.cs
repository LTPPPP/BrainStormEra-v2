using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Implementations;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class ChapterController : BaseController
    {
        private readonly IChapterService _chapterService;
        private readonly ILogger<ChapterController> _logger;

        public ChapterController(IChapterService chapterService, ILogger<ChapterController> logger) : base()
        {
            _chapterService = chapterService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> CreateChapter(string courseId)
        {
            // Use course ID directly without decoding
            var realCourseId = courseId;
            var result = await _chapterService.GetCreateChapterViewModelAsync(User, realCourseId);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController);
            }

            return View("~/Views/Chapters/CreateChapter.cshtml", result.ViewModel);
        }

        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChapter(CreateChapterViewModel model)
        {
            var result = await _chapterService.CreateChapterAsync(User, model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
                _logger.LogInformation("Chapter created successfully for course {CourseId}", model.CourseId);
                return RedirectToAction(result.RedirectAction, result.RedirectController, result.RouteValues);
            }

            if (result.ReturnView)
            {
                // Handle validation errors
                if (result.ValidationErrors != null)
                {
                    foreach (var error in result.ValidationErrors)
                    {
                        foreach (var message in error.Value)
                        {
                            ModelState.AddModelError(error.Key, message);
                        }
                    }
                }

                TempData["ErrorMessage"] = result.ErrorMessage;
                return View("~/Views/Chapters/CreateChapter.cshtml", result.ViewModel);
            }
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(result.RedirectAction, result.RedirectController, result.RouteValues);
        }

        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChapter(string id, string courseId)
        {
            // Use IDs directly without decoding
            var result = await _chapterService.DeleteChapterAsync(User, id, courseId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                _logger.LogInformation("Chapter deleted successfully: {ChapterId}", id);
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Details", "Course", new { id = courseId });
        }
        [HttpGet]
        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> EditChapter(string id)
        {
            // Use ID directly without decoding
            var result = await _chapterService.GetChapterForEditAsync(User, id);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController, result.RouteValues);
            }

            return View("~/Views/Chapters/EditChapter.cshtml", result.ViewModel);
        }
        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditChapter(string id, CreateChapterViewModel model)
        {
            // Use ID directly without decoding
            var result = await _chapterService.UpdateChapterAsync(User, id, model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
                _logger.LogInformation("Chapter updated successfully: {ChapterId}", id);
                return RedirectToAction(result.RedirectAction, result.RedirectController, result.RouteValues);
            }

            if (result.ReturnView)
            {
                // Handle validation errors
                if (result.ValidationErrors != null)
                {
                    foreach (var error in result.ValidationErrors)
                    {
                        foreach (var message in error.Value)
                        {
                            ModelState.AddModelError(error.Key, message);
                        }
                    }
                }

                TempData["ErrorMessage"] = result.ErrorMessage;
                return View("~/Views/Chapters/EditChapter.cshtml", result.ViewModel);
            }

            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(result.RedirectAction, result.RedirectController, result.RouteValues);
        }
    }
}

