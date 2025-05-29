using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BrainStormEraContext _context;

        public HomeController(ILogger<HomeController> logger, BrainStormEraContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Authentication helper properties from BaseController
        protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;
        protected string? CurrentUserId => User?.FindFirst("UserId")?.Value;
        protected string? CurrentUsername => User?.Identity?.Name;
        protected string? CurrentUserRole => User?.FindFirst("UserRole")?.Value;
        protected string? CurrentUserFullName => User?.FindFirst("FullName")?.Value;
        protected string? CurrentUserEmail => User?.FindFirst(ClaimTypes.Email)?.Value;
        protected bool IsAdmin => CurrentUserRole == "Admin";
        protected bool IsInstructor => CurrentUserRole == "Instructor";
        protected bool IsLearner => CurrentUserRole == "Learner";

        protected object GetCurrentUserInfo()
        {
            return new
            {
                UserId = CurrentUserId,
                Username = CurrentUsername,
                Role = CurrentUserRole,
                FullName = CurrentUserFullName,
                Email = CurrentUserEmail
            };
        }

        protected string GetCurrentUserDisplayName()
        {
            return CurrentUserFullName ?? CurrentUsername ?? "Guest";
        }

        protected IActionResult RedirectToUserDashboard()
        {
            if (IsAdmin)
            {
                return RedirectToAction("AdminDashboard", "Admin");
            }
            else if (IsInstructor)
            {
                return RedirectToAction("InstructorDashboard", "Home");
            }
            else if (IsLearner)
            {
                return RedirectToAction("LearnerDashboard", "Home");
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid user role. Please contact support.";
                return RedirectToAction("Index", "Login");
            }
        }

        public IActionResult Index()
        {
            var viewModel = new HomePageGuestViewModel();

            try
            {
                // Kiểm tra kết nối database
                bool isDatabaseConnected = IsDatabaseConnected();
                if (!isDatabaseConnected)
                {
                    _logger.LogError("Cannot connect to database");
                    ViewBag.DatabaseError = "Cannot connect to database. Please check your connection settings.";
                    return View(viewModel);
                }

                // Check if user is authenticated and log their information
                if (IsAuthenticated)
                {
                    _logger.LogInformation("Authenticated user accessing home page: {Username} (Role: {Role})",
                        CurrentUsername, CurrentUserRole);

                    // Pass user information to view for personalization
                    ViewBag.CurrentUser = GetCurrentUserInfo();
                    ViewBag.IsAuthenticated = true;
                    ViewBag.UserDisplayName = GetCurrentUserDisplayName();
                }
                else
                {
                    ViewBag.IsAuthenticated = false;
                }

                // Get top 4 featured courses
                var recommendedCourses = _context.Courses
                    .Where(c => c.IsFeatured == true && c.CourseStatus == 1)
                    .Include(c => c.Author)
                    .Include(c => c.CourseCategories)
                    .Take(4)
                    .ToList(); if (recommendedCourses != null && recommendedCourses.Any())
                {
                    viewModel.RecommendedCourses = recommendedCourses.Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/images/default-course.jpg",
                        Price = c.Price,
                        CreatedBy = c.Author?.FullName ?? "Unknown Author",
                        Description = c.CourseDescription,
                        StarRating = (int)Math.Round(CalculateAverageRating(c.CourseId)),
                        EnrollmentCount = _context.Enrollments.Count(e => e.CourseId == c.CourseId),
                        CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList()
                    }).ToList();

                    _logger.LogInformation("Successfully loaded {Count} recommended courses", recommendedCourses.Count);
                }
                else
                {
                    viewModel.RecommendedCourses = new List<CourseViewModel>();
                    _logger.LogWarning("No featured courses found in database");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recommended courses");
                ViewBag.Error = "An error occurred while loading courses. Please try again later.";
            }

            return View(viewModel);
        }

        private bool IsDatabaseConnected()
        {
            try
            {
                return _context.Database.CanConnect();
            }
            catch
            {
                return false;
            }
        }
        private double CalculateAverageRating(string courseId)
        {
            try
            {
                var ratings = _context.Feedbacks
                    .Where(f => f.CourseId == courseId && f.StarRating.HasValue)
                    .Select(f => (double)f.StarRating!.Value)
                    .ToList();

                return ratings.Any() ? ratings.Average() : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average rating for course {CourseId}", courseId);
                return 0;
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize(Roles = "Learner")]
        public IActionResult LearnerDashboard()
        {
            try
            {
                if (!IsAuthenticated)
                {
                    TempData["ErrorMessage"] = "You must be logged in to access the dashboard.";
                    return RedirectToAction("Index", "Login");
                }

                if (!IsLearner)
                {
                    TempData["ErrorMessage"] = "Access denied. You don't have permission to access the learner dashboard.";
                    return RedirectToUserDashboard();
                }

                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Invalid user session. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }

                var user = _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefault(a => a.UserId == userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User account not found. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }
                var viewModel = new LearnerDashboardViewModel
                {
                    UserName = user.Username,
                    FullName = user.FullName ?? user.Username,
                    UserImage = user.UserImage ?? "/images/default-avatar.jpg"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading learner dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize(Roles = "Instructor")]
        public IActionResult InstructorDashboard()
        {
            try
            {
                if (!IsAuthenticated)
                {
                    TempData["ErrorMessage"] = "You must be logged in to access the dashboard.";
                    return RedirectToAction("Index", "Login");
                }

                if (!IsInstructor)
                {
                    TempData["ErrorMessage"] = "Access denied. You don't have permission to access the instructor dashboard.";
                    return RedirectToUserDashboard();
                }

                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Invalid user session. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }

                var instructor = _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefault(a => a.UserId == userId);

                if (instructor == null)
                {
                    TempData["ErrorMessage"] = "Instructor account not found. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }
                var viewModel = new InstructorDashboardViewModel
                {
                    InstructorName = instructor.FullName ?? instructor.Username,
                    InstructorImage = instructor.UserImage ?? "/images/default-avatar.jpg",
                    TotalCourses = _context.Courses.Count(c => c.AuthorId == userId),
                    TotalStudents = _context.Enrollments.Count(e => _context.Courses.Any(c => c.CourseId == e.CourseId && c.AuthorId == userId)),
                    TotalRevenue = 0 // Calculate actual revenue if needed
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading instructor dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again."; return RedirectToAction("Index", "Home");
            }
        }
    }
}
