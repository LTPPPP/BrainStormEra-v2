using System;
using System.Diagnostics;
using System.Linq;
using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
                    return View(viewModel); // Trả về view với thông báo lỗi
                }

                // Get top 4 featured courses (based on the sequence diagram)
                var recommendedCourses = _context.Courses
                    .Where(c => c.IsFeatured == true && c.CourseStatus == 1) // Assuming 1 is active status
                    .Include(c => c.Author)
                    .Include(c => c.CourseCategories)
                    .Take(4)
                    .ToList();

                if (recommendedCourses != null && recommendedCourses.Any())
                {
                    // Courses found - map to view model
                    viewModel.RecommendedCourses = recommendedCourses.Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "~/lib/img/default-course.jpg",
                        Price = c.Price,
                        CreatedBy = c.Author.Username ?? "Unknown",
                        StarRating = CalculateAverageRating(c.CourseId),
                        CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList()
                    }).ToList();
                }
                // If no courses found, viewModel.RecommendedCourses will be an empty list
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recommended courses");
                ViewBag.Error = "An error occurred while loading courses. Please try again later.";
                // Keep viewModel.RecommendedCourses as an empty list
            }

            return View(viewModel);
        }

        private bool IsDatabaseConnected()
        {
            try
            {
                // Thử thực hiện một truy vấn đơn giản để kiểm tra kết nối
                _context.Database.CanConnect();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return false;
            }
        }

        private int CalculateAverageRating(string courseId)
        {
            try
            {
                // Calculate average rating based on feedback
                var ratings = _context.Feedbacks
                    .Where(f => f.CourseId == courseId && f.StarRating.HasValue)
                    .Select(f => (int)f.StarRating!.Value);

                if (!ratings.Any())
                    return 0;

                return (int)Math.Round(ratings.Average());
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

        // Learner dashboard
        [HttpGet]
        public IActionResult LearnerDashboard()
        {
            // Check if user is authenticated
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Index", "Login");
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Login");
            }

            try
            {
                // Get user details
                var user = _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefault(a => a.UserId == userId);

                if (user == null)
                {
                    return RedirectToAction("Index", "Login");
                }

                // Get enrolled courses
                var enrolledCourses = _context.Enrollments
                    .Where(e => e.UserId == userId)
                    .Include(e => e.Course)
                    .ThenInclude(c => c.Author)
                    .OrderByDescending(e => e.EnrollmentCreatedAt)
                    .Take(6)
                    .ToList();

                // Get recommended courses (not enrolled)
                var enrolledCourseIds = enrolledCourses.Select(ec => ec.CourseId).ToList();
                var recommendedCourses = _context.Courses
                    .Where(c => !enrolledCourseIds.Contains(c.CourseId) && c.CourseStatus == 1)
                    .Include(c => c.Author)
                    .OrderByDescending(c => c.IsFeatured)
                    .Take(6)
                    .ToList();

                // Get recent notifications
                var recentNotifications = _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.NotificationCreatedAt)
                    .Take(5)
                    .ToList();

                // Create the dashboard view model
                var viewModel = new LearnerDashboardViewModel
                {
                    UserName = user.Username,
                    FullName = user.FullName ?? user.Username,
                    UserImage = user.UserImage ?? "/img/default-avatar.png",
                    EnrolledCourses = enrolledCourses.Select(e => new EnrolledCourseViewModel
                    {
                        CourseId = e.CourseId,
                        CourseName = e.Course.CourseName,
                        CourseImage = e.Course.CourseImage ?? "/img/default-course.png",
                        AuthorName = e.Course.Author.FullName ?? e.Course.Author.Username,
                        EnrolledDate = e.EnrollmentCreatedAt,
                        LastAccessDate = null, // We'll need to get this from user progress
                        CompletionPercentage = CalculateCourseCompletionPercentage(userId, e.CourseId)
                    }).ToList(),
                    RecommendedCourses = recommendedCourses.Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/img/default-course.png",
                        Price = c.Price,
                        CreatedBy = c.Author.FullName ?? c.Author.Username,
                        Description = c.CourseDescription
                    }).ToList(),
                    Notifications = recentNotifications.Select(n => new NotificationViewModel
                    {
                        NotificationId = n.NotificationId,
                        Title = n.NotificationTitle,
                        Message = n.NotificationContent,
                        CreatedAt = n.NotificationCreatedAt,
                        IsRead = n.IsRead ?? false
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading learner dashboard for user ID: {UserId}", userId);
                return RedirectToAction("Index", "Home");
            }
        }

        // Helper method to calculate course completion percentage
        private int CalculateCourseCompletionPercentage(string userId, string courseId)
        {
            try
            {
                // Get total lessons in the course
                var totalLessons = _context.Lessons
                    .Count(l => l.Chapter.CourseId == courseId);

                if (totalLessons == 0)
                {
                    return 0;
                }

                // Get completed lessons
                var completedLessons = _context.UserProgresses
                    .Count(up => up.UserId == userId && up.Lesson.Chapter.CourseId == courseId && up.IsCompleted == true);

                // Calculate percentage
                return (int)Math.Round((double)completedLessons / totalLessons * 100);
            }
            catch
            {
                return 0;
            }
        }        // InstructorDashboard action
        [HttpGet]
        public IActionResult InstructorDashboard()
        {
            // Check if user is authenticated and is an instructor
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Index", "Login");
            }

            // If not an instructor, redirect to appropriate dashboard
            if (!User.IsInRole("Instructor"))
            {
                return RedirectToAction("LearnerDashboard");
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Login");
            }

            try
            {
                // Get instructor details
                var instructor = _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefault(a => a.UserId == userId);

                if (instructor == null)
                {
                    return RedirectToAction("Index", "Login");
                }                // Get instructor's courses with enrollment counts
                var courseIds = _context.Courses
                    .Where(c => c.AuthorId == userId)
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Take(6)
                    .Select(c => c.CourseId)
                    .ToList();

                // Create custom anonymous type for courses with enrollment count
                var coursesWithEnrollments = _context.Courses
                    .Where(c => courseIds.Contains(c.CourseId))
                    .Select(c => new
                    {
                        Course = c,
                        EnrollmentCount = _context.Enrollments.Count(e => e.CourseId == c.CourseId)
                    })
                    .ToList()
                    .Select(x => new CourseViewModel
                    {
                        CourseId = x.Course.CourseId,
                        CourseName = x.Course.CourseName,
                        CoursePicture = x.Course.CourseImage ?? "/images/default-course.jpg",
                        Price = x.Course.Price,
                        CreatedBy = instructor.FullName ?? instructor.Username,
                        Description = x.Course.CourseDescription,
                        StarRating = 5, // Default rating or calculate from reviews
                        EnrollmentCount = x.EnrollmentCount
                    })
                    .ToList();

                // Get total student count across all instructor's courses
                var totalStudents = _context.Enrollments
                    .Count(e => courseIds.Contains(e.CourseId));                // Create the instructor dashboard view model (you'll need to create this class)
                var viewModel = new InstructorDashboardViewModel
                {
                    InstructorName = instructor.FullName ?? instructor.Username,
                    InstructorImage = instructor.UserImage ?? "/img/default-avatar.png",
                    TotalCourses = coursesWithEnrollments.Count(),
                    TotalStudents = totalStudents,
                    TotalRevenue = coursesWithEnrollments.Sum(c => c.Price),
                    RecentCourses = coursesWithEnrollments
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading instructor dashboard for user ID: {UserId}", userId);
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
