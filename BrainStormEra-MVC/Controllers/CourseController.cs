using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_MVC.Controllers
{
    public class CourseController : Controller
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<CourseController> _logger;

        public CourseController(BrainStormEraContext context, ILogger<CourseController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IActionResult> Index(string? search, string? category, int page = 1, int pageSize = 12)
        {
            try
            {
                var query = _context.Courses
                    .AsNoTracking()
                    .Where(c => c.CourseStatus == 1) // Only active courses
                    .Include(c => c.Author)
                    .Include(c => c.Enrollments)
                    .Include(c => c.CourseCategories)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.CourseName.Contains(search) ||
                                           c.CourseDescription!.Contains(search));
                }

                // Apply category filter
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(c => c.CourseCategories
                        .Any(cc => cc.CourseCategoryName == category));
                }

                // Get total count for pagination
                var totalCourses = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

                // Apply pagination
                var courses = await query
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/img/default-course.png",
                        Description = c.CourseDescription,
                        Price = c.Price,
                        CreatedBy = c.Author.FullName ?? c.Author.Username,
                        CourseCategories = c.CourseCategories
                            .Select(cc => cc.CourseCategoryName)
                            .ToList(),
                        EnrollmentCount = c.Enrollments.Count()
                    })
                    .ToListAsync();

                // Calculate star ratings (simplified for now)
                foreach (var course in courses)
                {
                    course.StarRating = 4; // Placeholder - you can implement actual rating calculation
                }

                // Get categories for filter dropdown
                var categories = await _context.CourseCategories
                    .AsNoTracking()
                    .Where(cc => cc.IsActive == true)
                    .Select(cc => new CourseCategoryViewModel
                    {
                        CategoryId = cc.CourseCategoryId,
                        CategoryName = cc.CourseCategoryName,
                        CourseCount = cc.Courses.Count(c => c.CourseStatus == 1)
                    })
                    .ToListAsync();

                var viewModel = new CourseListViewModel
                {
                    Courses = courses,
                    Categories = categories,
                    SearchQuery = search,
                    SelectedCategory = category,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalCourses = totalCourses,
                    PageSize = pageSize
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses");
                ViewBag.Error = "An error occurred while loading courses. Please try again later.";
                return View(new CourseListViewModel());
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var course = await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.Author)
                    .Include(c => c.Chapters.OrderBy(ch => ch.ChapterOrder))
                        .ThenInclude(ch => ch.Lessons.OrderBy(l => l.LessonOrder))
                    .Include(c => c.CourseCategories)
                    .Include(c => c.Feedbacks)
                        .ThenInclude(f => f.User)
                    .Include(c => c.Enrollments)
                    .FirstOrDefaultAsync(c => c.CourseId == id && c.CourseStatus == 1);

                if (course == null)
                {
                    return NotFound();
                }
                var viewModel = new CourseDetailViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription ?? "",
                    CourseImage = course.CourseImage ?? "/img/default-course.png",
                    Price = course.Price,
                    AuthorName = course.Author.FullName ?? course.Author.Username,
                    AuthorImage = course.Author.UserImage ?? "/img/default-avatar.png",
                    EstimatedDuration = course.EstimatedDuration ?? 0,
                    DifficultyLevel = GetDifficultyLevelText(course.DifficultyLevel),
                    Categories = course.CourseCategories
                        .Select(cc => cc.CourseCategoryName)
                        .ToList(),
                    TotalStudents = course.Enrollments.Count,
                    CourseCreatedAt = course.CourseCreatedAt,
                    CourseUpdatedAt = course.CourseUpdatedAt
                };                // Calculate ratings
                if (course.Feedbacks.Any(f => f.StarRating.HasValue))
                {
                    viewModel.AverageRating = (double)Math.Round((decimal)course.Feedbacks.Where(f => f.StarRating.HasValue).Average(f => f.StarRating!.Value), 1);
                    viewModel.TotalReviews = course.Feedbacks.Count;
                }// Map chapters and lessons
                viewModel.Chapters = course.Chapters.Select(ch => new ChapterViewModel
                {
                    ChapterId = ch.ChapterId,
                    ChapterName = ch.ChapterName,
                    ChapterDescription = ch.ChapterDescription ?? "",
                    ChapterOrder = ch.ChapterOrder ?? 0,
                    Lessons = ch.Lessons.Select(l => new LessonViewModel
                    {
                        LessonId = l.LessonId,
                        LessonName = l.LessonName,
                        LessonDescription = l.LessonDescription ?? "",
                        LessonOrder = l.LessonOrder,
                        EstimatedDuration = 0, // Default since property doesn't exist
                        IsLocked = l.IsLocked ?? false
                    }).ToList()
                }).ToList();                // Map reviews
                viewModel.Reviews = course.Feedbacks
                    .OrderByDescending(f => f.FeedbackCreatedAt)
                    .Take(10)
                    .Select(f => new ReviewViewModel
                    {
                        ReviewId = f.FeedbackId,
                        UserName = f.User.FullName ?? f.User.Username,
                        UserImage = f.User.UserImage ?? "/img/default-avatar.png",
                        StarRating = f.StarRating ?? 0,
                        ReviewComment = f.Comment ?? "",
                        ReviewDate = f.FeedbackCreatedAt,
                        IsVerifiedPurchase = f.IsVerifiedPurchase ?? false
                    }).ToList();

                // Check if user is enrolled (if authenticated)
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirst("UserId")?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        viewModel.IsEnrolled = await _context.Enrollments
                            .AnyAsync(e => e.UserId == userId && e.CourseId == id);
                        viewModel.CanEnroll = !viewModel.IsEnrolled;
                    }
                }
                else
                {
                    viewModel.CanEnroll = true;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course details for course {CourseId}", id);
                return NotFound();
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Enroll(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Check if already enrolled
                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (existingEnrollment != null)
                {
                    return Json(new { success = false, message = "Already enrolled in this course" });
                }

                // Get course details
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.CourseStatus == 1);

                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found" });
                }                // For free courses, enroll directly
                if (course.Price == 0)
                {
                    var enrollment = new Enrollment
                    {
                        EnrollmentId = Guid.NewGuid().ToString(),
                        UserId = userId,
                        CourseId = courseId,
                        EnrollmentCreatedAt = DateTime.UtcNow,
                        EnrollmentUpdatedAt = DateTime.UtcNow,
                        EnrollmentStatus = 1, // Active
                        ProgressPercentage = 0
                    };

                    _context.Enrollments.Add(enrollment);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Successfully enrolled in course!" });
                }
                else
                {
                    // For paid courses, redirect to payment
                    return Json(new
                    {
                        success = true,
                        requiresPayment = true,
                        redirectUrl = Url.Action("Payment", "Course", new { courseId = courseId })
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling user in course {CourseId}", courseId);
                return Json(new { success = false, message = "An error occurred during enrollment" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchCourses(string? search, string? category, int page = 1, int pageSize = 12, string? sortBy = "newest")
        {
            try
            {
                var query = _context.Courses
                    .AsNoTracking()
                    .Where(c => c.CourseStatus == 1) // Only active courses
                    .Include(c => c.Author)
                    .Include(c => c.Enrollments)
                    .Include(c => c.CourseCategories)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.CourseName.Contains(search) ||
                                           c.CourseDescription!.Contains(search) ||
                                           c.Author.FullName!.Contains(search));
                }

                // Apply category filter
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(c => c.CourseCategories
                        .Any(cc => cc.CourseCategoryName == category));
                }

                // Apply sorting
                query = sortBy switch
                {
                    "price_asc" => query.OrderBy(c => c.Price),
                    "price_desc" => query.OrderByDescending(c => c.Price),
                    "name_asc" => query.OrderBy(c => c.CourseName),
                    "name_desc" => query.OrderByDescending(c => c.CourseName),
                    "popular" => query.OrderByDescending(c => c.Enrollments.Count()),
                    _ => query.OrderByDescending(c => c.CourseCreatedAt) // newest (default)
                };

                // Get total count for pagination
                var totalCourses = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

                // Apply pagination
                var courses = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "/img/default-course.png",
                        Description = c.CourseDescription,
                        Price = c.Price,
                        CreatedBy = c.Author.FullName ?? c.Author.Username,
                        CourseCategories = c.CourseCategories
                            .Select(cc => cc.CourseCategoryName)
                            .ToList(),
                        EnrollmentCount = c.Enrollments.Count()
                    })
                    .ToListAsync();

                // Calculate star ratings (simplified for now)
                foreach (var course in courses)
                {
                    course.StarRating = 4; // Placeholder - you can implement actual rating calculation
                }

                var result = new
                {
                    success = true,
                    courses = courses,
                    totalCourses = totalCourses,
                    totalPages = totalPages,
                    currentPage = page,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching courses");
                return Json(new { success = false, message = "An error occurred while searching courses" });
            }
        }

        private string GetDifficultyLevelText(byte? level)
        {
            return level switch
            {
                1 => "Beginner",
                2 => "Intermediate",
                3 => "Advanced",
                _ => "Not specified"
            };
        }
    }
}
