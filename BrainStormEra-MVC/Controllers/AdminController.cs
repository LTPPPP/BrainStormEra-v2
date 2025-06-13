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
        }        // Helper method to redirect user to appropriate dashboard based on role
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

