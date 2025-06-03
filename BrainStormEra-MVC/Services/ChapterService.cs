using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Services
{
    public class ChapterService : IChapterService
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<ChapterService> _logger;

        public ChapterService(BrainStormEraContext context, ILogger<ChapterService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> CreateChapterAsync(CreateChapterViewModel model, string authorId)
        {
            try
            {
                // Verify that the user is the author of the course
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseId == model.CourseId && c.AuthorId == authorId);

                if (course == null)
                {
                    _logger.LogWarning("Course not found or user not authorized to add chapters to course {CourseId}", model.CourseId);
                    throw new UnauthorizedAccessException("You are not authorized to add chapters to this course.");
                }

                var chapterId = Guid.NewGuid().ToString();

                var chapter = new Chapter
                {
                    ChapterId = chapterId,
                    CourseId = model.CourseId,
                    ChapterName = model.ChapterName,
                    ChapterDescription = model.ChapterDescription,
                    ChapterOrder = model.ChapterOrder,
                    ChapterStatus = 1, // Active status
                    IsLocked = model.IsLocked,
                    UnlockAfterChapterId = model.UnlockAfterChapterId,
                    ChapterCreatedAt = DateTime.UtcNow,
                    ChapterUpdatedAt = DateTime.UtcNow
                };

                _context.Chapters.Add(chapter);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Chapter created successfully: {ChapterId} for course {CourseId}", chapterId, model.CourseId);
                return chapterId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chapter for course {CourseId}", model.CourseId);
                throw;
            }
        }
        public async Task<CreateChapterViewModel?> GetCreateChapterViewModelAsync(string courseId, string authorId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Chapters)
                    .ThenInclude(ch => ch.Lessons)
                    .ThenInclude(l => l.LessonType)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.AuthorId == authorId);

                if (course == null)
                {
                    _logger.LogWarning("Course not found or user not authorized: {CourseId} for user {AuthorId}", courseId, authorId);
                    return null;
                }

                var existingChapters = course.Chapters
                    .OrderBy(c => c.ChapterOrder)
                    .Select(c => new ChapterViewModel
                    {
                        ChapterId = c.ChapterId,
                        ChapterName = c.ChapterName,
                        ChapterDescription = c.ChapterDescription ?? "",
                        ChapterOrder = c.ChapterOrder ?? 0,
                        Lessons = c.Lessons?.Select(l => new LessonViewModel
                        {
                            LessonId = l.LessonId,
                            LessonName = l.LessonName,
                            LessonDescription = l.LessonDescription ?? "",
                            LessonOrder = l.LessonOrder,
                            LessonType = l.LessonType?.LessonTypeName ?? "Content",
                            EstimatedDuration = 0, // Calculate if needed
                            IsLocked = l.IsLocked ?? false
                        }).ToList() ?? new List<LessonViewModel>()
                    }).ToList();

                var nextChapterOrder = existingChapters.Any() ? existingChapters.Max(c => c.ChapterOrder) + 1 : 1;

                return new CreateChapterViewModel
                {
                    CourseId = courseId,
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription ?? "",
                    ChapterOrder = nextChapterOrder,
                    ExistingChapters = existingChapters
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting create chapter view model for course {CourseId}", courseId);
                return null;
            }
        }

        public async Task<bool> UpdateChapterAsync(string chapterId, CreateChapterViewModel model, string authorId)
        {
            try
            {
                var chapter = await _context.Chapters
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId && c.Course.AuthorId == authorId);

                if (chapter == null)
                {
                    _logger.LogWarning("Chapter not found or user not authorized to update chapter {ChapterId}", chapterId);
                    return false;
                }

                chapter.ChapterName = model.ChapterName;
                chapter.ChapterDescription = model.ChapterDescription;
                chapter.ChapterOrder = model.ChapterOrder;
                chapter.IsLocked = model.IsLocked;
                chapter.UnlockAfterChapterId = model.UnlockAfterChapterId;
                chapter.ChapterUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Chapter updated successfully: {ChapterId}", chapterId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chapter {ChapterId}", chapterId);
                return false;
            }
        }

        public async Task<bool> DeleteChapterAsync(string chapterId, string authorId)
        {
            try
            {
                var chapter = await _context.Chapters
                    .Include(c => c.Course)
                    .Include(c => c.Lessons)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId && c.Course.AuthorId == authorId);

                if (chapter == null)
                {
                    _logger.LogWarning("Chapter not found or user not authorized to delete chapter {ChapterId}", chapterId);
                    return false;
                }

                // Remove associated lessons first
                if (chapter.Lessons.Any())
                {
                    _context.Lessons.RemoveRange(chapter.Lessons);
                }

                _context.Chapters.Remove(chapter);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Chapter deleted successfully: {ChapterId}", chapterId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chapter {ChapterId}", chapterId);
                return false;
            }
        }

        public async Task<List<ChapterViewModel>> GetChaptersByCourseIdAsync(string courseId)
        {
            try
            {
                var chapters = await _context.Chapters
                    .Where(c => c.CourseId == courseId)
                    .OrderBy(c => c.ChapterOrder)
                    .Select(c => new ChapterViewModel
                    {
                        ChapterId = c.ChapterId,
                        ChapterName = c.ChapterName,
                        ChapterDescription = c.ChapterDescription ?? "",
                        ChapterOrder = c.ChapterOrder ?? 0
                    })
                    .ToListAsync();

                return chapters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapters for course {CourseId}", courseId);
                return new List<ChapterViewModel>();
            }
        }
    }
}