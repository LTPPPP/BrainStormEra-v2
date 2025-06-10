using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Implementations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BrainStormEra_MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BrainStormEraContext _context;
        private readonly HomeServiceImpl _homeService;

        public HomeController(ILogger<HomeController> logger, BrainStormEraContext context, HomeServiceImpl homeService)
        {
            _logger = logger;
            _context = context;
            _homeService = homeService;
        }

        // Authentication helper properties
        protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;
        protected string? CurrentUserId => User?.FindFirst("UserId")?.Value;
        protected string? CurrentUsername => User?.Identity?.Name;
        protected string? CurrentUserRole => User?.FindFirst("UserRole")?.Value;
        protected string? CurrentUserFullName => User?.FindFirst("FullName")?.Value;
        protected string? CurrentUserEmail => User?.FindFirst(ClaimTypes.Email)?.Value; protected bool IsAdmin => string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);
        protected bool IsInstructor => string.Equals(CurrentUserRole, "Instructor", StringComparison.OrdinalIgnoreCase);
        protected bool IsLearner => string.Equals(CurrentUserRole, "Learner", StringComparison.OrdinalIgnoreCase); private async Task<List<dynamic>> GetRecommendedCoursesAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                // Get basic course info first (fastest query)
                var featuredCourses = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.IsFeatured == true && c.CourseStatus == 1)
                    .Select(c => new
                    {
                        c.CourseId,
                        c.CourseName,
                        c.CourseImage,
                        c.Price,
                        c.CourseDescription,
                        c.AuthorId
                    })
                    .Take(4)
                    .ToListAsync(cts.Token);

                if (!featuredCourses.Any())
                {
                    return new List<dynamic>();
                }
                var courseIds = featuredCourses.Select(c => c.CourseId).ToList();
                var authorIds = featuredCourses.Select(c => c.AuthorId).Distinct().ToList();

                // Run queries sequentially to avoid DbContext concurrency issues
                var authors = await GetAuthorNamesAsync(authorIds, cts.Token);
                var enrollmentCounts = await GetEnrollmentCountsAsync(courseIds, cts.Token);
                var averageRatings = await GetAverageRatingsAsync(courseIds, cts.Token);

                // Combine all data
                var recommendedCourses = new List<dynamic>();
                foreach (var course in featuredCourses)
                {
                    recommendedCourses.Add(new
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        CourseImage = course.CourseImage,
                        Price = course.Price,
                        CourseDescription = course.CourseDescription,
                        AuthorName = authors.ContainsKey(course.AuthorId) ? authors[course.AuthorId] : "Unknown Author",
                        CourseCategories = new List<string>(), // Load separately if needed
                        EnrollmentCount = enrollmentCounts.ContainsKey(course.CourseId) ? enrollmentCounts[course.CourseId] : 0,
                        AverageRating = averageRatings.ContainsKey(course.CourseId) ? averageRatings[course.CourseId] : 0.0
                    });
                }

                return recommendedCourses;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Course loading operation timed out after 30 seconds");
                return new List<dynamic>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recommended courses");
                return new List<dynamic>();
            }
        }

        private async Task<Dictionary<string, string>> GetAuthorNamesAsync(List<string> authorIds, CancellationToken cancellationToken)
        {
            try
            {
                return await _context.Accounts
                    .AsNoTracking()
                    .Where(a => authorIds.Contains(a.UserId))
                    .Select(a => new { a.UserId, a.FullName })
                    .ToDictionaryAsync(a => a.UserId, a => a.FullName ?? "Unknown Author", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw to be handled by the caller
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading author names");
                return authorIds.ToDictionary(id => id, _ => "Unknown Author");
            }
        }

        private async Task<Dictionary<string, int>> GetEnrollmentCountsAsync(List<string> courseIds, CancellationToken cancellationToken)
        {
            try
            {
                return await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => courseIds.Contains(e.CourseId))
                    .GroupBy(e => e.CourseId)
                    .Select(g => new { CourseId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.CourseId, x => x.Count, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw to be handled by the caller
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading enrollment counts");
                return courseIds.ToDictionary(id => id, _ => 0);
            }
        }
        private async Task<Dictionary<string, double>> GetAverageRatingsAsync(List<string> courseIds, CancellationToken cancellationToken)
        {
            try
            {
                return await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f => courseIds.Contains(f.CourseId) && f.StarRating.HasValue)
                    .GroupBy(f => f.CourseId)
                    .Select(g => new { CourseId = g.Key, AvgRating = g.Average(f => (double)f.StarRating!.Value) })
                    .ToDictionaryAsync(x => x.CourseId, x => x.AvgRating, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw to be handled by the caller
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading average ratings");
                return courseIds.ToDictionary(id => id, _ => 0.0);
            }
        }

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
            _logger.LogInformation("RedirectToUserDashboard - CurrentUserRole: '{Role}', IsAdmin: {IsAdmin}, IsInstructor: {IsInstructor}, IsLearner: {IsLearner}",
                CurrentUserRole, IsAdmin, IsInstructor, IsLearner);

            if (IsAdmin)
            {
                _logger.LogInformation("Redirecting admin user to Admin Dashboard");
                return RedirectToAction("AdminDashboard", "Admin");
            }
            else if (IsInstructor)
            {
                _logger.LogInformation("Redirecting instructor user to Instructor Dashboard");
                return RedirectToAction("InstructorDashboard", "Home");
            }
            else if (IsLearner)
            {
                _logger.LogInformation("Redirecting learner user to Learner Dashboard");
                return RedirectToAction("LearnerDashboard", "Home");
            }
            else
            {
                _logger.LogWarning("Invalid user role detected: '{Role}' for user: {UserId}", CurrentUserRole, CurrentUserId);
                TempData["ErrorMessage"] = "Invalid user role. Please contact support.";
                return RedirectToAction("Index", "Login");
            }
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                // Check if the user is authenticated and redirect if needed
                var redirectResult = await CheckAuthenticationAndRedirect();
                if (redirectResult != null)
                {
                    return redirectResult; // Redirect to dashboard if authenticated
                }

                // If we reach here, the user is not authenticated - show guest home page
                var result = await _homeService.GetGuestHomePageAsync();

                if (!result.Success)
                {
                    _logger.LogError("Error getting guest home page: {Message}", result.ErrorMessage);
                    ViewBag.Error = result.ErrorMessage;
                    return View(new HomePageGuestViewModel());
                }

                ViewBag.IsAuthenticated = false;
                return View(result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Index action");
                ViewBag.Error = "An unexpected error occurred. Please try again later.";
                return View(new HomePageGuestViewModel());
            }
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [Authorize(Roles = "Learner,learner")]
        public async Task<IActionResult> LearnerDashboard()
        {
            try
            {
                var result = await _homeService.GetLearnerDashboardAsync(User);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;

                    if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                    {
                        return RedirectToAction(result.RedirectAction, result.RedirectController);
                    }

                    if (result.RedirectToUserDashboard)
                    {
                        return RedirectToUserDashboard();
                    }

                    return RedirectToAction("Index", "Home");
                }

                return View(result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in LearnerDashboard action");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }
        [Authorize(Roles = "Instructor,instructor")]
        public async Task<IActionResult> InstructorDashboard()
        {
            try
            {
                var result = await _homeService.GetInstructorDashboardAsync(User);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;

                    if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                    {
                        return RedirectToAction(result.RedirectAction, result.RedirectController);
                    }

                    if (result.RedirectToUserDashboard)
                    {
                        return RedirectToUserDashboard();
                    }

                    return RedirectToAction("Index", "Home");
                }

                return View(result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in InstructorDashboard action");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Instructor,instructor")]
        public async Task<IActionResult> GetIncomeData(int days = 30)
        {
            try
            {
                var result = await _homeService.GetIncomeDataAsync(User, days);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }

                return Json(new { success = true, data = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetIncomeData action");
                return Json(new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Checks if a user is authenticated and redirects to appropriate dashboard
        /// </summary>
        /// <returns>Redirect action result if authenticated, null if not</returns>
        private async Task<IActionResult?> CheckAuthenticationAndRedirect()
        {
            if (!IsAuthenticated)
            {
                return null; // Not authenticated, continue with guest view
            }

            _logger.LogInformation("Authenticated user accessing page: {Username} (Role: {Role}), redirecting to dashboard",
                CurrentUsername, CurrentUserRole);

            try
            {
                // Redirect to appropriate dashboard based on user role
                return RedirectToUserDashboard();
            }
            catch (Exception ex)
            {
                // If we encounter any error during redirection (e.g., invalid role)
                _logger.LogError(ex, "Error redirecting authenticated user: {Username}, {Role}",
                    CurrentUsername, CurrentUserRole);

                // Clear the invalid authentication cookie
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = "Your session has expired or is invalid. Please log in again.";
                return RedirectToAction("Index", "Login");
            }
        }
    }
}
