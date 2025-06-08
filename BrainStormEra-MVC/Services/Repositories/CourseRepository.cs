using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BrainStormEra_MVC.Services.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly BrainStormEraContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CourseRepository> _logger;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

        public CourseRepository(BrainStormEraContext context, IMemoryCache cache, ILogger<CourseRepository> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }
        public IQueryable<Course> GetActiveCourses()
        {
            return _context.Courses
                .AsNoTracking()
                .Where(c => c.CourseStatus == 1);
        }

        public async Task<Course?> GetCourseByIdAsync(string courseId)
        {
            var cacheKey = $"Course_{courseId}";

            if (_cache.TryGetValue(cacheKey, out Course? cachedCourse))
            {
                return cachedCourse;
            }

            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.CourseStatus == 1);

            if (course != null)
            {
                _cache.Set(cacheKey, course, CacheExpiration);
            }

            return course;
        }

        public async Task<Course?> GetCourseDetailAsync(string courseId)
        {
            var cacheKey = $"CourseDetail_{courseId}";

            if (_cache.TryGetValue(cacheKey, out Course? cachedCourse))
            {
                return cachedCourse;
            }

            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Author)
                .Include(c => c.Chapters.OrderBy(ch => ch.ChapterOrder))
                    .ThenInclude(ch => ch.Lessons.OrderBy(l => l.LessonOrder))
                        .ThenInclude(l => l.LessonType)
                .Include(c => c.Chapters)
                    .ThenInclude(ch => ch.Lessons)
                        .ThenInclude(l => l.Quizzes)
                            .ThenInclude(q => q.Questions.OrderBy(qu => qu.QuestionOrder))
                                .ThenInclude(qu => qu.AnswerOptions.OrderBy(ao => ao.OptionOrder))
                .Include(c => c.CourseCategories)
                .Include(c => c.Feedbacks)
                    .ThenInclude(f => f.User)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.CourseStatus == 1);

            if (course != null)
            {
                _cache.Set(cacheKey, course, TimeSpan.FromMinutes(10));
            }

            return course;
        }

        public async Task<Course?> GetCourseDetailAsync(string courseId, string? currentUserId = null)
        {
            // If no current user provided, use the original method
            if (string.IsNullOrEmpty(currentUserId))
            {
                return await GetCourseDetailAsync(courseId);
            }

            // For authenticated users, check if they're the course author
            var cacheKey = $"CourseDetail_{courseId}_{currentUserId}";

            if (_cache.TryGetValue(cacheKey, out Course? cachedCourse))
            {
                return cachedCourse;
            }

            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Author)
                .Include(c => c.Chapters.OrderBy(ch => ch.ChapterOrder))
                    .ThenInclude(ch => ch.Lessons.OrderBy(l => l.LessonOrder))
                        .ThenInclude(l => l.LessonType)
                .Include(c => c.Chapters)
                    .ThenInclude(ch => ch.Lessons)
                        .ThenInclude(l => l.Quizzes)
                            .ThenInclude(q => q.Questions.OrderBy(qu => qu.QuestionOrder))
                                .ThenInclude(qu => qu.AnswerOptions.OrderBy(ao => ao.OptionOrder))
                .Include(c => c.CourseCategories)
                .Include(c => c.Feedbacks)
                    .ThenInclude(f => f.User)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.CourseId == courseId &&
                    (c.CourseStatus == 1 || c.AuthorId == currentUserId)); // Allow authors to view their own courses regardless of status

            if (course != null)
            {
                _cache.Set(cacheKey, course, TimeSpan.FromMinutes(10));
            }

            return course;
        }

        public async Task<List<Course>> GetFeaturedCoursesAsync(int take = 4)
        {
            var cacheKey = $"FeaturedCourses_{take}";

            if (_cache.TryGetValue(cacheKey, out List<Course>? cachedCourses))
            {
                return cachedCourses!;
            }

            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.IsFeatured == true && c.CourseStatus == 1)
                .Include(c => c.Author)
                .Take(take)
                .ToListAsync();

            _cache.Set(cacheKey, courses, CacheExpiration);
            return courses;
        }

        public async Task<bool> CourseExistsAsync(string courseId)
        {
            return await _context.Courses
                .AsNoTracking()
                .AnyAsync(c => c.CourseId == courseId && c.CourseStatus == 1);
        }

        public async Task<int> GetTotalCoursesCountAsync(string? search, string? category)
        {
            var query = _context.Courses
                .AsNoTracking()
                .Where(c => c.CourseStatus == 1);

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

            return await query.CountAsync();
        }
    }
}
