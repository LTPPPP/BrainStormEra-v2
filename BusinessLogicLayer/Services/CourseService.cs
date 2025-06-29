using Microsoft.Extensions.Logging;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BusinessLogicLayer.Constants;

namespace BusinessLogicLayer.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepo _courseRepo;
        private readonly IUserRepo _userRepo;
        private readonly IEnrollmentService _enrollmentService;
        private readonly BrainStormEraContext _context; // Keep for complex queries temporarily
        private readonly IMemoryCache _cache;
        private readonly ILogger<CourseService> _logger;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

        public CourseService(
            ICourseRepo courseRepo,
            IUserRepo userRepo,
            IEnrollmentService enrollmentService,
            BrainStormEraContext context,
            IMemoryCache cache,
            ILogger<CourseService> logger)
        {
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _enrollmentService = enrollmentService;
            _context = context;
            _cache = cache;
            _logger = logger;

            // Clear category cache to ensure updated counts
            RefreshCategoryCache();
        }
        public async Task<CourseListViewModel> GetCoursesAsync(string? search, string? category, int page, int pageSize)
        {
            try
            {
                var courses = await _courseRepo.SearchCoursesAsync(search, category, page, pageSize, "date");

                var courseViewModels = courses.Select(c => new CourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CoursePicture = c.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    Description = c.CourseDescription,
                    Price = c.Price,
                    CreatedBy = c.Author.FullName ?? c.Author.Username,
                    CourseCategories = c.CourseCategories
                        .Select(cc => cc.CourseCategoryName)
                        .ToList(),
                    EnrollmentCount = c.Enrollments.Count(),
                    StarRating = 4 // Default rating
                }).ToList();

                // Count total courses using the same logic as category counting
                var totalCourses = await _context.Courses
                    .CountAsync(c => (c.CourseStatus == 1 || c.CourseStatus == 2) && c.ApprovalStatus == "Approved");
                var totalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

                // Get categories with proper course count mapping and sorting
                var categoryViewModels = await GetCategoriesAsync();

                // Log category information for debugging
                _logger.LogInformation("Loaded {Count} categories for course listing", categoryViewModels.Count);
                foreach (var cat in categoryViewModels)
                {
                    _logger.LogInformation("Category: {Name} - {Count} courses", cat.CategoryName, cat.CourseCount);
                }

                return new CourseListViewModel
                {
                    Courses = courseViewModels,
                    Categories = categoryViewModels,
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
                var course = await _courseRepo.GetCourseDetailAsync(courseId);
                if (course == null) return null;

                var viewModel = new CourseDetailViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription ?? "",
                    CourseImage = course.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    Price = course.Price,
                    AuthorId = course.AuthorId,
                    AuthorName = course.Author.FullName ?? course.Author.Username,
                    AuthorImage = course.Author.UserImage ?? MediaConstants.Defaults.DefaultAvatarPath,
                    EstimatedDuration = course.EstimatedDuration ?? 0,
                    DifficultyLevel = GetDifficultyLevelText(course.DifficultyLevel),
                    Categories = course.CourseCategories.Select(cc => cc.CourseCategoryName).ToList(),
                    TotalStudents = course.Enrollments.Count,
                    ApprovalStatus = course.ApprovalStatus,
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
                    Lessons = ch.Lessons
                        .Where(l => l.LessonType?.LessonTypeName != "Quiz") // Exclude quiz lessons from regular lessons
                        .Select(l => new LessonViewModel
                        {
                            LessonId = l.LessonId,
                            LessonName = l.LessonName,
                            LessonDescription = l.LessonDescription ?? "",
                            LessonOrder = l.LessonOrder,
                            LessonType = l.LessonType?.LessonTypeName ?? "Video",
                            EstimatedDuration = 0,
                            IsLocked = l.IsLocked ?? false
                        }).ToList(),
                    Quizzes = ch.Lessons
                        .Where(l => l.Quizzes != null && l.Quizzes.Any())
                        .SelectMany(l => l.Quizzes)
                        .Select(q => new QuizViewModel
                        {
                            QuizId = q.QuizId,
                            QuizName = q.QuizName,
                            QuizDescription = q.QuizDescription ?? "",
                            LessonId = q.LessonId,
                            LessonName = q.Lesson?.LessonName ?? "",
                            TimeLimit = q.TimeLimit,
                            PassingScore = q.PassingScore,
                            MaxAttempts = q.MaxAttempts,
                            IsFinalQuiz = q.IsFinalQuiz ?? false,
                            IsPrerequisiteQuiz = q.IsPrerequisiteQuiz ?? false,
                            BlocksLessonCompletion = q.BlocksLessonCompletion ?? false,
                            QuizCreatedAt = q.QuizCreatedAt,
                            QuizUpdatedAt = q.QuizUpdatedAt,
                            Questions = q.Questions?.OrderBy(qu => qu.QuestionOrder).Select(qu => new QuestionViewModel
                            {
                                QuestionId = qu.QuestionId,
                                QuestionText = qu.QuestionText,
                                QuestionType = qu.QuestionType,
                                Points = qu.Points ?? 0,
                                QuestionOrder = qu.QuestionOrder ?? 0,
                                Explanation = qu.Explanation ?? "",
                                AnswerOptions = qu.AnswerOptions?.OrderBy(ao => ao.OptionOrder).Select(ao => new AnswerOptionViewModel
                                {
                                    OptionId = ao.OptionId,
                                    OptionText = ao.OptionText,
                                    IsCorrect = ao.IsCorrect ?? false,
                                    OptionOrder = ao.OptionOrder ?? 0
                                }).ToList() ?? new List<AnswerOptionViewModel>()
                            }).ToList() ?? new List<QuestionViewModel>()
                        }).ToList()
                }).ToList();

                viewModel.Reviews = course.Feedbacks
                    .OrderByDescending(f => f.FeedbackCreatedAt)
                    .Take(10)
                    .Select(f => new ReviewViewModel
                    {
                        ReviewId = f.FeedbackId,
                        UserName = f.User.FullName ?? f.User.Username,
                        UserImage = f.User.UserImage ?? MediaConstants.Defaults.DefaultAvatarPath,
                        StarRating = f.StarRating ?? 0,
                        ReviewComment = f.Comment ?? "",
                        ReviewDate = f.FeedbackCreatedAt,
                        IsVerifiedPurchase = f.IsVerifiedPurchase ?? false,
                        UserId = f.UserId
                    }).ToList();

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course detail {CourseId}", courseId);
                return null;
            }
        }
        public async Task<CourseDetailViewModel?> GetCourseDetailAsync(string courseId, string? currentUserId = null)
        {
            try
            {
                var course = await _courseRepo.GetCourseDetailAsync(courseId, currentUserId);
                if (course == null) return null;

                var viewModel = new CourseDetailViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription ?? "",
                    CourseImage = course.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    Price = course.Price,
                    AuthorId = course.AuthorId,
                    AuthorName = course.Author.FullName ?? course.Author.Username,
                    AuthorImage = course.Author.UserImage ?? MediaConstants.Defaults.DefaultAvatarPath,
                    EstimatedDuration = course.EstimatedDuration ?? 0,
                    DifficultyLevel = GetDifficultyLevelText(course.DifficultyLevel),
                    Categories = course.CourseCategories.Select(cc => cc.CourseCategoryName).ToList(),
                    TotalStudents = course.Enrollments.Count,
                    ApprovalStatus = course.ApprovalStatus,
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
                    Lessons = ch.Lessons
                        .Where(l => l.LessonType?.LessonTypeName != "Quiz") // Exclude quiz lessons from regular lessons
                        .Select(l => new LessonViewModel
                        {
                            LessonId = l.LessonId,
                            LessonName = l.LessonName,
                            LessonDescription = l.LessonDescription ?? "",
                            LessonOrder = l.LessonOrder,
                            LessonType = l.LessonType?.LessonTypeName ?? "Video",
                            EstimatedDuration = 0,
                            IsLocked = l.IsLocked ?? false
                        }).ToList(),
                    Quizzes = ch.Lessons
                        .Where(l => l.Quizzes != null && l.Quizzes.Any())
                        .SelectMany(l => l.Quizzes)
                        .Select(q => new QuizViewModel
                        {
                            QuizId = q.QuizId,
                            QuizName = q.QuizName,
                            QuizDescription = q.QuizDescription ?? "",
                            LessonId = q.LessonId,
                            LessonName = q.Lesson?.LessonName ?? "",
                            TimeLimit = q.TimeLimit,
                            PassingScore = q.PassingScore,
                            MaxAttempts = q.MaxAttempts,
                            IsFinalQuiz = q.IsFinalQuiz ?? false,
                            IsPrerequisiteQuiz = q.IsPrerequisiteQuiz ?? false,
                            BlocksLessonCompletion = q.BlocksLessonCompletion ?? false,
                            QuizCreatedAt = q.QuizCreatedAt,
                            QuizUpdatedAt = q.QuizUpdatedAt,
                            Questions = q.Questions?.OrderBy(qu => qu.QuestionOrder).Select(qu => new QuestionViewModel
                            {
                                QuestionId = qu.QuestionId,
                                QuestionText = qu.QuestionText,
                                QuestionType = qu.QuestionType,
                                Points = qu.Points ?? 0,
                                QuestionOrder = qu.QuestionOrder ?? 0,
                                Explanation = qu.Explanation ?? "",
                                AnswerOptions = qu.AnswerOptions?.OrderBy(ao => ao.OptionOrder).Select(ao => new AnswerOptionViewModel
                                {
                                    OptionId = ao.OptionId,
                                    OptionText = ao.OptionText,
                                    IsCorrect = ao.IsCorrect ?? false,
                                    OptionOrder = ao.OptionOrder ?? 0
                                }).ToList() ?? new List<AnswerOptionViewModel>()
                            }).ToList() ?? new List<QuestionViewModel>()
                        }).ToList()
                }).ToList();

                viewModel.Reviews = course.Feedbacks
                    .OrderByDescending(f => f.FeedbackCreatedAt)
                    .Take(10)
                    .Select(f => new ReviewViewModel
                    {
                        ReviewId = f.FeedbackId,
                        UserName = f.User.FullName ?? f.User.Username,
                        UserImage = f.User.UserImage ?? MediaConstants.Defaults.DefaultAvatarPath,
                        StarRating = f.StarRating ?? 0,
                        ReviewComment = f.Comment ?? "",
                        ReviewDate = f.FeedbackCreatedAt,
                        IsVerifiedPurchase = f.IsVerifiedPurchase ?? false,
                        UserId = f.UserId
                    }).ToList();

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course detail {CourseId} for user {UserId}", courseId, currentUserId);
                return null;
            }
        }
        public async Task<List<CourseViewModel>> SearchCoursesAsync(string? search, string? category, int page, int pageSize, string? sortBy)
        {
            try
            {
                // Use consistent filtering logic with category counting
                var query = _context.Courses
                    .Where(c => (c.CourseStatus == 1 || c.CourseStatus == 2) && c.ApprovalStatus == "Approved");

                query = query.Include(c => c.Author)
                           .Include(c => c.Enrollments)
                           .Include(c => c.CourseCategories);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.CourseName.Contains(search) ||
                                           c.CourseDescription!.Contains(search) ||
                                           c.Author.FullName!.Contains(search) ||
                                           c.CourseCategories.Any(cc => cc.CourseCategoryName!.Contains(search)));
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
                        CoursePicture = c.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
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

        public async Task<(List<CourseViewModel> courses, int totalCount)> SearchCoursesWithPaginationAsync(
            string? courseSearch,
            string? categorySearch,
            int page,
            int pageSize,
            string? sortBy,
            string? price = null,
            string? difficulty = null,
            string? duration = null,
            string? userRole = null,
            string? userId = null)
        {
            try
            {
                // Role-based filtering logic
                var query = _context.Courses.AsQueryable();

                if (userRole?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Admin sees ALL courses including deleted ones
                    query = query.Where(c => true); // No filtering for admin
                }
                else if (userRole?.Equals("instructor", StringComparison.OrdinalIgnoreCase) == true && !string.IsNullOrEmpty(userId))
                {
                    // Instructor sees their own courses (all statuses) + public approved courses
                    query = query.Where(c =>
                        (c.AuthorId == userId) || // Their own courses regardless of status
                        ((c.CourseStatus == 1 || c.CourseStatus == 2) && c.ApprovalStatus == "Approved") // Public approved courses
                    );
                }
                else
                {
                    // Regular users see only approved and published courses
                    query = query.Where(c => (c.CourseStatus == 1 || c.CourseStatus == 2) && c.ApprovalStatus == "Approved");
                }

                query = query.Include(c => c.Author)
                           .Include(c => c.Enrollments)
                           .Include(c => c.CourseCategories);

                // Separate course search - search in course name, description, and author name
                if (!string.IsNullOrWhiteSpace(courseSearch))
                {
                    query = query.Where(c => c.CourseName.Contains(courseSearch) ||
                                           c.CourseDescription!.Contains(courseSearch) ||
                                           c.Author.FullName!.Contains(courseSearch));
                }

                // Separate category search - search in category names
                if (!string.IsNullOrWhiteSpace(categorySearch))
                {
                    query = query.Where(c => c.CourseCategories
                        .Any(cc => cc.CourseCategoryName!.Contains(categorySearch)));
                }

                // Price filter
                if (!string.IsNullOrWhiteSpace(price))
                {
                    query = price switch
                    {
                        "free" => query.Where(c => c.Price == 0),
                        "0-50" => query.Where(c => c.Price > 0 && c.Price <= 50),
                        "50-100" => query.Where(c => c.Price > 50 && c.Price <= 100),
                        "100-200" => query.Where(c => c.Price > 100 && c.Price <= 200),
                        "200+" => query.Where(c => c.Price > 200),
                        _ => query
                    };
                }

                // Difficulty filter (using DifficultyLevel: 1=Beginner, 2=Intermediate, 3=Advanced)
                if (!string.IsNullOrWhiteSpace(difficulty))
                {
                    query = difficulty.ToLower() switch
                    {
                        "beginner" => query.Where(c => c.DifficultyLevel == 1),
                        "intermediate" => query.Where(c => c.DifficultyLevel == 2),
                        "advanced" => query.Where(c => c.DifficultyLevel == 3),
                        _ => query
                    };
                }

                // Duration filter (using EstimatedDuration in hours)
                if (!string.IsNullOrWhiteSpace(duration))
                {
                    query = duration switch
                    {
                        "short" => query.Where(c => c.EstimatedDuration <= 2),
                        "medium" => query.Where(c => c.EstimatedDuration > 2 && c.EstimatedDuration <= 10),
                        "long" => query.Where(c => c.EstimatedDuration > 10),
                        _ => query
                    };
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

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
                        CoursePicture = c.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                        Description = c.CourseDescription,
                        Price = c.Price,
                        CreatedBy = c.Author.FullName ?? c.Author.Username,
                        CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList(),
                        EnrollmentCount = c.Enrollments.Count(),
                        ApprovalStatus = c.ApprovalStatus,
                        CourseStatus = c.CourseStatus,
                        AuthorId = c.AuthorId
                    })
                    .ToListAsync();

                foreach (var course in courses)
                {
                    course.StarRating = 4;
                }

                return (courses, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching courses with pagination");
                return (new List<CourseViewModel>(), 0);
            }
        }

        public async Task<bool> EnrollUserAsync(string userId, string courseId)
        {
            try
            {
                var course = await _courseRepo.GetCourseByIdAsync(courseId);
                if (course == null) return false;

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
            return await _courseRepo.IsUserEnrolledAsync(userId, courseId);
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

                _logger.LogInformation("Loading categories from database with course count mapping");

                var categories = await _context.CourseCategories
                    .AsNoTracking()
                    .Where(cc => cc.IsActive == true)
                    .Select(cc => new CourseCategoryViewModel
                    {
                        CategoryId = cc.CourseCategoryId,
                        CategoryName = cc.CourseCategoryName ?? "Unknown Category",
                        CategoryDescription = cc.CategoryDescription,
                        CategoryIcon = cc.CategoryIcon ?? "fas fa-tag",
                        CourseCount = cc.Courses.Count(c =>
                            (c.CourseStatus == 1 || c.CourseStatus == 2) && // Published or Active
                            c.ApprovalStatus == "Approved" // Only approved courses
                        )
                    })
                    .OrderByDescending(cc => cc.CourseCount) // Sort by course count from highest to lowest
                    .ThenBy(cc => cc.CategoryName) // Secondary sort by name for consistent ordering
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} categories from database, sorted by course count", categories.Count);

                // Log each category with course count for debugging
                foreach (var category in categories)
                {
                    _logger.LogInformation("Category: ID={CategoryId}, Name={CategoryName}, CourseCount={CourseCount}",
                        category.CategoryId, category.CategoryName, category.CourseCount);
                }

                // Additional debugging: Log approval status distribution
                var statusDistribution = await _context.Courses
                    .GroupBy(c => c.ApprovalStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                _logger.LogInformation("Course approval status distribution:");
                foreach (var status in statusDistribution)
                {
                    _logger.LogInformation("ApprovalStatus: {Status} - Count: {Count}", status.Status, status.Count);
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
                    ApprovalStatus = "draft", // Course is in draft mode, not yet submitted for approval
                    CourseCreatedAt = DateTime.UtcNow,
                    CourseUpdatedAt = DateTime.UtcNow,
                    CourseImage = MediaConstants.Defaults.DefaultCoursePath // Default image, will be updated if file is uploaded
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
                RefreshCategoryCache();

                return courseId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course for author {AuthorId}", authorId);
                throw;
            }
        }

        /// <summary>
        /// Refreshes the category cache to ensure course counts are up to date
        /// </summary>
        public void RefreshCategoryCache()
        {
            try
            {
                _cache.Remove("CourseCategories");
                _logger.LogInformation("Category cache cleared - will be refreshed on next request with updated course counts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing category cache");
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

        public async Task<CreateCourseViewModel?> GetCourseForEditAsync(string courseId, string authorId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.CourseCategories)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.AuthorId == authorId);

                if (course == null)
                {
                    _logger.LogWarning("Course not found or user not authorized to edit course {CourseId}", courseId);
                    return null;
                }

                var viewModel = new CreateCourseViewModel
                {
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription ?? string.Empty,
                    Price = course.Price,
                    EstimatedDuration = course.EstimatedDuration,
                    DifficultyLevel = course.DifficultyLevel ?? 1,
                    IsFeatured = course.IsFeatured ?? false,
                    EnforceSequentialAccess = course.EnforceSequentialAccess ?? true,
                    AllowLessonPreview = course.AllowLessonPreview ?? false,
                    SelectedCategories = course.CourseCategories
                        .Select(cc => cc.CourseCategoryId)
                        .ToList(),
                    AvailableCategories = await GetCategoriesAsync()
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course for edit: {CourseId}", courseId);
                return null;
            }
        }

        public async Task<bool> UpdateCourseAsync(string courseId, CreateCourseViewModel model, string authorId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.CourseCategories)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.AuthorId == authorId);

                if (course == null)
                {
                    _logger.LogWarning("Course not found or user not authorized to update course {CourseId}", courseId);
                    return false;
                }

                // Update course properties
                course.CourseName = model.CourseName;
                course.CourseDescription = model.CourseDescription;
                course.Price = model.Price;
                course.EstimatedDuration = model.EstimatedDuration;
                course.DifficultyLevel = model.DifficultyLevel;
                course.IsFeatured = model.IsFeatured;
                course.EnforceSequentialAccess = model.EnforceSequentialAccess;
                course.AllowLessonPreview = model.AllowLessonPreview;
                course.CourseUpdatedAt = DateTime.UtcNow;

                // Update categories
                var existingCategoryIds = course.CourseCategories.Select(cc => cc.CourseCategoryId).ToList();
                var newCategoryIds = model.SelectedCategories;

                // Remove categories that are no longer selected
                var categoriesToRemove = course.CourseCategories
                    .Where(cc => !newCategoryIds.Contains(cc.CourseCategoryId))
                    .ToList();

                foreach (var category in categoriesToRemove)
                {
                    course.CourseCategories.Remove(category);
                }

                // Add new categories
                var categoriesToAddIds = newCategoryIds
                    .Where(id => !existingCategoryIds.Contains(id))
                    .ToList();

                if (categoriesToAddIds.Any())
                {
                    var categoriesToAdd = await _context.CourseCategories
                        .Where(cc => categoriesToAddIds.Contains(cc.CourseCategoryId))
                        .ToListAsync();

                    foreach (var category in categoriesToAdd)
                    {
                        course.CourseCategories.Add(category);
                    }
                }

                await _context.SaveChangesAsync();

                // Clear cache since categories were modified
                RefreshCategoryCache();

                _logger.LogInformation("Course updated successfully: {CourseId}", courseId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseId}", courseId);
                return false;
            }
        }

        /// <summary>
        /// Delete course - performs soft delete by setting status to Archived
        /// </summary>
        /// <param name="courseId">Course ID to delete</param>
        /// <param name="authorId">Author ID for authorization</param>
        /// <returns>Success result</returns>
        public async Task<bool> DeleteCourseAsync(string courseId, string authorId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Enrollments)
                    .Include(c => c.CourseCategories)
                    .Include(c => c.Chapters)
                        .ThenInclude(ch => ch.Lessons)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.AuthorId == authorId);

                if (course == null)
                {
                    _logger.LogWarning("Course not found or user not authorized to delete course {CourseId}", courseId);
                    return false;
                }

                // Check if course has active enrollments
                var activeEnrollments = course.Enrollments.Where(e => e.EnrollmentStatus != 4).Count(); // Not Archived
                if (activeEnrollments > 0)
                {
                    _logger.LogWarning("Cannot delete course {CourseId} as it has {Count} active enrollment(s)", courseId, activeEnrollments);
                    return false;
                }

                // Perform soft delete by setting status to Archived (4)
                course.CourseStatus = 4; // Archived
                course.CourseUpdatedAt = DateTime.UtcNow;

                // Also archive related chapters and lessons
                foreach (var chapter in course.Chapters)
                {
                    chapter.ChapterStatus = 4; // Archived
                    chapter.ChapterUpdatedAt = DateTime.UtcNow;

                    foreach (var lesson in chapter.Lessons)
                    {
                        lesson.LessonStatus = 4; // Archived
                        lesson.LessonUpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                // Clear cache since a course was deleted
                RefreshCategoryCache();

                _logger.LogInformation("Course soft deleted (archived) successfully: {CourseId}", courseId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", courseId);
                return false;
            }
        }

        public async Task<bool> UpdateCourseApprovalStatusAsync(string courseId, string approvalStatus)
        {
            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
                if (course == null)
                    return false;

                course.ApprovalStatus = approvalStatus;
                course.CourseUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course approval status for course {CourseId}", courseId);
                return false;
            }
        }



        public async Task<CourseListViewModel> GetInstructorCoursesAsync(string authorId, string? search, string? category, int page, int pageSize)
        {
            try
            {
                var query = _context.Courses
                    .AsNoTracking()
                    .Where(c => c.AuthorId == authorId && c.CourseStatus != 4); // Filter by instructor's authorId and exclude archived courses

                query = query.Include(c => c.Author)
                           .Include(c => c.Enrollments)
                           .Include(c => c.CourseCategories);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.CourseName.Contains(search) ||
                                           c.CourseDescription!.Contains(search) ||
                                           c.CourseCategories.Any(cc => cc.CourseCategoryName!.Contains(search)));
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
                        CoursePicture = c.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
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
                _logger.LogError(ex, "Error loading instructor courses for authorId: {AuthorId}", authorId);
                return new CourseListViewModel();
            }
        }

        public async Task<Course?> GetCourseByIdAsync(string courseId)
        {
            try
            {
                return await _context.Courses
                    .Include(c => c.Author)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course by ID: {CourseId}", courseId);
                return null;
            }
        }

        public async Task<List<Chapter>> GetChaptersByCourseIdAsync(string courseId)
        {
            try
            {
                return await _context.Chapters
                    .Where(c => c.CourseId == courseId)
                    .OrderBy(c => c.ChapterOrder)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapters for course: {CourseId}", courseId);
                return new List<Chapter>();
            }
        }

        public async Task<List<Lesson>> GetLessonsByChapterIdAsync(string chapterId)
        {
            try
            {
                return await _context.Lessons
                    .Include(l => l.LessonType)
                    .Include(l => l.UnlockAfterLesson)
                    .Include(l => l.Quizzes)
                    .Where(l => l.ChapterId == chapterId)
                    .OrderBy(l => l.LessonOrder)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lessons for chapter: {ChapterId}", chapterId);
                return new List<Lesson>();
            }
        }
    }
}








