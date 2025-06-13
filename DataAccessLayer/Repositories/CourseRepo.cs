using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataAccessLayer.Repositories
{
    public class CourseRepo : BaseRepo<Course>, ICourseRepo
    {
        private readonly ILogger<CourseRepo>? _logger;

        public CourseRepo(BrainStormEraContext context, ILogger<CourseRepo>? logger = null)
            : base(context)
        {
            _logger = logger;
        }

        // Course query methods
        public IQueryable<Course> GetActiveCourses()
        {
            return _dbSet.Where(c => c.CourseStatus == 1); // Active status
        }

        public async Task<Course?> GetCourseDetailAsync(string courseId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Author)
                    .Include(c => c.Enrollments)
                    .Include(c => c.CourseCategories)
                    .Include(c => c.Feedbacks)
                        .ThenInclude(f => f.User)
                    .Include(c => c.Chapters.OrderBy(ch => ch.ChapterOrder))
                        .ThenInclude(ch => ch.Lessons.OrderBy(l => l.LessonOrder))
                            .ThenInclude(l => l.LessonType)
                    .Include(c => c.Chapters)
                        .ThenInclude(ch => ch.Lessons)
                            .ThenInclude(l => l.Quizzes)
                                .ThenInclude(q => q.Questions.OrderBy(qu => qu.QuestionOrder))
                                    .ThenInclude(qu => qu.AnswerOptions.OrderBy(ao => ao.OptionOrder))
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving course detail: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<Course?> GetCourseWithChaptersAsync(string courseId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Chapters.OrderBy(ch => ch.ChapterOrder))
                        .ThenInclude(ch => ch.Lessons.OrderBy(l => l.LessonOrder))
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving course with chapters: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<Course?> GetCourseWithChaptersAsync(string courseId, string authorId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Chapters.OrderBy(ch => ch.ChapterOrder))
                        .ThenInclude(ch => ch.Lessons.OrderBy(l => l.LessonOrder))
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.AuthorId == authorId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving course with chapters for author: {CourseId}, {AuthorId}", courseId, authorId);
                throw;
            }
        }

        public async Task<Course?> GetCourseWithEnrollmentsAsync(string courseId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.User)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving course with enrollments: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<List<Course>> SearchCoursesAsync(string? search, string? category, int page, int pageSize, string? sortBy)
        {
            try
            {
                IQueryable<Course> query = GetActiveCourses()
                    .Include(c => c.Author)
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

                // Apply sorting
                query = sortBy?.ToLower() switch
                {
                    "name" => query.OrderBy(c => c.CourseName),
                    "price" => query.OrderBy(c => c.Price),
                    "date" => query.OrderByDescending(c => c.CourseCreatedAt),
                    _ => query.OrderByDescending(c => c.CourseCreatedAt)
                };

                return await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching courses");
                throw;
            }
        }

        // Course management methods
        public async Task<string> CreateCourseAsync(Course course)
        {
            try
            {
                await AddAsync(course);
                await SaveChangesAsync();
                return course.CourseId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating course");
                throw;
            }
        }

        public async Task<bool> UpdateCourseAsync(Course course)
        {
            try
            {
                await UpdateAsync(course);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating course");
                throw;
            }
        }

        public async Task<bool> UpdateCourseImageAsync(string courseId, string imagePath)
        {
            try
            {
                var course = await GetByIdAsync(courseId);
                if (course == null)
                    return false;

                course.CourseImage = imagePath;
                course.CourseUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating course image");
                throw;
            }
        }

        public async Task<bool> DeleteCourseAsync(string courseId, string authorId)
        {
            try
            {
                var course = await _dbSet
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.AuthorId == authorId);

                if (course == null)
                    return false;

                // Soft delete by changing status
                course.CourseStatus = 0; // Inactive/Deleted status
                course.CourseUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting course");
                throw;
            }
        }

        public async Task<Course?> GetCourseForEditAsync(string courseId, string authorId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.CourseCategories)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.AuthorId == authorId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting course for edit");
                throw;
            }
        }

        // Instructor-specific methods
        public async Task<List<Course>> GetInstructorCoursesAsync(string authorId, string? search, string? category, int page, int pageSize)
        {
            try
            {
                IQueryable<Course> query = _dbSet
                    .Where(c => c.AuthorId == authorId && c.CourseStatus != 4) // Exclude archived courses
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

                return await query
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting instructor courses");
                throw;
            }
        }

        public async Task<List<Course>> GetCoursesForFilterAsync(string instructorId)
        {
            try
            {
                return await _dbSet
                    .Where(c => c.AuthorId == instructorId && c.CourseStatus != 4) // Exclude archived courses
                    .Select(c => new Course
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting courses for filter");
                throw;
            }
        }

        // Enrollment methods
        public async Task<bool> EnrollUserAsync(string userId, string courseId)
        {
            try
            {
                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (existingEnrollment != null)
                    return false; // Already enrolled

                var enrollment = new Enrollment
                {
                    EnrollmentId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    CourseId = courseId,
                    EnrollmentCreatedAt = DateTime.UtcNow,
                    EnrollmentUpdatedAt = DateTime.UtcNow,
                    EnrollmentStatus = 1, // Active status
                    ProgressPercentage = 0
                };

                _context.Enrollments.Add(enrollment);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error enrolling user");
                throw;
            }
        }

        public async Task<bool> IsUserEnrolledAsync(string userId, string courseId)
        {
            try
            {
                return await _context.Enrollments
                    .AnyAsync(e => e.UserId == userId && e.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking user enrollment");
                throw;
            }
        }

        // Categories methods
        public async Task<List<CourseCategory>> GetCategoriesAsync()
        {
            try
            {
                return await _context.CourseCategories
                    .OrderBy(c => c.CourseCategoryName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting categories");
                throw;
            }
        }

        public async Task<List<CourseCategory>> SearchCategoriesAsync(string searchTerm)
        {
            try
            {
                return await _context.CourseCategories
                    .Where(c => c.CourseCategoryName.Contains(searchTerm))
                    .OrderBy(c => c.CourseCategoryName)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching categories");
                throw;
            }
        }

        // Statistics methods
        public async Task<int> GetTotalStudentsForCourseAsync(string courseId)
        {
            try
            {
                return await _context.Enrollments
                    .CountAsync(e => e.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting total students for course");
                throw;
            }
        }

        public async Task<decimal> GetAverageRatingForCourseAsync(string courseId)
        {
            try
            {
                var ratings = await _context.Feedbacks
                    .Where(f => f.CourseId == courseId && f.StarRating.HasValue)
                    .Select(f => f.StarRating!.Value)
                    .ToListAsync();

                return ratings.Any() ? (decimal)ratings.Select(r => (double)r).Average() : 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting average rating for course");
                throw;
            }
        }

        public async Task<int> GetTotalReviewsForCourseAsync(string courseId)
        {
            try
            {
                return await _context.Feedbacks
                    .CountAsync(f => f.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting total reviews for course");
                throw;
            }
        }

        // Missing methods implementation
        public async Task<Course?> GetCourseDetailAsync(string courseId, string? currentUserId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Author)
                    .Include(c => c.CourseCategories)
                    .Include(c => c.Enrollments)
                    .Include(c => c.Feedbacks)
                        .ThenInclude(f => f.User)
                    .Include(c => c.Chapters)
                        .ThenInclude(ch => ch.Lessons)
                            .ThenInclude(l => l.LessonType)
                    .Include(c => c.Chapters)
                        .ThenInclude(ch => ch.Lessons)
                            .ThenInclude(l => l.Quizzes)
                                .ThenInclude(q => q.Questions)
                                    .ThenInclude(qu => qu.AnswerOptions)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting course detail with user context");
                throw;
            }
        }

        public async Task<Course?> GetCourseByIdAsync(string courseId)
        {
            try
            {
                return await GetByIdAsync(courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting course by ID");
                throw;
            }
        }

        public async Task<List<Enrollment>> GetUserEnrollmentsAsync(string userId)
        {
            try
            {
                return await _context.Enrollments
                    .Where(e => e.UserId == userId)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Author)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user enrollments for user {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetCourseEnrollmentCountAsync(string courseId)
        {
            try
            {
                return await _context.Enrollments
                    .CountAsync(e => e.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting enrollment count for course {CourseId}", courseId);
                throw;
            }
        }

        public async Task<List<string>> GetEnrolledUserIdsAsync(string courseId)
        {
            try
            {
                return await _context.Enrollments
                    .Where(e => e.CourseId == courseId && e.EnrollmentStatus == 1) // 1 for Active
                    .Select(e => e.UserId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting enrolled user IDs for course {CourseId}", courseId);
                throw;
            }
        }

        public async Task<List<Course>> GetFeaturedCoursesAsync(int count = 6)
        {
            try
            {
                return await _dbSet
                    .Where(c => c.IsFeatured == true && c.CourseStatus == 1)
                    .Include(c => c.Author)
                    .Include(c => c.Enrollments)
                    .Include(c => c.CourseCategories)
                    .OrderByDescending(c => c.Enrollments.Count)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting featured courses");
                throw;
            }
        }

        public async Task<List<Course>> GetRecommendedCoursesForUserAsync(string userId, List<string> excludeCourseIds, int count = 6)
        {
            try
            {
                var recommendedCourses = new List<Course>();

                // First, try to get featured courses
                var featuredCourses = await _dbSet
                    .Where(c => c.IsFeatured == true &&
                               c.CourseStatus == 1 &&
                               !excludeCourseIds.Contains(c.CourseId))
                    .Include(c => c.Author)
                    .Include(c => c.CourseCategories)
                    .Include(c => c.Enrollments)
                    .OrderByDescending(c => c.Enrollments.Count)
                    .Take(count)
                    .ToListAsync();

                recommendedCourses.AddRange(featuredCourses);

                // If we don't have enough courses, add popular courses as fallback
                if (recommendedCourses.Count < count)
                {
                    var remainingCount = count - recommendedCourses.Count;
                    var usedIds = recommendedCourses.Select(c => c.CourseId).Concat(excludeCourseIds).ToList();

                    var popularCourses = await _dbSet
                        .Where(c => c.CourseStatus == 1 &&
                                   !usedIds.Contains(c.CourseId))
                        .Include(c => c.Author)
                        .Include(c => c.CourseCategories)
                        .Include(c => c.Enrollments)
                        .OrderByDescending(c => c.Enrollments.Count)
                        .ThenByDescending(c => c.CourseCreatedAt)
                        .Take(remainingCount)
                        .ToListAsync();

                    recommendedCourses.AddRange(popularCourses);
                }

                // If still not enough, add recent courses as final fallback
                if (recommendedCourses.Count < count)
                {
                    var remainingCount = count - recommendedCourses.Count;
                    var usedIds = recommendedCourses.Select(c => c.CourseId).Concat(excludeCourseIds).ToList();

                    var recentCourses = await _dbSet
                        .Where(c => c.CourseStatus == 1 &&
                                   !usedIds.Contains(c.CourseId))
                        .Include(c => c.Author)
                        .Include(c => c.CourseCategories)
                        .Include(c => c.Enrollments)
                        .OrderByDescending(c => c.CourseCreatedAt)
                        .Take(remainingCount)
                        .ToListAsync();

                    recommendedCourses.AddRange(recentCourses);
                }

                return recommendedCourses.Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting recommended courses for user {UserId}", userId);
                // Return empty list instead of throwing to prevent crashes
                return new List<Course>();
            }
        }

        public async Task<List<CourseCategory>> GetCategoriesWithCourseCountAsync(int count = 8)
        {
            try
            {
                // Get all active categories with their active courses included
                var categoriesWithCourses = await _context.CourseCategories
                    .Include(cc => cc.Courses.Where(c => c.CourseStatus == 1))
                    .Where(cc => cc.IsActive == true)
                    .ToListAsync();

                // Order by course count and take the specified number
                return categoriesWithCourses
                    .OrderByDescending(cc => cc.Courses.Count)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting categories with course count");
                throw;
            }
        }

        public async Task<List<Course>> GetRecentCoursesAsync(int count = 5)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Author)
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting recent courses");
                throw;
            }
        }

        // Admin course management methods
        public async Task<List<Course>> GetAllCoursesAsync(string? search = null, string? categoryFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                IQueryable<Course> query = _dbSet
                    .Where(c => c.CourseStatus != 4) // Exclude archived/soft deleted courses
                    .Include(c => c.Author)
                    .Include(c => c.Enrollments)
                    .Include(c => c.CourseCategories);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.CourseName.Contains(search) ||
                                           (c.CourseDescription != null && c.CourseDescription.Contains(search)) ||
                                           c.CourseCategories.Any(cc => cc.CourseCategoryName!.Contains(search)));
                }

                if (!string.IsNullOrWhiteSpace(categoryFilter))
                {
                    query = query.Where(c => c.CourseCategories
                        .Any(cc => cc.CourseCategoryName == categoryFilter));
                }

                return await query
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting all courses for admin");
                throw;
            }
        }

        public async Task<int> GetCourseCountAsync(string? search = null, string? categoryFilter = null)
        {
            try
            {
                IQueryable<Course> query = _dbSet
                    .Where(c => c.CourseStatus != 4) // Exclude archived/soft deleted courses
                    .Include(c => c.CourseCategories); // Need to include for category search

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.CourseName.Contains(search) ||
                                           (c.CourseDescription != null && c.CourseDescription.Contains(search)) ||
                                           c.CourseCategories.Any(cc => cc.CourseCategoryName!.Contains(search)));
                }

                if (!string.IsNullOrWhiteSpace(categoryFilter))
                {
                    query = query.Where(c => c.CourseCategories
                        .Any(cc => cc.CourseCategoryName == categoryFilter));
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting course count");
                throw;
            }
        }

        public async Task<int> GetApprovedCourseCountAsync()
        {
            try
            {
                return await _dbSet.CountAsync(c => c.ApprovalStatus == "Approved");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting approved course count");
                throw;
            }
        }

        public async Task<int> GetPendingCourseCountAsync()
        {
            try
            {
                return await _dbSet.CountAsync(c => c.ApprovalStatus == "Pending");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting pending course count");
                throw;
            }
        }

        public async Task<int> GetRejectedCourseCountAsync()
        {
            try
            {
                return await _dbSet.CountAsync(c => c.ApprovalStatus == "Rejected");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting rejected course count");
                throw;
            }
        }

        public async Task<int> GetFreeCourseCountAsync()
        {
            try
            {
                return await _dbSet.CountAsync(c => c.Price == 0);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting free course count");
                throw;
            }
        }

        public async Task<int> GetPaidCourseCountAsync()
        {
            try
            {
                return await _dbSet.CountAsync(c => c.Price > 0);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting paid course count");
                throw;
            }
        }

        public async Task<bool> UpdateCourseApprovalAsync(string courseId, bool isApproved)
        {
            try
            {
                var course = await GetByIdAsync(courseId);
                if (course == null)
                    return false;

                course.ApprovalStatus = isApproved ? "Approved" : "Rejected";
                course.ApprovedAt = DateTime.UtcNow;
                course.CourseUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating course approval status");
                throw;
            }
        }

        public async Task<bool> DeleteCourseAsync(string courseId)
        {
            try
            {
                var course = await GetByIdAsync(courseId);
                if (course == null)
                    return false;

                // Soft delete by changing status
                course.CourseStatus = 0; // Inactive/Deleted status
                course.CourseUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting course");
                throw;
            }
        }
    }
}
