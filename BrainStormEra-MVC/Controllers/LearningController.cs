using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BrainStormEra_MVC.Filters;
using DataAccessLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Controllers
{
    [RequireAuthentication("You need to login to access learning content. Please login to continue.")]
    public class LearningController : BaseController
    {
        private readonly ILearningService _learningService;
        private readonly IEnrollmentService _enrollmentService;
        private readonly ILogger<LearningController> _logger;
        private readonly BrainStormEraContext _context;

        public LearningController(
            ILearningService learningService,
            IEnrollmentService enrollmentService,
            ILogger<LearningController> logger,
            IUrlHashService urlHashService,
            BrainStormEraContext context) : base(urlHashService)
        {
            _learningService = learningService;
            _enrollmentService = enrollmentService;
            _logger = logger;
            _context = context;
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
                    _logger.LogWarning("GetCourseNavigation: User not authenticated");
                    return Unauthorized("User not authenticated");
                }



                // Decode hash ID to real ID
                var realCourseId = DecodeHashId(courseId);
                if (string.IsNullOrEmpty(realCourseId))
                {
                    _logger.LogWarning("GetCourseNavigation: Failed to decode course hash ID: {CourseId}", courseId);
                    return NotFound("Invalid course ID");
                }



                var navigation = await _learningService.GetCourseNavigationAsync(userId, realCourseId);
                if (navigation == null)
                {
                    _logger.LogWarning("GetCourseNavigation: Navigation data not found for course {RealCourseId}", realCourseId);
                    return NotFound("Course navigation not found");
                }



                return PartialView("_CourseNavigation", navigation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course navigation for course {CourseId}", courseId);
                return StatusCode(500, "An error occurred while loading navigation: " + ex.Message);
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

                // Additional validation for undefined/null lesson ID
                if (string.IsNullOrEmpty(lessonId) || lessonId == "undefined" || lessonId == "null")
                {
                    _logger.LogWarning("UpdateTimeSpent called with invalid lessonId: '{LessonId}' for user {UserId}", lessonId, userId);
                    return Json(new { success = false, message = "Invalid lesson ID provided" });
                }

                // Decode hash ID to real ID
                var realLessonId = DecodeHashId(lessonId);
                if (string.IsNullOrEmpty(realLessonId))
                {
                    _logger.LogWarning("UpdateTimeSpent: Failed to decode lesson hash ID '{LessonId}' for user {UserId}", lessonId, userId);
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

        // Debug endpoint to check navigation data
        [HttpGet]
        public async Task<IActionResult> DebugNavigation(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                var realCourseId = DecodeHashId(courseId);

                var debugInfo = new
                {
                    UserId = userId,
                    CourseHashId = courseId,
                    RealCourseId = realCourseId,
                    IsAuthenticated = !string.IsNullOrEmpty(userId),
                    CanDecodeHash = !string.IsNullOrEmpty(realCourseId)
                };

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(realCourseId))
                {
                    var navigation = await _learningService.GetCourseNavigationAsync(userId, realCourseId);
                    return Json(new
                    {
                        Debug = debugInfo,
                        HasNavigation = navigation != null,
                        ChapterCount = navigation?.Chapters?.Count ?? 0,
                        Navigation = navigation
                    });
                }

                return Json(new { Debug = debugInfo });
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        // Enroll in course
        [HttpPost]
        public async Task<IActionResult> Enroll(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Additional validation for undefined/null course ID
                if (string.IsNullOrEmpty(courseId) || courseId == "undefined" || courseId == "null")
                {
                    _logger.LogWarning("Enroll called with invalid courseId: '{CourseId}' for user {UserId}", courseId, userId);
                    return Json(new { success = false, message = "Invalid course ID provided" });
                }

                // Decode hash ID to real ID
                var realCourseId = DecodeHashId(courseId);
                if (string.IsNullOrEmpty(realCourseId))
                {
                    _logger.LogWarning("Enroll: Failed to decode course hash ID '{CourseId}' for user {UserId}", courseId, userId);
                    return Json(new { success = false, message = "Invalid course ID" });
                }

                // Check if already enrolled
                var isAlreadyEnrolled = await _enrollmentService.IsEnrolledAsync(userId, realCourseId);
                if (isAlreadyEnrolled)
                {
                    return Json(new { success = false, message = "You are already enrolled in this course" });
                }

                // Perform enrollment
                var enrollmentSuccess = await _enrollmentService.EnrollAsync(userId, realCourseId);
                if (enrollmentSuccess)
                {

                    return Json(new
                    {
                        success = true,
                        message = "Successfully enrolled in course!",
                        redirectUrl = Url.Action("Course", "Learning", new { id = EncodeToHash(realCourseId) })
                    });
                }

                return Json(new { success = false, message = "Failed to enroll in course. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling in course {CourseId}", courseId);
                return Json(new { success = false, message = "An error occurred while enrolling in the course" });
            }
        }

        // Check enrollment status
        [HttpGet]
        public async Task<IActionResult> CheckEnrollment(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { isEnrolled = false, canEnroll = false, message = "User not authenticated" });
                }

                var realCourseId = DecodeHashId(courseId);
                if (string.IsNullOrEmpty(realCourseId))
                {
                    return Json(new { isEnrolled = false, canEnroll = false, message = "Invalid course ID" });
                }

                var isEnrolled = await _enrollmentService.IsEnrolledAsync(userId, realCourseId);
                var canAccess = await _learningService.CanUserAccessCourseAsync(userId, realCourseId);

                return Json(new
                {
                    isEnrolled = isEnrolled,
                    canAccess = canAccess,
                    canEnroll = !isEnrolled,
                    message = isEnrolled ? "Already enrolled" : "Can enroll"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking enrollment for course {CourseId}", courseId);
                return Json(new { isEnrolled = false, canEnroll = false, message = "Error checking enrollment status" });
            }
        }

        // Get user's learning progress summary
        [HttpGet]
        public async Task<IActionResult> GetProgressSummary(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { error = "User not authenticated" });
                }

                var realCourseId = DecodeHashId(courseId);
                if (string.IsNullOrEmpty(realCourseId))
                {
                    return Json(new { error = "Invalid course ID" });
                }

                var coursePlayer = await _learningService.GetCoursePlayerAsync(userId, realCourseId);
                if (coursePlayer == null)
                {
                    return Json(new { error = "Course not found or access denied" });
                }

                var totalTimeSpent = await _learningService.GetTotalTimeSpentInCourseAsync(userId, realCourseId);
                var isCompleted = await _learningService.CheckCourseCompletionAsync(userId, realCourseId);

                return Json(new
                {
                    courseId = coursePlayer.CourseId,
                    courseName = coursePlayer.CourseName,
                    progressPercentage = coursePlayer.ProgressPercentage,
                    completedLessons = coursePlayer.CompletedLessons,
                    totalLessons = coursePlayer.TotalLessons,
                    totalTimeSpent = totalTimeSpent.ToString(@"hh\:mm\:ss"),
                    isCompleted = isCompleted,
                    currentLessonId = coursePlayer.CurrentLessonId,
                    lastAccessedDate = coursePlayer.LastAccessedDate,
                    estimatedTimeRemaining = coursePlayer.EstimatedTimeRemaining.ToString(@"hh\:mm\:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress summary for course {CourseId}", courseId);
                return Json(new { error = "An error occurred while getting progress summary" });
            }
        }

        // Reset course progress (for testing/admin purposes)
        [HttpPost]
        public async Task<IActionResult> ResetProgress(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var realCourseId = DecodeHashId(courseId);
                if (string.IsNullOrEmpty(realCourseId))
                {
                    return Json(new { success = false, message = "Invalid course ID" });
                }

                // Reset all lesson progress for this course
                await ResetCourseProgressAsync(userId, realCourseId);

                return Json(new { success = true, message = "Course progress reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting progress for course {CourseId}", courseId);
                return Json(new { success = false, message = "An error occurred while resetting progress" });
            }
        }

        // Helper method to reset course progress
        private async Task ResetCourseProgressAsync(string userId, string courseId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Reset user progress for all lessons in the course
                var userProgresses = await _context.UserProgresses
                    .Where(up => up.UserId == userId && up.Lesson.Chapter.CourseId == courseId)
                    .ToListAsync();

                _context.UserProgresses.RemoveRange(userProgresses);

                // Reset enrollment progress
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (enrollment != null)
                {
                    enrollment.ProgressPercentage = 0;
                    enrollment.CurrentLessonId = null;
                    enrollment.LastAccessedLessonId = null;
                    enrollment.EnrollmentUpdatedAt = DateTime.UtcNow;
                    _context.Enrollments.Update(enrollment);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();


            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}