using DataAccessLayer.Data;
using DataAccessLayer.Models;
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
        private readonly BrainStormEraContext _context;
        private readonly ILogger<HomeService> _logger;

        public HomeService(BrainStormEraContext context, ILogger<HomeService> logger)
        {
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
                var featuredCourses = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.IsFeatured == true && c.CourseStatus == 1)
                    .Include(c => c.Author)
                    .Include(c => c.Enrollments)
                    .Include(c => c.CourseCategories)
                    .Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/img/defaults/default-course.svg",
                        Price = c.Price,
                        CreatedBy = c.Author.FullName ?? c.Author.Username,
                        Description = c.CourseDescription,
                        EnrollmentCount = c.Enrollments.Count(),
                        CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList()
                    })
                    .OrderByDescending(c => c.EnrollmentCount)
                    .Take(6)
                    .ToListAsync();

                // Get categories ordered by course count (most courses first)
                var categories = await _context.CourseCategories
                    .AsNoTracking()
                    .Where(cc => cc.IsActive == true) // Only active categories
                    .Select(cc => new CourseCategoryViewModel
                    {
                        CategoryId = cc.CourseCategoryId,
                        CategoryName = cc.CourseCategoryName,
                        CourseCount = cc.Courses.Where(c => c.CourseStatus == 1).Count()
                    })
                    .Where(c => c.CourseCount > 0) // Only categories with courses
                    .OrderByDescending(c => c.CourseCount)
                    .Take(8) // Limit to top 8 categories
                    .ToListAsync();

                return new HomePageGuestViewModel
                {
                    RecommendedCourses = featuredCourses,
                    Categories = categories
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
                var user = await _context.Accounts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .Select(a => new { a.Username, a.FullName, a.UserImage })
                    .FirstOrDefaultAsync();

                if (user == null)
                    throw new ArgumentException("User not found", nameof(userId));

                // Get enrolled courses
                var enrolledCourses = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => e.UserId == userId && e.EnrollmentStatus == 1)
                    .Include(e => e.Course)
                    .ThenInclude(c => c.Author)
                    .Select(e => new EnrolledCourseViewModel
                    {
                        CourseId = e.Course.CourseId,
                        CourseName = e.Course.CourseName,
                        CourseImage = e.Course.CourseImage ?? "/img/defaults/default-course.svg",
                        AuthorName = e.Course.Author.FullName ?? e.Course.Author.Username,
                        CompletionPercentage = (int)(e.ProgressPercentage ?? 0)
                    })
                    .ToListAsync();

                // Get recommended courses
                var enrolledCourseIds = enrolledCourses.Select(ec => ec.CourseId).ToList();
                var recommendedCourses = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.IsFeatured == true &&
                               c.CourseStatus == 1 &&
                               !enrolledCourseIds.Contains(c.CourseId))
                    .Include(c => c.Author)
                    .Include(c => c.CourseCategories)
                    .Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/img/defaults/default-course.svg",
                        Price = c.Price,
                        CreatedBy = c.Author.FullName ?? c.Author.Username,
                        Description = c.CourseDescription,
                        CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList()
                    })
                    .Take(6)
                    .ToListAsync();

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
                    EnrolledCourses = enrolledCourses,
                    RecommendedCourses = recommendedCourses,
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
                var user = await _context.Accounts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .Select(a => new { a.Username, a.FullName, a.UserImage, a.PaymentPoint })
                    .FirstOrDefaultAsync();

                if (user == null)
                    throw new ArgumentException("User not found", nameof(userId));

                // Get instructor's courses
                var instructorCourses = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.AuthorId == userId)
                    .Include(c => c.Enrollments)
                    .Include(c => c.CourseCategories)
                    .Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/img/defaults/default-course.svg",
                        Price = c.Price,
                        CreatedBy = user.FullName ?? user.Username,
                        Description = c.CourseDescription,
                        EnrollmentCount = c.Enrollments.Count(),
                        CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList()
                    })
                    .ToListAsync();

                // Calculate statistics
                var totalCourses = instructorCourses.Count;
                var totalStudents = instructorCourses.Sum(c => c.EnrollmentCount);
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
                    RecentCourses = instructorCourses.Take(5).ToList(),
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
