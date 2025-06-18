using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Implementations;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_MVC.Controllers
{
    public class ChapterController : BaseController
    {
        private readonly ChapterServiceImpl _chapterServiceImpl;
        private readonly ILogger<ChapterController> _logger;

        public ChapterController(
            ChapterServiceImpl chapterServiceImpl,
            ILogger<ChapterController> logger,
            IUrlHashService urlHashService) : base(urlHashService)
        {
            _chapterServiceImpl = chapterServiceImpl;
            _logger = logger;
        }
        [HttpGet]
        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> CreateChapter(string courseId)
        {
            // Decode hash ID to real ID
            var realCourseId = DecodeHashId(courseId);
            var result = await _chapterServiceImpl.GetCreateChapterViewModelAsync(User, realCourseId);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController, result.RouteValues);
            }

            return View("~/Views/Chapters/CreateChapter.cshtml", result.ViewModel);
        }
        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChapter(CreateChapterViewModel model)
        {
            var result = await _chapterServiceImpl.CreateChapterAsync(User, model);

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
            // Decode hash IDs to real IDs
            var realId = DecodeHashId(id);
            var realCourseId = DecodeHashId(courseId);
            var result = await _chapterServiceImpl.DeleteChapterAsync(User, realId, realCourseId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                _logger.LogInformation("Chapter deleted successfully: {ChapterId}", id);
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToActionWithHash("Details", "Course", courseId);
        }
        [HttpGet]
        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> EditChapter(string id)
        {
            // Decode hash ID to real ID
            var realId = DecodeHashId(id);
            var result = await _chapterServiceImpl.GetChapterForEditAsync(User, realId);

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
            // Decode hash ID to real ID
            var realId = DecodeHashId(id);
            var result = await _chapterServiceImpl.UpdateChapterAsync(User, realId, model);

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

