using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessLogicLayer.Constants;

namespace BusinessLogicLayer.Services.Implementations
{
    public class LearningServiceImpl : ILearningService
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<LearningServiceImpl> _logger;

        public LearningServiceImpl(BrainStormEraContext context, ILogger<LearningServiceImpl> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CoursePlayerViewModel?> GetCoursePlayerAsync(string userId, string courseId)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Author)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.CourseCategories)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Chapters.OrderBy(ch => ch.ChapterOrder))
                            .ThenInclude(ch => ch.Lessons.OrderBy(l => l.LessonOrder))
                                .ThenInclude(l => l.LessonType)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Chapters)
                            .ThenInclude(ch => ch.Lessons)
                                .ThenInclude(l => l.Quizzes)
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (enrollment == null) return null;

                var course = enrollment.Course;
                var userProgresses = await _context.UserProgresses
                    .Where(up => up.UserId == userId && up.Lesson.Chapter.CourseId == courseId)
                    .ToListAsync();

                var chapters = new List<ChapterLearningViewModel>();

                foreach (var chapter in course.Chapters.OrderBy(c => c.ChapterOrder))
                {
                    var lessons = new List<LessonLearningViewModel>();

                    foreach (var lesson in chapter.Lessons.OrderBy(l => l.LessonOrder))
                    {
                        var userProgress = userProgresses.FirstOrDefault(up => up.LessonId == lesson.LessonId);
                        var isUnlocked = await IsLessonUnlockedAsync(userId, lesson.LessonId);

                        lessons.Add(new LessonLearningViewModel
                        {
                            LessonId = lesson.LessonId,
                            LessonName = lesson.LessonName,
                            LessonDescription = lesson.LessonDescription,
                            LessonOrder = lesson.LessonOrder,
                            LessonTypeId = lesson.LessonTypeId?.ToString() ?? "1",
                            LessonTypeName = lesson.LessonType?.LessonTypeName ?? "Text",
                            IsCompleted = userProgress?.IsCompleted ?? false,
                            IsLocked = !isUnlocked,
                            IsCurrent = lesson.LessonId == enrollment.CurrentLessonId,
                            CompletionPercentage = userProgress?.ProgressPercentage ?? 0,
                            TimeSpent = userProgress?.TimeSpent != null ? TimeSpan.FromSeconds(userProgress.TimeSpent.Value) : null,
                            LastAccessedAt = userProgress?.LastAccessedAt,
                            RequiresQuizPass = lesson.RequiresQuizPass ?? false,
                            MinQuizScore = lesson.MinQuizScore,
                            HasQuiz = lesson.Quizzes.Any(),
                            HasPassedQuiz = await HasPassedLessonQuizAsync(userId, lesson.LessonId)
                        });
                    }

                    var chapterProgress = lessons.Any() ? lessons.Average(l => l.CompletionPercentage) : 0;

                    chapters.Add(new ChapterLearningViewModel
                    {
                        ChapterId = chapter.ChapterId,
                        ChapterName = chapter.ChapterName,
                        ChapterDescription = chapter.ChapterDescription,
                        ChapterOrder = chapter.ChapterOrder ?? 0,
                        CompletionPercentage = chapterProgress,
                        IsLocked = !await IsChapterUnlockedAsync(userId, chapter.ChapterId),
                        Lessons = lessons
                    });
                }

                var totalLessons = chapters.SelectMany(c => c.Lessons).Count();
                var completedLessons = chapters.SelectMany(c => c.Lessons).Count(l => l.IsCompleted);

                return new CoursePlayerViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription ?? "",
                    CourseImage = course.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    AuthorName = course.Author.FullName ?? course.Author.Username,
                    AuthorImage = course.Author.UserImage ?? MediaConstants.Defaults.DefaultAvatarPath,
                    ProgressPercentage = enrollment.ProgressPercentage ?? 0,
                    CurrentLessonId = enrollment.CurrentLessonId,
                    LastAccessedLessonId = enrollment.LastAccessedLessonId,
                    Chapters = chapters,
                    Categories = course.CourseCategories.Select(cc => cc.CourseCategoryName).ToList(),
                    EnrollmentDate = enrollment.EnrollmentCreatedAt,
                    LastAccessedDate = enrollment.EnrollmentUpdatedAt,
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessons,
                    EstimatedTimeRemaining = await CalculateEstimatedTimeRemainingAsync(userId, courseId)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course player for user {UserId} and course {CourseId}", userId, courseId);
                return null;
            }
        }

        public async Task<bool> IsUserEnrolledInCourseAsync(string userId, string courseId)
        {
            try
            {
                return await _context.Enrollments
                    .AnyAsync(e => e.UserId == userId && e.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking enrollment for user {UserId} and course {CourseId}", userId, courseId);
                return false;
            }
        }

        public async Task<bool> CanUserAccessCourseAsync(string userId, string courseId)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                return enrollment != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking course access for user {UserId} and course {CourseId}", userId, courseId);
                return false;
            }
        }

        public async Task<LessonDetailViewModel?> GetLessonDetailAsync(string userId, string lessonId)
        {
            try
            {
                var lesson = await _context.Lessons
                    .Include(l => l.Chapter)
                        .ThenInclude(c => c.Course)
                    .Include(l => l.LessonType)
                    .Include(l => l.Quizzes)
                        .ThenInclude(q => q.Questions)
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId);

                if (lesson == null) return null;

                // Check if user has access to this lesson
                if (!await CanUserAccessLessonAsync(userId, lessonId))
                    return null;

                var userProgress = await _context.UserProgresses
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lessonId);

                var previousLesson = await GetPreviousLessonIdAsync(userId, lessonId);
                var nextLesson = await GetNextLessonIdAsync(userId, lessonId);

                var quizzes = new List<QuizLearningViewModel>();
                foreach (var q in lesson.Quizzes)
                {
                    quizzes.Add(new QuizLearningViewModel
                    {
                        QuizId = q.QuizId,
                        QuizTitle = q.QuizName,
                        QuizDescription = q.QuizDescription,
                        TotalQuestions = q.Questions.Count,
                        PassingScore = q.PassingScore ?? 70,
                        TimeLimit = q.TimeLimit ?? 0,
                        MaxAttempts = q.MaxAttempts ?? 0,
                        AttemptsUsed = GetQuizAttemptsUsed(userId, q.QuizId),
                        HasPassed = await HasPassedQuizAsync(userId, q.QuizId),
                        BestScore = await GetBestQuizScoreAsync(userId, q.QuizId)
                    });
                }

                return new LessonDetailViewModel
                {
                    LessonId = lesson.LessonId,
                    LessonName = lesson.LessonName,
                    LessonDescription = lesson.LessonDescription,
                    LessonContent = lesson.LessonContent,
                    LessonTypeName = lesson.LessonType?.LessonTypeName ?? "Text",
                    LessonTypeId = lesson.LessonTypeId ?? 1,
                    LessonOrder = lesson.LessonOrder,
                    ChapterId = lesson.ChapterId,
                    ChapterName = lesson.Chapter.ChapterName,
                    CourseId = lesson.Chapter.CourseId,
                    CourseName = lesson.Chapter.Course.CourseName,
                    IsCompleted = userProgress?.IsCompleted ?? false,
                    RequiresQuizPass = lesson.RequiresQuizPass ?? false,
                    MinQuizScore = lesson.MinQuizScore,
                    CompletionPercentage = userProgress?.ProgressPercentage ?? 0,
                    TimeSpent = userProgress?.TimeSpent != null ? TimeSpan.FromSeconds(userProgress.TimeSpent.Value) : null,
                    PreviousLessonId = previousLesson,
                    NextLessonId = nextLesson,
                    Quizzes = quizzes,
                    HasPassedQuiz = await HasPassedLessonQuizAsync(userId, lessonId),
                    StartedAt = userProgress?.FirstAccessedAt,
                    LastAccessedAt = userProgress?.LastAccessedAt,
                    MinTimeSpent = lesson.MinTimeSpent ?? 0,
                    MinCompletionPercentage = lesson.MinCompletionPercentage ?? 100
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lesson detail for user {UserId} and lesson {LessonId}", userId, lessonId);
                return null;
            }
        }

        public async Task<bool> CanUserAccessLessonAsync(string userId, string lessonId)
        {
            try
            {
                // Check if user is enrolled in the course
                var lesson = await _context.Lessons
                    .Include(l => l.Chapter)
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId);

                if (lesson == null) return false;

                var isEnrolled = await IsUserEnrolledInCourseAsync(userId, lesson.Chapter.CourseId);
                if (!isEnrolled) return false;

                // Check if lesson is unlocked
                return await IsLessonUnlockedAsync(userId, lessonId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking lesson access for user {UserId} and lesson {LessonId}", userId, lessonId);
                return false;
            }
        }

        public async Task<string?> GetNextLessonIdAsync(string userId, string currentLessonId)
        {
            try
            {
                var currentLesson = await _context.Lessons
                    .Include(l => l.Chapter)
                        .ThenInclude(c => c.Course)
                            .ThenInclude(co => co.Chapters.OrderBy(ch => ch.ChapterOrder))
                                .ThenInclude(ch => ch.Lessons.OrderBy(l => l.LessonOrder))
                    .FirstOrDefaultAsync(l => l.LessonId == currentLessonId);

                if (currentLesson == null) return null;

                var course = currentLesson.Chapter.Course;
                var allLessons = course.Chapters
                    .OrderBy(c => c.ChapterOrder)
                    .SelectMany(c => c.Lessons.OrderBy(l => l.LessonOrder))
                    .ToList();

                var currentIndex = allLessons.FindIndex(l => l.LessonId == currentLessonId);
                if (currentIndex == -1 || currentIndex >= allLessons.Count - 1) return null;

                var nextLesson = allLessons[currentIndex + 1];

                // Check if the next lesson is unlocked
                if (await IsLessonUnlockedAsync(userId, nextLesson.LessonId))
                    return nextLesson.LessonId;

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next lesson for user {UserId} and lesson {LessonId}", userId, currentLessonId);
                return null;
            }
        }

        public async Task<string?> GetPreviousLessonIdAsync(string userId, string currentLessonId)
        {
            try
            {
                var currentLesson = await _context.Lessons
                    .Include(l => l.Chapter)
                        .ThenInclude(c => c.Course)
                            .ThenInclude(co => co.Chapters.OrderBy(ch => ch.ChapterOrder))
                                .ThenInclude(ch => ch.Lessons.OrderBy(l => l.LessonOrder))
                    .FirstOrDefaultAsync(l => l.LessonId == currentLessonId);

                if (currentLesson == null) return null;

                var course = currentLesson.Chapter.Course;
                var allLessons = course.Chapters
                    .OrderBy(c => c.ChapterOrder)
                    .SelectMany(c => c.Lessons.OrderBy(l => l.LessonOrder))
                    .ToList();

                var currentIndex = allLessons.FindIndex(l => l.LessonId == currentLessonId);
                if (currentIndex <= 0) return null;

                return allLessons[currentIndex - 1].LessonId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting previous lesson for user {UserId} and lesson {LessonId}", userId, currentLessonId);
                return null;
            }
        }

        public async Task<bool> UpdateLessonProgressAsync(string userId, string lessonId, decimal completionPercentage, int timeSpentSeconds)
        {
            try
            {
                var userProgress = await _context.UserProgresses
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lessonId);

                if (userProgress == null)
                {
                    userProgress = new UserProgress
                    {
                        UserId = userId,
                        LessonId = lessonId,
                        ProgressPercentage = completionPercentage,
                        TimeSpent = timeSpentSeconds,
                        FirstAccessedAt = DateTime.UtcNow,
                        LastAccessedAt = DateTime.UtcNow,
                        IsCompleted = completionPercentage >= 100
                    };
                    _context.UserProgresses.Add(userProgress);
                }
                else
                {
                    userProgress.ProgressPercentage = Math.Max(userProgress.ProgressPercentage ?? 0, completionPercentage);
                    userProgress.TimeSpent = (userProgress.TimeSpent ?? 0) + timeSpentSeconds;
                    userProgress.LastAccessedAt = DateTime.UtcNow;
                    userProgress.IsCompleted = userProgress.ProgressPercentage >= 100;
                }

                await _context.SaveChangesAsync();

                // Update course progress
                await UpdateCourseProgressAsync(userId, await GetCourseIdFromLessonAsync(lessonId));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lesson progress for user {UserId} and lesson {LessonId}", userId, lessonId);
                return false;
            }
        }

        public async Task<bool> MarkLessonAsCompletedAsync(string userId, string lessonId)
        {
            try
            {
                return await UpdateLessonProgressAsync(userId, lessonId, 100, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking lesson as completed for user {UserId} and lesson {LessonId}", userId, lessonId);
                return false;
            }
        }

        public async Task<bool> UpdateCourseProgressAsync(string userId, string courseId)
        {
            try
            {
                var progress = await CalculateCourseProgressAsync(userId, courseId);

                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (enrollment != null)
                {
                    enrollment.ProgressPercentage = progress;
                    enrollment.EnrollmentUpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Check for course completion
                    if (progress >= 100)
                    {
                        await CheckCourseCompletionAsync(userId, courseId);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course progress for user {UserId} and course {CourseId}", userId, courseId);
                return false;
            }
        }

        public async Task<decimal> CalculateCourseProgressAsync(string userId, string courseId)
        {
            try
            {
                var totalLessons = await _context.Lessons
                    .CountAsync(l => l.Chapter.CourseId == courseId);

                if (totalLessons == 0) return 0;

                var completedLessons = await _context.UserProgresses
                    .CountAsync(up => up.UserId == userId &&
                                     up.Lesson.Chapter.CourseId == courseId &&
                                     up.IsCompleted == true);

                return Math.Round((decimal)completedLessons / totalLessons * 100, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating course progress for user {UserId} and course {CourseId}", userId, courseId);
                return 0;
            }
        }

        public async Task<CourseNavigationViewModel?> GetCourseNavigationAsync(string userId, string courseId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Chapters.OrderBy(ch => ch.ChapterOrder))
                        .ThenInclude(ch => ch.Lessons.OrderBy(l => l.LessonOrder))
                            .ThenInclude(l => l.LessonType)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);

                if (course == null) return null;

                var currentLessonId = await GetCurrentLessonIdAsync(userId, courseId);
                var userProgresses = await _context.UserProgresses
                    .Where(up => up.UserId == userId && up.Lesson.Chapter.CourseId == courseId)
                    .ToListAsync();

                var chapters = new List<ChapterNavigationViewModel>();

                foreach (var chapter in course.Chapters.OrderBy(c => c.ChapterOrder))
                {
                    var lessons = new List<LessonNavigationViewModel>();

                    foreach (var lesson in chapter.Lessons.OrderBy(l => l.LessonOrder))
                    {
                        var userProgress = userProgresses.FirstOrDefault(up => up.LessonId == lesson.LessonId);
                        var isUnlocked = await IsLessonUnlockedAsync(userId, lesson.LessonId);

                        lessons.Add(new LessonNavigationViewModel
                        {
                            LessonId = lesson.LessonId,
                            LessonName = lesson.LessonName,
                            LessonOrder = lesson.LessonOrder,
                            LessonTypeName = lesson.LessonType?.LessonTypeName ?? "Text",
                            IsCompleted = userProgress?.IsCompleted ?? false,
                            IsLocked = !isUnlocked,
                            IsCurrent = lesson.LessonId == currentLessonId,
                            HasQuiz = lesson.Quizzes.Any(),
                            RequiresQuizPass = lesson.RequiresQuizPass ?? false
                        });
                    }

                    var chapterProgress = lessons.Any() ?
                        (decimal)lessons.Count(l => l.IsCompleted) / lessons.Count * 100 : 0;

                    chapters.Add(new ChapterNavigationViewModel
                    {
                        ChapterId = chapter.ChapterId,
                        ChapterName = chapter.ChapterName,
                        ChapterOrder = chapter.ChapterOrder ?? 0,
                        IsCompleted = lessons.All(l => l.IsCompleted),
                        IsLocked = !await IsChapterUnlockedAsync(userId, chapter.ChapterId),
                        CompletionPercentage = chapterProgress,
                        Lessons = lessons
                    });
                }

                var overallProgress = await CalculateCourseProgressAsync(userId, courseId);

                return new CourseNavigationViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    Chapters = chapters,
                    CurrentLessonId = currentLessonId,
                    OverallProgress = overallProgress
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course navigation for user {UserId} and course {CourseId}", userId, courseId);
                return null;
            }
        }

        public async Task<LearningDashboardViewModel> GetLearningDashboardAsync(string userId)
        {
            try
            {
                var enrollments = await _context.Enrollments
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Author)
                    .Where(e => e.UserId == userId && e.EnrollmentStatus == 1)
                    .ToListAsync();

                var enrolledCourses = new List<CourseProgressViewModel>();

                foreach (var enrollment in enrollments)
                {
                    var totalLessons = await _context.Lessons
                        .CountAsync(l => l.Chapter.CourseId == enrollment.CourseId);

                    var completedLessons = await _context.UserProgresses
                        .CountAsync(up => up.UserId == userId &&
                                         up.Lesson.Chapter.CourseId == enrollment.CourseId &&
                                         up.IsCompleted == true);

                    var currentLessonName = "";
                    if (!string.IsNullOrEmpty(enrollment.CurrentLessonId))
                    {
                        var currentLesson = await _context.Lessons
                            .FirstOrDefaultAsync(l => l.LessonId == enrollment.CurrentLessonId);
                        currentLessonName = currentLesson?.LessonName;
                    }

                    enrolledCourses.Add(new CourseProgressViewModel
                    {
                        CourseId = enrollment.CourseId,
                        CourseName = enrollment.Course.CourseName,
                        CourseImage = enrollment.Course.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                        AuthorName = enrollment.Course.Author.FullName ?? enrollment.Course.Author.Username,
                        ProgressPercentage = enrollment.ProgressPercentage ?? 0,
                        CurrentLessonName = currentLessonName,
                        LastAccessedDate = enrollment.EnrollmentUpdatedAt,
                        TotalLessons = totalLessons,
                        CompletedLessons = completedLessons,
                        EnrollmentDate = enrollment.EnrollmentCreatedAt
                    });
                }

                var recentLessons = await _context.UserProgresses
                    .Include(up => up.Lesson)
                        .ThenInclude(l => l.LessonType)
                    .Where(up => up.UserId == userId)
                    .OrderByDescending(up => up.LastAccessedAt)
                    .Take(5)
                    .Select(up => new LessonNavigationViewModel
                    {
                        LessonId = up.LessonId,
                        LessonName = up.Lesson.LessonName,
                        LessonOrder = up.Lesson.LessonOrder,
                        LessonTypeName = up.Lesson.LessonType!.LessonTypeName,
                        IsCompleted = up.IsCompleted ?? false,
                        IsLocked = false,
                        IsCurrent = false,
                        HasQuiz = up.Lesson.Quizzes.Any(),
                        RequiresQuizPass = up.Lesson.RequiresQuizPass ?? false
                    })
                    .ToListAsync();

                var totalCoursesEnrolled = enrollments.Count;
                var totalCoursesCompleted = enrolledCourses.Count(c => c.IsCompleted);
                var totalLessonsCompleted = await _context.UserProgresses
                    .CountAsync(up => up.UserId == userId && up.IsCompleted == true);

                var overallProgress = enrolledCourses.Any() ?
                    enrolledCourses.Average(c => c.ProgressPercentage) : 0;

                return new LearningDashboardViewModel
                {
                    EnrolledCourses = enrolledCourses,
                    RecentLessons = recentLessons,
                    TotalCoursesEnrolled = totalCoursesEnrolled,
                    TotalCoursesCompleted = totalCoursesCompleted,
                    TotalLessonsCompleted = totalLessonsCompleted,
                    OverallProgress = overallProgress
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting learning dashboard for user {UserId}", userId);
                return new LearningDashboardViewModel();
            }
        }

        public async Task<bool> IsLessonUnlockedAsync(string userId, string lessonId)
        {
            try
            {
                var lesson = await _context.Lessons
                    .Include(l => l.Chapter)
                        .ThenInclude(c => c.Course)
                            .ThenInclude(co => co.Chapters.OrderBy(ch => ch.ChapterOrder))
                                .ThenInclude(ch => ch.Lessons.OrderBy(l => l.LessonOrder))
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId);

                if (lesson == null) return false;

                // If lesson is not locked, it's available
                if (lesson.IsLocked != true) return true;

                // Check if unlock condition is met
                if (!string.IsNullOrEmpty(lesson.UnlockAfterLessonId))
                {
                    var unlockLesson = await _context.UserProgresses
                        .FirstOrDefaultAsync(up => up.UserId == userId &&
                                                  up.LessonId == lesson.UnlockAfterLessonId &&
                                                  up.IsCompleted == true);
                    return unlockLesson != null;
                }

                // Check if it's the first lesson in the course
                var course = lesson.Chapter.Course;
                var allLessons = course.Chapters
                    .OrderBy(c => c.ChapterOrder)
                    .SelectMany(c => c.Lessons.OrderBy(l => l.LessonOrder))
                    .ToList();

                var firstLesson = allLessons.FirstOrDefault();
                if (firstLesson != null && firstLesson.LessonId == lessonId) return true;

                // Check if previous lesson is completed
                var currentIndex = allLessons.FindIndex(l => l.LessonId == lessonId);
                if (currentIndex > 0)
                {
                    var previousLesson = allLessons[currentIndex - 1];
                    var previousProgress = await _context.UserProgresses
                        .FirstOrDefaultAsync(up => up.UserId == userId &&
                                                  up.LessonId == previousLesson.LessonId &&
                                                  up.IsCompleted == true);
                    return previousProgress != null;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if lesson is unlocked for user {UserId} and lesson {LessonId}", userId, lessonId);
                return false;
            }
        }

        public async Task<List<string>> GetUnlockedLessonIdsAsync(string userId, string courseId)
        {
            try
            {
                var lessons = await _context.Lessons
                    .Include(l => l.Chapter)
                    .Where(l => l.Chapter.CourseId == courseId)
                    .OrderBy(l => l.Chapter.ChapterOrder)
                    .ThenBy(l => l.LessonOrder)
                    .ToListAsync();

                var unlockedLessons = new List<string>();

                foreach (var lesson in lessons)
                {
                    if (await IsLessonUnlockedAsync(userId, lesson.LessonId))
                    {
                        unlockedLessons.Add(lesson.LessonId);
                    }
                }

                return unlockedLessons;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unlocked lessons for user {UserId} and course {CourseId}", userId, courseId);
                return new List<string>();
            }
        }

        public async Task<bool> UpdateLessonTimeSpentAsync(string userId, string lessonId, int additionalSeconds)
        {
            try
            {
                var userProgress = await _context.UserProgresses
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lessonId);

                if (userProgress == null)
                {
                    userProgress = new UserProgress
                    {
                        UserId = userId,
                        LessonId = lessonId,
                        TimeSpent = additionalSeconds,
                        FirstAccessedAt = DateTime.UtcNow,
                        LastAccessedAt = DateTime.UtcNow,
                        ProgressPercentage = 0,
                        IsCompleted = false
                    };
                    _context.UserProgresses.Add(userProgress);
                }
                else
                {
                    userProgress.TimeSpent = (userProgress.TimeSpent ?? 0) + additionalSeconds;
                    userProgress.LastAccessedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lesson time spent for user {UserId} and lesson {LessonId}", userId, lessonId);
                return false;
            }
        }

        public async Task<TimeSpan> GetTotalTimeSpentInCourseAsync(string userId, string courseId)
        {
            try
            {
                var totalSeconds = await _context.UserProgresses
                    .Where(up => up.UserId == userId && up.Lesson.Chapter.CourseId == courseId)
                    .SumAsync(up => up.TimeSpent ?? 0);

                return TimeSpan.FromSeconds(totalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total time spent for user {UserId} and course {CourseId}", userId, courseId);
                return TimeSpan.Zero;
            }
        }

        public async Task<TimeSpan> GetTimeSpentInLessonAsync(string userId, string lessonId)
        {
            try
            {
                var userProgress = await _context.UserProgresses
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lessonId);

                return TimeSpan.FromSeconds(userProgress?.TimeSpent ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time spent for user {UserId} and lesson {LessonId}", userId, lessonId);
                return TimeSpan.Zero;
            }
        }

        public async Task<bool> CheckCourseCompletionAsync(string userId, string courseId)
        {
            try
            {
                var progress = await CalculateCourseProgressAsync(userId, courseId);

                if (progress >= 100)
                {
                    var enrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                    if (enrollment != null && enrollment.EnrollmentStatus != 3)
                    {
                        enrollment.EnrollmentStatus = 3; // Completed
                        enrollment.EnrollmentUpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        // TODO: Generate certificate, trigger achievements, etc.

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking course completion for user {UserId} and course {CourseId}", userId, courseId);
                return false;
            }
        }

        public async Task<bool> CheckChapterCompletionAsync(string userId, string chapterId)
        {
            try
            {
                var totalLessons = await _context.Lessons
                    .CountAsync(l => l.ChapterId == chapterId);

                var completedLessons = await _context.UserProgresses
                    .CountAsync(up => up.UserId == userId &&
                                     up.Lesson.ChapterId == chapterId &&
                                     up.IsCompleted == true);

                return totalLessons > 0 && completedLessons == totalLessons;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking chapter completion for user {UserId} and chapter {ChapterId}", userId, chapterId);
                return false;
            }
        }

        public async Task<List<string>> GetCompletedLessonIdsAsync(string userId, string courseId)
        {
            try
            {
                return await _context.UserProgresses
                    .Where(up => up.UserId == userId &&
                                up.Lesson.Chapter.CourseId == courseId &&
                                up.IsCompleted == true)
                    .Select(up => up.LessonId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completed lesson IDs for user {UserId} and course {CourseId}", userId, courseId);
                return new List<string>();
            }
        }

        public async Task<string?> GetCurrentLessonIdAsync(string userId, string courseId)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (!string.IsNullOrEmpty(enrollment?.CurrentLessonId))
                    return enrollment.CurrentLessonId;

                // If no current lesson set, return the first available lesson
                var firstLesson = await _context.Lessons
                    .Include(l => l.Chapter)
                    .Where(l => l.Chapter.CourseId == courseId)
                    .OrderBy(l => l.Chapter.ChapterOrder)
                    .ThenBy(l => l.LessonOrder)
                    .FirstOrDefaultAsync();

                return firstLesson?.LessonId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current lesson for user {UserId} and course {CourseId}", userId, courseId);
                return null;
            }
        }

        public async Task<bool> SetCurrentLessonAsync(string userId, string courseId, string lessonId)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (enrollment != null)
                {
                    enrollment.CurrentLessonId = lessonId;
                    enrollment.LastAccessedLessonId = lessonId;
                    enrollment.EnrollmentUpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting current lesson for user {UserId} and course {CourseId}", userId, courseId);
                return false;
            }
        }

        #region Private Helper Methods

        private async Task<bool> IsChapterUnlockedAsync(string userId, string chapterId)
        {
            // Simple implementation - chapter is unlocked if it's the first chapter or previous chapter is completed
            var chapter = await _context.Chapters
                .Include(c => c.Course)
                    .ThenInclude(co => co.Chapters.OrderBy(ch => ch.ChapterOrder))
                .FirstOrDefaultAsync(c => c.ChapterId == chapterId);

            if (chapter == null) return false;

            var chapters = chapter.Course.Chapters.OrderBy(c => c.ChapterOrder).ToList();
            var currentIndex = chapters.FindIndex(c => c.ChapterId == chapterId);

            if (currentIndex == 0) return true; // First chapter is always unlocked

            if (currentIndex > 0)
            {
                var previousChapter = chapters[currentIndex - 1];
                return await CheckChapterCompletionAsync(userId, previousChapter.ChapterId);
            }

            return false;
        }

        private async Task<bool> HasPassedLessonQuizAsync(string userId, string lessonId)
        {
            var quizzes = await _context.Quizzes
                .Where(q => q.LessonId == lessonId)
                .ToListAsync();

            foreach (var quiz in quizzes)
            {
                if (!await HasPassedQuizAsync(userId, quiz.QuizId))
                    return false;
            }

            return quizzes.Any(); // Return true only if there are quizzes and all are passed
        }

        private async Task<bool> HasPassedQuizAsync(string userId, string quizId)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null) return false;

            var bestAttempt = await _context.QuizAttempts
                .Where(qa => qa.UserId == userId && qa.QuizId == quizId)
                .OrderByDescending(qa => qa.Score)
                .FirstOrDefaultAsync();

            return bestAttempt != null && bestAttempt.Score >= (quiz.PassingScore ?? 70);
        }

        private async Task<decimal?> GetBestQuizScoreAsync(string userId, string quizId)
        {
            var bestAttempt = await _context.QuizAttempts
                .Where(qa => qa.UserId == userId && qa.QuizId == quizId)
                .OrderByDescending(qa => qa.Score)
                .FirstOrDefaultAsync();

            return bestAttempt?.Score;
        }

        private int GetQuizAttemptsUsed(string userId, string quizId)
        {
            return _context.QuizAttempts
                .Count(qa => qa.UserId == userId && qa.QuizId == quizId);
        }

        private async Task<string> GetCourseIdFromLessonAsync(string lessonId)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Chapter)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            return lesson?.Chapter.CourseId ?? "";
        }

        private async Task<TimeSpan> CalculateEstimatedTimeRemainingAsync(string userId, string courseId)
        {
            // Simple calculation based on remaining lessons (assuming 10 minutes per lesson)
            var totalLessons = await _context.Lessons
                .CountAsync(l => l.Chapter.CourseId == courseId);

            var completedLessons = await _context.UserProgresses
                .CountAsync(up => up.UserId == userId &&
                                 up.Lesson.Chapter.CourseId == courseId &&
                                 up.IsCompleted == true);

            var remainingLessons = totalLessons - completedLessons;
            return TimeSpan.FromMinutes(remainingLessons * 10); // 10 minutes per lesson estimate
        }

        #endregion
    }
}