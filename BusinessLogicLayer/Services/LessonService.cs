using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogicLayer.Services
{
    public class LessonService : ILessonService
    {
        private readonly ILessonRepo _lessonRepo;
        private readonly IChapterRepo _chapterRepo;
        private readonly BrainStormEraContext _context;

        public LessonService(
            ILessonRepo lessonRepo,
            IChapterRepo chapterRepo,
            BrainStormEraContext context)
        {
            _lessonRepo = lessonRepo;
            _chapterRepo = chapterRepo;
            _context = context;
        }
        public async Task<bool> CreateLessonAsync(Lesson lesson)
        {
            if (lesson == null)
            {
                return false;
            }

            // Use execution strategy to handle transactions properly with SqlServerRetryingExecutionStrategy
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Check if lesson order is taken and adjust if necessary
                    var orderTaken = await IsLessonOrderTakenAsync(lesson.ChapterId, lesson.LessonOrder);

                    if (orderTaken)
                    {
                        await UpdateLessonOrdersAsync(lesson.ChapterId, lesson.LessonOrder);
                    }

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

                    var lessonId = await _lessonRepo.CreateLessonAsync(lesson);

                    await transaction.CommitAsync();

                    return true;
                }
                catch (Exception)
                {
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
                var lesson = await _context.Lessons
                    .Include(l => l.Chapter)
                    .ThenInclude(c => c.Course)
                    .Where(l => l.LessonId == lessonId && l.Chapter.Course.AuthorId == authorId)
                    .FirstOrDefaultAsync();

                if (lesson != null)
                {
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
            catch (Exception)
            {
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
                catch (Exception)
                {

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
                    .Where(l => l.LessonId == lessonId && l.Chapter.Course.AuthorId == authorId)
                    .FirstOrDefaultAsync();

                if (lesson == null)
                    return false;

                // Use execution strategy for transaction
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Get chapter and order of lesson to be deleted
                        var chapterId = lesson.ChapterId;
                        var deletedOrder = lesson.LessonOrder;

                        // Remove the lesson
                        _context.Lessons.Remove(lesson);

                        // Update order of subsequent lessons
                        var subsequentLessons = await _context.Lessons
                            .Where(l => l.ChapterId == chapterId && l.LessonOrder > deletedOrder)
                            .ToListAsync();

                        foreach (var subsequentLesson in subsequentLessons)
                        {
                            subsequentLesson.LessonOrder--;
                            subsequentLesson.LessonUpdatedAt = DateTime.Now;
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return true;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }
                });
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<LessonLearningResult> GetLessonLearningDataAsync(string lessonId, string userId)
        {
            try
            {
                // Get lesson with related data
                var lesson = await _context.Lessons
                    .Include(l => l.Chapter)
                        .ThenInclude(c => c.Course)
                            .ThenInclude(course => course.Enrollments.Where(e => e.UserId == userId))
                    .Include(l => l.LessonType)
                    .Include(l => l.Quizzes)
                    .Include(l => l.UserProgresses.Where(up => up.UserId == userId))
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId);

                if (lesson == null)
                {
                    return new LessonLearningResult
                    {
                        Success = false,
                        IsNotFound = true,
                        ErrorMessage = "Lesson not found"
                    };
                }

                // Check if user is enrolled in the course
                var enrollment = lesson.Chapter.Course.Enrollments.FirstOrDefault();
                if (enrollment == null)
                {
                    return new LessonLearningResult
                    {
                        Success = false,
                        IsUnauthorized = true,
                        ErrorMessage = "You are not enrolled in this course"
                    };
                }

                // Get navigation lessons (previous and next)
                var allLessonsInChapter = await _context.Lessons
                    .Where(l => l.ChapterId == lesson.ChapterId)
                    .OrderBy(l => l.LessonOrder)
                    .Select(l => new { l.LessonId, l.LessonName, l.LessonOrder })
                    .ToListAsync();

                var currentLessonIndex = allLessonsInChapter.FindIndex(l => l.LessonId == lessonId);
                var previousLesson = currentLessonIndex > 0 ? allLessonsInChapter[currentLessonIndex - 1] : null;
                var nextLesson = currentLessonIndex < allLessonsInChapter.Count - 1 ? allLessonsInChapter[currentLessonIndex + 1] : null;

                // Get user progress
                var userProgress = lesson.UserProgresses.FirstOrDefault();

                // Get lesson type icon
                string lessonTypeIcon = GetLessonTypeIcon(lesson.LessonType?.LessonTypeName ?? "");

                // Check if lesson has quiz
                var hasQuiz = lesson.Quizzes.Any();
                var quiz = lesson.Quizzes.FirstOrDefault();

                // Get all chapters with lessons for sidebar
                var chapters = await _context.Chapters
                    .Where(c => c.CourseId == lesson.Chapter.CourseId)
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.LessonType)
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.UserProgresses.Where(up => up.UserId == userId))
                    .OrderBy(c => c.ChapterOrder)
                    .ToListAsync();

                var chaptersViewModel = chapters.Select(c => new LearnChapterViewModel
                {
                    ChapterId = c.ChapterId,
                    ChapterName = c.ChapterName,
                    ChapterDescription = c.ChapterDescription ?? "",
                    ChapterOrder = c.ChapterOrder ?? 1,
                    Lessons = c.Lessons.OrderBy(l => l.LessonOrder).Select(l => new LearnLessonViewModel
                    {
                        LessonId = l.LessonId,
                        LessonName = l.LessonName,
                        LessonDescription = l.LessonDescription ?? "",
                        LessonOrder = l.LessonOrder,
                        LessonType = l.LessonType?.LessonTypeName ?? "",
                        LessonTypeIcon = GetLessonTypeIcon(l.LessonType?.LessonTypeName ?? ""),
                        EstimatedDuration = l.MinTimeSpent ?? 0,
                        IsCompleted = l.UserProgresses.Any(up => up.IsCompleted == true),
                        IsMandatory = l.IsMandatory ?? false,
                        ProgressPercentage = l.UserProgresses.FirstOrDefault()?.ProgressPercentage ?? 0
                    }).ToList()
                }).ToList();

                var viewModel = new LessonLearningViewModel
                {
                    LessonId = lesson.LessonId,
                    LessonName = lesson.LessonName,
                    LessonDescription = lesson.LessonDescription ?? "",
                    LessonContent = lesson.LessonContent,
                    LessonType = lesson.LessonType?.LessonTypeName ?? "",
                    LessonTypeId = lesson.LessonTypeId ?? 0,
                    LessonTypeIcon = lessonTypeIcon,
                    EstimatedDuration = lesson.MinTimeSpent ?? 0,

                    CourseId = lesson.Chapter.CourseId,
                    CourseName = lesson.Chapter.Course.CourseName,
                    CourseDescription = lesson.Chapter.Course.CourseDescription ?? "",
                    ChapterId = lesson.ChapterId,
                    ChapterName = lesson.Chapter.ChapterName,
                    ChapterNumber = lesson.Chapter.ChapterOrder ?? 1,

                    PreviousLessonId = previousLesson?.LessonId,
                    NextLessonId = nextLesson?.LessonId,
                    PreviousLessonName = previousLesson?.LessonName,
                    NextLessonName = nextLesson?.LessonName,

                    CurrentProgress = userProgress?.ProgressPercentage,
                    IsCompleted = userProgress?.IsCompleted ?? false,
                    IsMandatory = lesson.IsMandatory ?? false,

                    HasQuiz = hasQuiz,
                    QuizId = quiz?.QuizId,
                    MinQuizScore = lesson.MinQuizScore,
                    RequiresQuizPass = lesson.RequiresQuizPass,

                    // Sidebar data
                    Chapters = chaptersViewModel
                };

                return new LessonLearningResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                return new LessonLearningResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the lesson"
                };
            }
        }

        private string GetLessonTypeIcon(string lessonTypeName)
        {
            return lessonTypeName.ToLower() switch
            {
                "video" => "fas fa-play-circle",
                "text lesson" => "fas fa-file-text",
                "interactive lesson" => "fas fa-mouse-pointer",
                "quiz" => "fas fa-question-circle",
                "document" => "fas fa-file-pdf",
                _ => "fas fa-file-alt"
            };
        }
    }
}







