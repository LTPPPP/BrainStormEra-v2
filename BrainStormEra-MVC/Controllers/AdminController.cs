using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize(Roles = "Admin,admin")]
    public class AdminController : Controller
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(BrainStormEraContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult AdminDashboard()
        {
            // Check if user is authenticated and is an admin
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["ErrorMessage"] = "You must be logged in to access the admin dashboard.";
                return RedirectToAction("Index", "Login");
            }
            var userRole = User?.FindFirst("UserRole")?.Value ?? ""; if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Access denied. You don't have permission to access the admin dashboard.";
                return RedirectToUserDashboard();
            }

            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Invalid user session. Please log in again.";
                return RedirectToAction("Index", "Login");
            }

            try
            {
                // Get admin details
                var admin = _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefault(a => a.UserId == userId);

                if (admin == null)
                {
                    TempData["ErrorMessage"] = "Admin account not found. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }

                // Get statistics for admin dashboard
                var totalUsers = _context.Accounts.Count();
                var totalCourses = _context.Courses.Count();
                var totalEnrollments = _context.Enrollments.Count(); var totalRevenue = _context.PaymentTransactions
                    .Where(pt => pt.TransactionStatus == "Success")
                    .Sum(pt => pt.Amount);

                var recentUsers = _context.Accounts
                    .OrderByDescending(a => a.AccountCreatedAt)
                    .Take(5)
                    .ToList();

                var recentCourses = _context.Courses
                    .Include(c => c.Author)
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Take(5)
                    .ToList();

                // Create the admin dashboard view model
                var viewModel = new AdminDashboardViewModel
                {
                    AdminName = admin.FullName ?? admin.Username,
                    AdminImage = admin.UserImage ?? "/img/defaults/default-avatar.svg",
                    TotalUsers = totalUsers,
                    TotalCourses = totalCourses,
                    TotalEnrollments = totalEnrollments,
                    TotalRevenue = totalRevenue,
                    RecentUsers = recentUsers.Select(u => new UserViewModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName ?? u.Username,
                        UserEmail = u.UserEmail,
                        UserRole = u.UserRole,
                        AccountCreatedAt = u.AccountCreatedAt,
                        IsBanned = u.IsBanned ?? false
                    }).ToList(),
                    RecentCourses = recentCourses.Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/img/defaults/default-course.svg",
                        Price = c.Price,
                        CreatedBy = c.Author.FullName ?? c.Author.Username,
                        Description = c.CourseDescription
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard for user ID: {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }        // Helper method to redirect user to appropriate dashboard based on role
        private IActionResult RedirectToUserDashboard()
        {
            var userRole = User?.FindFirst("UserRole")?.Value ?? "";

            if (string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("AdminDashboard", "Admin");
            }
            else if (string.Equals(userRole, "Instructor", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("InstructorDashboard", "Home");
            }
            else if (string.Equals(userRole, "Learner", StringComparison.OrdinalIgnoreCase))
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
