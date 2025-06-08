using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class QuizController : Controller
    {
        private readonly BrainStormEraContext _context;
        private readonly ILessonService _lessonService;

        public QuizController(BrainStormEraContext context, ILessonService lessonService)
        {
            _context = context;
            _lessonService = lessonService;
        }        // GET: Quiz/Create
        public async Task<IActionResult> Create(string chapterId)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.ChapterId == chapterId);

            if (chapter == null)
            {
                return NotFound();
            }

            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            if (chapter.Course.AuthorId != userId)
            {
                return Forbid();
            }

            // Get lessons in the chapter to find the last one
            var lessonsInChapter = await _lessonService.GetLessonsInChapterAsync(chapterId);
            var orderedLessons = lessonsInChapter.OrderBy(l => l.LessonOrder).ToList();

            // Find the last lesson in the chapter by highest LessonOrder
            var lastLesson = orderedLessons.LastOrDefault();

            var viewModel = new CreateQuizViewModel
            {
                ChapterId = chapterId,
                CourseId = chapter.CourseId,
                CourseName = chapter.Course.CourseName,
                ChapterName = chapter.ChapterName,
                // Automatically set the quiz to be associated with the last lesson
                LessonId = lastLesson?.LessonId,
                LessonName = lastLesson?.LessonName,
                // Keep available lessons for display purposes (but won't be editable)
                AvailableLessons = orderedLessons.Select(l => new QuizLessonViewModel
                {
                    LessonId = l.LessonId,
                    LessonName = l.LessonName,
                    LessonDescription = l.LessonDescription ?? "",
                    LessonOrder = l.LessonOrder,
                    LessonType = l.LessonType?.LessonTypeName ?? "Content",
                    EstimatedDuration = 0,
                    IsLocked = l.IsLocked ?? false
                }).ToList(),
                // Set default values
                MaxAttempts = 3,
                IsFinalQuiz = false,
                IsPrerequisiteQuiz = false,
                BlocksLessonCompletion = false
            };

            return View(viewModel);
        }        // POST: Quiz/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQuizViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate lesson data if validation fails
                var validationLessons = await _lessonService.GetLessonsInChapterAsync(model.ChapterId);
                var validationOrderedLessons = validationLessons.OrderBy(l => l.LessonOrder).ToList();
                var validationLastLesson = validationOrderedLessons.LastOrDefault();

                model.LessonId = validationLastLesson?.LessonId;
                model.LessonName = validationLastLesson?.LessonName;
                model.AvailableLessons = validationOrderedLessons.Select(l => new QuizLessonViewModel
                {
                    LessonId = l.LessonId,
                    LessonName = l.LessonName,
                    LessonDescription = l.LessonDescription ?? "",
                    LessonOrder = l.LessonOrder,
                    LessonType = l.LessonType?.LessonTypeName ?? "Content",
                    EstimatedDuration = 0,
                    IsLocked = l.IsLocked ?? false
                }).ToList();

                return View(model);
            }

            var chapter = await _context.Chapters
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.ChapterId == model.ChapterId);

            if (chapter == null)
            {
                return NotFound();
            }

            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            if (chapter.Course.AuthorId != userId)
            {
                return Forbid();
            }

            // Automatically find the last lesson in the chapter
            var chapterLessons = await _lessonService.GetLessonsInChapterAsync(model.ChapterId);
            var finalLesson = chapterLessons.OrderBy(l => l.LessonOrder).LastOrDefault();

            // Validate that the last lesson exists
            if (finalLesson == null)
            {
                ModelState.AddModelError("", "Cannot create quiz: No lessons found in this chapter. Please add at least one lesson first.");
                // Repopulate model data
                model.AvailableLessons = new List<QuizLessonViewModel>();
                return View(model);
            }

            var quiz = new Quiz
            {
                QuizId = Guid.NewGuid().ToString(),
                QuizName = model.QuizTitle,
                QuizDescription = model.QuizDescription,
                CourseId = model.CourseId,
                LessonId = finalLesson.LessonId, // Automatically associate with the last lesson
                TimeLimit = model.TimeLimit,
                PassingScore = model.PassingScore,
                QuizStatus = 1, // Active
                QuizCreatedAt = DateTime.Now,
                QuizUpdatedAt = DateTime.Now
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Quiz '{model.QuizTitle}' created successfully and automatically associated with the last lesson '{finalLesson.LessonName}'!";
            return Redirect($"/Course/Details/{chapter.CourseId}#curriculum");
        }        // GET: Quiz/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Lesson)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
            {
                return NotFound();
            }

            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            if (quiz.Course?.AuthorId != userId)
            {
                return Forbid();
            }

            // Get chapter info
            var chapter = await _context.Chapters
                .FirstOrDefaultAsync(c => c.CourseId == quiz.CourseId);

            if (chapter == null)
            {
                return NotFound("Chapter not found for this quiz.");
            }

            // Get lessons in the chapter to find the last one
            var lessonsInChapter = await _lessonService.GetLessonsInChapterAsync(chapter.ChapterId);
            var orderedLessons = lessonsInChapter.OrderBy(l => l.LessonOrder).ToList();
            var lastLesson = orderedLessons.LastOrDefault();

            var viewModel = new CreateQuizViewModel
            {
                QuizId = quiz.QuizId,
                QuizTitle = quiz.QuizName,
                QuizDescription = quiz.QuizDescription,
                ChapterId = chapter.ChapterId,
                CourseId = quiz.CourseId ?? "",
                CourseName = quiz.Course?.CourseName ?? "",
                ChapterName = chapter.ChapterName,
                // Always set to the last lesson (for consistency with create behavior)
                LessonId = lastLesson?.LessonId,
                LessonName = lastLesson?.LessonName,
                AvailableLessons = orderedLessons.Select(l => new QuizLessonViewModel
                {
                    LessonId = l.LessonId,
                    LessonName = l.LessonName,
                    LessonDescription = l.LessonDescription ?? "",
                    LessonOrder = l.LessonOrder,
                    LessonType = l.LessonType?.LessonTypeName ?? "Content",
                    EstimatedDuration = 0,
                    IsLocked = l.IsLocked ?? false
                }).ToList(),
                TimeLimit = quiz.TimeLimit,
                PassingScore = quiz.PassingScore,
                // Set default values for new properties
                MaxAttempts = 3,
                IsFinalQuiz = false,
                IsPrerequisiteQuiz = false,
                BlocksLessonCompletion = false
            };

            return View("Create", viewModel);
        }        // POST: Quiz/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreateQuizViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate lesson data if validation fails
                var validationLessons = await _lessonService.GetLessonsInChapterAsync(model.ChapterId);
                var validationOrderedLessons = validationLessons.OrderBy(l => l.LessonOrder).ToList();
                var validationLastLesson = validationOrderedLessons.LastOrDefault();

                model.LessonId = validationLastLesson?.LessonId;
                model.LessonName = validationLastLesson?.LessonName;
                model.AvailableLessons = validationOrderedLessons.Select(l => new QuizLessonViewModel
                {
                    LessonId = l.LessonId,
                    LessonName = l.LessonName,
                    LessonDescription = l.LessonDescription ?? "",
                    LessonOrder = l.LessonOrder,
                    LessonType = l.LessonType?.LessonTypeName ?? "Content",
                    EstimatedDuration = 0,
                    IsLocked = l.IsLocked ?? false
                }).ToList();

                return View("Create", model);
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == model.QuizId);

            if (quiz == null)
            {
                return NotFound();
            }

            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            if (quiz.Course?.AuthorId != userId)
            {
                return Forbid();
            }

            // Automatically find the last lesson in the chapter
            var chapterLessons = await _lessonService.GetLessonsInChapterAsync(model.ChapterId);
            var finalLesson = chapterLessons.OrderBy(l => l.LessonOrder).LastOrDefault();

            // Validate that the last lesson exists
            if (finalLesson == null)
            {
                ModelState.AddModelError("", "Cannot update quiz: No lessons found in this chapter.");
                model.AvailableLessons = new List<QuizLessonViewModel>();
                return View("Create", model);
            }

            quiz.QuizName = model.QuizTitle;
            quiz.QuizDescription = model.QuizDescription;
            quiz.LessonId = finalLesson.LessonId; // Always associate with the last lesson
            quiz.TimeLimit = model.TimeLimit;
            quiz.PassingScore = model.PassingScore;
            quiz.QuizUpdatedAt = DateTime.Now;

            _context.Update(quiz);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Quiz '{model.QuizTitle}' updated successfully and associated with the last lesson '{finalLesson.LessonName}'!";
            return Redirect($"/Course/Details/{quiz.CourseId}#curriculum");
        }

        // POST: Quiz/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
            {
                return NotFound();
            }

            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            if (quiz.Course?.AuthorId != userId)
            {
                return Forbid();
            }

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Quiz deleted successfully!";
            return Redirect($"/Course/Details/{quiz.CourseId}#curriculum");
        }
    }
}
