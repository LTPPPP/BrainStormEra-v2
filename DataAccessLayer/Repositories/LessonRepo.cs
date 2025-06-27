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
    public class LessonRepo : BaseRepo<Lesson>, ILessonRepo
    {
        private readonly ILogger<LessonRepo>? _logger;

        public LessonRepo(BrainStormEraContext context, ILogger<LessonRepo>? logger = null)
            : base(context)
        {
            _logger = logger;
        }

        // Lesson query methods
        public async Task<Lesson?> GetLessonWithDetailsAsync(string lessonId)
        {
            try
            {
                return await _dbSet
                    .Include(l => l.Chapter)
                        .ThenInclude(c => c.Course)
                    .Include(l => l.LessonType)
                    .Include(l => l.Quizzes)
                        .ThenInclude(q => q.Questions)
                            .ThenInclude(qu => qu.AnswerOptions)
                    .Include(l => l.LessonPrerequisiteLessons)
                    .Include(l => l.LessonPrerequisitePrerequisiteLessons)
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving lesson with details: {LessonId}", lessonId);
                throw;
            }
        }

        public async Task<List<Lesson>> GetLessonsByChapterAsync(string chapterId)
        {
            try
            {
                return await _dbSet
                    .Where(l => l.ChapterId == chapterId)
                    .Include(l => l.LessonType)
                    .OrderBy(l => l.LessonOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting lessons by chapter: {ChapterId}", chapterId);
                throw;
            }
        }

        public async Task<List<Lesson>> GetLessonsByCourseAsync(string courseId)
        {
            try
            {
                return await _dbSet
                    .Include(l => l.Chapter)
                    .Include(l => l.LessonType)
                    .Where(l => l.Chapter.CourseId == courseId)
                    .OrderBy(l => l.Chapter.ChapterOrder)
                    .ThenBy(l => l.LessonOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting lessons by course: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<Lesson?> GetNextLessonAsync(string currentLessonId)
        {
            try
            {
                var currentLesson = await _dbSet
                    .Include(l => l.Chapter)
                    .FirstOrDefaultAsync(l => l.LessonId == currentLessonId);

                if (currentLesson == null) return null;

                // Try to find the next lesson in the same chapter
                var nextLessonInChapter = await _dbSet
                    .Where(l => l.ChapterId == currentLesson.ChapterId &&
                               l.LessonOrder > currentLesson.LessonOrder)
                    .OrderBy(l => l.LessonOrder)
                    .FirstOrDefaultAsync();

                if (nextLessonInChapter != null)
                    return nextLessonInChapter;

                // If no next lesson in chapter, find the first lesson of the next chapter
                var nextChapter = await _context.Chapters
                    .Where(c => c.CourseId == currentLesson.Chapter.CourseId &&
                               c.ChapterOrder > currentLesson.Chapter.ChapterOrder)
                    .OrderBy(c => c.ChapterOrder)
                    .FirstOrDefaultAsync();

                if (nextChapter != null)
                {
                    return await _dbSet
                        .Where(l => l.ChapterId == nextChapter.ChapterId)
                        .OrderBy(l => l.LessonOrder)
                        .FirstOrDefaultAsync();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting next lesson: {CurrentLessonId}", currentLessonId);
                throw;
            }
        }

        public async Task<Lesson?> GetPreviousLessonAsync(string currentLessonId)
        {
            try
            {
                var currentLesson = await _dbSet
                    .Include(l => l.Chapter)
                    .FirstOrDefaultAsync(l => l.LessonId == currentLessonId);

                if (currentLesson == null) return null;

                // Try to find the previous lesson in the same chapter
                var previousLessonInChapter = await _dbSet
                    .Where(l => l.ChapterId == currentLesson.ChapterId &&
                               l.LessonOrder < currentLesson.LessonOrder)
                    .OrderByDescending(l => l.LessonOrder)
                    .FirstOrDefaultAsync();

                if (previousLessonInChapter != null)
                    return previousLessonInChapter;

                // If no previous lesson in chapter, find the last lesson of the previous chapter
                var previousChapter = await _context.Chapters
                    .Where(c => c.CourseId == currentLesson.Chapter.CourseId &&
                               c.ChapterOrder < currentLesson.Chapter.ChapterOrder)
                    .OrderByDescending(c => c.ChapterOrder)
                    .FirstOrDefaultAsync();

                if (previousChapter != null)
                {
                    return await _dbSet
                        .Where(l => l.ChapterId == previousChapter.ChapterId)
                        .OrderByDescending(l => l.LessonOrder)
                        .FirstOrDefaultAsync();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting previous lesson: {CurrentLessonId}", currentLessonId);
                throw;
            }
        }

        // Lesson management methods
        public async Task<string> CreateLessonAsync(Lesson lesson)
        {
            try
            {
                await AddAsync(lesson);
                await SaveChangesAsync();
                return lesson.LessonId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating lesson");
                throw;
            }
        }

        public async Task<bool> UpdateLessonAsync(Lesson lesson)
        {
            try
            {
                await UpdateAsync(lesson);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating lesson");
                throw;
            }
        }

        public async Task<bool> DeleteLessonAsync(string lessonId, string authorId)
        {
            try
            {
                var lesson = await _dbSet
                    .Include(l => l.Chapter)
                        .ThenInclude(c => c.Course)
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId && l.Chapter.Course.AuthorId == authorId);

                if (lesson == null)
                    return false;

                await DeleteAsync(lesson);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting lesson");
                throw;
            }
        }

        public async Task<bool> UpdateLessonOrderAsync(string lessonId, int newOrder)
        {
            try
            {
                var lesson = await GetByIdAsync(lessonId);
                if (lesson == null)
                    return false;

                lesson.LessonOrder = newOrder;
                lesson.LessonUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating lesson order");
                throw;
            }
        }

        public async Task<bool> ReorderLessonsAsync(string chapterId, List<(string lessonId, int order)> lessonOrders)
        {
            try
            {
                var lessons = await _dbSet
                    .Where(l => l.ChapterId == chapterId)
                    .ToListAsync();

                foreach (var lessonOrder in lessonOrders)
                {
                    var lesson = lessons.FirstOrDefault(l => l.LessonId == lessonOrder.lessonId);
                    if (lesson != null)
                    {
                        lesson.LessonOrder = lessonOrder.order;
                        lesson.LessonUpdatedAt = DateTime.UtcNow;
                    }
                }

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reordering lessons");
                throw;
            }
        }

        // Lesson content methods
        public async Task<bool> UpdateLessonContentAsync(string lessonId, string content)
        {
            try
            {
                var lesson = await GetByIdAsync(lessonId);
                if (lesson == null)
                    return false;

                lesson.LessonContent = content;
                lesson.LessonUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating lesson content");
                throw;
            }
        }

        public async Task<bool> UpdateLessonVideoAsync(string lessonId, string videoUrl)
        {
            try
            {
                var lesson = await GetByIdAsync(lessonId);
                if (lesson == null)
                    return false;

                // VideoUrl property doesn't exist in Lesson model
                // This might need to be stored in LessonContent or a new property should be added to the model
                lesson.LessonUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating lesson video");
                throw;
            }
        }

        public async Task<bool> UpdateLessonDocumentAsync(string lessonId, string documentPath)
        {
            try
            {
                var lesson = await GetByIdAsync(lessonId);
                if (lesson == null)
                    return false;

                // DocumentPath property doesn't exist in Lesson model
                // This might need to be stored in LessonContent or a new property should be added to the model
                lesson.LessonUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating lesson document");
                throw;
            }
        }

        // Lesson progress methods
        public async Task<bool> MarkLessonAsCompletedAsync(string userId, string lessonId)
        {
            try
            {
                var existingProgress = await _context.UserProgresses
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lessonId);

                if (existingProgress != null)
                {
                    existingProgress.IsCompleted = true;
                    existingProgress.CompletedAt = DateTime.UtcNow;
                    existingProgress.LastAccessedAt = DateTime.UtcNow;
                }
                else
                {
                    var userProgress = new UserProgress
                    {
                        UserId = userId,
                        LessonId = lessonId,
                        IsCompleted = true,
                        CompletedAt = DateTime.UtcNow,
                        FirstAccessedAt = DateTime.UtcNow,
                        LastAccessedAt = DateTime.UtcNow
                    };

                    _context.UserProgresses.Add(userProgress);
                }

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error marking lesson as completed");
                throw;
            }
        }

        public async Task<bool> IsLessonCompletedAsync(string userId, string lessonId)
        {
            try
            {
                return await _context.UserProgresses
                    .AnyAsync(up => up.UserId == userId && up.LessonId == lessonId && up.IsCompleted == true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking lesson completion");
                throw;
            }
        }

        public async Task<List<Lesson>> GetCompletedLessonsAsync(string userId, string courseId)
        {
            try
            {
                return await _dbSet
                    .Include(l => l.Chapter)
                    .Where(l => l.Chapter.CourseId == courseId &&
                               _context.UserProgresses.Any(up => up.UserId == userId &&
                                                                 up.LessonId == l.LessonId &&
                                                                 up.IsCompleted == true))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting completed lessons");
                throw;
            }
        }

        public async Task<decimal> GetLessonCompletionPercentageAsync(string userId, string courseId)
        {
            try
            {
                var totalLessons = await GetTotalLessonsInCourseAsync(courseId);
                _logger?.LogInformation("Total lessons in course {CourseId}: {TotalLessons}", courseId, totalLessons);

                if (totalLessons == 0) return 0;

                var completedLessons = await GetCompletedLessonsCountAsync(userId, courseId);
                _logger?.LogInformation("Completed lessons for user {UserId} in course {CourseId}: {CompletedLessons}", userId, courseId, completedLessons);

                var percentage = Math.Round((decimal)completedLessons / totalLessons * 100, 2);
                _logger?.LogInformation("Calculated percentage: {Percentage}% ({CompletedLessons}/{TotalLessons})", percentage, completedLessons, totalLessons);

                return percentage;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating lesson completion percentage");
                throw;
            }
        }

        // Lesson prerequisites
        public async Task<bool> HasPrerequisitesCompletedAsync(string userId, string lessonId)
        {
            try
            {
                var prerequisites = await _context.LessonPrerequisites
                    .Where(lp => lp.LessonId == lessonId)
                    .Select(lp => lp.PrerequisiteLessonId)
                    .ToListAsync();

                if (!prerequisites.Any()) return true;

                foreach (var prerequisiteId in prerequisites)
                {
                    var isCompleted = await IsLessonCompletedAsync(userId, prerequisiteId);
                    if (!isCompleted) return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking lesson prerequisites");
                throw;
            }
        }

        public async Task<List<Lesson>> GetPrerequisiteLessonsAsync(string lessonId)
        {
            try
            {
                return await _context.LessonPrerequisites
                    .Where(lp => lp.LessonId == lessonId)
                    .Include(lp => lp.PrerequisiteLesson)
                    .Select(lp => lp.PrerequisiteLesson)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting prerequisite lessons");
                throw;
            }
        }

        public async Task<bool> AddLessonPrerequisiteAsync(string lessonId, string prerequisiteLessonId)
        {
            try
            {
                var existingPrerequisite = await _context.LessonPrerequisites
                    .FirstOrDefaultAsync(lp => lp.LessonId == lessonId && lp.PrerequisiteLessonId == prerequisiteLessonId);

                if (existingPrerequisite != null) return false;

                var lessonPrerequisite = new LessonPrerequisite
                {
                    LessonId = lessonId,
                    PrerequisiteLessonId = prerequisiteLessonId
                };

                _context.LessonPrerequisites.Add(lessonPrerequisite);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding lesson prerequisite");
                throw;
            }
        }

        public async Task<bool> RemoveLessonPrerequisiteAsync(string lessonId, string prerequisiteLessonId)
        {
            try
            {
                var prerequisite = await _context.LessonPrerequisites
                    .FirstOrDefaultAsync(lp => lp.LessonId == lessonId && lp.PrerequisiteLessonId == prerequisiteLessonId);

                if (prerequisite == null) return false;

                _context.LessonPrerequisites.Remove(prerequisite);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing lesson prerequisite");
                throw;
            }
        }

        // Lesson statistics
        public async Task<int> GetTotalLessonsInCourseAsync(string courseId)
        {
            try
            {
                return await _dbSet
                    .Include(l => l.Chapter)
                    .CountAsync(l => l.Chapter.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting total lessons in course");
                throw;
            }
        }

        public async Task<int> GetCompletedLessonsCountAsync(string userId, string courseId)
        {
            try
            {
                return await _dbSet
                    .Include(l => l.Chapter)
                    .CountAsync(l => l.Chapter.CourseId == courseId &&
                                    _context.UserProgresses.Any(up => up.UserId == userId &&
                                                                     up.LessonId == l.LessonId &&
                                                                     up.IsCompleted == true));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting completed lessons count");
                throw;
            }
        }

        public async Task<TimeSpan> GetEstimatedTimeForCourseAsync(string courseId)
        {
            try
            {
                // EstimatedDuration property doesn't exist in Lesson model
                // Return a default estimate based on lesson count or implement proper duration tracking
                var lessonCount = await _dbSet
                    .Include(l => l.Chapter)
                    .CountAsync(l => l.Chapter.CourseId == courseId);

                // Assume 10 minutes per lesson as default estimate
                var totalMinutes = lessonCount * 10;
                return TimeSpan.FromMinutes(totalMinutes);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting estimated time for course");
                throw;
            }
        }
    }
}
