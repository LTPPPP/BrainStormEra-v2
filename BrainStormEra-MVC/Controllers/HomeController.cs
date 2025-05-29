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
                // Check database connection
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

                // Get top 4 featured courses with timeout protection
                var recommendedCourses = await GetRecommendedCoursesAsync();

                if (recommendedCourses != null && recommendedCourses.Any())
                {
                    viewModel.RecommendedCourses = recommendedCourses.Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/images/default-course.jpg",
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
                        CourseImage = e.Course.CourseImage ?? "/images/default-course.jpg",
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
                        CoursePicture = c.CourseImage ?? "/images/default-course.jpg",
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
                    UserImage = user.UserImage ?? "/images/default-avatar.jpg",
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

                // Optimized query to get instructor data and statistics in one go
                var instructorData = await _context.Accounts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .Select(a => new
                    {
                        a.FullName,
                        a.Username,
                        a.UserImage,
                        TotalCourses = a.CourseAuthors.Count(),
                        TotalStudents = a.CourseAuthors.SelectMany(c => c.Enrollments).Count()
                    })
                    .FirstOrDefaultAsync();

                if (instructorData == null)
                {
                    TempData["ErrorMessage"] = "Instructor account not found. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }

                var viewModel = new InstructorDashboardViewModel
                {
                    InstructorName = instructorData.FullName ?? instructorData.Username,
                    InstructorImage = instructorData.UserImage ?? "/images/default-avatar.jpg",
                    TotalCourses = instructorData.TotalCourses,
                    TotalStudents = instructorData.TotalStudents,
                    TotalRevenue = 0 // Calculate actual revenue if needed
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
    }
}
