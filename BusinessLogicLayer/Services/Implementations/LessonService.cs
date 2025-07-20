using Microsoft.Extensions.Logging;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace BusinessLogicLayer.Services.Implementations
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

        // Result classes for structured returns
        public class SelectLessonTypeResult
        {
            public bool Success { get; set; }
            public SelectLessonTypeViewModel? ViewModel { get; set; }
            public string? ErrorMessage { get; set; }
            public string? RedirectAction { get; set; }
            public string? RedirectController { get; set; }
            public object? RedirectValues { get; set; }
        }

        public class CreateLessonViewResult
        {
            public bool Success { get; set; }
            public CreateLessonViewModel? ViewModel { get; set; }
            public string? ErrorMessage { get; set; }
        }

        public class CreateLessonResult
        {
            public bool Success { get; set; }
            public string? SuccessMessage { get; set; }
            public string? ErrorMessage { get; set; }
            public string? RedirectAction { get; set; }
            public string? RedirectController { get; set; }
            public object? RedirectValues { get; set; }
            public Dictionary<string, string>? ValidationErrors { get; set; }
            public CreateLessonViewModel? ViewModel { get; set; }
        }

        public class EditLessonResult
        {
            public bool Success { get; set; }
            public CreateLessonViewModel? ViewModel { get; set; }
            public string? ErrorMessage { get; set; }
        }

        public class UpdateLessonResult
        {
            public bool Success { get; set; }
            public string? SuccessMessage { get; set; }
            public string? ErrorMessage { get; set; }
            public string? RedirectAction { get; set; }
            public string? RedirectController { get; set; }
            public object? RedirectValues { get; set; }
            public Dictionary<string, string>? ValidationErrors { get; set; }
            public CreateLessonViewModel? ViewModel { get; set; }
        }

        public class DeleteLessonResult
        {
            public bool Success { get; set; }
            public string? SuccessMessage { get; set; }
            public string? ErrorMessage { get; set; }
            public string? RedirectAction { get; set; }
            public string? RedirectController { get; set; }
            public object? RedirectValues { get; set; }
        }

        public class MarkLessonCompleteResult
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
        }

        // ILessonService Implementation Methods
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
                    TextContent = lesson.LessonTypeId == 2 ? ParseLessonContentForDisplay(lesson.LessonContent ?? "", lesson.LessonTypeId ?? 0) : null,
                    DocumentDescription = lesson.LessonTypeId == 3 ? ParseLessonContentForDisplay(lesson.LessonContent ?? "", lesson.LessonTypeId ?? 0) : null
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
                    LessonContent = ParseLessonContentForDisplay(lesson.LessonContent, lesson.LessonTypeId ?? 0),
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

        // Business logic methods
        public async Task<SelectLessonTypeResult> GetSelectLessonTypeViewModelAsync(string chapterId)
        {
            try
            {
                if (string.IsNullOrEmpty(chapterId))
                {
                    return new SelectLessonTypeResult
                    {
                        Success = false,
                        ErrorMessage = "Chapter ID is required"
                    };
                }

                var chapter = await GetChapterByIdAsync(chapterId);
                if (chapter == null)
                {
                    return new SelectLessonTypeResult
                    {
                        Success = false,
                        ErrorMessage = "Chapter not found"
                    };
                }

                var viewModel = new SelectLessonTypeViewModel
                {
                    ChapterId = chapterId,
                    CourseId = chapter.CourseId,
                    ChapterName = chapter.ChapterName,
                    CourseName = chapter.Course.CourseName,
                    ChapterOrder = chapter.ChapterOrder ?? 1,
                    LessonTypes = await GetLessonTypesAsync()
                };

                return new SelectLessonTypeResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting select lesson type view model for chapter {ChapterId}", chapterId);
                return new SelectLessonTypeResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the lesson type selection page."
                };
            }
        }

        public async Task<SelectLessonTypeResult> ProcessSelectLessonTypeAsync(SelectLessonTypeViewModel model, ModelStateDictionary modelState)
        {
            try
            {
                if (!modelState.IsValid)
                {
                    // Reload lesson types for the view
                    model.LessonTypes = await GetLessonTypesAsync();
                    return new SelectLessonTypeResult
                    {
                        Success = false,
                        ViewModel = model
                    };
                }

                // Redirect to appropriate create lesson page based on lesson type
                switch (model.SelectedLessonTypeId)
                {
                    case 1: // Video
                        return new SelectLessonTypeResult
                        {
                            Success = true,
                            RedirectAction = "CreateVideoLesson",
                            RedirectController = "Lesson",
                            RedirectValues = new { chapterId = model.ChapterId }
                        };
                    case 2: // Text
                        return new SelectLessonTypeResult
                        {
                            Success = true,
                            RedirectAction = "CreateTextLesson",
                            RedirectController = "Lesson",
                            RedirectValues = new { chapterId = model.ChapterId }
                        };
                    case 3: // Interactive/Document
                        return new SelectLessonTypeResult
                        {
                            Success = true,
                            RedirectAction = "CreateInteractiveLesson",
                            RedirectController = "Lesson",
                            RedirectValues = new { chapterId = model.ChapterId }
                        };
                    default:
                        model.LessonTypes = await GetLessonTypesAsync();
                        return new SelectLessonTypeResult
                        {
                            Success = false,
                            ViewModel = model,
                            ErrorMessage = "Invalid lesson type selected."
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing select lesson type for chapter {ChapterId}", model?.ChapterId);
                return new SelectLessonTypeResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while processing the lesson type selection."
                };
            }
        }

        public async Task<CreateLessonViewResult> GetCreateLessonViewModelAsync(string chapterId, int lessonTypeId)
        {
            try
            {
                if (string.IsNullOrEmpty(chapterId))
                {
                    return new CreateLessonViewResult
                    {
                        Success = false,
                        ErrorMessage = "Chapter ID is required"
                    };
                }

                var chapter = await GetChapterByIdAsync(chapterId);
                if (chapter == null)
                {
                    return new CreateLessonViewResult
                    {
                        Success = false,
                        ErrorMessage = "Chapter not found"
                    };
                }

                var existingLessons = await GetLessonsInChapterAsync(chapterId);

                var viewModel = new CreateLessonViewModel
                {
                    ChapterId = chapterId,
                    CourseId = chapter.CourseId,
                    ChapterName = chapter.ChapterName,
                    CourseName = chapter.Course.CourseName,
                    ChapterOrder = chapter.ChapterOrder ?? 1,
                    Order = await GetNextLessonOrderAsync(chapterId),
                    LessonTypeId = lessonTypeId,
                    LessonTypes = await GetLessonTypesAsync(),
                    ExistingLessons = existingLessons.ToList(),
                    // Set default values
                    IsLocked = false,
                    IsMandatory = true,
                    RequiresQuizPass = false,
                    MinQuizScore = 70,
                    MinCompletionPercentage = 100,
                    MinTimeSpent = 0
                };

                return new CreateLessonViewResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting create lesson view model for chapter {ChapterId}, type {LessonTypeId}", chapterId, lessonTypeId);
                return new CreateLessonViewResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the lesson creation page."
                };
            }
        }

        public async Task<CreateLessonResult> ProcessCreateLessonAsync(CreateLessonViewModel model, ModelStateDictionary modelState)
        {
            try
            {
                _logger.LogInformation("ProcessCreateLesson called with LessonName: {LessonName}, ChapterId: {ChapterId}", model?.LessonName, model?.ChapterId);

                if (model == null)
                {
                    _logger.LogError("Model is null");
                    return new CreateLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid request data.",
                        RedirectAction = "Index",
                        RedirectController = "Courses"
                    };
                }

                if (modelState.IsValid)
                {
                    _logger.LogInformation("ModelState is valid. Starting additional validations...");

                    // Additional validations
                    var validationErrors = await ValidateCreateLessonModelAsync(model);
                    _logger.LogInformation("Validation completed. Error count: {ErrorCount}", validationErrors.Count);

                    if (validationErrors.Count == 0)
                    {
                        _logger.LogInformation("Creating lesson object...");

                        // Determine content based on lesson type
                        string finalContent = await ProcessLessonContentByTypeAsync(model);

                        var lesson = new Lesson
                        {
                            LessonId = Guid.NewGuid().ToString(),
                            LessonName = model.LessonName.Trim(),
                            LessonDescription = model.Description?.Trim(),
                            LessonContent = finalContent,
                            LessonTypeId = model.LessonTypeId,
                            LessonOrder = model.Order,
                            ChapterId = model.ChapterId,
                            IsLocked = model.IsLocked,
                            UnlockAfterLessonId = string.IsNullOrEmpty(model.UnlockAfterLessonId) ? null : model.UnlockAfterLessonId,
                            IsMandatory = model.IsMandatory,
                            RequiresQuizPass = model.RequiresQuizPass,
                            MinQuizScore = model.MinQuizScore ?? 70.00m,
                            MinCompletionPercentage = model.MinCompletionPercentage ?? 100.00m,
                            MinTimeSpent = model.MinTimeSpent ?? 0,
                            LessonStatus = 1 // Active
                        };

                        _logger.LogInformation("Calling lesson service to create lesson with ID: {LessonId}", lesson.LessonId);

                        var result = await CreateLessonAsync(lesson);
                        _logger.LogInformation("Lesson service result: {Result}", result);

                        if (result)
                        {
                            _logger.LogInformation("Lesson created successfully. Setting success message and redirecting...");

                            var chapter = await GetChapterByIdAsync(model.ChapterId);
                            _logger.LogInformation("Retrieved chapter for redirect. CourseId: {CourseId}", chapter?.CourseId);

                            return new CreateLessonResult
                            {
                                Success = true,
                                SuccessMessage = $"Lesson '{model.LessonName}' has been created successfully!",
                                RedirectAction = "Details",
                                RedirectController = "Course",
                                RedirectValues = new { id = chapter?.CourseId, tab = "curriculum" }
                            };
                        }
                        else
                        {
                            _logger.LogError("Lesson service returned false when creating lesson");
                            await ReloadViewModelDataAsync(model);
                            return new CreateLessonResult
                            {
                                Success = false,
                                ErrorMessage = "An error occurred while creating the lesson. Please try again.",
                                ViewModel = model
                            };
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Validation errors found");
                        await ReloadViewModelDataAsync(model);
                        return new CreateLessonResult
                        {
                            Success = false,
                            ValidationErrors = validationErrors,
                            ViewModel = model
                        };
                    }
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid");
                    await ReloadViewModelDataAsync(model);
                    return new CreateLessonResult
                    {
                        Success = false,
                        ViewModel = model
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating lesson");
                await ReloadViewModelDataAsync(model);
                return new CreateLessonResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while creating the lesson. Please try again.",
                    ViewModel = model
                };
            }
        }

        public async Task<EditLessonResult> GetEditLessonViewModelAsync(string lessonId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(lessonId))
                {
                    return new EditLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Lesson ID is required"
                    };
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return new EditLessonResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                var viewModel = await GetLessonForEditAsync(lessonId, userId);
                if (viewModel == null)
                {
                    return new EditLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Lesson not found or you don't have permission to edit it"
                    };
                }

                return new EditLessonResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting edit lesson view model for lesson {LessonId}, user {UserId}", lessonId, userId);
                return new EditLessonResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the lesson for editing."
                };
            }
        }

        public async Task<UpdateLessonResult> ProcessUpdateLessonAsync(string lessonId, CreateLessonViewModel model, ModelStateDictionary modelState, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(lessonId))
                {
                    return new UpdateLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Lesson ID is required"
                    };
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return new UpdateLessonResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                if (model == null)
                {
                    return new UpdateLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid request data.",
                        RedirectAction = "EditLesson",
                        RedirectValues = new { id = lessonId }
                    };
                }

                if (modelState.IsValid)
                {
                    // Additional validations
                    var validationErrors = await ValidateEditLessonModelAsync(model, lessonId);

                    if (validationErrors.Count == 0)
                    {
                        // Update lesson content based on lesson type
                        await ProcessEditLessonContentAsync(model);

                        var result = await UpdateLessonAsync(lessonId, model, userId);

                        if (result)
                        {
                            var chapter = await GetChapterByIdAsync(model.ChapterId);
                            return new UpdateLessonResult
                            {
                                Success = true,
                                SuccessMessage = $"Lesson '{model.LessonName}' has been updated successfully!",
                                RedirectAction = "Details",
                                RedirectController = "Course",
                                RedirectValues = new { id = chapter?.CourseId, tab = "curriculum" }
                            };
                        }
                        else
                        {
                            await ReloadViewModelDataAsync(model);
                            return new UpdateLessonResult
                            {
                                Success = false,
                                ErrorMessage = "An error occurred while updating the lesson. Please try again.",
                                ViewModel = model
                            };
                        }
                    }
                    else
                    {
                        await ReloadViewModelDataAsync(model);
                        return new UpdateLessonResult
                        {
                            Success = false,
                            ValidationErrors = validationErrors,
                            ViewModel = model
                        };
                    }
                }

                await ReloadViewModelDataAsync(model);
                return new UpdateLessonResult
                {
                    Success = false,
                    ViewModel = model
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating lesson {LessonId}", lessonId);
                await ReloadViewModelDataAsync(model);
                return new UpdateLessonResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while updating the lesson. Please try again.",
                    ViewModel = model
                };
            }
        }

        public async Task<DeleteLessonResult> ProcessDeleteLessonAsync(string lessonId, string userId, string courseId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return new DeleteLessonResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Details",
                        RedirectController = "Course",
                        RedirectValues = new { id = courseId }
                    };
                }

                var success = await DeleteLessonAsync(lessonId, userId);
                if (success)
                {
                    return new DeleteLessonResult
                    {
                        Success = true,
                        SuccessMessage = "Lesson deleted successfully!",
                        RedirectAction = "Details",
                        RedirectController = "Course",
                        RedirectValues = new { id = courseId }
                    };
                }
                else
                {
                    return new DeleteLessonResult
                    {
                        Success = false,
                        ErrorMessage = "Lesson not found or you are not authorized to delete this lesson.",
                        RedirectAction = "Details",
                        RedirectController = "Course",
                        RedirectValues = new { id = courseId }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lesson {LessonId} by user {UserId}", lessonId, userId);
                return new DeleteLessonResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while deleting the lesson.",
                    RedirectAction = "Details",
                    RedirectController = "Course",
                    RedirectValues = new { id = courseId }
                };
            }
        }

        // Private helper methods
        private async Task<Dictionary<string, string>> ValidateCreateLessonModelAsync(CreateLessonViewModel model)
        {
            var errors = new Dictionary<string, string>();

            // Check for duplicate lesson name
            if (await IsDuplicateLessonNameAsync(model.LessonName, model.ChapterId))
            {
                errors.Add("LessonName", "A lesson with this name already exists in this chapter.");
            }

            // Validate content based on lesson type
            switch (model.LessonTypeId)
            {
                case 1: // Video lesson type
                    if (string.IsNullOrEmpty(model.VideoUrl) && (model.VideoFile == null || model.VideoFile.Length == 0))
                    {
                        errors.Add("Video", "Please provide either a video URL or upload a video file.");
                    }
                    if (!string.IsNullOrEmpty(model.VideoUrl) && !IsValidVideoUrl(model.VideoUrl))
                    {
                        errors.Add("VideoUrl", "Please enter a valid video URL (YouTube, Vimeo, etc.).");
                    }
                    if (model.VideoFile != null && model.VideoFile.Length > 100 * 1024 * 1024) // 100MB
                    {
                        errors.Add("VideoFile", "Video file size must not exceed 100MB.");
                    }
                    break;
                case 2: // Text lesson type
                    if (string.IsNullOrEmpty(model.TextContent))
                    {
                        errors.Add("TextContent", "Text content is required for text lessons.");
                    }
                    break;
                case 3: // Document lesson type
                    if (model.DocumentFiles == null || model.DocumentFiles.Count == 0 || model.DocumentFiles.All(f => f.Length == 0))
                    {
                        errors.Add("DocumentFiles", "Please upload at least one document file.");
                    }
                    if (model.DocumentFiles != null)
                    {
                        foreach (var file in model.DocumentFiles)
                        {
                            if (file.Length > 10 * 1024 * 1024) // 10MB
                            {
                                errors.Add("DocumentFiles", $"Document file '{file.FileName}' exceeds 10MB size limit.");
                                break;
                            }
                        }
                    }
                    break;
            }

            // Validate unlock after lesson if specified
            if (!string.IsNullOrEmpty(model.UnlockAfterLessonId))
            {
                if (!await ValidateUnlockAfterLessonAsync(model.ChapterId, model.UnlockAfterLessonId))
                {
                    errors.Add("UnlockAfterLessonId", "The specified unlock lesson does not exist in this chapter.");
                }
            }

            // Validate quiz requirements
            if (model.RequiresQuizPass && (!model.MinQuizScore.HasValue || model.MinQuizScore < 0 || model.MinQuizScore > 100))
            {
                errors.Add("MinQuizScore", "Minimum quiz score must be between 0 and 100 when quiz pass is required.");
            }

            // Validate completion percentage
            if (model.MinCompletionPercentage.HasValue && (model.MinCompletionPercentage < 0 || model.MinCompletionPercentage > 100))
            {
                errors.Add("MinCompletionPercentage", "Minimum completion percentage must be between 0 and 100.");
            }

            // Validate lesson order
            if (model.Order <= 0)
            {
                errors.Add("Order", "Lesson order must be a positive number.");
            }

            return errors;
        }

        private async Task<Dictionary<string, string>> ValidateEditLessonModelAsync(CreateLessonViewModel model, string currentLessonId)
        {
            var errors = new Dictionary<string, string>();

            // Check for duplicate lesson name (excluding current lesson)
            if (await IsDuplicateLessonNameForEditAsync(model.LessonName, model.ChapterId, currentLessonId))
            {
                errors.Add("LessonName", "A lesson with this name already exists in this chapter.");
            }

            // Validate content based on lesson type (reuse existing validation logic)
            switch (model.LessonTypeId)
            {
                case 1: // Video lesson type
                    if (string.IsNullOrEmpty(model.VideoUrl) && (model.VideoFile == null || model.VideoFile.Length == 0))
                    {
                        errors.Add("Video", "Please provide either a video URL or upload a video file.");
                    }
                    if (!string.IsNullOrEmpty(model.VideoUrl) && !IsValidVideoUrl(model.VideoUrl))
                    {
                        errors.Add("VideoUrl", "Please enter a valid video URL (YouTube, Vimeo, etc.).");
                    }
                    if (model.VideoFile != null && model.VideoFile.Length > 100 * 1024 * 1024) // 100MB
                    {
                        errors.Add("VideoFile", "Video file size must not exceed 100MB.");
                    }
                    break;
                case 2: // Text lesson type
                    if (string.IsNullOrEmpty(model.TextContent))
                    {
                        errors.Add("TextContent", "Text content is required for text lessons.");
                    }
                    break;
                case 3: // Document lesson type
                    // For edit, documents are optional (existing ones might be kept)
                    if (model.DocumentFiles != null)
                    {
                        foreach (var file in model.DocumentFiles)
                        {
                            if (file.Length > 10 * 1024 * 1024) // 10MB
                            {
                                errors.Add("DocumentFiles", $"Document file '{file.FileName}' exceeds 10MB size limit.");
                                break;
                            }
                        }
                    }
                    break;
            }

            // Validate unlock after lesson if specified
            if (!string.IsNullOrEmpty(model.UnlockAfterLessonId))
            {
                if (!await ValidateUnlockAfterLessonAsync(model.ChapterId, model.UnlockAfterLessonId))
                {
                    errors.Add("UnlockAfterLessonId", "The specified unlock lesson does not exist in this chapter.");
                }
            }

            // Validate quiz requirements
            if (model.RequiresQuizPass && (!model.MinQuizScore.HasValue || model.MinQuizScore < 0 || model.MinQuizScore > 100))
            {
                errors.Add("MinQuizScore", "Minimum quiz score must be between 0 and 100 when quiz pass is required.");
            }

            // Validate completion percentage
            if (model.MinCompletionPercentage.HasValue && (model.MinCompletionPercentage < 0 || model.MinCompletionPercentage > 100))
            {
                errors.Add("MinCompletionPercentage", "Minimum completion percentage must be between 0 and 100.");
            }

            // Validate lesson order
            if (model.Order <= 0)
            {
                errors.Add("Order", "Lesson order must be a positive number.");
            }

            return errors;
        }

        private bool IsValidVideoUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            try
            {
                var uri = new Uri(url);
                var host = uri.Host.ToLower();
                return host.Contains("youtube.com") || host.Contains("youtu.be") ||
                       host.Contains("vimeo.com") || host.Contains("dailymotion.com") ||
                       host.Contains("twitch.tv") || host.Contains("facebook.com");
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> ProcessLessonContentByTypeAsync(CreateLessonViewModel model)
        {
            try
            {
                switch (model.LessonTypeId)
                {
                    case 1: // Video lesson type
                        return await ProcessVideoContentAsync(model);
                    case 2: // Text lesson type
                        return ProcessTextContent(model);
                    case 3: // Document lesson type
                        return await ProcessDocumentContentAsync(model);
                    default:
                        return model.Content?.Trim() ?? "";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing lesson content for type {LessonTypeId}", model.LessonTypeId);
                return model.Content?.Trim() ?? "";
            }
        }

        private async Task<string> ProcessVideoContentAsync(CreateLessonViewModel model)
        {
            var content = model.Content?.Trim() ?? "";

            // Add video URL if provided
            if (!string.IsNullOrEmpty(model.VideoUrl))
            {
                content += $"\n\n[VIDEO_URL]{model.VideoUrl}[/VIDEO_URL]";
            }

            // Handle video file upload if provided
            if (model.VideoFile != null && model.VideoFile.Length > 0)
            {
                try
                {
                    // Create uploads directory if it doesn't exist
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "SharedMedia", "uploads");
                    Directory.CreateDirectory(uploadsDir);

                    // Generate unique filename
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.VideoFile.FileName)}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.VideoFile.CopyToAsync(stream);
                    }

                    // Add file reference to content
                    content += $"\n\n[VIDEO_FILE]/SharedMedia/uploads/{fileName}[/VIDEO_FILE]";

                    _logger.LogInformation("Video file uploaded: {FileName}", fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading video file");
                    throw new InvalidOperationException("Failed to upload video file");
                }
            }

            return content;
        }

        private string ProcessTextContent(CreateLessonViewModel model)
        {
            var content = model.Content?.Trim() ?? "";

            // Add the rich text content
            if (!string.IsNullOrEmpty(model.TextContent))
            {
                content += $"\n\n[TEXT_CONTENT]{model.TextContent.Trim()}[/TEXT_CONTENT]";
            }

            return content;
        }

        private async Task<string> ProcessDocumentContentAsync(CreateLessonViewModel model)
        {
            var content = model.Content?.Trim() ?? "";

            // Add document description if provided
            if (!string.IsNullOrEmpty(model.DocumentDescription))
            {
                content += $"\n\n[DOCUMENT_DESCRIPTION]{model.DocumentDescription.Trim()}[/DOCUMENT_DESCRIPTION]";
            }

            // Handle document file uploads
            if (model.DocumentFiles != null && model.DocumentFiles.Count > 0)
            {
                try
                {
                    // Create uploads directory if it doesn't exist
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "SharedMedia", "documents");
                    Directory.CreateDirectory(uploadsDir);

                    var uploadedFiles = new List<string>();

                    foreach (var file in model.DocumentFiles)
                    {
                        if (file.Length > 0)
                        {
                            // Generate unique filename
                            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                            var filePath = Path.Combine(uploadsDir, fileName);

                            // Save file
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            uploadedFiles.Add($"/SharedMedia/documents/{fileName}|{Path.GetFileName(file.FileName)}");
                            _logger.LogInformation("Document file uploaded: {FileName}", fileName);
                        }
                    }

                    if (uploadedFiles.Count > 0)
                    {
                        content += $"\n\n[DOCUMENT_FILES]{string.Join(";", uploadedFiles)}[/DOCUMENT_FILES]";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading document files");
                    throw new InvalidOperationException("Failed to upload document files");
                }
            }

            return content;
        }

        private async Task ProcessEditLessonContentAsync(CreateLessonViewModel model)
        {
            // Process content based on lesson type, similar to create but for editing
            switch (model.LessonTypeId)
            {
                case 1: // Video lesson
                    if (model.VideoFile != null && model.VideoFile.Length > 0)
                    {
                        // Handle new video file upload
                        await ProcessVideoContentAsync(model);
                    }
                    break;
                case 3: // Document lesson
                    if (model.DocumentFiles != null && model.DocumentFiles.Any(f => f.Length > 0))
                    {
                        // Handle new document uploads
                        await ProcessDocumentContentAsync(model);
                    }
                    break;
                    // Text lessons don't need special file processing
                    // Content parsing is handled in LessonService.ProcessLessonContent
            }
        }

        private async Task ReloadViewModelDataAsync(CreateLessonViewModel model)
        {
            try
            {
                // Reload lesson types
                model.LessonTypes = await GetLessonTypesAsync();

                // Reload existing lessons
                if (!string.IsNullOrEmpty(model.ChapterId))
                {
                    var lessons = await GetLessonsInChapterAsync(model.ChapterId);
                    model.ExistingLessons = lessons.ToList();
                }

                // Get chapter details for display
                if (!string.IsNullOrEmpty(model.ChapterId))
                {
                    var chapter = await GetChapterByIdAsync(model.ChapterId);
                    if (chapter != null)
                    {
                        model.ChapterName = chapter.ChapterName;
                        model.CourseName = chapter.Course.CourseName;
                        model.ChapterOrder = chapter.ChapterOrder ?? 1;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading view model data for chapter {ChapterId}", model?.ChapterId);
            }
        }





        private async Task UpdateCourseProgressAfterLessonCompletion(string userId, string lessonId)
        {
            // Get the course ID from the lesson
            var lesson = await GetLessonWithDetailsAsync(lessonId);
            if (lesson?.Chapter?.CourseId == null)
            {
                return;
            }

            var courseId = lesson.Chapter.CourseId;

            // Calculate new course progress percentage
            var progressPercentage = await GetLessonCompletionPercentageAsync(userId, courseId);

            // Update the enrollment progress
            await UpdateEnrollmentProgressAsync(userId, courseId, progressPercentage, lessonId);
        }

        // Method to parse lesson content and remove tags for display
        public static string ParseLessonContentForDisplay(string lessonContent, int lessonTypeId)
        {
            if (string.IsNullOrEmpty(lessonContent))
                return string.Empty;

            try
            {
                switch (lessonTypeId)
                {
                    case 1: // Video lesson
                        return ParseVideoContentForDisplay(lessonContent);
                    case 2: // Text lesson
                        return ParseTextContentForDisplay(lessonContent);
                    case 3: // Document lesson
                        return ParseDocumentContentForDisplay(lessonContent);
                    default:
                        return lessonContent;
                }
            }
            catch (Exception)
            {
                // Log error but return original content to avoid breaking display
                return lessonContent;
            }
        }

        private static string ParseVideoContentForDisplay(string content)
        {
            var result = content;
            var videoUrl = "";

            // Extract VIDEO_URL tags and keep the URL
            if (result.Contains("[VIDEO_URL]") && result.Contains("[/VIDEO_URL]"))
            {
                var startIndex = result.IndexOf("[VIDEO_URL]");
                var endIndex = result.IndexOf("[/VIDEO_URL]");
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    videoUrl = result.Substring(startIndex + "[VIDEO_URL]".Length, endIndex - startIndex - "[VIDEO_URL]".Length);
                    result = result.Replace($"[VIDEO_URL]{videoUrl}[/VIDEO_URL]", "");
                    result = result.Trim();
                }
            }

            // Extract VIDEO_FILE tags and keep the file path
            if (result.Contains("[VIDEO_FILE]") && result.Contains("[/VIDEO_FILE]"))
            {
                var startIndex = result.IndexOf("[VIDEO_FILE]");
                var endIndex = result.IndexOf("[/VIDEO_FILE]");
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    var videoFile = result.Substring(startIndex + "[VIDEO_FILE]".Length, endIndex - startIndex - "[VIDEO_FILE]".Length);
                    result = result.Replace($"[VIDEO_FILE]{videoFile}[/VIDEO_FILE]", "");
                    result = result.Trim();

                    // If we have a video file, use it as the video URL
                    if (string.IsNullOrEmpty(videoUrl))
                    {
                        videoUrl = videoFile;
                    }
                }
            }

            // Return the video URL if found, otherwise return the remaining content
            return !string.IsNullOrEmpty(videoUrl) ? videoUrl : result;
        }

        private static string ParseTextContentForDisplay(string content)
        {
            var result = content;
            var textContent = "";

            // Extract TEXT_CONTENT tags and keep the content inside
            if (result.Contains("[TEXT_CONTENT]") && result.Contains("[/TEXT_CONTENT]"))
            {
                var startIndex = result.IndexOf("[TEXT_CONTENT]");
                var endIndex = result.IndexOf("[/TEXT_CONTENT]");
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    textContent = result.Substring(startIndex + "[TEXT_CONTENT]".Length, endIndex - startIndex - "[TEXT_CONTENT]".Length);
                    result = result.Replace($"[TEXT_CONTENT]{textContent}[/TEXT_CONTENT]", "");
                    result = result.Trim();
                }
            }

            // Return the text content if found, otherwise return the remaining content
            return !string.IsNullOrEmpty(textContent) ? textContent : result;
        }

        private static string ParseDocumentContentForDisplay(string content)
        {
            var result = content;
            var documentDescription = "";

            // Extract DOCUMENT_DESCRIPTION tags and keep the description
            if (result.Contains("[DOCUMENT_DESCRIPTION]") && result.Contains("[/DOCUMENT_DESCRIPTION]"))
            {
                var startIndex = result.IndexOf("[DOCUMENT_DESCRIPTION]");
                var endIndex = result.IndexOf("[/DOCUMENT_DESCRIPTION]");
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    documentDescription = result.Substring(startIndex + "[DOCUMENT_DESCRIPTION]".Length, endIndex - startIndex - "[DOCUMENT_DESCRIPTION]".Length);
                    result = result.Replace($"[DOCUMENT_DESCRIPTION]{documentDescription}[/DOCUMENT_DESCRIPTION]", "");
                    result = result.Trim();
                }
            }

            // Extract DOCUMENT_FILES tags and keep the file paths
            if (result.Contains("[DOCUMENT_FILES]") && result.Contains("[/DOCUMENT_FILES]"))
            {
                var startIndex = result.IndexOf("[DOCUMENT_FILES]");
                var endIndex = result.IndexOf("[/DOCUMENT_FILES]");
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    var files = result.Substring(startIndex + "[DOCUMENT_FILES]".Length, endIndex - startIndex - "[DOCUMENT_FILES]".Length);
                    result = result.Replace($"[DOCUMENT_FILES]{files}[/DOCUMENT_FILES]", "");
                    result = result.Trim();

                    // If we have document files, use the first one as the main content
                    if (!string.IsNullOrEmpty(files))
                    {
                        var fileList = files.Split(';');
                        if (fileList.Length > 0)
                        {
                            var firstFile = fileList[0].Split('|')[0]; // Get file path without filename
                            return firstFile;
                        }
                    }
                }
            }

            // Return the document description if found, otherwise return the remaining content
            return !string.IsNullOrEmpty(documentDescription) ? documentDescription : result;
        }

        // Additional private helper methods from LessonService
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
            var parsedContent = ParseLessonContentForDisplay(lessonContent, 1);
            return string.IsNullOrEmpty(parsedContent) ? null : parsedContent;
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








