using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface ILessonRepo : IBaseRepo<Lesson>
    {
        // Lesson query methods
        Task<Lesson?> GetLessonWithDetailsAsync(string lessonId);
        Task<List<Lesson>> GetLessonsByChapterAsync(string chapterId);
        Task<List<Lesson>> GetLessonsByCourseAsync(string courseId);
        Task<Lesson?> GetNextLessonAsync(string currentLessonId);
        Task<Lesson?> GetPreviousLessonAsync(string currentLessonId);

        // Lesson management methods
        Task<string> CreateLessonAsync(Lesson lesson);
        Task<bool> UpdateLessonAsync(Lesson lesson);
        Task<bool> DeleteLessonAsync(string lessonId, string authorId);
        Task<bool> UpdateLessonOrderAsync(string lessonId, int newOrder);
        Task<bool> ReorderLessonsAsync(string chapterId, List<(string lessonId, int order)> lessonOrders);

        // Lesson content methods
        Task<bool> UpdateLessonContentAsync(string lessonId, string content);
        Task<bool> UpdateLessonVideoAsync(string lessonId, string videoUrl);
        Task<bool> UpdateLessonDocumentAsync(string lessonId, string documentPath);

        // Lesson progress methods
        Task<bool> MarkLessonAsCompletedAsync(string userId, string lessonId);
        Task<bool> IsLessonCompletedAsync(string userId, string lessonId);
        Task<List<Lesson>> GetCompletedLessonsAsync(string userId, string courseId);
        Task<decimal> GetLessonCompletionPercentageAsync(string userId, string courseId);

        // Lesson prerequisites
        Task<bool> HasPrerequisitesCompletedAsync(string userId, string lessonId);
        Task<List<Lesson>> GetPrerequisiteLessonsAsync(string lessonId);
        Task<bool> AddLessonPrerequisiteAsync(string lessonId, string prerequisiteLessonId);
        Task<bool> RemoveLessonPrerequisiteAsync(string lessonId, string prerequisiteLessonId);

        // Lesson statistics
        Task<int> GetTotalLessonsInCourseAsync(string courseId);
        Task<int> GetCompletedLessonsCountAsync(string userId, string courseId);
        Task<TimeSpan> GetEstimatedTimeForCourseAsync(string courseId);
    }
}
