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
            Console.WriteLine("=== LESSON SERVICE: CreateLessonAsync CALLED ===");
            Console.WriteLine($"Lesson ID: {lesson?.LessonId}");
            Console.WriteLine($"Lesson Name: {lesson?.LessonName}");
            Console.WriteLine($"Chapter ID: {lesson?.ChapterId}");
            Console.WriteLine($"Lesson Type ID: {lesson?.LessonTypeId}");
            Console.WriteLine($"Lesson Order: {lesson?.LessonOrder}");

            if (lesson == null)
            {
                Console.WriteLine("=== LESSON IS NULL ===");
                return false;
            }

            // Use execution strategy to handle transactions properly with SqlServerRetryingExecutionStrategy
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    Console.WriteLine("=== CHECKING LESSON ORDER ===");
                    // Check if lesson order is taken and adjust if necessary
                    var orderTaken = await IsLessonOrderTakenAsync(lesson.ChapterId, lesson.LessonOrder);
                    Console.WriteLine($"Is lesson order {lesson.LessonOrder} taken: {orderTaken}");

                    if (orderTaken)
                    {
                        Console.WriteLine("=== UPDATING LESSON ORDERS ===");
                        await UpdateLessonOrdersAsync(lesson.ChapterId, lesson.LessonOrder);
                    }

                    Console.WriteLine("=== SETTING DEFAULT VALUES ===");
                    // Set default values
                    lesson.LessonStatus = lesson.LessonStatus ?? 1; // Active status
                    lesson.IsLocked = lesson.IsLocked ?? false;
                    lesson.IsMandatory = lesson.IsMandatory ?? true;
                    lesson.RequiresQuizPass = lesson.RequiresQuizPass ?? false;
                    lesson.MinCompletionPercentage = lesson.MinCompletionPercentage ?? 100.00m;
                    lesson.MinQuizScore = lesson.MinQuizScore ?? 70.00m;
                    lesson.MinTimeSpent = lesson.MinTimeSpent ?? 0;
                    lesson.LessonCreatedAt = DateTime.Now;
                    lesson.LessonUpdatedAt = DateTime.Now;

                    Console.WriteLine("=== ADDING LESSON TO CONTEXT ===");
                    _context.Lessons.Add(lesson);

                    Console.WriteLine("=== SAVING CHANGES ===");
                    await _context.SaveChangesAsync();

                    Console.WriteLine("=== COMMITTING TRANSACTION ===");
                    await transaction.CommitAsync();

                    Console.WriteLine("=== LESSON CREATED SUCCESSFULLY ===");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== LESSON SERVICE EXCEPTION ===");
                    Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                    Console.WriteLine($"Exception Message: {ex.Message}");
                    Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                    await transaction.RollbackAsync();
                    // Log exception here if you have logging
                    return false;
                }
            });
        }

        public async Task<IEnumerable<LessonType>> GetLessonTypesAsync()
        {
            return await _context.LessonTypes.OrderBy(lt => lt.LessonTypeName).ToListAsync();
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
                .AnyAsync(l => l.LessonName.ToLower().Trim() == lessonName.ToLower().Trim()
                              && l.ChapterId == chapterId);
        }

        public async Task<Chapter?> GetChapterByIdAsync(string chapterId)
        {
            return await _context.Chapters
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.ChapterId == chapterId);
        }

        public async Task<bool> IsLessonOrderTakenAsync(string chapterId, int order)
        {
            return await _context.Lessons
                .AnyAsync(l => l.ChapterId == chapterId && l.LessonOrder == order);
        }

        public async Task<bool> UpdateLessonOrdersAsync(string chapterId, int insertOrder)
        {
            try
            {
                var lessonsToUpdate = await _context.Lessons
                    .Where(l => l.ChapterId == chapterId && l.LessonOrder >= insertOrder)
                    .ToListAsync();

                foreach (var lesson in lessonsToUpdate)
                {
                    lesson.LessonOrder++;
                    lesson.LessonUpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<Lesson>> GetLessonsInChapterAsync(string chapterId)
        {
            return await _context.Lessons
                .Where(l => l.ChapterId == chapterId)
                .OrderBy(l => l.LessonOrder)
                .ToListAsync();
        }

        public async Task<bool> ValidateUnlockAfterLessonAsync(string chapterId, string? unlockAfterLessonId)
        {
            if (string.IsNullOrEmpty(unlockAfterLessonId))
                return true;

            return await _context.Lessons
                .AnyAsync(l => l.LessonId == unlockAfterLessonId && l.ChapterId == chapterId);
        }
    }
}
