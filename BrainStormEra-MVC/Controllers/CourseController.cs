using Microsoft.AspNetCore.Mvc;
using BrainStormEra_MVC.Services.Interfaces;
using BrainStormEra_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_MVC.Controllers
{
    public class CourseController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly ILogger<CourseController> _logger;

        public CourseController(ICourseService courseService, ILogger<CourseController> logger)
        {
            _courseService = courseService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? category, int page = 1, int pageSize = 12)
        {
            try
            {
                var viewModel = await _courseService.GetCoursesAsync(search, category, page, pageSize);
                return View("~/Views/Courses/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses");
                ViewBag.Error = "An error occurred while loading courses. Please try again later.";
                return View("~/Views/Courses/Index.cshtml", new CourseListViewModel());
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var viewModel = await _courseService.GetCourseDetailAsync(id);
                if (viewModel == null)
                {
                    return NotFound();
                }

                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirst("UserId")?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        viewModel.IsEnrolled = await _courseService.IsUserEnrolledAsync(userId, id);
                    }
                }

                viewModel.CanEnroll = !viewModel.IsEnrolled;
                return View("~/Views/Courses/Details.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course details for {CourseId}", id);
                return NotFound();
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Enroll(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var isAlreadyEnrolled = await _courseService.IsUserEnrolledAsync(userId, courseId);
                if (isAlreadyEnrolled)
                {
                    return Json(new { success = false, message = "Already enrolled in this course" });
                }

                var success = await _courseService.EnrollUserAsync(userId, courseId);
                if (success)
                {
                    return Json(new { success = true, message = "Successfully enrolled in course!" });
                }
                else
                {
                    return Json(new { success = false, message = "Course requires payment or enrollment failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling user in course {CourseId}", courseId);
                return Json(new { success = false, message = "An error occurred during enrollment" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchCourses(string? search, string? category, int page = 1, int pageSize = 12, string? sortBy = "newest")
        {
            try
            {
                var courses = await _courseService.SearchCoursesAsync(search, category, page, pageSize, sortBy);
                var totalCourses = courses.Count;
                var totalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

                var result = new
                {
                    success = true,
                    courses = courses,
                    totalCourses = totalCourses,
                    totalPages = totalPages,
                    currentPage = page,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching courses");
                return Json(new { success = false, message = "An error occurred while searching courses" });
            }
        }
    }
}
