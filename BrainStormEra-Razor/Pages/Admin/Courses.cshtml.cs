using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class CoursesModel : PageModel
    {
        private readonly ILogger<CoursesModel> _logger;
        private readonly IAdminService _adminService;


        public string? AdminName { get; set; }
        public string? UserId { get; set; }
        public AdminCoursesViewModel CoursesData { get; set; } = new AdminCoursesViewModel();

        // Filter and pagination properties
        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PriceFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? DifficultyFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? InstructorFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; } = "newest";

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 12;

        public CoursesModel(ILogger<CoursesModel> logger, IAdminService adminService)
        {
            _logger = logger;
            _adminService = adminService;
        }

        public async Task OnGetAsync()
        {
            try
            {
                AdminName = HttpContext.User?.Identity?.Name ?? "Admin";
                UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";

                // Load courses data with pagination and filters
                if (!string.IsNullOrEmpty(UserId))
                {
                    CoursesData = await _adminService.GetAllCoursesAsync(
                        search: SearchQuery,
                        categoryFilter: CategoryFilter,
                        statusFilter: StatusFilter,
                        priceFilter: PriceFilter,
                        difficultyFilter: DifficultyFilter,
                        instructorFilter: InstructorFilter,
                        sortBy: SortBy,
                        page: CurrentPage,
                        pageSize: PageSize
                    );
                }
                else
                {
                    _logger.LogWarning("User ID not found in claims");
                    CoursesData = new AdminCoursesViewModel();
                }

                _logger.LogInformation("Admin courses page accessed by: {AdminName} at {AccessTime}", AdminName, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses data for admin: {UserId}", UserId);
                CoursesData = new AdminCoursesViewModel();
            }
        }

        public async Task<IActionResult> OnGetCourseDetailsAsync(string courseId)
        {
            try
            {
                if (string.IsNullOrEmpty(courseId))
                {
                    return BadRequest("Course ID is required");
                }


                var courseDetails = await _adminService.GetCourseDetailsAsync(courseId);

                if (courseDetails == null)
                {
                    return NotFound("Course not found");
                }

                return new JsonResult(new { success = true, data = courseDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course details for course: {CourseId}", courseId);
                return new JsonResult(new { success = false, message = "An error occurred while getting course details" });
            }
        }

        public async Task<IActionResult> OnPostUpdateCourseStatusAsync(string courseId, bool isApproved)
        {
            try
            {
                if (string.IsNullOrEmpty(courseId))
                {
                    return BadRequest("Course ID is required");
                }


                var result = await _adminService.UpdateCourseStatusAsync(courseId, isApproved, UserId);

                if (result)
                {
                    _logger.LogInformation("Course status updated successfully by admin {AdminName} for course {CourseId}",
                        HttpContext.User?.Identity?.Name, courseId);
                    return new JsonResult(new { success = true, message = "Course status updated successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to update course status" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course status for course: {CourseId}", courseId);
                return new JsonResult(new { success = false, message = "An error occurred while updating course status" });
            }
        }

        public async Task<IActionResult> OnPostBanCourseAsync(string courseId)
        {
            try
            {
                if (string.IsNullOrEmpty(courseId))
                {
                    return BadRequest("Course ID is required");
                }


                var result = await _adminService.BanCourseAsync(courseId, UserId);

                if (result)
                {
                    _logger.LogInformation("Course banned successfully by admin {AdminName} for course {CourseId}",
                        HttpContext.User?.Identity?.Name, courseId);
                    return new JsonResult(new { success = true, message = "Course banned successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to ban course" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning course: {CourseId}", courseId);
                return new JsonResult(new { success = false, message = "An error occurred while banning course" });
            }
        }

        public async Task<IActionResult> OnPostDeleteCourseAsync(string courseId)
        {
            try
            {
                if (string.IsNullOrEmpty(courseId))
                {
                    return BadRequest("Course ID is required");
                }


                var result = await _adminService.DeleteCourseAsync(courseId);

                if (result)
                {
                    _logger.LogInformation("Course deleted successfully by admin {AdminName} for course {CourseId}",
                        HttpContext.User?.Identity?.Name, courseId);
                    return new JsonResult(new { success = true, message = "Course deleted successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to delete course" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course: {CourseId}", courseId);
                return new JsonResult(new { success = false, message = "An error occurred while deleting course" });
            }
        }
    }
}