using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
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

        public HomeController(ILogger<HomeController> logger, BrainStormEraContext context)
        {
            _logger = logger;
            _context = context;
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
            var viewModel = new HomePageGuestViewModel();

            try
            {
                // Check if the user is authenticated and redirect if needed
                var redirectResult = await CheckAuthenticationAndRedirect();
                if (redirectResult != null)
                {
                    return redirectResult; // Redirect to dashboard if authenticated
                }

                // If we reach here, the user is not authenticated - show guest home page

                // Check database connection
                bool isDatabaseConnected = IsDatabaseConnected();
                if (!isDatabaseConnected)
                {
                    _logger.LogError("Cannot connect to database");
                    ViewBag.DatabaseError = "Cannot connect to database. Please check your connection settings.";
                    return View(viewModel);
                }

                // For guest users
                ViewBag.IsAuthenticated = false;

                // Get top 4 featured courses with timeout protection
                var recommendedCourses = await GetRecommendedCoursesAsync();

                if (recommendedCourses != null && recommendedCourses.Any())
                {
                    viewModel.RecommendedCourses = recommendedCourses.Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/img/default-course.png",
                        Price = c.Price,
                        CreatedBy = c.AuthorName,
                        Description = c.CourseDescription,
                        StarRating = (int)Math.Round(c.AverageRating),
                        EnrollmentCount = c.EnrollmentCount,
                        CourseCategories = c.CourseCategories
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

                // Get user data
                var user = await _context.Accounts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .Select(a => new { a.Username, a.FullName, a.UserImage })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User account not found. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }                // Get enrolled courses with progress
                var enrolledCourses = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => e.UserId == userId)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Author)
                    .Select(e => new EnrolledCourseViewModel
                    {
                        CourseId = e.CourseId,
                        CourseName = e.Course.CourseName,
                        CourseImage = e.Course.CourseImage ?? "/img/default-course.png",
                        AuthorName = e.Course.Author.FullName ?? e.Course.Author.Username,
                        EnrolledDate = e.EnrollmentCreatedAt,
                        LastAccessDate = e.EnrollmentUpdatedAt, // Use EnrollmentUpdatedAt as last access
                        CompletionPercentage = (int)(e.ProgressPercentage ?? 0) // Convert decimal to int
                    })
                    .ToListAsync();                // Get recommended courses (featured courses not already enrolled)
                var enrolledCourseIds = enrolledCourses.Select(ec => ec.CourseId).ToList();
                var recommendedCourses = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.IsFeatured == true &&
                               c.CourseStatus == 1 &&
                               !enrolledCourseIds.Contains(c.CourseId))
                    .Include(c => c.Author)
                    .Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/img/default-course.png",
                        Price = c.Price,
                        CreatedBy = c.Author.FullName ?? c.Author.Username,
                        Description = c.CourseDescription
                    })
                    .Take(6)
                    .ToListAsync();

                // Get notifications (you can create a Notification system)
                var notifications = new List<NotificationViewModel>
                {
                    new NotificationViewModel
                    {
                        NotificationId = "1",
                        Title = "Welcome to BrainStormEra!",
                        Message = "Start your learning journey today with our recommended courses.",
                        CreatedAt = DateTime.Now.AddDays(-1),
                        IsRead = false
                    },
                    new NotificationViewModel
                    {
                        NotificationId = "2",
                        Title = "New Course Available",
                        Message = "Check out the latest courses in Programming category.",
                        CreatedAt = DateTime.Now.AddDays(-3),
                        IsRead = true
                    }
                };

                var viewModel = new LearnerDashboardViewModel
                {
                    UserName = user.Username,
                    FullName = user.FullName ?? user.Username,
                    UserImage = user.UserImage ?? "/img/default-avatar.svg",
                    EnrolledCourses = enrolledCourses,
                    RecommendedCourses = recommendedCourses,
                    Notifications = notifications
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading learner dashboard for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }
        [Authorize(Roles = "Instructor,instructor")]
        public async Task<IActionResult> InstructorDashboard()
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

                // Get instructor basic data including PaymentPoint
                var user = await _context.Accounts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .Select(a => new { a.Username, a.FullName, a.UserImage, a.PaymentPoint })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Instructor account not found. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }

                // Get instructor's courses with detailed stats
                var instructorCourses = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.AuthorId == userId)
                    .Include(c => c.Enrollments)
                    .Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/img/default-course.png",
                        Price = c.Price,
                        CreatedBy = user.FullName ?? user.Username,
                        Description = c.CourseDescription,
                        EnrollmentCount = c.Enrollments.Count()
                    })
                    .ToListAsync();

                // Calculate statistics
                var totalCourses = instructorCourses.Count;
                var totalStudents = instructorCourses.Sum(c => c.EnrollmentCount);
                var totalRevenue = user.PaymentPoint ?? 0; // Use instructor's PaymentPoint as revenue

                // Get notifications for instructor
                var notifications = new List<NotificationViewModel>
                {
                    new NotificationViewModel
                    {
                        NotificationId = "1",
                        Title = "Welcome to Instructor Dashboard!",
                        Message = "Start creating amazing courses and help students learn.",
                        CreatedAt = DateTime.Now.AddDays(-1),
                        IsRead = false
                    },
                    new NotificationViewModel
                    {
                        NotificationId = "2",
                        Title = "Course Creation Tips",
                        Message = "Check out our guide for creating engaging course content.",
                        CreatedAt = DateTime.Now.AddDays(-3),
                        IsRead = true
                    }
                };

                var viewModel = new InstructorDashboardViewModel
                {
                    InstructorName = user.FullName ?? user.Username,
                    InstructorImage = user.UserImage ?? "/img/default-avatar.svg",
                    TotalCourses = totalCourses,
                    TotalStudents = totalStudents,
                    TotalRevenue = totalRevenue,
                    TotalReviews = 0, // Calculate if needed
                    AverageRating = 4.5, // Calculate if needed
                    RecentCourses = instructorCourses.Take(5).ToList(),
                    Notifications = notifications
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading instructor dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Instructor,instructor")]
        public async Task<IActionResult> GetIncomeData(int days = 30)
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var startDate = DateTime.Now.AddDays(-days);

                // Try to get instructor's income from payment transactions
                var incomeData = await _context.PaymentTransactions
                    .AsNoTracking()
                    .Include(pt => pt.Course)
                    .Where(pt => pt.Course.AuthorId == userId &&
                                pt.TransactionStatus == "Completed" &&
                                pt.PaymentDate.HasValue &&
                                pt.PaymentDate >= startDate &&
                                pt.PaymentDate <= DateTime.Now)
                    .GroupBy(pt => pt.PaymentDate!.Value.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Amount = g.Sum(pt => pt.NetAmount ?? pt.Amount)
                    })
                    .OrderBy(g => g.Date)
                    .ToListAsync();

                // If no payment transaction data, simulate some data based on enrollments
                if (!incomeData.Any())
                {
                    var enrollmentData = await _context.Enrollments
                        .AsNoTracking()
                        .Include(e => e.Course)
                        .Where(e => e.Course.AuthorId == userId &&
                                   e.EnrollmentCreatedAt >= startDate &&
                                   e.EnrollmentCreatedAt <= DateTime.Now)
                        .GroupBy(e => e.EnrollmentCreatedAt.Date)
                        .Select(g => new
                        {
                            Date = g.Key,
                            Amount = g.Sum(e => e.Course.Price) // Use course price as simulated income
                        })
                        .OrderBy(g => g.Date)
                        .ToListAsync();

                    incomeData = enrollmentData;
                }

                // Fill in missing dates with 0 income
                var result = new List<dynamic>();
                for (int i = days - 1; i >= 0; i--)
                {
                    var date = DateTime.Now.AddDays(-i).Date;
                    var income = incomeData.FirstOrDefault(x => x.Date == date);
                    result.Add(new
                    {
                        Date = date.ToString(days <= 7 ? "MMM dd" : days <= 30 ? "MMM dd" : "MMM yyyy"),
                        Amount = income?.Amount ?? 0
                    });
                }

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting income data for instructor: {UserId}", CurrentUserId);
                return Json(new { success = false, message = "Error loading income data" });
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
