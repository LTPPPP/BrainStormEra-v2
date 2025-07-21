using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using DataAccessLayer.Models;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using BusinessLogicLayer.Constants;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Implementation class that handles complex business logic for Course operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// authorization, validation, and error handling.
    /// </summary>
    public class CourseService : ICourseService
    {
        private readonly ICourseRepo _courseRepo;
        private readonly IUserRepo _userRepo;
        private readonly IEnrollmentService _enrollmentService;
        private readonly BrainStormEraContext _context;
        private readonly ICourseImageService _courseImageService;
        private readonly Func<ILessonService> _lessonServiceFactory;
        private readonly ILogger<CourseService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

        public CourseService(
            ICourseRepo courseRepo,
            IUserRepo userRepo,
            IEnrollmentService enrollmentService,
            BrainStormEraContext context,
            ICourseImageService courseImageService,
            Func<ILessonService> lessonServiceFactory,
            ILogger<CourseService> logger,
            IServiceProvider serviceProvider)
        {
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _enrollmentService = enrollmentService;
            _context = context;
            _courseImageService = courseImageService;
            _lessonServiceFactory = lessonServiceFactory;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        #region ICourseService Implementation

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

                // Get progress percentage if user is enrolled
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var enrollment = course.Enrollments.FirstOrDefault(e => e.UserId == currentUserId);
                    if (enrollment != null)
                    {
                        viewModel.ProgressPercentage = enrollment.ProgressPercentage ?? 0;
                    }
                }

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

        public async Task<List<CourseViewModel>> SearchCoursesAsync(string? search, string? category, int page, int pageSize, string? sortBy)
        {
            try
            {
                var courses = await _courseRepo.SearchCoursesAsync(search, category, page, pageSize, sortBy ?? "date");

                return courses.Select(c => new CourseViewModel
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
                // Build query based on user role
                var query = _context.Courses.AsQueryable();

                // Role-based filtering
                if (userRole?.Equals("Instructor", StringComparison.OrdinalIgnoreCase) == true && !string.IsNullOrEmpty(userId))
                {
                    // Instructors see their own courses (including denied/pending)
                    query = query.Where(c => c.AuthorId == userId);
                }
                else if (userRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Admins see all courses including deleted ones
                    query = query.Where(c => c.CourseStatus >= 0);
                }
                else
                {
                    // Regular users see only approved and active courses
                    query = query.Where(c => (c.CourseStatus == 1 || c.CourseStatus == 2) && c.ApprovalStatus == "Approved");
                }

                // Apply search filters
                if (!string.IsNullOrWhiteSpace(courseSearch))
                {
                    query = query.Where(c => c.CourseName.Contains(courseSearch) || (c.CourseDescription != null && c.CourseDescription.Contains(courseSearch)));
                }

                if (!string.IsNullOrWhiteSpace(categorySearch))
                {
                    query = query.Where(c => c.CourseCategories.Any(cc => cc.CourseCategoryName != null && cc.CourseCategoryName.Contains(categorySearch)));
                }

                // Apply additional filters
                if (!string.IsNullOrWhiteSpace(price))
                {
                    switch (price.ToLower())
                    {
                        case "free":
                            query = query.Where(c => c.Price == 0);
                            break;
                        case "paid":
                            query = query.Where(c => c.Price > 0);
                            break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(difficulty))
                {
                    if (byte.TryParse(difficulty, out byte difficultyLevel))
                    {
                        query = query.Where(c => c.DifficultyLevel == difficultyLevel);
                    }
                }

                if (!string.IsNullOrWhiteSpace(duration))
                {
                    switch (duration.ToLower())
                    {
                        case "short":
                            query = query.Where(c => c.EstimatedDuration <= 60);
                            break;
                        case "medium":
                            query = query.Where(c => c.EstimatedDuration > 60 && c.EstimatedDuration <= 180);
                            break;
                        case "long":
                            query = query.Where(c => c.EstimatedDuration > 180);
                            break;
                    }
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply sorting
                query = sortBy?.ToLower() switch
                {
                    "name" => query.OrderBy(c => c.CourseName),
                    "price" => query.OrderBy(c => c.Price),
                    "enrollment" => query.OrderByDescending(c => c.Enrollments.Count),
                    "rating" => query.OrderByDescending(c => c.Feedbacks.Average(f => f.StarRating ?? 0)),
                    "oldest" => query.OrderBy(c => c.CourseCreatedAt),
                    _ => query.OrderByDescending(c => c.CourseCreatedAt) // newest
                };

                // Apply pagination
                var courses = await query
                    .Include(c => c.Author)
                    .Include(c => c.CourseCategories)
                    .Include(c => c.Enrollments)
                    .Include(c => c.Feedbacks)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var courseViewModels = courses.Select(c => new CourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CoursePicture = c.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    Description = c.CourseDescription,
                    Price = c.Price,
                    CreatedBy = c.Author.FullName ?? c.Author.Username,
                    CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList(),
                    EnrollmentCount = c.Enrollments.Count,
                    StarRating = c.Feedbacks.Any(f => f.StarRating.HasValue)
                        ? (int)Math.Round(c.Feedbacks.Where(f => f.StarRating.HasValue).Average(f => f.StarRating!.Value))
                        : 0
                }).ToList();

                return (courseViewModels, totalCount);
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
            try
            {
                return await _enrollmentService.IsEnrolledAsync(userId, courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking enrollment for user {UserId} in course {CourseId}", userId, courseId);
                return false;
            }
        }

        public async Task<List<CourseCategoryViewModel>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _context.CourseCategories
                    .Include(cc => cc.Courses)
                    .Where(cc => cc.Courses.Any(c => (c.CourseStatus == 1 || c.CourseStatus == 2) && c.ApprovalStatus == "Approved"))
                    .Select(cc => new CourseCategoryViewModel
                    {
                        CategoryId = cc.CourseCategoryId,
                        CategoryName = cc.CourseCategoryName,
                        CourseCount = cc.Courses.Count(c => (c.CourseStatus == 1 || c.CourseStatus == 2) && c.ApprovalStatus == "Approved")
                    })
                    .Where(cc => cc.CourseCount > 0)
                    .OrderBy(cc => cc.CategoryName)
                    .ToListAsync();
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course categories");
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

                var categories = await _context.CourseCategories
                    .Where(cc => cc.CourseCategoryName.Contains(searchTerm))
                    .Select(cc => new CategoryAutocompleteItem
                    {
                        CategoryId = cc.CourseCategoryId,
                        CategoryName = cc.CourseCategoryName
                    })
                    .Take(10)
                    .ToListAsync();

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
                var course = new Course
                {
                    CourseId = Guid.NewGuid().ToString(),
                    CourseName = model.CourseName,
                    CourseDescription = model.CourseDescription,
                    Price = model.Price,
                    EstimatedDuration = model.EstimatedDuration,
                    DifficultyLevel = model.DifficultyLevel,
                    AuthorId = authorId,
                    CourseStatus = 1, // Active
                    ApprovalStatus = "Draft", // Start as draft
                    CourseCreatedAt = DateTime.UtcNow,
                    CourseUpdatedAt = DateTime.UtcNow
                };

                // Add categories
                if (model.SelectedCategories != null && model.SelectedCategories.Any())
                {
                    var categories = await _context.CourseCategories
                        .Where(cc => model.SelectedCategories.Contains(cc.CourseCategoryId))
                        .ToListAsync();

                    foreach (var category in categories)
                    {
                        course.CourseCategories.Add(category);
                    }
                }

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                return course.CourseId;
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
                if (course == null) return false;

                course.CourseImage = imagePath;
                course.CourseUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course image for course {CourseId}", courseId);
                return false;
            }
        }

        public async Task<CreateCourseViewModel?> GetCourseForEditAsync(string courseId, string authorId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.CourseCategories)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.AuthorId == authorId);

                if (course == null) return null;

                var model = new CreateCourseViewModel
                {
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription ?? "",
                    Price = course.Price,
                    EstimatedDuration = course.EstimatedDuration ?? 0,
                    DifficultyLevel = course.DifficultyLevel ?? 1,
                    SelectedCategories = course.CourseCategories.Select(cc => cc.CourseCategoryId).ToList(),
                    AvailableCategories = await GetCategoriesAsync()
                };

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course for edit {CourseId}", courseId);
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

                if (course == null) return false;

                // Update basic properties
                course.CourseName = model.CourseName;
                course.CourseDescription = model.CourseDescription;
                course.Price = model.Price;
                course.EstimatedDuration = model.EstimatedDuration;
                course.DifficultyLevel = model.DifficultyLevel;
                course.CourseUpdatedAt = DateTime.UtcNow;

                // Update categories
                course.CourseCategories.Clear();
                if (model.SelectedCategories != null && model.SelectedCategories.Any())
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
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseId}", courseId);
                return false;
            }
        }

        public async Task<bool> DeleteCourseAsync(string courseId, string authorId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Enrollments)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.AuthorId == authorId);

                if (course == null) return false;

                // Check if course has enrolled students
                if (course.Enrollments.Any())
                {
                    return false; // Cannot delete course with enrolled students
                }

                // Soft delete
                course.CourseStatus = -1; // Deleted
                course.CourseUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
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
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null) return false;

                course.ApprovalStatus = approvalStatus;
                course.CourseUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course approval status {CourseId}", courseId);
                return false;
            }
        }

        public async Task<Course?> GetCourseByIdAsync(string courseId)
        {
            try
            {
                return await _context.Courses
                    .Include(c => c.Author)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course by ID {CourseId}", courseId);
                return null;
            }
        }

        public async Task<List<Chapter>> GetChaptersByCourseIdAsync(string courseId)
        {
            try
            {
                return await _context.Chapters
                    .Include(c => c.Lessons)
                    .Where(c => c.CourseId == courseId)
                    .OrderBy(c => c.ChapterOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapters for course {CourseId}", courseId);
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
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lessons for chapter {ChapterId}", chapterId);
                return new List<Lesson>();
            }
        }

        public async Task<CourseListViewModel> GetInstructorCoursesAsync(string authorId, string? search, string? category, int page, int pageSize)
        {
            try
            {
                var query = _context.Courses
                    .Include(c => c.Author)
                    .Include(c => c.CourseCategories)
                    .Include(c => c.Enrollments)
                    .Where(c => c.AuthorId == authorId && c.CourseStatus >= 0); // Include all statuses for instructor

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.CourseName.Contains(search) || (c.CourseDescription != null && c.CourseDescription.Contains(search)));
                }

                // Apply category filter
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(c => c.CourseCategories.Any(cc => cc.CourseCategoryName != null && cc.CourseCategoryName.Contains(category)));
                }

                var totalCourses = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

                var courses = await query
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var courseViewModels = courses.Select(c => new CourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CoursePicture = c.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    Description = c.CourseDescription,
                    Price = c.Price,
                    CreatedBy = c.Author.FullName ?? c.Author.Username,
                    CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList(),
                    EnrollmentCount = c.Enrollments.Count,
                    StarRating = 4 // Default rating
                }).ToList();

                var categoryViewModels = await GetCategoriesAsync();

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
                _logger.LogError(ex, "Error loading instructor courses for author {AuthorId}", authorId);
                return new CourseListViewModel();
            }
        }

        public void RefreshCategoryCache()
        {
            // No cache to refresh
        }

        private string GetDifficultyLevelText(byte? level)
        {
            return level switch
            {
                1 => "Beginner",
                2 => "Intermediate",
                3 => "Advanced",
                _ => "Beginner"
            };
        }

        #endregion

        #region Business Logic Methods

        /// <summary>
        /// Get courses based on user role and permissions
        /// </summary>
        public async Task<CourseIndexResult> GetCoursesAsync(
            ClaimsPrincipal user,
            string? search,
            string? category,
            int page = 1,
            int pageSize = 50)
        {
            try
            {
                CourseListViewModel viewModel;
                var currentUserId = user.FindFirst("UserId")?.Value;
                var currentUserRole = user.FindFirst(ClaimTypes.Role)?.Value;

                // Check if user is instructor and should see only their courses
                if (currentUserRole?.Equals("Instructor", StringComparison.OrdinalIgnoreCase) == true
                    && !string.IsNullOrEmpty(currentUserId))
                {
                    viewModel = await GetInstructorCoursesAsync(currentUserId, search, category, page, pageSize);
                }
                else
                {
                    viewModel = await GetCoursesAsync(search, category, page, pageSize);
                }

                return new CourseIndexResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading courses");
                return new CourseIndexResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading courses. Please try again later.",
                    ViewModel = new CourseListViewModel()
                };
            }
        }                        /// <summary>
                                 /// Search courses with advanced filtering and sorting (separated course and category search)
                                 /// Role-based: Instructors see denied/pending courses, Admins see deleted courses
                                 /// </summary>
        public async Task<CourseSearchResult> SearchCoursesAsync(
            ClaimsPrincipal user,
            string? courseSearch,
            string? categorySearch,
            int page = 1,
            int pageSize = 50,
            string? sortBy = "newest",
            string? price = null,
            string? difficulty = null,
            string? duration = null)
        {
            try
            {
                // Get user role and ID for role-based filtering
                string? userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                string? userId = user.FindFirst("UserId")?.Value;

                var (courses, totalCount) = await SearchCoursesWithPaginationAsync(
                    courseSearch, categorySearch, page, pageSize, sortBy, price, difficulty, duration, userRole, userId);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return new CourseSearchResult
                {
                    Success = true,
                    Courses = courses,
                    TotalCourses = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching courses");
                return new CourseSearchResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while searching courses",
                    Courses = new List<CourseViewModel>()
                };
            }
        }

        #endregion

        #region Course Detail Operations

        /// <summary>
        /// Get course details with user-specific information
        /// </summary>
        public async Task<CourseDetailResult> GetCourseDetailsAsync(ClaimsPrincipal user, string courseId, string? tab = null)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                return new CourseDetailResult
                {
                    Success = false,
                    IsNotFound = true
                };
            }

            try
            {
                // Get current user ID if authenticated
                string? currentUserId = null;
                if (user.Identity?.IsAuthenticated == true)
                {
                    currentUserId = user.FindFirst("UserId")?.Value;
                }

                var viewModel = await GetCourseDetailAsync(courseId, currentUserId);
                if (viewModel == null)
                {
                    return new CourseDetailResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                // Set user-specific properties
                if (user.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(currentUserId))
                {
                    viewModel.IsEnrolled = await _enrollmentService.IsEnrolledAsync(currentUserId, courseId);
                    viewModel.CanEnroll = !viewModel.IsEnrolled;
                }

                return new CourseDetailResult
                {
                    Success = true,
                    ViewModel = viewModel,
                    IsAuthor = viewModel.AuthorId == currentUserId,
                    CurrentUserId = currentUserId,
                    ActiveTab = tab
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading course details for course {CourseId}", courseId);
                return new CourseDetailResult
                {
                    Success = false,
                    IsNotFound = true
                };
            }
        }

        #endregion

        #region Course Enrollment Operations

        /// <summary>
        /// Handle course enrollment with validation and point deduction
        /// </summary>
        public async Task<EnrollmentResult> EnrollInCourseAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                var isAlreadyEnrolled = await _enrollmentService.IsEnrolledAsync(userId, courseId);
                if (isAlreadyEnrolled)
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Message = "Already enrolled in this course"
                    };
                }

                // Get course info to check price
                var course = await GetCourseByIdAsync(courseId);
                if (course == null)
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Message = "Course not found"
                    };
                }

                // If course is free, proceed with normal enrollment
                if (course.Price == 0)
                {
                    var success = await _enrollmentService.EnrollAsync(userId, courseId);
                    if (success)
                    {
                        return new EnrollmentResult
                        {
                            Success = true,
                            Message = "Successfully enrolled in course!"
                        };
                    }
                    else
                    {
                        return new EnrollmentResult
                        {
                            Success = false,
                            Message = "Enrollment failed"
                        };
                    }
                }

                // If course has price, check user points and deduct
                using var scope = _serviceProvider.CreateScope();
                var pointsService = scope.ServiceProvider.GetRequiredService<IPointsService>();
                var context = scope.ServiceProvider.GetRequiredService<DataAccessLayer.Data.BrainStormEraContext>();

                var userPoints = await pointsService.GetUserPointsAsync(userId);

                if (userPoints < course.Price)
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Message = $"Insufficient points! You have {userPoints:N0} points but need {course.Price:N0} points. Please top up your account to enroll in this course."
                    };
                }

                // Use execution strategy for transaction
                var executionStrategy = context.Database.CreateExecutionStrategy();
                return await executionStrategy.ExecuteAsync<object, EnrollmentResult>(
                    new object(),
                    async (dbContext, state, cancellationToken) =>
                    {
                        var realContext = (DataAccessLayer.Data.BrainStormEraContext)dbContext;
                        using var transaction = await realContext.Database.BeginTransactionAsync(cancellationToken);
                        try
                        {
                            // Deduct points using PointsService
                            var pointsDeducted = await pointsService.UpdateUserPointsAsync(userId, -course.Price);
                            if (!pointsDeducted)
                            {
                                await transaction.RollbackAsync(cancellationToken);
                                return new EnrollmentResult
                                {
                                    Success = false,
                                    Message = "Failed to deduct points"
                                };
                            }

                            // Check if already enrolled
                            var existingEnrollment = await realContext.Enrollments
                                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, cancellationToken);

                            if (existingEnrollment != null)
                            {
                                await transaction.RollbackAsync(cancellationToken);
                                return new EnrollmentResult
                                {
                                    Success = false,
                                    Message = "Already enrolled in this course"
                                };
                            }

                            // Create enrollment directly in the transaction context
                            var enrollment = new DataAccessLayer.Models.Enrollment
                            {
                                EnrollmentId = Guid.NewGuid().ToString(),
                                UserId = userId,
                                CourseId = courseId,
                                EnrollmentCreatedAt = DateTime.UtcNow,
                                EnrollmentUpdatedAt = DateTime.UtcNow,
                                EnrollmentStatus = 1, // Active status
                                ProgressPercentage = 0,
                                Approved = true // Set approved = true when user enrolls
                            };

                            realContext.Enrollments.Add(enrollment);

                            await realContext.SaveChangesAsync(cancellationToken);
                            await transaction.CommitAsync(cancellationToken);

                            return new EnrollmentResult
                            {
                                Success = true,
                                Message = $"Successfully enrolled! {course.Price:N0} points deducted from your account."
                            };
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            _logger.LogError(ex, "Error during enrollment transaction for course {CourseId}, user {UserId}", courseId, userId);
                            return new EnrollmentResult
                            {
                                Success = false,
                                Message = "An error occurred during enrollment"
                            };
                        }
                    },
                    null,
                    System.Threading.CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during enrollment for course {CourseId}", courseId);
                return new EnrollmentResult
                {
                    Success = false,
                    Message = "An error occurred during enrollment"
                };
            }
        }

        #endregion

        #region Course Management Operations (Instructor)

        /// <summary>
        /// Get create course view model with available categories
        /// </summary>
        public async Task<CreateCourseResult> GetCreateCourseViewModelAsync()
        {
            try
            {
                var model = new CreateCourseViewModel
                {
                    AvailableCategories = await GetCategoriesAsync()
                };

                return new CreateCourseResult
                {
                    Success = true,
                    ViewModel = model
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading create course page");
                return new CreateCourseResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the create course page.",
                    RedirectAction = "InstructorDashboard",
                    RedirectController = "Home"
                };
            }
        }

        /// <summary>
        /// Create a new course with image upload handling
        /// </summary>
        public async Task<CreateCourseResult> CreateCourseAsync(
            ClaimsPrincipal user,
            CreateCourseViewModel model)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new CreateCourseResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var courseId = await CreateCourseAsync(model, userId);

                // Handle course image upload if provided
                string? warningMessage = null;
                if (model.CourseImage != null)
                {
                    var uploadResult = await _courseImageService.UploadCourseImageAsync(model.CourseImage, courseId);
                    if (uploadResult.Success && !string.IsNullOrEmpty(uploadResult.ImagePath))
                    {
                        // Update course with image path
                        await UpdateCourseImageAsync(courseId, uploadResult.ImagePath);
                    }
                    else
                    {
                        warningMessage = uploadResult.ErrorMessage ?? "Failed to upload course image.";
                    }
                }

                return new CreateCourseResult
                {
                    Success = true,
                    SuccessMessage = "Course created successfully! Your course is saved as draft. Visit the course details page to request approval when you're ready to publish.",
                    WarningMessage = warningMessage,
                    RedirectAction = "InstructorDashboard",
                    RedirectController = "Home"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating course");

                // Reload categories for the view
                try
                {
                    model.AvailableCategories = await GetCategoriesAsync();
                }
                catch (Exception)
                {
                    model.AvailableCategories = new List<CourseCategoryViewModel>();
                }

                return new CreateCourseResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while creating the course. Please try again.",
                    ViewModel = model,
                    ReturnView = true
                };
            }
        }

        /// <summary>
        /// Get course for editing with authorization check
        /// </summary>
        public async Task<EditCourseResult> GetCourseForEditAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditCourseResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var model = await GetCourseForEditAsync(courseId, userId);
                if (model == null)
                {
                    return new EditCourseResult
                    {
                        Success = false,
                        ErrorMessage = "Course not found or you are not authorized to edit this course.",
                        RedirectAction = "InstructorDashboard",
                        RedirectController = "Home"
                    };
                }

                return new EditCourseResult
                {
                    Success = true,
                    ViewModel = model,
                    CourseId = courseId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading edit course page for course {CourseId}", courseId);
                return new EditCourseResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the edit course page.",
                    RedirectAction = "InstructorDashboard",
                    RedirectController = "Home"
                };
            }
        }

        /// <summary>
        /// Update course with image upload handling
        /// </summary>
        public async Task<EditCourseResult> UpdateCourseAsync(
            ClaimsPrincipal user,
            string courseId,
            CreateCourseViewModel model)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditCourseResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var success = await UpdateCourseAsync(courseId, model, userId);
                if (!success)
                {
                    return new EditCourseResult
                    {
                        Success = false,
                        ErrorMessage = "Course not found or you are not authorized to edit this course.",
                        RedirectAction = "InstructorDashboard",
                        RedirectController = "Home"
                    };
                }

                // Handle course image upload if provided
                string? warningMessage = null;
                if (model.CourseImage != null)
                {
                    var uploadResult = await _courseImageService.UploadCourseImageAsync(model.CourseImage, courseId);
                    if (uploadResult.Success && !string.IsNullOrEmpty(uploadResult.ImagePath))
                    {
                        await UpdateCourseImageAsync(courseId, uploadResult.ImagePath);
                    }
                    else
                    {
                        warningMessage = uploadResult.ErrorMessage ?? "Failed to upload course image.";
                    }
                }

                return new EditCourseResult
                {
                    Success = true,
                    SuccessMessage = "Course updated successfully!",
                    WarningMessage = warningMessage,
                    RedirectAction = "InstructorDashboard",
                    RedirectController = "Home"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating course {CourseId}", courseId);

                // Reload categories for the view
                try
                {
                    model.AvailableCategories = await GetCategoriesAsync();
                }
                catch (Exception)
                {
                    model.AvailableCategories = new List<CourseCategoryViewModel>();
                }

                return new EditCourseResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while updating the course. Please try again.",
                    ViewModel = model,
                    CourseId = courseId,
                    ReturnView = true
                };
            }
        }

        /// <summary>
        /// Delete course with authorization check
        /// </summary>
        public async Task<DeleteCourseResult> DeleteCourseAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new DeleteCourseResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                var success = await DeleteCourseAsync(courseId, userId);
                if (success)
                {
                    return new DeleteCourseResult
                    {
                        Success = true,
                        Message = "Course deleted successfully!"
                    };
                }
                else
                {
                    return new DeleteCourseResult
                    {
                        Success = false,
                        Message = "Course not found, you are not authorized to delete this course, or the course has enrolled students."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting course {CourseId}", courseId);
                return new DeleteCourseResult
                {
                    Success = false,
                    Message = "An error occurred while deleting the course."
                };
            }
        }

        #endregion



        #region Instructor Dashboard Operations

        /// <summary>
        /// Get user courses for notifications (Instructor only)
        /// </summary>
        public async Task<InstructorCoursesResult> GetUserCoursesAsync(ClaimsPrincipal user)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new InstructorCoursesResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                List<dynamic> courseList;

                if (userRole?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Admin can see all courses
                    var allCourses = await GetCoursesAsync(null, null, 1, int.MaxValue);
                    courseList = allCourses.Courses.Select(c => new
                    {
                        courseId = c.CourseId,
                        courseName = c.CourseName,
                        enrollmentCount = c.EnrollmentCount
                    }).ToList<dynamic>();
                }
                else
                {
                    // Instructor can see only their courses
                    var courses = await GetInstructorCoursesAsync(userId, null, null, 1, int.MaxValue);
                    courseList = courses.Courses.Select(c => new
                    {
                        courseId = c.CourseId,
                        courseName = c.CourseName,
                        enrollmentCount = c.EnrollmentCount
                    }).ToList<dynamic>();
                }

                return new InstructorCoursesResult
                {
                    Success = true,
                    Courses = courseList
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user courses for notifications");
                return new InstructorCoursesResult
                {
                    Success = false,
                    Message = "An error occurred while loading courses."
                };
            }
        }

        #endregion

        #region Course Approval Operations

        /// <summary>
        /// Request course approval for admin review
        /// </summary>
        public async Task<CourseApprovalResult> RequestCourseApprovalAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                // Check if user is the author of the course
                var course = await GetCourseDetailAsync(courseId, userId);
                if (course == null)
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "Course not found"
                    };
                }

                if (course.AuthorId != userId)
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "You are not authorized to request approval for this course"
                    };
                }

                // Check current approval status
                if (course.ApprovalStatus?.ToLower() == "approved")
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "This course is already approved"
                    };
                }

                if (course.ApprovalStatus?.ToLower() == "pending")
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "This course is already pending approval"
                    };
                }

                // Only allow request for courses that are in draft mode or were rejected
                if (!string.IsNullOrEmpty(course.ApprovalStatus) &&
                    course.ApprovalStatus.ToLower() != "draft" &&
                    course.ApprovalStatus.ToLower() != "rejected")
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "This course is not eligible for approval request"
                    };
                }

                // Update course status to pending
                var success = await UpdateCourseApprovalStatusAsync(courseId, "Pending");
                if (success)
                {
                    // Send notification to all admins about the approval request
                    try
                    {
                        var notificationService = _serviceProvider.GetService<INotificationService>();
                        if (notificationService != null)
                        {
                            await notificationService.SendToRoleAsync(
                                "admin",
                                "New Course Approval Request",
                                $"Instructor {course.AuthorName} has requested approval for course '{course.CourseName}'. Please review and approve/reject this course.",
                                "course_approval",
                                userId
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send notification to admins about course approval request");
                        // Don't fail the whole operation if notification fails
                    }

                    return new CourseApprovalResult
                    {
                        Success = true,
                        Message = "Course approval request submitted successfully! Your course is now pending admin review."
                    };
                }
                else
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "Failed to submit approval request. Please try again."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while requesting course approval for course {CourseId}", courseId);
                return new CourseApprovalResult
                {
                    Success = false,
                    Message = "An error occurred while submitting approval request."
                };
            }
        }

        #endregion

        #region Learn Management Operations

        /// <summary>
        /// Get course learning data with chapters and lessons organized by type
        /// </summary>
        public async Task<LearnManagementResult> GetLearnManagementDataAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new LearnManagementResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                // Check if user is enrolled in the course
                var isEnrolled = await _enrollmentService.IsEnrolledAsync(userId, courseId);
                if (!isEnrolled)
                {
                    return new LearnManagementResult
                    {
                        Success = false,
                        ErrorMessage = "You must be enrolled in this course to access learning content"
                    };
                }

                // Get course basic info first
                var course = await GetCourseByIdAsync(courseId);
                if (course == null)
                {
                    return new LearnManagementResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                // Get all chapters for this course
                var chapters = await GetChaptersByCourseIdAsync(courseId);

                var chapterViewModels = new List<LearnChapterViewModel>();

                foreach (var chapter in chapters.OrderBy(c => c.ChapterOrder))
                {
                    // Get all lessons for each chapter
                    var lessons = await GetLessonsByChapterIdAsync(chapter.ChapterId);

                    var lessonViewModels = new List<LearnLessonViewModel>();
                    var quizViewModels = new List<LearnQuizViewModel>();

                    foreach (var lesson in lessons.OrderBy(l => l.LessonOrder))
                    {
                        // Check if lesson is completed
                        var isCompleted = await _lessonServiceFactory().IsLessonCompletedAsync(userId, lesson.LessonId);

                        lessonViewModels.Add(new LearnLessonViewModel
                        {
                            LessonId = lesson.LessonId,
                            LessonName = lesson.LessonName,
                            LessonDescription = lesson.LessonDescription ?? "",
                            LessonOrder = lesson.LessonOrder,
                            LessonType = lesson.LessonType?.LessonTypeName ?? "Content",
                            LessonTypeIcon = GetLessonTypeIcon(lesson.LessonType?.LessonTypeName),
                            IsLocked = await IsLessonLockedAsync(userId, lesson),
                            IsMandatory = lesson.IsMandatory ?? true,
                            EstimatedDuration = lesson.MinTimeSpent ?? 0,
                            IsCompleted = isCompleted,
                            ProgressPercentage = isCompleted ? 100 : 0,
                            PrerequisiteLessonId = lesson.UnlockAfterLessonId,
                            PrerequisiteLessonName = lesson.UnlockAfterLesson?.LessonName
                        });

                        // Load quizzes for this lesson
                        if (lesson.Quizzes != null && lesson.Quizzes.Any())
                        {
                            foreach (var quiz in lesson.Quizzes)
                            {
                                var quizAttempts = await GetQuizAttemptsAsync(userId, quiz.QuizId);
                                var isQuizCompleted = quizAttempts.Any(ua => ua.IsPassed == true);
                                var bestScore = quizAttempts.Any() ? quizAttempts.Max(ua => ua.Score) : null;

                                quizViewModels.Add(new LearnQuizViewModel
                                {
                                    QuizId = quiz.QuizId,
                                    QuizName = quiz.QuizName,
                                    QuizDescription = quiz.QuizDescription ?? "",
                                    LessonId = quiz.LessonId ?? "",
                                    LessonName = lesson.LessonName,
                                    TimeLimit = quiz.TimeLimit,
                                    PassingScore = quiz.PassingScore,
                                    MaxAttempts = quiz.MaxAttempts,
                                    IsFinalQuiz = quiz.IsFinalQuiz ?? false,
                                    IsPrerequisiteQuiz = quiz.IsPrerequisiteQuiz ?? false,
                                    IsCompleted = isQuizCompleted,
                                    AttemptsUsed = quizAttempts.Count,
                                    BestScore = bestScore
                                });
                            }
                        }
                    }

                    chapterViewModels.Add(new LearnChapterViewModel
                    {
                        ChapterId = chapter.ChapterId,
                        ChapterName = chapter.ChapterName,
                        ChapterDescription = chapter.ChapterDescription ?? "",
                        ChapterOrder = chapter.ChapterOrder ?? 0,
                        IsLocked = chapter.IsLocked ?? false,
                        Lessons = lessonViewModels,
                        Quizzes = quizViewModels
                    });
                }

                // Xác định trạng thái khóa/mở cho từng chapter
                for (int i = 0; i < chapterViewModels.Count; i++)
                {
                    if (i == 0)
                    {
                        chapterViewModels[i].IsLocked = false;
                    }
                    else
                    {
                        var prevChapter = chapterViewModels[i - 1];
                        chapterViewModels[i].IsLocked = !prevChapter.Lessons.All(l => l.IsCompleted);
                    }
                }

                // Calculate overall course progress
                var courseProgress = await _lessonServiceFactory().GetLessonCompletionPercentageAsync(userId, courseId);

                var viewModel = new LearnManagementViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription ?? "",
                    CourseImage = course.CourseImage ?? "/SharedMedia/defaults/default-course.png",
                    AuthorName = course.Author?.FullName ?? "Unknown Author",
                    IsEnrolled = true,
                    ProgressPercentage = courseProgress,
                    Chapters = chapterViewModels
                };

                return new LearnManagementResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading learn management data for course {CourseId}", courseId);
                return new LearnManagementResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the learning content."
                };
            }
        }

        private string GetLessonTypeIcon(string? lessonType)
        {
            return lessonType?.ToLower() switch
            {
                "video" => "fas fa-play-circle",
                "text" => "fas fa-file-text",
                "interactive" => "fas fa-gamepad",
                "quiz" => "fas fa-question-circle",
                "assignment" => "fas fa-tasks",
                "document" => "fas fa-file-pdf",
                _ => "fas fa-book"
            };
        }

        private async Task<bool> IsLessonLockedAsync(string userId, Lesson lesson)
        {
            try
            {
                // If lesson is not locked by default, it's always accessible
                if (lesson.IsLocked != true)
                {
                    return false;
                }

                // If lesson requires a specific lesson to be completed first
                if (!string.IsNullOrEmpty(lesson.UnlockAfterLessonId))
                {
                    var isPrerequisiteCompleted = await _lessonServiceFactory().IsLessonCompletedAsync(userId, lesson.UnlockAfterLessonId);
                    return !isPrerequisiteCompleted;
                }

                // If lesson is locked but no specific prerequisite, check if previous lesson in same chapter is completed
                var previousLesson = await GetLessonsByChapterIdAsync(lesson.ChapterId);
                var previousLessonInOrder = previousLesson
                    .Where(l => l.LessonOrder < lesson.LessonOrder &&
                               l.LessonType?.LessonTypeName != "Quiz")
                    .OrderByDescending(l => l.LessonOrder)
                    .FirstOrDefault();

                if (previousLessonInOrder != null)
                {
                    var isPreviousCompleted = await _lessonServiceFactory().IsLessonCompletedAsync(userId, previousLessonInOrder.LessonId);
                    return !isPreviousCompleted;
                }

                // If no previous lesson, unlock it
                return false;
            }
            catch (Exception)
            {
                // If there's an error, assume lesson is locked for safety
                return true;
            }
        }

        private async Task<List<QuizAttempt>> GetQuizAttemptsAsync(string userId, string quizId)
        {
            try
            {
                // Sử dụng service provider để lấy context
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BrainStormEraContext>();

                return await context.QuizAttempts
                    .Where(qa => qa.UserId == userId && qa.QuizId == quizId)
                    .OrderByDescending(qa => qa.StartTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz attempts for user {UserId} and quiz {QuizId}", userId, quizId);
                return new List<QuizAttempt>();
            }
        }

        #endregion
    }

    #region Result Classes

    public class CourseIndexResult
    {
        public bool Success { get; set; }
        public CourseListViewModel ViewModel { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class CourseSearchResult
    {
        public bool Success { get; set; }
        public List<CourseViewModel> Courses { get; set; } = new();
        public int TotalCourses { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CourseDetailResult
    {
        public bool Success { get; set; }
        public bool IsNotFound { get; set; }
        public CourseDetailViewModel? ViewModel { get; set; }
        public bool IsAuthor { get; set; }
        public string? CurrentUserId { get; set; }
        public string? ActiveTab { get; set; }
    }

    public class EnrollmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CreateCourseResult
    {
        public bool Success { get; set; }
        public CreateCourseViewModel? ViewModel { get; set; }
        public string? SuccessMessage { get; set; }
        public string? WarningMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public bool ReturnView { get; set; }
    }

    public class EditCourseResult
    {
        public bool Success { get; set; }
        public CreateCourseViewModel? ViewModel { get; set; }
        public string? CourseId { get; set; }
        public string? SuccessMessage { get; set; }
        public string? WarningMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public bool ReturnView { get; set; }
    }

    public class DeleteCourseResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class InstructorCoursesResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Courses { get; set; }
    }

    public class CourseApprovalResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion
}









