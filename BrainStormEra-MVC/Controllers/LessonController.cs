using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Implementations;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class LessonController : BaseController
    {
        private readonly LessonServiceImpl _lessonServiceImpl;
        private readonly ILogger<LessonController> _logger;

        public LessonController(LessonServiceImpl lessonServiceImpl, ILogger<LessonController> logger, IUrlHashService urlHashService)
            : base(urlHashService)
        {
            _lessonServiceImpl = lessonServiceImpl;
            _logger = logger;
        }

        // Step 1: Select Lesson Type
        [HttpGet]
        public async Task<IActionResult> SelectLessonType(string chapterId)
        {
            // Decode hash ID to real ID
            var realChapterId = DecodeHashId(chapterId);
            var result = await _lessonServiceImpl.GetSelectLessonTypeViewModelAsync(realChapterId);

            if (!result.Success)
            {
                if (result.ErrorMessage?.Contains("required") == true)
                    return BadRequest(result.ErrorMessage);
                else
                    return NotFound(result.ErrorMessage);
            }

            return View(result.ViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectLessonType(SelectLessonTypeViewModel model)
        {
            var result = await _lessonServiceImpl.ProcessSelectLessonTypeAsync(model, ModelState);

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
            var realChapterId = DecodeHashId(chapterId);
            var result = await _lessonServiceImpl.GetCreateLessonViewModelAsync(realChapterId, 1); // Video type

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
            var realChapterId = DecodeHashId(chapterId);
            var result = await _lessonServiceImpl.GetCreateLessonViewModelAsync(realChapterId, 2); // Text type

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
            var realChapterId = DecodeHashId(chapterId);
            var result = await _lessonServiceImpl.GetCreateLessonViewModelAsync(realChapterId, 3); // Interactive type

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
            return RedirectToActionWithHash("SelectLessonType", chapterId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(CreateLessonViewModel model)
        {
            var result = await _lessonServiceImpl.ProcessCreateLessonAsync(model, ModelState);

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
            var result = await _lessonServiceImpl.ProcessCreateLessonAsync(model, ModelState);

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
            var result = await _lessonServiceImpl.ProcessCreateLessonAsync(model, ModelState);

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
            var result = await _lessonServiceImpl.ProcessCreateLessonAsync(model, ModelState);

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
            var realId = DecodeHashId(id);
            var result = await _lessonServiceImpl.GetEditLessonViewModelAsync(realId, userId);

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
            var realId = DecodeHashId(id);
            var result = await _lessonServiceImpl.ProcessUpdateLessonAsync(realId, model, ModelState, userId);

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
                return RedirectToActionWithHash("Details", "Course", courseId);
            }

            // Decode hash IDs to real IDs
            var realId = DecodeHashId(id);
            var realCourseId = DecodeHashId(courseId);
            var result = await _lessonServiceImpl.ProcessDeleteLessonAsync(realId, userId, realCourseId);

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
    }
}

