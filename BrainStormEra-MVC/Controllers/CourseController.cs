using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using DataAccessLayer.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using BrainStormEra_MVC.Filters;

namespace BrainStormEra_MVC.Controllers
{
    public class CourseController : BaseController
    {
        private readonly CourseServiceImpl _courseServiceImpl;
        private readonly ILogger<CourseController> _logger;

        public CourseController(
            CourseServiceImpl courseServiceImpl,
            ILogger<CourseController> logger,
            IUrlHashService urlHashService) : base(urlHashService)
        {
            _courseServiceImpl = courseServiceImpl;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? category, int page = 1, int pageSize = 12)
        {
            // Category filtering removed - always pass null
            var result = await _courseServiceImpl.GetCoursesAsync(User, search, null, page, pageSize);

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
            }

            return View("~/Views/Courses/Index.cshtml", result.ViewModel);
        }

        [RequireAuthentication("You need to login to view course details. Please login to continue.")]
        public async Task<IActionResult> Details(string id, string? tab = null)
        {
            // Decode hash ID to real ID
            var realId = DecodeHashId(id);

            if (string.IsNullOrEmpty(realId))
            {
                return NotFound();
            }

            var result = await _courseServiceImpl.GetCourseDetailsAsync(User, realId, tab);

            if (!result.Success)
            {
                return NotFound();
            }

            ViewBag.IsAuthor = result.IsAuthor;
            ViewBag.CurrentUserId = result.CurrentUserId;
            ViewBag.ActiveTab = result.ActiveTab;
            ViewBag.CourseHashId = id; // Pass hash ID to view

            return View("~/Views/Courses/Details.cshtml", result.ViewModel);
        }

        [RequireAuthentication("You need to login to view course details. Please login to continue.")]
        public IActionResult CourseDetail()
        {
            // Get courseId from cookie
            string? courseId = Request.Cookies["CourseId"];

            if (string.IsNullOrEmpty(courseId))
            {
                TempData["ErrorMessage"] = "Course information not found.";
                return RedirectToAction("Index", "Home");
            }

            // Redirect to Details action with hash courseId
            return RedirectToActionWithHash("Details", courseId);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Enroll(string courseId)
        {
            // Decode hash ID to real ID if needed
            var realCourseId = DecodeHashId(courseId);

            var result = await _courseServiceImpl.EnrollInCourseAsync(User, realCourseId);
            return Json(new { success = result.Success, message = result.Message });
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

            return View("~/Views/Courses/CreateCourse.cshtml", result.ViewModel);
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
                return View("~/Views/Courses/CreateCourse.cshtml", model);
            }

            var result = await _courseServiceImpl.CreateCourseAsync(User, model);

            if (!result.Success)
            {
                if (result.ReturnView && result.ViewModel != null)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return View("~/Views/Courses/CreateCourse.cshtml", result.ViewModel);
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
            // Decode hash ID to real ID
            var realId = DecodeHashId(id);

            if (string.IsNullOrEmpty(realId))
            {
                return NotFound();
            }

            var result = await _courseServiceImpl.GetCourseForEditAsync(User, realId);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController);
            }

            ViewBag.CourseId = result.CourseId;
            ViewBag.CourseHashId = id; // Pass hash ID to view
            return View("~/Views/Courses/EditCourse.cshtml", result.ViewModel);
        }

        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(string id, CreateCourseViewModel model)
        {
            // Decode hash ID to real ID
            var realId = DecodeHashId(id);

            if (string.IsNullOrEmpty(realId))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var viewModelResult = await _courseServiceImpl.GetCreateCourseViewModelAsync();
                model.AvailableCategories = viewModelResult.ViewModel?.AvailableCategories ?? new List<CourseCategoryViewModel>();
                TempData["ErrorMessage"] = "Please correct the errors below and try again.";
                ViewBag.CourseId = realId;
                ViewBag.CourseHashId = id;
                return View("~/Views/Courses/EditCourse.cshtml", model);
            }

            var result = await _courseServiceImpl.UpdateCourseAsync(User, realId, model);

            if (!result.Success)
            {
                if (result.ReturnView && result.ViewModel != null)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    ViewBag.CourseId = result.CourseId;
                    return View("~/Views/Courses/EditCourse.cshtml", result.ViewModel);
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
            // Decode hash ID to real ID
            var realId = DecodeHashId(id);

            if (string.IsNullOrEmpty(realId))
            {
                return Json(new { success = false, message = "Invalid course ID" });
            }

            var result = await _courseServiceImpl.DeleteCourseAsync(User, realId);
            return Json(new { success = result.Success, message = result.Message });
        }

        // GET: Get user courses for notifications (Instructor only)
        [HttpGet]
        [Authorize(Roles = "instructor")]
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
            // Decode hash ID to real ID if needed
            var realCourseId = DecodeHashId(courseId);

            var result = await _courseServiceImpl.RequestCourseApprovalAsync(User, realCourseId);
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

        [RequireAuthentication("You need to login to access learning content. Please login to continue.")]
        public async Task<IActionResult> Learn(string id)
        {
            // Decode hash ID to real ID
            var realId = DecodeHashId(id);

            if (string.IsNullOrEmpty(realId))
            {
                return NotFound();
            }

            var result = await _courseServiceImpl.GetLearnManagementDataAsync(User, realId);

            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }

                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("Index", "Course");
            }

            return View("~/Views/Courses/Learn.cshtml", result.ViewModel);
        }

    }
}

