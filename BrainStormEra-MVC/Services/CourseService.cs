using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BrainStormEra_MVC.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentService _enrollmentService;
        private readonly BrainStormEraContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CourseService> _logger;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

        public CourseService(
            ICourseRepository courseRepository,
            IEnrollmentService enrollmentService,
            BrainStormEraContext context,
            IMemoryCache cache,
            ILogger<CourseService> logger)
        {
            _courseRepository = courseRepository;
            _enrollmentService = enrollmentService;
            _context = context;
            _cache = cache;
            _logger = logger;
        }
        public async Task<CourseListViewModel> GetCoursesAsync(string? search, string? category, int page, int pageSize)
        {
            try
            {
                var query = _courseRepository.GetActiveCourses();

                query = query.Include(c => c.Author)
                           .Include(c => c.Enrollments)
                           .Include(c => c.CourseCategories);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.CourseName.Contains(search) ||
                                           c.CourseDescription!.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(c => c.CourseCategories
                        .Any(cc => cc.CourseCategoryName == category));
                }

                var totalCourses = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

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

                foreach (var course in courses)
                {
                    course.StarRating = 4;
                }

                var categories = await GetCategoriesAsync();

                return new CourseListViewModel
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses");
                return new CourseListViewModel();
            }
        }

        public async Task<CourseDetailViewModel?> GetCourseDetailAsync(string courseId)
        {
            try
            {
                var course = await _courseRepository.GetCourseDetailAsync(courseId);
                if (course == null) return null;

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
                    Categories = course.CourseCategories.Select(cc => cc.CourseCategoryName).ToList(),
                    TotalStudents = course.Enrollments.Count,
                    CourseCreatedAt = course.CourseCreatedAt,
                    CourseUpdatedAt = course.CourseUpdatedAt
                };

                if (course.Feedbacks.Any(f => f.StarRating.HasValue))
                {
                    viewModel.AverageRating = (double)Math.Round((decimal)course.Feedbacks.Where(f => f.StarRating.HasValue).Average(f => f.StarRating!.Value), 1);
                    viewModel.TotalReviews = course.Feedbacks.Count;
                }

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
                        LessonType = l.LessonType?.LessonTypeName ?? "Video",
                        EstimatedDuration = 0,
                        IsLocked = l.IsLocked ?? false
                    }).ToList()
                }).ToList();

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

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course detail {CourseId}", courseId);
                return null;
            }
        }
        public async Task<List<CourseViewModel>> SearchCoursesAsync(string? search, string? category, int page, int pageSize, string? sortBy)
        {
            try
            {
                var query = _courseRepository.GetActiveCourses();

                query = query.Include(c => c.Author)
                           .Include(c => c.Enrollments)
                           .Include(c => c.CourseCategories);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.CourseName.Contains(search) ||
                                           c.CourseDescription!.Contains(search) ||
                                           c.Author.FullName!.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(c => c.CourseCategories
                        .Any(cc => cc.CourseCategoryName == category));
                }

                query = sortBy switch
                {
                    "price_asc" => query.OrderBy(c => c.Price),
                    "price_desc" => query.OrderByDescending(c => c.Price),
                    "name_asc" => query.OrderBy(c => c.CourseName),
                    "name_desc" => query.OrderByDescending(c => c.CourseName),
                    "popular" => query.OrderByDescending(c => c.Enrollments.Count()),
                    _ => query.OrderByDescending(c => c.CourseCreatedAt)
                };

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
                        CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList(),
                        EnrollmentCount = c.Enrollments.Count()
                    })
                    .ToListAsync();

                foreach (var course in courses)
                {
                    course.StarRating = 4;
                }

                return courses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching courses");
                return new List<CourseViewModel>();
            }
        }

        public async Task<bool> EnrollUserAsync(string userId, string courseId)
        {
            try
            {
                var course = await _courseRepository.GetCourseByIdAsync(courseId);
                if (course == null || course.Price > 0) return false;

                return await _enrollmentService.EnrollAsync(userId, courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling user {UserId} in course {CourseId}", userId, courseId);
                return false;
            }
        }

        public async Task<bool> IsUserEnrolledAsync(string userId, string courseId)
        {
            return await _enrollmentService.IsEnrolledAsync(userId, courseId);
        }

        public async Task<List<CourseCategoryViewModel>> GetCategoriesAsync()
        {
            try
            {
                var cacheKey = "CourseCategories";

                if (_cache.TryGetValue(cacheKey, out List<CourseCategoryViewModel>? cachedCategories))
                {
                    return cachedCategories!;
                }

                _logger.LogInformation("Loading categories from database");

                var categories = await _context.CourseCategories
                    .AsNoTracking()
                    .Where(cc => cc.IsActive == true)
                    .Select(cc => new CourseCategoryViewModel
                    {
                        CategoryId = cc.CourseCategoryId,
                        CategoryName = cc.CourseCategoryName ?? "Unknown Category",
                        CategoryDescription = cc.CategoryDescription,
                        CategoryIcon = cc.CategoryIcon ?? "fas fa-tag",
                        CourseCount = cc.Courses.Count(c => c.CourseStatus == 1)
                    })
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} categories from database", categories.Count);

                // Log each category for debugging
                foreach (var category in categories)
                {
                    _logger.LogInformation("Category: ID={CategoryId}, Name={CategoryName}", category.CategoryId, category.CategoryName);
                }

                _cache.Set(cacheKey, categories, CacheExpiration);
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories from database");
                return new List<CourseCategoryViewModel>();
            }
        }

        public async Task<List<CategoryAutocompleteItem>> SearchCategoriesAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return new List<CategoryAutocompleteItem>();
                }

                _logger.LogInformation("Searching categories with term: {SearchTerm}", searchTerm);

                var categories = await _context.CourseCategories
                    .AsNoTracking()
                    .Where(cc => cc.IsActive == true && cc.CourseCategoryName.Contains(searchTerm))
                    .OrderBy(cc => cc.CourseCategoryName)
                    .Take(10)
                    .Select(cc => new CategoryAutocompleteItem
                    {
                        CategoryId = cc.CourseCategoryId,
                        CategoryName = cc.CourseCategoryName ?? "Unknown Category",
                        CategoryIcon = cc.CategoryIcon ?? "fas fa-tag"
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} categories for search term: {SearchTerm}", categories.Count, searchTerm);

                // Log each category for debugging
                foreach (var category in categories)
                {
                    _logger.LogInformation("Category: ID={CategoryId}, Name={CategoryName}", category.CategoryId, category.CategoryName);
                }

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching categories with term: {SearchTerm}", searchTerm);
                return new List<CategoryAutocompleteItem>();
            }
        }

        public async Task<string> CreateCourseAsync(CreateCourseViewModel model, string authorId)
        {
            try
            {
                var courseId = Guid.NewGuid().ToString();

                var course = new Course
                {
                    CourseId = courseId,
                    AuthorId = authorId,
                    CourseName = model.CourseName,
                    CourseDescription = model.CourseDescription,
                    Price = model.Price,
                    EstimatedDuration = model.EstimatedDuration,
                    DifficultyLevel = model.DifficultyLevel,
                    IsFeatured = model.IsFeatured,
                    EnforceSequentialAccess = model.EnforceSequentialAccess,
                    AllowLessonPreview = model.AllowLessonPreview,
                    CourseStatus = 2, // Inactive status
                    ApprovalStatus = "Pending",
                    CourseCreatedAt = DateTime.UtcNow,
                    CourseUpdatedAt = DateTime.UtcNow,
                    CourseImage = "/img/default-course.png" // Default image, will be updated if file is uploaded
                };

                _context.Courses.Add(course);

                // Add categories
                if (model.SelectedCategories.Any())
                {
                    var categories = await _context.CourseCategories
                        .Where(cc => model.SelectedCategories.Contains(cc.CourseCategoryId))
                        .ToListAsync();

                    foreach (var category in categories)
                    {
                        course.CourseCategories.Add(category);
                    }
                }

                await _context.SaveChangesAsync();

                // Clear cache since we added a new course
                _cache.Remove("CourseCategories");

                return courseId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course for author {AuthorId}", authorId);
                throw;
            }
        }

        public async Task<bool> UpdateCourseImageAsync(string courseId, string imagePath)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    _logger.LogWarning("Course not found for image update: {CourseId}", courseId);
                    return false;
                }

                course.CourseImage = imagePath;
                course.CourseUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Course image updated successfully for course {CourseId}: {ImagePath}", courseId, imagePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course image for course {CourseId}", courseId);
                return false;
            }
        }

        private string GetDifficultyLevelText(byte? level)
        {
            return level switch
            {
                1 => "Beginner",
                2 => "Intermediate",
                3 => "Advanced",
                4 => "Expert",
                _ => "All Levels"
            };
        }
    }
}
