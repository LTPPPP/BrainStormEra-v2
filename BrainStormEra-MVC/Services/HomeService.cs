using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrainStormEra_MVC.Services
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
                    CoursePicture = c.CourseImage ?? "/img/defaults/default-course.svg",
                    Price = c.Price,
                    CreatedBy = c.Author?.FullName ?? c.Author?.Username ?? "Unknown",
                    Description = c.CourseDescription,
                    EnrollmentCount = c.Enrollments?.Count ?? 0,
                    CourseCategories = c.CourseCategories?.Select(cc => cc.CourseCategoryName).ToList() ?? new List<string>()
                }).ToList();

                // Use repository for categories
                var categories = await _courseRepo.GetCategoriesWithCourseCountAsync(8);
                var categoryViewModels = categories.Select(cc => new CourseCategoryViewModel
                {
                    CategoryId = cc.CourseCategoryId,
                    CategoryName = cc.CourseCategoryName,
                    CourseCount = cc.Courses?.Count(c => c.CourseStatus == 1) ?? 0
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
                    CourseImage = e.Course?.CourseImage ?? "/img/defaults/default-course.svg",
                    AuthorName = e.Course?.Author?.FullName ?? e.Course?.Author?.Username ?? "Unknown",
                    CompletionPercentage = (int)(e.ProgressPercentage ?? 0)
                }).ToList();

                // Get recommended courses using repository
                var enrolledCourseIds = enrolledCourseViewModels.Select(ec => ec.CourseId).ToList();
                var recommendedCourses = await _courseRepo.GetRecommendedCoursesForUserAsync(userId, enrolledCourseIds, 6);
                var recommendedCourseViewModels = recommendedCourses.Select(c => new CourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CoursePicture = c.CourseImage ?? "/img/defaults/default-course.svg",
                    Price = c.Price,
                    CreatedBy = c.Author?.FullName ?? c.Author?.Username ?? "Unknown",
                    Description = c.CourseDescription,
                    CourseCategories = c.CourseCategories?.Select(cc => cc.CourseCategoryName).ToList() ?? new List<string>()
                }).ToList();

                // Get notifications (placeholder - implement actual notification system if needed)
                var notifications = new List<NotificationViewModel>
                {
                    new NotificationViewModel
                    {
                        NotificationId = "1",
                        Title = "Welcome to BrainStormEra!",
                        Message = "Start your learning journey today with our recommended courses.",
                        CreatedAt = DateTime.Now.AddDays(-1),
                        IsRead = false
                    }
                };

                return new LearnerDashboardViewModel
                {
                    UserName = user.Username,
                    FullName = user.FullName ?? user.Username,
                    UserImage = user.UserImage ?? "/img/default-avatar.svg",
                    EnrolledCourses = enrolledCourseViewModels,
                    RecommendedCourses = recommendedCourseViewModels,
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
                    CoursePicture = c.CourseImage ?? "/img/defaults/default-course.svg",
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
                    InstructorImage = user.UserImage ?? "/img/default-avatar.svg",
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
                    .Take(4)
                    .ToListAsync();

                return featuredCourses.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recommended courses");
                throw;
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

                // If no payment transaction data, simulate based on enrollments
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
                            Amount = g.Sum(e => e.Course.Price)
                        })
                        .OrderBy(g => g.Date)
                        .ToListAsync();

                    incomeData = enrollmentData;
                }

                return incomeData.Cast<dynamic>().ToList();
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
