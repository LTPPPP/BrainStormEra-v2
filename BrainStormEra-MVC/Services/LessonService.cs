using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Services
{
    public class LessonService : ILessonService
    {
        private readonly BrainStormEraContext _context;

        public LessonService(BrainStormEraContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateLessonAsync(Lesson lesson)
        {
            try
            {
                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<LessonType>> GetLessonTypesAsync()
        {
            return await _context.LessonTypes.ToListAsync();
        }
        public async Task<int> GetNextLessonOrderAsync(string chapterId)
        {
            var maxOrder = await _context.Lessons
                .Where(l => l.ChapterId == chapterId)
                .MaxAsync(l => (int?)l.LessonOrder) ?? 0;
            return maxOrder + 1;
        }
        public async Task<bool> IsDuplicateLessonNameAsync(string lessonName, string chapterId)
        {
            return await _context.Lessons
                .AnyAsync(l => l.LessonName.ToLower() == lessonName.ToLower() && l.ChapterId == chapterId);
        }
        public async Task<Chapter?> GetChapterByIdAsync(string chapterId)
        {
            return await _context.Chapters
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.ChapterId == chapterId);
        }
    }
}
