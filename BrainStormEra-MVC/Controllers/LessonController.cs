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

        public LessonController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }
        [HttpGet]
        public async Task<IActionResult> CreateLesson(string chapterId)
        {
            var chapter = await _lessonService.GetChapterByIdAsync(chapterId);
            if (chapter == null)
            {
                return NotFound();
            }

            var viewModel = new CreateLessonViewModel
            {
                ChapterId = chapterId,
                ChapterName = chapter.ChapterName,
                CourseName = chapter.Course.CourseName,
                Order = await _lessonService.GetNextLessonOrderAsync(chapterId),
                LessonTypes = await _lessonService.GetLessonTypesAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(CreateLessonViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate lesson name
                if (await _lessonService.IsDuplicateLessonNameAsync(model.LessonName, model.ChapterId))
                {
                    ModelState.AddModelError("LessonName", "A lesson with this name already exists in this chapter.");
                }
                else
                {
                    var lesson = new Lesson
                    {
                        LessonId = Guid.NewGuid().ToString(),
                        LessonName = model.LessonName,
                        LessonDescription = model.Description,
                        LessonContent = model.Content,
                        LessonTypeId = model.LessonTypeId,
                        LessonOrder = model.Order,
                        ChapterId = model.ChapterId,
                        LessonCreatedAt = DateTime.Now,
                        LessonUpdatedAt = DateTime.Now
                    };

                    if (await _lessonService.CreateLessonAsync(lesson))
                    {
                        TempData["SuccessMessage"] = "Lesson created successfully!";
                        return RedirectToAction("Details", "Courses", new { id = (await _lessonService.GetChapterByIdAsync(model.ChapterId))?.CourseId });
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "An error occurred while creating the lesson.";
                    }
                }
            }

            // Reload data for the view
            var chapter = await _lessonService.GetChapterByIdAsync(model.ChapterId);
            if (chapter != null)
            {
                model.ChapterName = chapter.ChapterName;
                model.CourseName = chapter.Course.CourseName;
            }
            model.LessonTypes = await _lessonService.GetLessonTypesAsync();

            return View(model);
        }
    }
}
