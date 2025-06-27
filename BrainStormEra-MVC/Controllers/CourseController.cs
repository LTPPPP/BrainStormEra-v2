using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using DataAccessLayer.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using BrainStormEra_MVC.Filters;
using DataAccessLayer.Models;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class CourseController : BaseController
    {
        private readonly CourseServiceImpl _courseServiceImpl;
        private readonly ILogger<CourseController> _logger;

        public CourseController(CourseServiceImpl courseServiceImpl, ILogger<CourseController> logger) : base()
        {
            _courseServiceImpl = courseServiceImpl;
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
                if (result.Success)
                {
                    // Redirect to Details action with course ID
                    return RedirectToAction("Details", new { id = courseId });
                }
                return Json(new { success = false, message = result.Message });
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
    }
}

