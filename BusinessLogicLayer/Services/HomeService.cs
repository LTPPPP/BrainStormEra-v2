using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessLogicLayer.Constants;

namespace BusinessLogicLayer.Services
{    /// <summary>
     /// Service class that handles data access operations for Home functionality.
     /// This class implements the core data access layer for home page operations.
     /// </summary>
    public class HomeService : IHomeService
    {
        private readonly ICourseRepo _courseRepo;
        private readonly IUserRepo _userRepo;
        private readonly INotificationRepo _notificationRepo;
        private readonly BrainStormEraContext _context;
        private readonly ILogger<HomeService> _logger;

        public HomeService(
            ICourseRepo courseRepo,
            IUserRepo userRepo,
            INotificationRepo notificationRepo,
            BrainStormEraContext context,
            ILogger<HomeService> logger)
        {
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _notificationRepo = notificationRepo;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get home page data for guest users
        /// </summary>
        public async Task<HomePageGuestViewModel> GetGuestHomePageAsync()
        {
            try
            {
                // Use repository for featured courses
                var featuredCourses = await _courseRepo.GetFeaturedCoursesAsync(6);
                var courseViewModels = featuredCourses.Select(c => new CourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CoursePicture = c.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    Price = c.Price,
                    CreatedBy = c.Author?.FullName ?? c.Author?.Username ?? "Unknown",
                    Description = c.CourseDescription,
                    StarRating = 4, // Default rating - could be calculated from actual feedback in the future
                    EnrollmentCount = c.Enrollments?.Count ?? 0,
                    CourseCategories = c.CourseCategories?.Select(cc => cc.CourseCategoryName).ToList() ?? new List<string>()
                }).ToList();

                // Use repository for categories
                var categories = await _courseRepo.GetCategoriesWithCourseCountAsync(8);
                var categoryViewModels = categories.Select(cc => new CourseCategoryViewModel
                {
                    CategoryId = cc.CourseCategoryId,
                    CategoryName = cc.CourseCategoryName,
                    CourseCount = cc.Courses.Count() // Use the loaded courses count
                }).ToList();

                return new HomePageGuestViewModel
                {
                    RecommendedCourses = courseViewModels,
                    Categories = categoryViewModels
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving guest home page data");
                throw;
            }
        }

        /// <summary>
        /// Get learner dashboard data
        /// </summary>
        public async Task<LearnerDashboardViewModel> GetLearnerDashboardAsync(string userId)
        {
            try
            {
                // Use repository for user basic info
                var user = await _userRepo.GetUserBasicInfoAsync(userId);

                if (user == null)
                    throw new ArgumentException("User not found", nameof(userId));

                // Get enrolled courses using repository
                var enrolledCourses = await _courseRepo.GetUserEnrollmentsAsync(userId);
                var enrolledCourseViewModels = enrolledCourses.Select(e => new EnrolledCourseViewModel
                {
                    CourseId = e.Course?.CourseId ?? "",
                    CourseName = e.Course?.CourseName ?? "",
                    CourseImage = e.Course?.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    AuthorName = e.Course?.Author?.FullName ?? e.Course?.Author?.Username ?? "Unknown",
                    CompletionPercentage = (int)(e.ProgressPercentage ?? 0)
                }).ToList();

                // Get notifications (enhanced with dynamic content)
                var notifications = new List<NotificationViewModel>();

                // Add additional helpful notifications
                if (!enrolledCourseViewModels.Any())
                {
                    notifications.Add(new NotificationViewModel
                    {
                        NotificationId = "first_course",
                        Title = "Get Started with Your First Course",
                        Message = "Browse our course catalog and enroll in your first course to begin learning!",
                        CreatedAt = DateTime.Now.AddHours(-2),
                        IsRead = false
                    });
                }

                return new LearnerDashboardViewModel
                {
                    UserName = user.Username,
                    FullName = user.FullName ?? user.Username,
                    UserImage = user.UserImage ?? MediaConstants.Defaults.DefaultAvatarPath,
                    EnrolledCourses = enrolledCourseViewModels,
                    Notifications = notifications
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving learner dashboard data for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get instructor dashboard data
        /// </summary>
        public async Task<InstructorDashboardViewModel> GetInstructorDashboardAsync(string userId)
        {
            try
            {
                // Use repository for user data with payment point
                var user = await _userRepo.GetUserWithPaymentPointAsync(userId);

                if (user == null)
                    throw new ArgumentException("User not found", nameof(userId));

                // Use repository for instructor courses
                var instructorCourses = await _courseRepo.GetInstructorCoursesAsync(userId, null, null, 1, 50);
                var courseViewModels = instructorCourses.Select(c => new CourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CoursePicture = c.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    Price = c.Price,
                    CreatedBy = user.FullName ?? user.Username,
                    Description = c.CourseDescription,
                    EnrollmentCount = c.Enrollments?.Count ?? 0,
                    CourseCategories = c.CourseCategories?.Select(cc => cc.CourseCategoryName).ToList() ?? new List<string>()
                }).ToList();

                // Calculate statistics
                var totalCourses = courseViewModels.Count;
                var totalStudents = courseViewModels.Sum(c => c.EnrollmentCount);
                var totalRevenue = user.PaymentPoint ?? 0;

                // Get notifications (placeholder)
                var notifications = new List<NotificationViewModel>
                {
                    new NotificationViewModel
                    {
                        NotificationId = "1",
                        Title = "Welcome to Instructor Dashboard!",
                        Message = "Start creating amazing courses and help students learn.",
                        CreatedAt = DateTime.Now.AddDays(-1),
                        IsRead = false
                    }
                };

                return new InstructorDashboardViewModel
                {
                    InstructorName = user.FullName ?? user.Username,
                    InstructorImage = user.UserImage ?? MediaConstants.Defaults.DefaultAvatarPath,
                    TotalCourses = totalCourses,
                    TotalStudents = totalStudents,
                    TotalRevenue = totalRevenue,
                    TotalReviews = 0, // Calculate if needed
                    AverageRating = 4.5, // Calculate if needed
                    RecentCourses = courseViewModels.Take(5).ToList(),
                    Notifications = notifications
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving instructor dashboard data for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get recommended courses for home page
        /// </summary>
        public async Task<List<dynamic>> GetRecommendedCoursesAsync()
        {
            try
            {
                var recommendedCourses = new List<dynamic>();

                // First, try to get featured courses
                var featuredCourses = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.IsFeatured == true && c.CourseStatus == 1)
                    .Include(c => c.Author)
                    .Include(c => c.Enrollments)
                    .Include(c => c.CourseCategories)
                    .Select(c => new
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CourseImage = c.CourseImage,
                        Price = c.Price,
                        CourseDescription = c.CourseDescription,
                        AuthorName = c.Author.FullName ?? c.Author.Username,
                        EnrollmentCount = c.Enrollments.Count(),
                        CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList(),
                        AverageRating = 4.5 // Calculate if needed
                    })
                    .OrderByDescending(c => c.EnrollmentCount)
                    .Take(4)
                    .ToListAsync();

                recommendedCourses.AddRange(featuredCourses.Cast<dynamic>());

                // If no featured courses, fallback to popular courses
                if (!recommendedCourses.Any())
                {
                    var popularCourses = await _context.Courses
                        .AsNoTracking()
                        .Where(c => c.CourseStatus == 1)
                        .Include(c => c.Author)
                        .Include(c => c.Enrollments)
                        .Include(c => c.CourseCategories)
                        .Select(c => new
                        {
                            CourseId = c.CourseId,
                            CourseName = c.CourseName,
                            CourseImage = c.CourseImage,
                            Price = c.Price,
                            CourseDescription = c.CourseDescription,
                            AuthorName = c.Author.FullName ?? c.Author.Username,
                            EnrollmentCount = c.Enrollments.Count(),
                            CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList(),
                            AverageRating = 4.5
                        })
                        .OrderByDescending(c => c.EnrollmentCount)
                        .ThenByDescending(c => c.CourseId) // Use CourseId as a proxy for creation date
                        .Take(4)
                        .ToListAsync();

                    recommendedCourses.AddRange(popularCourses.Cast<dynamic>());
                }

                // If still no courses, fallback to any active courses
                if (!recommendedCourses.Any())
                {
                    var anyCourses = await _context.Courses
                        .AsNoTracking()
                        .Where(c => c.CourseStatus == 1)
                        .Include(c => c.Author)
                        .Include(c => c.Enrollments)
                        .Include(c => c.CourseCategories)
                        .Select(c => new
                        {
                            CourseId = c.CourseId,
                            CourseName = c.CourseName,
                            CourseImage = c.CourseImage,
                            Price = c.Price,
                            CourseDescription = c.CourseDescription,
                            AuthorName = c.Author.FullName ?? c.Author.Username,
                            EnrollmentCount = c.Enrollments.Count(),
                            CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList(),
                            AverageRating = 4.5
                        })
                        .Take(4)
                        .ToListAsync();

                    recommendedCourses.AddRange(anyCourses.Cast<dynamic>());
                }

                return recommendedCourses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recommended courses");
                // Return empty list instead of throwing to prevent crashes
                return new List<dynamic>();
            }
        }

        /// <summary>
        /// Get income data for instructor dashboard
        /// </summary>
        public async Task<List<dynamic>> GetIncomeDataAsync(string userId, int days = 30)
        {
            try
            {
                var startDate = DateTime.Now.AddDays(-days);

                // Note: Since PaymentTransaction no longer has CourseId, 
                // we simulate income based on enrollments for now
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
                        Amount = g.Sum(e => e.Course.Price)
                    })
                    .OrderBy(g => g.Date)
                    .ToListAsync();

                return enrollmentData.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving income data for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Check if database is connected
        /// </summary>
        public async Task<bool> IsDatabaseConnectedAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connection");
                return false;
            }
        }
    }
}







