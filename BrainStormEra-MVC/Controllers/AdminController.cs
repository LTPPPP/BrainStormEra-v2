using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataAccessLayer.Models;
using DataAccessLayer.Data;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize(Roles = "admin,Admin")]
    public class AdminController : Controller
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly AdminServiceImpl _adminService;

        public AdminController(BrainStormEraContext context, ILogger<AdminController> logger, AdminServiceImpl adminService)
        {
            _context = context;
            _logger = logger;
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
                var result = await _adminService.GetAdminDashboardAsync(User);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Message;

                    // Check if it's an authentication issue
                    if (result.Message.Contains("not authenticated") || result.Message.Contains("not found"))
                    {
                        return RedirectToAction("Index", "Login");
                    }

                    // Check if it's an authorization issue
                    if (result.Message.Contains("Access denied"))
                    {
                        return RedirectToUserDashboard();
                    }

                    return RedirectToAction("Index", "Home");
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AdminDashboard action");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageUsers(string? search = null, string? roleFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var result = await _adminService.GetAllUsersAsync(User, search, roleFilter, page, pageSize);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Message;

                    // Check if it's an authentication issue
                    if (result.Message.Contains("not authenticated") || result.Message.Contains("not found"))
                    {
                        return RedirectToAction("Index", "Login");
                    }

                    // Check if it's an authorization issue
                    if (result.Message.Contains("Access denied"))
                    {
                        return RedirectToUserDashboard();
                    }

                    return RedirectToAction("AdminDashboard");
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_AdminUserTable", result.Data);
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ManageUsers action");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading user management.";
                return RedirectToAction("AdminDashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageCourses(string? search = null, string? categoryFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var result = await _adminService.GetAllCoursesAsync(User, search, categoryFilter, page, pageSize);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Message;

                    // Check if it's an authentication issue
                    if (result.Message.Contains("not authenticated") || result.Message.Contains("not found"))
                    {
                        return RedirectToAction("Index", "Login");
                    }

                    // Check if it's an authorization issue
                    if (result.Message.Contains("Access denied"))
                    {
                        return RedirectToUserDashboard();
                    }

                    return RedirectToAction("AdminDashboard");
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_AdminCourseTable", result.Data);
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ManageCourses action");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading course management.";
                return RedirectToAction("AdminDashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserStatus(string userId, bool isBanned)
        {
            try
            {
                var result = await _adminService.UpdateUserStatusAsync(User, userId, isBanned);

                if (!result.IsSuccess)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, message = "User status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while updating user status" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var result = await _adminService.DeleteUserAsync(User, userId);

                if (!result.IsSuccess)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while deleting user" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCourseStatus(string courseId, bool isApproved)
        {
            try
            {
                var result = await _adminService.UpdateCourseStatusAsync(User, courseId, isApproved);

                if (!result.IsSuccess)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, message = "Course status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course status for course {CourseId}", courseId);
                return Json(new { success = false, message = "An error occurred while updating course status" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(string courseId)
        {
            try
            {
                var result = await _adminService.DeleteCourseAsync(User, courseId);

                if (!result.IsSuccess)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, message = "Course deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", courseId);
                return Json(new { success = false, message = "An error occurred while deleting course" });
            }
        }

        // Helper method to redirect user to appropriate dashboard based on role
        private IActionResult RedirectToUserDashboard()
        {
            var userRole = User?.FindFirst("UserRole")?.Value ?? "";

            if (string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("AdminDashboard", "Admin");
            }
            else if (string.Equals(userRole, "instructor", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("InstructorDashboard", "Home");
            }
            else if (string.Equals(userRole, "learner", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("LearnerDashboard", "Home");
            }
            else
            {
                _logger.LogWarning("Invalid user role detected in AdminController: '{Role}' for user: {UserId}",
                    userRole, User?.FindFirst("UserId")?.Value);
                TempData["ErrorMessage"] = "Invalid user role. Please contact support.";
                return RedirectToAction("Index", "Login");
            }
        }
    }
}

