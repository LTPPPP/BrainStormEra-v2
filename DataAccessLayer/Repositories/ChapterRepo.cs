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
    public class ChapterRepo : BaseRepo<Chapter>, IChapterRepo
    {
        private readonly ILogger<ChapterRepo>? _logger;

        public ChapterRepo(BrainStormEraContext context, ILogger<ChapterRepo>? logger = null)
            : base(context)
        {
            _logger = logger;
        }

        // Chapter query methods
        public async Task<Chapter?> GetChapterWithLessonsAsync(string chapterId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Lessons.OrderBy(l => l.LessonOrder))
                        .ThenInclude(l => l.LessonType)
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving chapter with lessons: {ChapterId}", chapterId);
                throw;
            }
        }

        public async Task<Chapter?> GetChapterWithCourseAsync(string chapterId, string authorId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId && c.Course.AuthorId == authorId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving chapter with course for author: {ChapterId}, {AuthorId}", chapterId, authorId);
                throw;
            }
        }

        public async Task<List<Chapter>> GetChaptersByCourseAsync(string courseId)
        {
            try
            {
                return await _dbSet
                    .Where(c => c.CourseId == courseId)
                    .Include(c => c.Lessons.OrderBy(l => l.LessonOrder))
                    .OrderBy(c => c.ChapterOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting chapters by course: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<Chapter?> GetChapterWithQuizzesAsync(string chapterId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.Quizzes)
                            .ThenInclude(q => q.Questions)
                                .ThenInclude(qu => qu.AnswerOptions)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving chapter with quizzes: {ChapterId}", chapterId);
                throw;
            }
        }

        public async Task<Chapter?> GetNextChapterAsync(string currentChapterId)
        {
            try
            {
                var currentChapter = await _dbSet
                    .FirstOrDefaultAsync(c => c.ChapterId == currentChapterId);

                if (currentChapter == null) return null;

                return await _dbSet
                    .Where(c => c.CourseId == currentChapter.CourseId &&
                               c.ChapterOrder > currentChapter.ChapterOrder)
                    .OrderBy(c => c.ChapterOrder)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting next chapter: {CurrentChapterId}", currentChapterId);
                throw;
            }
        }

        public async Task<Chapter?> GetPreviousChapterAsync(string currentChapterId)
        {
            try
            {
                var currentChapter = await _dbSet
                    .FirstOrDefaultAsync(c => c.ChapterId == currentChapterId);

                if (currentChapter == null) return null;

                return await _dbSet
                    .Where(c => c.CourseId == currentChapter.CourseId &&
                               c.ChapterOrder < currentChapter.ChapterOrder)
                    .OrderByDescending(c => c.ChapterOrder)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting previous chapter: {CurrentChapterId}", currentChapterId);
                throw;
            }
        }

        // Chapter management methods
        public async Task<string> CreateChapterAsync(Chapter chapter)
        {
            try
            {
                await AddAsync(chapter);
                await SaveChangesAsync();
                return chapter.ChapterId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating chapter");
                throw;
            }
        }

        public async Task<bool> UpdateChapterAsync(Chapter chapter)
        {
            try
            {
                await UpdateAsync(chapter);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating chapter");
                throw;
            }
        }

        public async Task<bool> DeleteChapterAsync(string chapterId, string authorId)
        {
            try
            {
                var chapter = await _dbSet
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId && c.Course.AuthorId == authorId);

                if (chapter == null)
                    return false;

                await DeleteAsync(chapter);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting chapter");
                throw;
            }
        }

        public async Task<bool> UpdateChapterOrderAsync(string chapterId, int newOrder)
        {
            try
            {
                var chapter = await GetByIdAsync(chapterId);
                if (chapter == null)
                    return false;

                chapter.ChapterOrder = newOrder;
                chapter.ChapterUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating chapter order");
                throw;
            }
        }

        public async Task<bool> ReorderChaptersAsync(string courseId, List<(string chapterId, int order)> chapterOrders)
        {
            try
            {
                var chapters = await _dbSet
                    .Where(c => c.CourseId == courseId)
                    .ToListAsync();

                foreach (var chapterOrder in chapterOrders)
                {
                    var chapter = chapters.FirstOrDefault(c => c.ChapterId == chapterOrder.chapterId);
                    if (chapter != null)
                    {
                        chapter.ChapterOrder = chapterOrder.order;
                        chapter.ChapterUpdatedAt = DateTime.UtcNow;
                    }
                }

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reordering chapters");
                throw;
            }
        }

        // Chapter content methods
        public async Task<bool> UpdateChapterDescriptionAsync(string chapterId, string description)
        {
            try
            {
                var chapter = await GetByIdAsync(chapterId);
                if (chapter == null)
                    return false;

                chapter.ChapterDescription = description;
                chapter.ChapterUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating chapter description");
                throw;
            }
        }

        public async Task<bool> UpdateChapterNameAsync(string chapterId, string name)
        {
            try
            {
                var chapter = await GetByIdAsync(chapterId);
                if (chapter == null)
                    return false;

                chapter.ChapterName = name;
                chapter.ChapterUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating chapter name");
                throw;
            }
        }

        // Chapter progress methods
        public async Task<bool> IsChapterCompletedAsync(string userId, string chapterId)
        {
            try
            {
                var totalLessons = await _context.Lessons
                    .CountAsync(l => l.ChapterId == chapterId);

                if (totalLessons == 0) return false;

                var completedLessons = await _context.UserProgresses
                    .CountAsync(up => up.UserId == userId &&
                                     up.Lesson.ChapterId == chapterId &&
                                     up.IsCompleted == true);

                return completedLessons == totalLessons;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking chapter completion");
                throw;
            }
        }

        public async Task<List<Chapter>> GetCompletedChaptersAsync(string userId, string courseId)
        {
            try
            {
                var chapters = await GetChaptersByCourseAsync(courseId);
                var completedChapters = new List<Chapter>();

                foreach (var chapter in chapters)
                {
                    if (await IsChapterCompletedAsync(userId, chapter.ChapterId))
                    {
                        completedChapters.Add(chapter);
                    }
                }

                return completedChapters;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting completed chapters");
                throw;
            }
        }

        public async Task<decimal> GetChapterCompletionPercentageAsync(string userId, string chapterId)
        {
            try
            {
                var totalLessons = await _context.Lessons
                    .CountAsync(l => l.ChapterId == chapterId);

                if (totalLessons == 0) return 0;

                var completedLessons = await _context.UserProgresses
                    .CountAsync(up => up.UserId == userId &&
                                     up.Lesson.ChapterId == chapterId &&
                                     up.IsCompleted == true);

                return Math.Round((decimal)completedLessons / totalLessons * 100, 2);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating chapter completion percentage");
                throw;
            }
        }

        // Chapter statistics
        public async Task<int> GetTotalChaptersInCourseAsync(string courseId)
        {
            try
            {
                return await _dbSet
                    .CountAsync(c => c.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting total chapters in course");
                throw;
            }
        }

        public async Task<int> GetCompletedChaptersCountAsync(string userId, string courseId)
        {
            try
            {
                var chapters = await GetChaptersByCourseAsync(courseId);
                int completedCount = 0;

                foreach (var chapter in chapters)
                {
                    if (await IsChapterCompletedAsync(userId, chapter.ChapterId))
                    {
                        completedCount++;
                    }
                }

                return completedCount;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting completed chapters count");
                throw;
            }
        }

        public async Task<int> GetLessonsCountInChapterAsync(string chapterId)
        {
            try
            {
                return await _context.Lessons
                    .CountAsync(l => l.ChapterId == chapterId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting lessons count in chapter");
                throw;
            }
        }

        public async Task<TimeSpan> GetEstimatedTimeForChapterAsync(string chapterId)
        {
            try
            {
                var totalMinutes = await _context.Lessons
                                .Where(l => l.ChapterId == chapterId && l.MinTimeSpent.HasValue)
            .SumAsync(l => l.MinTimeSpent!.Value);

                return TimeSpan.FromMinutes(totalMinutes);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting estimated time for chapter");
                throw;
            }
        }
    }
}
