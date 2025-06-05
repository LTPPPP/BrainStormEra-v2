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
            }

            // Add page type context
            if (path.Contains("/Course"))
                context.Add("Người dùng đang xem trang khóa học");
            else if (path.Contains("/Chapter"))
                context.Add("Người dùng đang xem trang chương học");
            else if (path.Contains("/Lesson"))
                context.Add("Người dùng đang xem trang bài học");
            else if (path.Contains("/Home"))
                context.Add("Người dùng đang ở trang chủ");

            return string.Join(". ", context);
        }
        public async Task<string> GetCourseContextAsync(string courseId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.CourseCategories)
                    .Include(c => c.Author)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);

                if (course == null) return "";

                var categories = course.CourseCategories?.Select(cc => cc.CourseCategoryName).ToList() ?? new List<string>();
                var categoryText = categories.Any() ? string.Join(", ", categories) : "Chưa phân loại";

                return $"Khóa học hiện tại: '{course.CourseName}' thuộc danh mục {categoryText}, " +
                       $"được giảng dạy bởi {course.Author?.FullName}. " +
                       $"Mô tả: {course.CourseDescription?.Substring(0, Math.Min(100, course.CourseDescription?.Length ?? 0))}";
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
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId);

                if (chapter == null) return "";

                return $"Chương hiện tại: '{chapter.ChapterName}' trong khóa học '{chapter.Course?.CourseName}'. " +
                       $"Thứ tự chương: {chapter.ChapterOrder}";
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
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId);

                if (lesson == null) return "";

                return $"Bài học hiện tại: '{lesson.LessonName}' thuộc chương '{lesson.Chapter?.ChapterName}' " +
                       $"của khóa học '{lesson.Chapter?.Course?.CourseName}'. " +
                       $"Loại bài học: {lesson.LessonType?.LessonTypeName}. " +
                       $"Thứ tự bài: {lesson.LessonOrder}";
            }
            catch
            {
                return "";
            }
        }
    }
}
