using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using DataAccessLayer.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using BrainStormEra_MVC.Filters;
using DataAccessLayer.Models;
using BusinessLogicLayer.DTOs.Course;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class CourseController : BaseController
    {
        private readonly CourseServiceImpl _courseServiceImpl;
        private readonly IFeedbackService _feedbackService;
        private readonly ILogger<CourseController> _logger;

        public CourseController(
            CourseServiceImpl courseServiceImpl,
            IFeedbackService feedbackService,
            ILogger<CourseController> logger) : base()
        {
            _courseServiceImpl = courseServiceImpl;
            _feedbackService = feedbackService;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string search = "", string category = "", int page = 1)
        {
            var result = await _courseServiceImpl.GetCoursesAsync(User, search, category, page);
            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
            }
            return View(result.ViewModel);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                // Use ID directly without decoding
                var result = await _courseServiceImpl.GetCourseDetailsAsync(User, id);

                if (!result.Success)
                {
                    return NotFound();
                }

                ViewBag.IsAuthor = result.IsAuthor;
                ViewBag.CurrentUserId = result.CurrentUserId;
                ViewBag.ActiveTab = result.ActiveTab;
                ViewBag.CourseId = id;

                // Add user points to ViewBag if user is authenticated and is a learner
                if (User.Identity?.IsAuthenticated == true && User.IsInRole("learner"))
                {
                    try
                    {
                        using var scope = HttpContext.RequestServices.CreateScope();
                        var userRepo = scope.ServiceProvider.GetRequiredService<DataAccessLayer.Repositories.Interfaces.IUserRepo>();
                        var userId = User.FindFirst("UserId")?.Value;

                        if (!string.IsNullOrEmpty(userId))
                        {
                            var userWithPoints = await userRepo.GetUserWithPaymentPointAsync(userId);
                            ViewBag.UserPoints = userWithPoints?.PaymentPoint ?? 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not retrieve user points for course details");
                        ViewBag.UserPoints = 0;
                    }
                }

                // Check if user has certificate for this course
                if (User.Identity?.IsAuthenticated == true && result.ViewModel.IsEnrolled)
                {
                    try
                    {
                        using var scope = HttpContext.RequestServices.CreateScope();
                        var certificateRepo = scope.ServiceProvider.GetRequiredService<DataAccessLayer.Repositories.Interfaces.ICertificateRepo>();
                        var userId = User.FindFirst("UserId")?.Value;

                        if (!string.IsNullOrEmpty(userId))
                        {
                            var hasCertificate = await certificateRepo.HasValidCertificateAsync(userId, id);
                            ViewBag.HasCertificate = hasCertificate;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not check certificate status for course details");
                        ViewBag.HasCertificate = false;
                    }
                }

                return View(result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving course details for ID: {Id}", id);
                return NotFound();
            }
        }

        [HttpPost]
        [Authorize(Roles = "learner")]
        public async Task<IActionResult> EnrollInCourse(string courseId)
        {
            if (string.IsNullOrEmpty(courseId) || string.IsNullOrEmpty(CurrentUserId))
            {
                return Json(new { success = false, message = "Invalid request" });
            }

            try
            {
                // Use course ID directly without decoding
                var result = await _courseServiceImpl.EnrollInCourseAsync(User, courseId);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling user {UserId} in course {CourseId}", CurrentUserId, courseId);
                return Json(new { success = false, message = "An error occurred while enrolling in the course" });
            }
        }

        public async Task<IActionResult> Learn(string courseId)
        {
            // Use course ID directly without decoding
            var realCourseId = courseId;

            if (string.IsNullOrEmpty(realCourseId) || string.IsNullOrEmpty(CurrentUserId))
            {
                return NotFound();
            }

            try
            {
                var result = await _courseServiceImpl.GetLearnManagementDataAsync(User, realCourseId);

                if (!result.Success)
                {
                    if (result.IsNotFound)
                    {
                        return NotFound();
                    }

                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Course");
                }

                ViewBag.CourseId = realCourseId;
                return View(result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving course learn data for CourseId: {CourseId}, UserId: {UserId}", realCourseId, CurrentUserId);
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchCourses(
            string? courseSearch,
            string? categorySearch,
            int page = 1,
            int pageSize = 12,
            string? sortBy = "newest",
            string? price = null,
            string? difficulty = null,
            string? duration = null)
        {
            // Pass user info for role-based search (instructors see denied/pending, admins see deleted)
            var result = await _courseServiceImpl.SearchCoursesAsync(User, courseSearch, categorySearch, page, pageSize, sortBy, price, difficulty, duration);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }

            var response = new
            {
                success = true,
                courses = result.Courses,
                totalCourses = result.TotalCourses,
                totalPages = result.TotalPages,
                currentPage = result.CurrentPage,
                hasNextPage = result.HasNextPage,
                hasPreviousPage = result.HasPreviousPage
            };

            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> CreateCourse()
        {
            var result = await _courseServiceImpl.GetCreateCourseViewModelAsync();

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController);
            }

            return View("~/Views/Course/CreateCourse.cshtml", result.ViewModel);
        }

        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CreateCourseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var viewModelResult = await _courseServiceImpl.GetCreateCourseViewModelAsync();
                model.AvailableCategories = viewModelResult.ViewModel?.AvailableCategories ?? new List<CourseCategoryViewModel>();
                TempData["ErrorMessage"] = "Please correct the errors below and try again.";
                return View("~/Views/Course/CreateCourse.cshtml", model);
            }

            var result = await _courseServiceImpl.CreateCourseAsync(User, model);

            if (!result.Success)
            {
                if (result.ReturnView && result.ViewModel != null)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return View("~/Views/Course/CreateCourse.cshtml", result.ViewModel);
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }
            }

            TempData["SuccessMessage"] = result.SuccessMessage;
            if (!string.IsNullOrEmpty(result.WarningMessage))
            {
                TempData["WarningMessage"] = result.WarningMessage;
            }

            return RedirectToAction(result.RedirectAction, result.RedirectController);
        }

        [HttpGet]
        public async Task<IActionResult> SearchCategories(string term)
        {
            var categories = await _courseServiceImpl.SearchCategoriesAsync(term);
            return Json(categories);
        }

        [HttpGet]
        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> EditCourse(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var result = await _courseServiceImpl.GetCourseForEditAsync(User, id);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController);
            }

            ViewBag.CourseId = result.CourseId;
            return View("~/Views/Course/EditCourse.cshtml", result.ViewModel);
        }

        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(string id, CreateCourseViewModel model)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var viewModelResult = await _courseServiceImpl.GetCreateCourseViewModelAsync();
                model.AvailableCategories = viewModelResult.ViewModel?.AvailableCategories ?? new List<CourseCategoryViewModel>();
                TempData["ErrorMessage"] = "Please correct the errors below and try again.";
                ViewBag.CourseId = id;
                return View("~/Views/Course/EditCourse.cshtml", model);
            }

            var result = await _courseServiceImpl.UpdateCourseAsync(User, id, model);

            if (!result.Success)
            {
                if (result.ReturnView && result.ViewModel != null)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    ViewBag.CourseId = result.CourseId;
                    return View("~/Views/Course/EditCourse.cshtml", result.ViewModel);
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }
            }

            TempData["SuccessMessage"] = result.SuccessMessage;
            if (!string.IsNullOrEmpty(result.WarningMessage))
            {
                TempData["WarningMessage"] = result.WarningMessage;
            }

            return RedirectToAction(result.RedirectAction, result.RedirectController);
        }

        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "Invalid course ID" });
            }

            var result = await _courseServiceImpl.DeleteCourseAsync(User, id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // GET: Get user courses for notifications (Instructor only)
        [HttpGet]
        [Authorize(Roles = "instructor,admin")]
        public async Task<IActionResult> GetUserCourses()
        {
            var result = await _courseServiceImpl.GetUserCoursesAsync(User);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true, courses = result.Courses });
        }

        // POST: Request course approval (Instructor only)
        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCourseApproval(string courseId)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                return Json(new { success = false, message = "Invalid course ID" });
            }

            var result = await _courseServiceImpl.RequestCourseApprovalAsync(User, courseId);
            return Json(new { success = result.Success, message = result.Message });
        }

        // GET: Get updated course learn data for AJAX refresh
        [HttpGet]
        [Authorize(Roles = "learner")]
        public async Task<IActionResult> GetCourseLearnData(string courseId)
        {
            if (string.IsNullOrEmpty(courseId) || string.IsNullOrEmpty(CurrentUserId))
            {
                return Json(new { success = false, message = "Invalid request" });
            }

            try
            {
                var result = await _courseServiceImpl.GetLearnManagementDataAsync(User, courseId);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }

                return Json(new
                {
                    success = true,
                    progressPercentage = result.ViewModel.ProgressPercentage,
                    completedLessons = result.ViewModel.CompletedLessons,
                    totalLessons = result.ViewModel.TotalLessons,
                    chapters = result.ViewModel.Chapters,
                    message = "Course data retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course learn data for courseId: {CourseId}, userId: {UserId}", courseId, CurrentUserId);
                return Json(new { success = false, message = "An error occurred while retrieving course data" });
            }
        }

        // Temporary endpoint to clear cache for debugging
        [HttpPost]
        [Authorize(Roles = "admin")]
        public IActionResult ClearCache()
        {
            try
            {
                // This will be implemented via dependency injection
                // For now, return success
                return Json(new { success = true, message = "Cache cleared successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #region Review Actions

        // GET: Check if user can create review for a course
        [HttpGet]
        [Authorize(Roles = "learner")]
        public async Task<IActionResult> CheckReviewEligibility(string courseId)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                return Json(new { success = false, message = "Course ID is required" });
            }

            try
            {
                var result = await _feedbackService.CheckReviewEligibilityAsync(User, courseId);

                return Json(new
                {
                    success = true,
                    canCreateReview = result.CanCreateReview,
                    isEnrolled = result.IsEnrolled,
                    hasExistingReview = result.HasExistingReview,
                    existingReviewId = result.ExistingReviewId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking review eligibility for course {CourseId}", courseId);
                return Json(new { success = false, message = "An error occurred while checking review eligibility" });
            }
        }

        // POST: Create a new review
        [HttpPost]
        [Authorize(Roles = "learner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid review data" });
            }

            try
            {
                var result = await _feedbackService.CreateReviewAsync(User, request);
                return Json(new { success = result.Success, message = result.Message, reviewId = result.ReviewId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review for course {CourseId}", request.CourseId);
                return Json(new { success = false, message = "An error occurred while creating the review" });
            }
        }

        // POST: Update an existing review
        [HttpPost]
        [Authorize(Roles = "learner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReview([FromBody] UpdateReviewRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid review data" });
            }

            try
            {
                var result = await _feedbackService.UpdateReviewAsync(User, request);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review {ReviewId}", request.ReviewId);
                return Json(new { success = false, message = "An error occurred while updating the review" });
            }
        }

        // POST: Delete a review
        [HttpPost]
        [Authorize(Roles = "learner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview([FromBody] DeleteReviewRequest request)
        {
            if (string.IsNullOrEmpty(request.ReviewId))
            {
                return Json(new { success = false, message = "Review ID is required" });
            }

            try
            {
                var result = await _feedbackService.DeleteReviewAsync(User, request.ReviewId);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", request.ReviewId);
                return Json(new { success = false, message = "An error occurred while deleting the review" });
            }
        }

        // GET: Get reviews for a course
        [HttpGet]
        public async Task<IActionResult> GetCourseReviews(string courseId, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                return Json(new { success = false, message = "Course ID is required" });
            }

            try
            {
                var result = await _feedbackService.GetCourseReviewsAsync(courseId, page, pageSize);
                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new
                {
                    success = true,
                    reviews = result.ViewModel?.Reviews,
                    averageRating = result.ViewModel?.AverageRating,
                    totalReviews = result.ViewModel?.TotalReviews,
                    currentPage = result.ViewModel?.CurrentPage,
                    totalPages = result.ViewModel?.TotalPages,
                    hasNextPage = result.ViewModel?.HasNextPage,
                    hasPreviousPage = result.ViewModel?.HasPreviousPage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews for course {CourseId}", courseId);
                return Json(new { success = false, message = "An error occurred while loading reviews" });
            }
        }

        #endregion
    }
}

