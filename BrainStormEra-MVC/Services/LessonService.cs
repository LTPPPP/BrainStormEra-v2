using DataAccessLayer.Data;
using DataAccessLayer.Models;
using BrainStormEra_MVC.Models.ViewModels;
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

        public async Task<CreateLessonViewModel?> GetLessonForEditAsync(string lessonId, string authorId)
        {
            try
            {
                Console.WriteLine($"=== GetLessonForEditAsync Called ===");
                Console.WriteLine($"LessonId: {lessonId}");
                Console.WriteLine($"AuthorId: {authorId}");

                var lesson = await _context.Lessons
                    .Include(l => l.Chapter)
                    .ThenInclude(c => c.Course)
                    .Where(l => l.LessonId == lessonId && l.Chapter.Course.AuthorId == authorId)
                    .FirstOrDefaultAsync();

                Console.WriteLine($"Lesson found: {lesson != null}");
                if (lesson != null)
                {
                    Console.WriteLine($"Lesson Name: {lesson.LessonName}");
                    Console.WriteLine($"Course AuthorId: {lesson.Chapter.Course.AuthorId}");
                    Console.WriteLine($"Requested AuthorId: {authorId}");
                }

                if (lesson == null)
                    return null;

                // Get lesson types
                var lessonTypes = await GetLessonTypesAsync();

                // Get existing lessons in the same chapter (excluding current lesson)
                var existingLessons = await _context.Lessons
                    .Where(l => l.ChapterId == lesson.ChapterId && l.LessonId != lessonId)
                    .OrderBy(l => l.LessonOrder)
                    .ToListAsync();

                var viewModel = new CreateLessonViewModel
                {
                    ChapterId = lesson.ChapterId,
                    CourseId = lesson.Chapter.CourseId,
                    LessonName = lesson.LessonName,
                    Description = lesson.LessonDescription,
                    Content = lesson.LessonContent ?? string.Empty,
                    LessonTypeId = lesson.LessonTypeId ?? 1,
                    Order = lesson.LessonOrder,
                    IsLocked = lesson.IsLocked ?? false,
                    UnlockAfterLessonId = lesson.UnlockAfterLessonId,
                    IsMandatory = lesson.IsMandatory ?? true,
                    RequiresQuizPass = lesson.RequiresQuizPass ?? false,
                    MinQuizScore = lesson.MinQuizScore,
                    MinCompletionPercentage = lesson.MinCompletionPercentage,
                    MinTimeSpent = lesson.MinTimeSpent,
                    CourseName = lesson.Chapter.Course.CourseName,
                    ChapterName = lesson.Chapter.ChapterName,
                    ChapterOrder = lesson.Chapter.ChapterOrder ?? 1,
                    LessonTypes = lessonTypes,
                    ExistingLessons = existingLessons,

                    // Parse lesson content based on lesson type
                    VideoUrl = lesson.LessonTypeId == 1 ? ExtractVideoUrl(lesson.LessonContent) : null,
                    TextContent = lesson.LessonTypeId == 2 ? lesson.LessonContent : null,
                    DocumentDescription = lesson.LessonTypeId == 3 ? lesson.LessonContent : null
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLessonForEditAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateLessonAsync(string lessonId, CreateLessonViewModel model, string authorId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Get the existing lesson with authorization check
                    var existingLesson = await _context.Lessons
                        .Include(l => l.Chapter)
                        .ThenInclude(c => c.Course)
                        .Where(l => l.LessonId == lessonId && l.Chapter.Course.AuthorId == authorId)
                        .FirstOrDefaultAsync();

                    if (existingLesson == null)
                        return false;

                    var oldOrder = existingLesson.LessonOrder;
                    var newOrder = model.Order;

                    // Handle lesson order changes
                    if (oldOrder != newOrder)
                    {
                        await HandleLessonOrderChangeAsync(existingLesson.ChapterId, lessonId, oldOrder, newOrder);
                    }

                    // Update lesson properties
                    existingLesson.LessonName = model.LessonName;
                    existingLesson.LessonDescription = model.Description;
                    existingLesson.LessonTypeId = model.LessonTypeId;
                    existingLesson.LessonOrder = model.Order;
                    existingLesson.IsLocked = model.IsLocked;
                    existingLesson.UnlockAfterLessonId = string.IsNullOrEmpty(model.UnlockAfterLessonId) ? null : model.UnlockAfterLessonId;
                    existingLesson.IsMandatory = model.IsMandatory;
                    existingLesson.RequiresQuizPass = model.RequiresQuizPass;
                    existingLesson.MinQuizScore = model.MinQuizScore;
                    existingLesson.MinCompletionPercentage = model.MinCompletionPercentage;
                    existingLesson.MinTimeSpent = model.MinTimeSpent;
                    existingLesson.LessonUpdatedAt = DateTime.Now;

                    // Update lesson content based on lesson type
                    existingLesson.LessonContent = ProcessLessonContent(model);

                    _context.Lessons.Update(existingLesson);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in UpdateLessonAsync: {ex.Message}");
                    await transaction.RollbackAsync();
                    return false;
                }
            });
        }

        public async Task<bool> IsDuplicateLessonNameForEditAsync(string lessonName, string chapterId, string currentLessonId)
        {
            return await _context.Lessons
                .AnyAsync(l => l.LessonName.ToLower().Trim() == lessonName.ToLower().Trim()
                              && l.ChapterId == chapterId
                              && l.LessonId != currentLessonId);
        }

        private async Task HandleLessonOrderChangeAsync(string chapterId, string lessonId, int oldOrder, int newOrder)
        {
            if (oldOrder < newOrder)
            {
                // Moving lesson down: decrease order of lessons between old and new position
                var lessonsToUpdate = await _context.Lessons
                    .Where(l => l.ChapterId == chapterId
                               && l.LessonId != lessonId
                               && l.LessonOrder > oldOrder
                               && l.LessonOrder <= newOrder)
                    .ToListAsync();

                foreach (var lesson in lessonsToUpdate)
                {
                    lesson.LessonOrder--;
                    lesson.LessonUpdatedAt = DateTime.Now;
                }
            }
            else if (oldOrder > newOrder)
            {
                // Moving lesson up: increase order of lessons between new and old position
                var lessonsToUpdate = await _context.Lessons
                    .Where(l => l.ChapterId == chapterId
                               && l.LessonId != lessonId
                               && l.LessonOrder >= newOrder
                               && l.LessonOrder < oldOrder)
                    .ToListAsync();

                foreach (var lesson in lessonsToUpdate)
                {
                    lesson.LessonOrder++;
                    lesson.LessonUpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
        }

        private string ProcessLessonContent(CreateLessonViewModel model)
        {
            return model.LessonTypeId switch
            {
                1 => model.VideoUrl ?? string.Empty, // Video lesson
                2 => model.TextContent ?? string.Empty, // Text lesson
                3 => model.DocumentDescription ?? string.Empty, // Document lesson
                _ => model.Content
            };
        }

        private string? ExtractVideoUrl(string? lessonContent)
        {
            // For video lessons, the content is typically just the URL
            // You might need to adjust this based on how video content is stored
            return lessonContent;
        }

        /// <summary>
        /// Delete lesson - performs smart delete (soft/hard based on user progress)
        /// </summary>
        /// <param name="lessonId">Lesson ID to delete</param>
        /// <param name="authorId">Author ID for authorization</param>
        /// <returns>Success result</returns>
        public async Task<bool> DeleteLessonAsync(string lessonId, string authorId)
        {
            try
            {
                var lesson = await _context.Lessons
                    .Include(l => l.Chapter)
                        .ThenInclude(c => c.Course)
                    .Include(l => l.UserProgresses)
                    .Include(l => l.Quizzes)
                        .ThenInclude(q => q.QuizAttempts)
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId && l.Chapter.Course.AuthorId == authorId);

                if (lesson == null)
                {
                    return false;
                }

                // Check if lesson has user progress or quiz attempts
                var hasUserProgress = lesson.UserProgresses.Any();
                var hasQuizAttempts = lesson.Quizzes.Any(q => q.QuizAttempts.Any());
                var isDependency = await _context.Lessons.AnyAsync(l => l.UnlockAfterLessonId == lessonId);

                if (hasUserProgress || hasQuizAttempts || isDependency)
                {
                    // Soft delete - set status to Archived
                    lesson.LessonStatus = 4; // Archived
                    lesson.LessonUpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Log the operation
                    Console.WriteLine($"Lesson {lessonId} soft deleted (archived) due to user progress/dependencies");
                }
                else
                {
                    // Hard delete - remove completely as there's no user data to preserve
                    _context.Lessons.Remove(lesson);
                    await _context.SaveChangesAsync();

                    // Log the operation
                    Console.WriteLine($"Lesson {lessonId} hard deleted - no user progress or dependencies");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting lesson {lessonId}: {ex.Message}");
                return false;
            }
        }
    }
}
