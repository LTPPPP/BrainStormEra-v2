using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Services
{
    public class PageContextService : IPageContextService
    {
        private readonly BrainStormEraContext _context;

        public PageContextService(BrainStormEraContext context)
        {
            _context = context;
        }

        public async Task<string> GetPageContextAsync(string path, string? courseId = null, string? chapterId = null, string? lessonId = null)
        {
            var context = new List<string>();

            if (!string.IsNullOrEmpty(courseId))
            {
                var courseContext = await GetCourseContextAsync(courseId);
                if (!string.IsNullOrEmpty(courseContext))
                    context.Add(courseContext);
            }

            if (!string.IsNullOrEmpty(chapterId))
            {
                var chapterContext = await GetChapterContextAsync(chapterId);
                if (!string.IsNullOrEmpty(chapterContext))
                    context.Add(chapterContext);
            }

            if (!string.IsNullOrEmpty(lessonId))
            {
                var lessonContext = await GetLessonContextAsync(lessonId);
                if (!string.IsNullOrEmpty(lessonContext))
                    context.Add(lessonContext);
            }            // Add page type context
            if (path.Contains("/Course"))
                context.Add("User is viewing course page");
            else if (path.Contains("/Chapter"))
                context.Add("User is viewing chapter page");
            else if (path.Contains("/Lesson"))
                context.Add("User is viewing lesson page");
            else if (path.Contains("/Home"))
                context.Add("User is on home page");

            return string.Join(". ", context);
        }
        public async Task<string> GetCourseContextAsync(string courseId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.CourseCategories)
                    .Include(c => c.Author)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId); if (course == null) return "";

                var categories = course.CourseCategories?.Select(cc => cc.CourseCategoryName).ToList() ?? new List<string>();
                var categoryText = categories.Any() ? string.Join(", ", categories) : "Uncategorized";

                return $"Current course: '{course.CourseName}' in category {categoryText}, " +
                       $"taught by {course.Author?.FullName}. " +
                       $"Description: {course.CourseDescription?.Substring(0, Math.Min(100, course.CourseDescription?.Length ?? 0))}";
            }
            catch
            {
                return "";
            }
        }
        public async Task<string> GetChapterContextAsync(string chapterId)
        {
            try
            {
                var chapter = await _context.Chapters
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId); if (chapter == null) return "";

                return $"Current chapter: '{chapter.ChapterName}' in course '{chapter.Course?.CourseName}'. " +
                       $"Chapter order: {chapter.ChapterOrder}";
            }
            catch
            {
                return "";
            }
        }
        public async Task<string> GetLessonContextAsync(string lessonId)
        {
            try
            {
                var lesson = await _context.Lessons
                    .Include(l => l.Chapter)
                        .ThenInclude(c => c.Course)
                    .Include(l => l.LessonType)
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId); if (lesson == null) return "";

                return $"Current lesson: '{lesson.LessonName}' in chapter '{lesson.Chapter?.ChapterName}' " +
                       $"of course '{lesson.Chapter?.Course?.CourseName}'. " +
                       $"Lesson type: {lesson.LessonType?.LessonTypeName}. " +
                       $"Lesson order: {lesson.LessonOrder}";
            }
            catch
            {
                return "";
            }
        }
    }
}
