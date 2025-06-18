using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BrainStormEra_MVC.Filters;

namespace BrainStormEra_MVC.Controllers
{
    [RequireAuthentication("You need to login to access learning content. Please login to continue.")]
    public class LearningController : BaseController
    {
        private readonly ILearningService _learningService;
        private readonly ILogger<LearningController> _logger;

        public LearningController(
            ILearningService learningService,
            ILogger<LearningController> logger,
            IUrlHashService urlHashService) : base(urlHashService)
        {
            _learningService = learningService;
            _logger = logger;
        }

        // Course player - main learning interface
        [HttpGet]
        public async Task<IActionResult> Course(string id)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Decode hash ID to real ID
                var realCourseId = DecodeHashId(id);
                if (string.IsNullOrEmpty(realCourseId))
                {
                    TempData["ErrorMessage"] = "Course not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Check if user is enrolled
                var isEnrolled = await _learningService.IsUserEnrolledInCourseAsync(userId, realCourseId);
                if (!isEnrolled)
                {
                    TempData["ErrorMessage"] = "You must be enrolled in this course to access it.";
                    return RedirectToActionWithHash("Details", "Course", realCourseId);
                }

                // Get course player data
                var coursePlayer = await _learningService.GetCoursePlayerAsync(userId, realCourseId);
                if (coursePlayer == null)
                {
                    TempData["ErrorMessage"] = "Unable to load course content.";
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.CourseHashId = id;
                return View(coursePlayer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course player for course {CourseId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the course.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Lesson viewer
        [HttpGet]
        public async Task<IActionResult> Lesson(string id)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Decode hash ID to real ID
                var realLessonId = DecodeHashId(id);
                if (string.IsNullOrEmpty(realLessonId))
                {
                    TempData["ErrorMessage"] = "Lesson not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Check if user can access this lesson
                var canAccess = await _learningService.CanUserAccessLessonAsync(userId, realLessonId);
                if (!canAccess)
                {
                    TempData["ErrorMessage"] = "You don't have access to this lesson.";
                    return RedirectToAction("Index", "Home");
                }

                // Get lesson detail
                var lessonDetail = await _learningService.GetLessonDetailAsync(userId, realLessonId);
                if (lessonDetail == null)
                {
                    TempData["ErrorMessage"] = "Unable to load lesson content.";
                    return RedirectToAction("Index", "Home");
                }

                // Set current lesson
                await _learningService.SetCurrentLessonAsync(userId, lessonDetail.CourseId, realLessonId);

                ViewBag.LessonHashId = id;
                ViewBag.CourseHashId = EncodeToHash(lessonDetail.CourseId);
                ViewBag.PreviousLessonHashId = !string.IsNullOrEmpty(lessonDetail.PreviousLessonId)
                    ? EncodeToHash(lessonDetail.PreviousLessonId) : null;
                ViewBag.NextLessonHashId = !string.IsNullOrEmpty(lessonDetail.NextLessonId)
                    ? EncodeToHash(lessonDetail.NextLessonId) : null;

                return View(lessonDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lesson {LessonId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the lesson.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Continue learning from where user left off
        [HttpGet]
        public async Task<IActionResult> Continue(string id)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Decode hash ID to real ID
                var realCourseId = DecodeHashId(id);
                if (string.IsNullOrEmpty(realCourseId))
                {
                    TempData["ErrorMessage"] = "Course not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Get current lesson
                var currentLessonId = await _learningService.GetCurrentLessonIdAsync(userId, realCourseId);
                if (!string.IsNullOrEmpty(currentLessonId))
                {
                    return RedirectToActionWithHash("Lesson", currentLessonId);
                }

                // If no current lesson, redirect to course overview
                return RedirectToActionWithHash("Course", realCourseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error continuing course {CourseId}", id);
                TempData["ErrorMessage"] = "An error occurred while continuing the course.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Get course navigation sidebar
        [HttpGet]
        public async Task<IActionResult> GetCourseNavigation(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Decode hash ID to real ID
                var realCourseId = DecodeHashId(courseId);
                if (string.IsNullOrEmpty(realCourseId))
                {
                    return NotFound();
                }

                var navigation = await _learningService.GetCourseNavigationAsync(userId, realCourseId);
                if (navigation == null)
                {
                    return NotFound();
                }

                return PartialView("_CourseNavigation", navigation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course navigation for course {CourseId}", courseId);
                return StatusCode(500, "An error occurred while loading navigation.");
            }
        }

        // Update lesson progress
        [HttpPost]
        public async Task<IActionResult> UpdateProgress([FromBody] LearningProgressUpdateRequest request)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Decode hash ID to real ID
                var realLessonId = DecodeHashId(request.LessonId);
                if (string.IsNullOrEmpty(realLessonId))
                {
                    return Json(new { success = false, message = "Invalid lesson ID" });
                }

                var success = await _learningService.UpdateLessonProgressAsync(
                    userId, realLessonId, request.CompletionPercentage, request.TimeSpentSeconds);

                if (success)
                {
                    // If lesson is completed, check if it unlocks next lessons
                    if (request.IsCompleted)
                    {
                        await _learningService.MarkLessonAsCompletedAsync(userId, realLessonId);
                    }

                    return Json(new { success = true, message = "Progress updated successfully" });
                }

                return Json(new { success = false, message = "Failed to update progress" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lesson progress");
                return Json(new { success = false, message = "An error occurred while updating progress" });
            }
        }

        // Mark lesson as completed
        [HttpPost]
        public async Task<IActionResult> CompleteLesson(string lessonId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Decode hash ID to real ID
                var realLessonId = DecodeHashId(lessonId);
                if (string.IsNullOrEmpty(realLessonId))
                {
                    return Json(new { success = false, message = "Invalid lesson ID" });
                }

                var success = await _learningService.MarkLessonAsCompletedAsync(userId, realLessonId);

                if (success)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Lesson completed successfully!",
                        nextLesson = await _learningService.GetNextLessonIdAsync(userId, realLessonId)
                    });
                }

                return Json(new { success = false, message = "Failed to mark lesson as completed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing lesson {LessonId}", lessonId);
                return Json(new { success = false, message = "An error occurred while completing the lesson" });
            }
        }

        // Update time spent in lesson
        [HttpPost]
        public async Task<IActionResult> UpdateTimeSpent(string lessonId, int seconds)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Decode hash ID to real ID
                var realLessonId = DecodeHashId(lessonId);
                if (string.IsNullOrEmpty(realLessonId))
                {
                    return Json(new { success = false, message = "Invalid lesson ID" });
                }

                var success = await _learningService.UpdateLessonTimeSpentAsync(userId, realLessonId, seconds);

                return Json(new { success = success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating time spent for lesson {LessonId}", lessonId);
                return Json(new { success = false, message = "An error occurred while updating time spent" });
            }
        }

        // Learning dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var dashboard = await _learningService.GetLearningDashboardAsync(userId);
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading learning dashboard for user {UserId}", User.FindFirst("UserId")?.Value);
                TempData["ErrorMessage"] = "An error occurred while loading your learning dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Check if lesson is unlocked
        [HttpGet]
        public async Task<IActionResult> CheckLessonAccess(string lessonId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { hasAccess = false, message = "User not authenticated" });
                }

                // Decode hash ID to real ID
                var realLessonId = DecodeHashId(lessonId);
                if (string.IsNullOrEmpty(realLessonId))
                {
                    return Json(new { hasAccess = false, message = "Invalid lesson ID" });
                }

                var hasAccess = await _learningService.CanUserAccessLessonAsync(userId, realLessonId);
                var isUnlocked = await _learningService.IsLessonUnlockedAsync(userId, realLessonId);

                return Json(new
                {
                    hasAccess = hasAccess,
                    isUnlocked = isUnlocked,
                    message = hasAccess ? "Access granted" : "Access denied"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking lesson access for lesson {LessonId}", lessonId);
                return Json(new { hasAccess = false, message = "An error occurred while checking access" });
            }
        }
    }
}