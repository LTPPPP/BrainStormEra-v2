using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BrainStormEra_MVC.Filters;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize(Roles = "Instructor,instructor")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        }

        // GET: User Management Dashboard
        public async Task<IActionResult> Index(string? courseId = null, string? search = null, string? statusFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var instructorId = GetCurrentUserId();
                if (string.IsNullOrEmpty(instructorId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var viewModel = await _userService.GetUserManagementDataAsync(instructorId, courseId, search, statusFilter, page, pageSize);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_UserTable", viewModel);
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management page for instructor {InstructorId}", GetCurrentUserId());
                return View(new UserManagementViewModel { InstructorId = GetCurrentUserId(), InstructorName = "Unknown" });
            }
        }        // GET: User Detail
        [RequireAuthentication("You need to login to view user details. Please login to continue.")]
        public async Task<IActionResult> Detail(string userId)
        {
            try
            {
                var instructorId = GetCurrentUserId();
                if (string.IsNullOrEmpty(instructorId) || string.IsNullOrEmpty(userId))
                {
                    return NotFound();
                }

                var userDetail = await _userService.GetUserDetailForInstructorAsync(instructorId, userId);
                if (userDetail == null)
                {
                    return NotFound();
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_UserDetail", userDetail);
                }

                return View(userDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user detail for user {UserId} and instructor {InstructorId}", userId, GetCurrentUserId());
                return NotFound();
            }
        }

        // POST: Update User Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string userId, string courseId, int status)
        {
            try
            {
                var instructorId = GetCurrentUserId();
                if (string.IsNullOrEmpty(instructorId) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(courseId))
                {
                    return Json(new { success = false, message = "Invalid parameters" });
                }

                var result = await _userService.UpdateUserEnrollmentStatusAsync(instructorId, userId, courseId, status);

                if (result)
                {
                    var statusText = status switch
                    {
                        1 => "Active",
                        2 => "Suspended",
                        3 => "Completed",
                        _ => "Unknown"
                    };
                    return Json(new { success = true, message = $"User status updated to {statusText}" });
                }

                return Json(new { success = false, message = "Failed to update status" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status");
                return Json(new { success = false, message = "An error occurred while updating status" });
            }
        }

        // POST: Unenroll User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unenroll(string userId, string courseId)
        {
            try
            {
                var instructorId = GetCurrentUserId();
                if (string.IsNullOrEmpty(instructorId) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(courseId))
                {
                    return Json(new { success = false, message = "Invalid parameters" });
                }

                var result = await _userService.UnenrollUserFromCourseAsync(instructorId, userId, courseId);

                if (result)
                {
                    return Json(new { success = true, message = "User unenrolled successfully" });
                }

                return Json(new { success = false, message = "Failed to unenroll user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unenrolling user");
                return Json(new { success = false, message = "An error occurred while unenrolling user" });
            }
        }

        // POST: Bulk Actions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction([FromBody] BulkUserActionRequest request)
        {
            try
            {
                var instructorId = GetCurrentUserId();
                if (string.IsNullOrEmpty(instructorId) || request.UserIds == null || !request.UserIds.Any())
                {
                    return Json(new { success = false, message = "Invalid parameters" });
                }

                int affectedCount = 0;
                string message = "";

                switch (request.Action.ToLower())
                {
                    case "activate":
                        affectedCount = await _userService.BulkUpdateUserStatusAsync(instructorId, request.UserIds, request.CourseId ?? "", 1);
                        message = $"Activated {affectedCount} user(s)";
                        break;
                    case "suspend":
                        affectedCount = await _userService.BulkUpdateUserStatusAsync(instructorId, request.UserIds, request.CourseId ?? "", 2);
                        message = $"Suspended {affectedCount} user(s)";
                        break;
                    case "complete":
                        affectedCount = await _userService.BulkUpdateUserStatusAsync(instructorId, request.UserIds, request.CourseId ?? "", 3);
                        message = $"Marked {affectedCount} user(s) as completed";
                        break;
                    case "unenroll":
                        foreach (var userId in request.UserIds)
                        {
                            if (await _userService.UnenrollUserFromCourseAsync(instructorId, userId, request.CourseId ?? ""))
                            {
                                affectedCount++;
                            }
                        }
                        message = $"Unenrolled {affectedCount} user(s)";
                        break;
                    default:
                        return Json(new { success = false, message = "Invalid action" });
                }

                if (affectedCount > 0)
                {
                    return Json(new { success = true, message = message, affectedCount = affectedCount });
                }

                return Json(new { success = false, message = "No users were affected" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action");
                return Json(new { success = false, message = "An error occurred while performing bulk action" });
            }
        }

        // GET: Export Users Data
        public async Task<IActionResult> Export(string? courseId = null, string? search = null, string? statusFilter = null, string format = "csv")
        {
            try
            {
                var instructorId = GetCurrentUserId();
                if (string.IsNullOrEmpty(instructorId))
                {
                    return Unauthorized();
                }

                var users = await _userService.GetEnrolledUsersForInstructorAsync(instructorId, courseId, search, statusFilter, 1, int.MaxValue);

                if (format.ToLower() == "csv")
                {
                    var csv = GenerateCsv(users);
                    var fileName = $"enrolled_users_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
                }

                return BadRequest("Unsupported export format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting user data");
                return StatusCode(500, "An error occurred while exporting data");
            }
        }
        private string GenerateCsv(List<EnrolledUserViewModel> users)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Full Name,Email,Course Name,Enrollment Date,Progress %,Status,Last Access");

            foreach (var user in users)
            {
                csv.AppendLine($"\"{user.FullName}\",\"{user.Email}\",\"{user.CourseName}\"," +
                              $"\"{user.EnrollmentDate:yyyy-MM-dd}\",\"{user.ProgressPercentage}%\",\"{user.StatusText}\"," +
                              $"\"{user.LastAccessDate:yyyy-MM-dd HH:mm}\"");
            }

            return csv.ToString();
        }
    }
}

