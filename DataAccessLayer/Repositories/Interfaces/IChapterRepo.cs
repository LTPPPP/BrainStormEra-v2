using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IChapterRepo : IBaseRepo<Chapter>
    {        // Chapter query methods
        Task<Chapter?> GetChapterWithLessonsAsync(string chapterId);
        Task<Chapter?> GetChapterWithCourseAsync(string chapterId, string authorId);
        Task<List<Chapter>> GetChaptersByCourseAsync(string courseId);
        Task<Chapter?> GetChapterWithQuizzesAsync(string chapterId);
        Task<Chapter?> GetNextChapterAsync(string currentChapterId);
        Task<Chapter?> GetPreviousChapterAsync(string currentChapterId);

        // Chapter management methods
        Task<string> CreateChapterAsync(Chapter chapter);
        Task<bool> UpdateChapterAsync(Chapter chapter);
        Task<bool> DeleteChapterAsync(string chapterId, string authorId);
        Task<bool> UpdateChapterOrderAsync(string chapterId, int newOrder);
        Task<bool> ReorderChaptersAsync(string courseId, List<(string chapterId, int order)> chapterOrders);

        // Chapter content methods
        Task<bool> UpdateChapterDescriptionAsync(string chapterId, string description);
        Task<bool> UpdateChapterNameAsync(string chapterId, string name);

        // Chapter progress methods
        Task<bool> IsChapterCompletedAsync(string userId, string chapterId);
        Task<List<Chapter>> GetCompletedChaptersAsync(string userId, string courseId);
        Task<decimal> GetChapterCompletionPercentageAsync(string userId, string chapterId);

        // Chapter statistics
        Task<int> GetTotalChaptersInCourseAsync(string courseId);
        Task<int> GetCompletedChaptersCountAsync(string userId, string courseId);
        Task<int> GetLessonsCountInChapterAsync(string chapterId);
        Task<TimeSpan> GetEstimatedTimeForChapterAsync(string chapterId);
    }
}
