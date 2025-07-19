using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services
{
    public class LessonService : ILessonService
    {
        private readonly ILessonRepo _lessonRepo;
        private readonly IChapterRepo _chapterRepo;
        private readonly BrainStormEraContext _context;
        private readonly IAchievementUnlockService _achievementUnlockService;
        private readonly ILogger<LessonService> _logger;

        public LessonService(
            ILessonRepo lessonRepo,
            IChapterRepo chapterRepo,
            BrainStormEraContext context,
            IAchievementUnlockService achievementUnlockService,
            ILogger<LessonService> logger)
        {
            _lessonRepo = lessonRepo;
            _chapterRepo = chapterRepo;
            _context = context;
            _achievementUnlockService = achievementUnlockService;
            _logger = logger;
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
                    TextContent = lesson.LessonTypeId == 2 ? LessonServiceImpl.ParseLessonContentForDisplay(lesson.LessonContent, lesson.LessonTypeId ?? 0) : null,
                    DocumentDescription = lesson.LessonTypeId == 3 ? LessonServiceImpl.ParseLessonContentForDisplay(lesson.LessonContent, lesson.LessonTypeId ?? 0) : null
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
            // This method is used for updating lessons, so we need to use the LessonServiceImpl
            // to process content with proper tags
            switch (model.LessonTypeId)
            {
                case 1: // Video lesson
                    var videoContent = model.Content?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(model.VideoUrl))
                    {
                        videoContent += $"\n\n[VIDEO_URL]{model.VideoUrl}[/VIDEO_URL]";
                    }
                    return videoContent;
                case 2: // Text lesson
                    var textContent = model.Content?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(model.TextContent))
                    {
                        textContent += $"\n\n[TEXT_CONTENT]{model.TextContent.Trim()}[/TEXT_CONTENT]";
                    }
                    return textContent;
                case 3: // Document lesson
                    var documentContent = model.Content?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(model.DocumentDescription))
                    {
                        documentContent += $"\n\n[DOCUMENT_DESCRIPTION]{model.DocumentDescription.Trim()}[/DOCUMENT_DESCRIPTION]";
                    }
                    return documentContent;
                default:
                    return model.Content ?? string.Empty;
            }
        }

        private string? ExtractVideoUrl(string? lessonContent)
        {
            if (string.IsNullOrEmpty(lessonContent))
                return null;

            // Parse video content to extract URL from tags
            var parsedContent = LessonServiceImpl.ParseLessonContentForDisplay(lessonContent, 1);
            return string.IsNullOrEmpty(parsedContent) ? null : parsedContent;
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

                // Get all chapters with lessons and quizzes for sidebar
                var chapters = await _context.Chapters
                    .Where(c => c.CourseId == lesson.Chapter.CourseId)
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.LessonType)
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.UserProgresses.Where(up => up.UserId == userId))
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.UnlockAfterLesson)
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.Quizzes)
                            .ThenInclude(q => q.QuizAttempts.Where(ua => ua.UserId == userId))
                    .OrderBy(c => c.ChapterOrder)
                    .ToListAsync();

                var chaptersViewModel = chapters.Select(c => new LearnChapterViewModel
                {
                    ChapterId = c.ChapterId,
                    ChapterName = c.ChapterName,
                    ChapterDescription = c.ChapterDescription ?? "",
                    ChapterOrder = c.ChapterOrder ?? 1,
                    Lessons = new List<LearnLessonViewModel>(), // Will be populated below
                    Quizzes = c.Lessons
                        .Where(l => l.Quizzes != null && l.Quizzes.Any())
                        .SelectMany(l => l.Quizzes)
                        .Select(q => new LearnQuizViewModel
                        {
                            QuizId = q.QuizId,
                            QuizName = q.QuizName,
                            QuizDescription = q.QuizDescription ?? "",
                            LessonId = q.LessonId ?? "",
                            LessonName = q.Lesson?.LessonName ?? "",
                            TimeLimit = q.TimeLimit,
                            PassingScore = q.PassingScore,
                            MaxAttempts = q.MaxAttempts,
                            IsFinalQuiz = q.IsFinalQuiz ?? false,
                            IsPrerequisiteQuiz = q.IsPrerequisiteQuiz ?? false,
                            IsCompleted = q.QuizAttempts?.Any(ua => ua.IsPassed == true) ?? false,
                            AttemptsUsed = q.QuizAttempts?.Count ?? 0,
                            BestScore = q.QuizAttempts?.Any() == true ? q.QuizAttempts.Max(ua => ua.Score) : null
                        }).ToList()
                }).ToList();

                // Populate lessons for each chapter
                for (int i = 0; i < chapters.Count; i++)
                {
                    var chapter = chapters[i];
                    var chapterViewModel = chaptersViewModel[i];

                    var lessonViewModels = new List<LearnLessonViewModel>();
                    foreach (var l in chapter.Lessons.Where(l => l.LessonType?.LessonTypeName != "Quiz").OrderBy(l => l.LessonOrder))
                    {
                        var isLocked = await IsLessonLockedAsync(userId, l);
                        lessonViewModels.Add(new LearnLessonViewModel
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
                            ProgressPercentage = l.UserProgresses.FirstOrDefault()?.ProgressPercentage ?? 0,
                            IsLocked = isLocked,
                            PrerequisiteLessonId = l.UnlockAfterLessonId,
                            PrerequisiteLessonName = l.UnlockAfterLesson?.LessonName
                        });
                    }
                    chapterViewModel.Lessons = lessonViewModels;
                }

                // Xác định trạng thái khóa/mở cho từng chapter
                for (int i = 0; i < chaptersViewModel.Count; i++)
                {
                    if (i == 0)
                    {
                        chaptersViewModel[i].IsLocked = false;
                    }
                    else
                    {
                        // Chapter trước đó đã hoàn thành hết lesson chưa?
                        var prevChapter = chaptersViewModel[i - 1];
                        chaptersViewModel[i].IsLocked = !prevChapter.Lessons.All(l => l.IsCompleted);
                    }
                }

                var viewModel = new LessonLearningViewModel
                {
                    LessonId = lesson.LessonId,
                    LessonName = lesson.LessonName,
                    LessonDescription = lesson.LessonDescription ?? "",
                    LessonContent = LessonServiceImpl.ParseLessonContentForDisplay(lesson.LessonContent, lesson.LessonTypeId ?? 0),
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
            catch (Exception)
            {
                return new LessonLearningResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the lesson"
                };
            }
        }

        public async Task<bool> MarkLessonAsCompletedAsync(string userId, string lessonId)
        {
            try
            {
                return await _lessonRepo.MarkLessonAsCompletedAsync(userId, lessonId);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> IsLessonCompletedAsync(string userId, string lessonId)
        {
            try
            {
                return await _lessonRepo.IsLessonCompletedAsync(userId, lessonId);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Lesson?> GetLessonWithDetailsAsync(string lessonId)
        {
            try
            {
                return await _lessonRepo.GetLessonWithDetailsAsync(lessonId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<decimal> GetLessonCompletionPercentageAsync(string userId, string courseId)
        {
            try
            {
                return await _lessonRepo.GetLessonCompletionPercentageAsync(userId, courseId);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<bool> UpdateEnrollmentProgressAsync(string userId, string courseId, decimal progressPercentage, string? currentLessonId = null)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (enrollment == null)
                {
                    // Log not found
                    return false;
                }

                var oldProgress = enrollment.ProgressPercentage;
                enrollment.ProgressPercentage = progressPercentage;
                if (!string.IsNullOrEmpty(currentLessonId))
                {
                    enrollment.CurrentLessonId = currentLessonId;
                }
                enrollment.EnrollmentUpdatedAt = DateTime.UtcNow;

                // Check if course is completed (progress = 100%) and certificate should be issued
                if (progressPercentage >= 100 && oldProgress < 100)
                {
                    // Update enrollment status to completed
                    enrollment.EnrollmentStatus = 3; // Completed status

                    // Issue certificate if not already issued
                    if (!enrollment.CertificateIssuedDate.HasValue)
                    {
                        enrollment.CertificateIssuedDate = DateOnly.FromDateTime(DateTime.UtcNow);

                        // Create certificate record
                        var certificate = new Certificate
                        {
                            CertificateId = Guid.NewGuid().ToString(),
                            EnrollmentId = enrollment.EnrollmentId,
                            UserId = userId,
                            CourseId = courseId,
                            CertificateCode = GenerateCertificateCode(),
                            CertificateName = "Certificate of Completion",
                            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                            IsValid = true,
                            FinalScore = progressPercentage,
                            CertificateCreatedAt = DateTime.UtcNow
                        };

                        _context.Certificates.Add(certificate);

                        // Check and unlock course completion achievements
                        try
                        {
                            var unlockedAchievements = await _achievementUnlockService.CheckCourseCompletionAchievementsAsync(userId, courseId);
                            if (unlockedAchievements.Any())
                            {
                                _logger.LogInformation("Unlocked {AchievementCount} achievements for user {UserId} completing course {CourseId}",
                                    unlockedAchievements.Count, userId, courseId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error checking achievements for course completion: user {UserId}, course {CourseId}", userId, courseId);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Log successful update
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating enrollment progress for user {UserId}, course {CourseId}", userId, courseId);
                return false;
            }
        }

        private string GenerateCertificateCode()
        {
            return Guid.NewGuid().ToString("N")[..8].ToUpper();
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

        private async Task<bool> IsLessonLockedAsync(string userId, Lesson lesson)
        {
            try
            {
                // If lesson is not locked by default, it's always accessible
                if (lesson.IsLocked != true)
                {
                    return false;
                }

                // If lesson requires a specific lesson to be completed first
                if (!string.IsNullOrEmpty(lesson.UnlockAfterLessonId))
                {
                    var isPrerequisiteCompleted = await IsLessonCompletedAsync(userId, lesson.UnlockAfterLessonId);
                    return !isPrerequisiteCompleted;
                }

                // If lesson is locked but no specific prerequisite, check if previous lesson in same chapter is completed
                var previousLesson = await _context.Lessons
                    .Where(l => l.ChapterId == lesson.ChapterId &&
                               l.LessonOrder < lesson.LessonOrder &&
                               l.LessonType != null && l.LessonType.LessonTypeName != "Quiz")
                    .OrderByDescending(l => l.LessonOrder)
                    .FirstOrDefaultAsync();

                if (previousLesson != null)
                {
                    var isPreviousCompleted = await IsLessonCompletedAsync(userId, previousLesson.LessonId);
                    return !isPreviousCompleted;
                }

                // If no previous lesson, unlock it
                return false;
            }
            catch (Exception)
            {
                // If there's an error, assume lesson is locked for safety
                return true;
            }
        }
    }
}







