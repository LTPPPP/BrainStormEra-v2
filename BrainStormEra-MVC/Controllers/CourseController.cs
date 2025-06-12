using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using DataAccessLayer.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_MVC.Controllers
{
    public class CourseController : BaseController
    {
        private readonly CourseServiceImpl _courseServiceImpl;
        private readonly ILogger<CourseController> _logger;

        public CourseController(
            CourseServiceImpl courseServiceImpl,
            ILogger<CourseController> logger)
        {
            _courseServiceImpl = courseServiceImpl;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? category, int page = 1, int pageSize = 12)
        {
            var result = await _courseServiceImpl.GetCoursesAsync(User, search, category, page, pageSize);

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
            }

            return View("~/Views/Courses/Index.cshtml", result.ViewModel);
        }

        public async Task<IActionResult> Details(string id, string? tab = null)
        {
            var result = await _courseServiceImpl.GetCourseDetailsAsync(User, id, tab);

            if (!result.Success)
            {
                return NotFound();
            }

            ViewBag.IsAuthor = result.IsAuthor;
            ViewBag.CurrentUserId = result.CurrentUserId;
            ViewBag.ActiveTab = result.ActiveTab;

            return View("~/Views/Courses/Details.cshtml", result.ViewModel);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Enroll(string courseId)
        {
            var result = await _courseServiceImpl.EnrollInCourseAsync(User, courseId);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> SearchCourses(string? search, string? category, int page = 1, int pageSize = 12, string? sortBy = "newest")
        {
            var result = await _courseServiceImpl.SearchCoursesAsync(search, category, page, pageSize, sortBy);

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
            var result = await _courseServiceImpl.GetCourseForEditAsync(User, id);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController);
            }

            ViewBag.CourseId = result.CourseId;
            return View("~/Views/Courses/EditCourse.cshtml", result.ViewModel);
        }

        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(string id, CreateCourseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var viewModelResult = await _courseServiceImpl.GetCreateCourseViewModelAsync();
                model.AvailableCategories = viewModelResult.ViewModel?.AvailableCategories ?? new List<CourseCategoryViewModel>();
                TempData["ErrorMessage"] = "Please correct the errors below and try again.";
                ViewBag.CourseId = id;
                return View("~/Views/Courses/EditCourse.cshtml", model);
            }

            var result = await _courseServiceImpl.UpdateCourseAsync(User, id, model);

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
            var result = await _courseServiceImpl.DeleteCourseAsync(User, id);
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
    }
}

