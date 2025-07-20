using Microsoft.Extensions.Logging;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Implementation for handling automatic achievement unlocking based on user activities
    /// </summary>
    public class AchievementUnlockService : IAchievementUnlockService
    {
        private readonly BrainStormEraContext _context;
        private readonly IAchievementRepo _achievementRepo;
        private readonly IAchievementNotificationService _notificationService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AchievementUnlockService> _logger;

        public AchievementUnlockService(
            BrainStormEraContext context,
            IAchievementRepo achievementRepo,
            IAchievementNotificationService notificationService,
            IMemoryCache cache,
            ILogger<AchievementUnlockService> logger)
        {
            _context = context;
            _achievementRepo = achievementRepo;
            _notificationService = notificationService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Achievement>> CheckCourseCompletionAchievementsAsync(string userId, string courseId)
        {
            try
            {
                var unlockedAchievements = new List<Achievement>();

                // Get course completion achievements
                var courseCompletionAchievements = await _achievementRepo.GetAchievementsByTypeAsync("course_completion");
                var firstCourseAchievements = await _achievementRepo.GetAchievementsByTypeAsync("first_course");

                var allAchievements = courseCompletionAchievements.Concat(firstCourseAchievements).ToList();

                // Get user's completed courses count
                var completedCoursesCount = await _context.Enrollments
                    .CountAsync(e => e.UserId == userId && e.CertificateIssuedDate.HasValue);

                // Get user's total courses count
                var totalCoursesCount = await _context.Enrollments
                    .CountAsync(e => e.UserId == userId);

                foreach (var achievement in allAchievements)
                {
                    if (await _achievementRepo.HasUserAchievementAsync(userId, achievement.AchievementId))
                        continue;

                    bool shouldUnlock = false;

                    // Check first course achievement
                    if (achievement.AchievementType == "first_course" && completedCoursesCount == 1)
                    {
                        shouldUnlock = true;
                    }
                    // Check course completion achievements based on description
                    else if (achievement.AchievementType == "course_completion" &&
                             !string.IsNullOrEmpty(achievement.AchievementDescription))
                    {
                        if (int.TryParse(achievement.AchievementDescription, out int requiredCourses))
                        {
                            if (completedCoursesCount >= requiredCourses)
                            {
                                shouldUnlock = true;
                            }
                        }
                    }

                    if (shouldUnlock)
                    {
                        var success = await ProcessAchievementUnlockAsync(userId, achievement.AchievementId, courseId);
                        if (success)
                        {
                            unlockedAchievements.Add(achievement);
                        }
                    }
                }

                return unlockedAchievements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking course completion achievements for user {UserId}, course {CourseId}", userId, courseId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> CheckQuizAchievementsAsync(string userId, string quizId, decimal score, bool isPassed)
        {
            try
            {
                var unlockedAchievements = new List<Achievement>();

                // Get quiz mastery achievements
                var quizAchievements = await _achievementRepo.GetAchievementsByTypeAsync("quiz_master");

                foreach (var achievement in quizAchievements)
                {
                    if (await _achievementRepo.HasUserAchievementAsync(userId, achievement.AchievementId))
                        continue;

                    bool shouldUnlock = false;

                    // Check quiz performance achievements
                    if (!string.IsNullOrEmpty(achievement.AchievementDescription))
                    {
                        // Parse achievement requirements from description
                        var requirements = ParseQuizRequirements(achievement.AchievementDescription);

                        if (requirements.HasValue)
                        {
                            var (requiredQuizzes, requiredScore) = requirements.Value;

                            // Get user's quiz performance statistics
                            var userQuizStats = await GetUserQuizStatisticsAsync(userId, requiredScore);

                            if (userQuizStats.PassedQuizzesCount >= requiredQuizzes)
                            {
                                shouldUnlock = true;
                            }
                        }
                    }

                    if (shouldUnlock)
                    {
                        var success = await ProcessAchievementUnlockAsync(userId, achievement.AchievementId);
                        if (success)
                        {
                            unlockedAchievements.Add(achievement);
                        }
                    }
                }

                return unlockedAchievements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking quiz achievements for user {UserId}, quiz {QuizId}", userId, quizId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> CheckStreakAchievementsAsync(string userId)
        {
            try
            {
                var unlockedAchievements = new List<Achievement>();

                // Get streak achievements
                var streakAchievements = await _achievementRepo.GetAchievementsByTypeAsync("streak");

                // Get user's learning streak
                var currentStreak = await CalculateLearningStreakAsync(userId);

                foreach (var achievement in streakAchievements)
                {
                    if (await _achievementRepo.HasUserAchievementAsync(userId, achievement.AchievementId))
                        continue;

                    bool shouldUnlock = false;

                    if (!string.IsNullOrEmpty(achievement.AchievementDescription))
                    {
                        if (int.TryParse(achievement.AchievementDescription, out int requiredDays))
                        {
                            if (currentStreak >= requiredDays)
                            {
                                shouldUnlock = true;
                            }
                        }
                    }

                    if (shouldUnlock)
                    {
                        var success = await ProcessAchievementUnlockAsync(userId, achievement.AchievementId);
                        if (success)
                        {
                            unlockedAchievements.Add(achievement);
                        }
                    }
                }

                return unlockedAchievements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking streak achievements for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> CheckInstructorAchievementsAsync(string userId)
        {
            try
            {
                var unlockedAchievements = new List<Achievement>();

                // Get instructor achievements
                var instructorAchievements = await _achievementRepo.GetAchievementsByTypeAsync("instructor");

                // Get user's instructor statistics
                var instructorStats = await GetInstructorStatisticsAsync(userId);

                foreach (var achievement in instructorAchievements)
                {
                    if (await _achievementRepo.HasUserAchievementAsync(userId, achievement.AchievementId))
                        continue;

                    bool shouldUnlock = false;

                    if (!string.IsNullOrEmpty(achievement.AchievementDescription))
                    {
                        // Parse instructor requirements
                        var requirements = ParseInstructorRequirements(achievement.AchievementDescription);

                        if (requirements.HasValue)
                        {
                            var (requiredCourses, requiredStudents) = requirements.Value;

                            if (instructorStats.PublishedCourses >= requiredCourses &&
                                instructorStats.TotalStudents >= requiredStudents)
                            {
                                shouldUnlock = true;
                            }
                        }
                    }

                    if (shouldUnlock)
                    {
                        var success = await ProcessAchievementUnlockAsync(userId, achievement.AchievementId);
                        if (success)
                        {
                            unlockedAchievements.Add(achievement);
                        }
                    }
                }

                return unlockedAchievements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking instructor achievements for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> CheckEngagementAchievementsAsync(string userId)
        {
            try
            {
                var unlockedAchievements = new List<Achievement>();

                // Get student engagement achievements
                var engagementAchievements = await _achievementRepo.GetAchievementsByTypeAsync("student_engagement");

                // Get user's engagement statistics
                var engagementStats = await GetEngagementStatisticsAsync(userId);

                foreach (var achievement in engagementAchievements)
                {
                    if (await _achievementRepo.HasUserAchievementAsync(userId, achievement.AchievementId))
                        continue;

                    bool shouldUnlock = false;

                    if (!string.IsNullOrEmpty(achievement.AchievementDescription))
                    {
                        // Parse engagement requirements
                        var requirements = ParseEngagementRequirements(achievement.AchievementDescription);

                        if (requirements.HasValue)
                        {
                            var (requiredFeedback, requiredTime) = requirements.Value;

                            if (engagementStats.FeedbackCount >= requiredFeedback &&
                                engagementStats.TotalLearningTime >= requiredTime)
                            {
                                shouldUnlock = true;
                            }
                        }
                    }

                    if (shouldUnlock)
                    {
                        var success = await ProcessAchievementUnlockAsync(userId, achievement.AchievementId);
                        if (success)
                        {
                            unlockedAchievements.Add(achievement);
                        }
                    }
                }

                return unlockedAchievements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking engagement achievements for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> CheckAllAchievementsAsync(string userId)
        {
            try
            {
                var allUnlockedAchievements = new List<Achievement>();

                // Check all types of achievements
                var courseAchievements = await CheckCourseCompletionAchievementsAsync(userId, "");
                var quizAchievements = await CheckQuizAchievementsAsync(userId, "", 0, false);
                var streakAchievements = await CheckStreakAchievementsAsync(userId);
                var instructorAchievements = await CheckInstructorAchievementsAsync(userId);
                var engagementAchievements = await CheckEngagementAchievementsAsync(userId);

                allUnlockedAchievements.AddRange(courseAchievements);
                allUnlockedAchievements.AddRange(quizAchievements);
                allUnlockedAchievements.AddRange(streakAchievements);
                allUnlockedAchievements.AddRange(instructorAchievements);
                allUnlockedAchievements.AddRange(engagementAchievements);

                return allUnlockedAchievements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking all achievements for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> GetNewlyUnlockedAchievementsAsync(string userId, TimeSpan timeWindow)
        {
            try
            {
                var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.Subtract(timeWindow));

                var newlyUnlocked = await _context.UserAchievements
                    .Include(ua => ua.Achievement)
                    .Where(ua => ua.UserId == userId && ua.ReceivedDate >= cutoffDate)
                    .Select(ua => ua.Achievement)
                    .ToListAsync();

                return newlyUnlocked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting newly unlocked achievements for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        public async Task<bool> ProcessAchievementUnlockAsync(string userId, string achievementId, string? relatedCourseId = null, string? enrollmentId = null)
        {
            try
            {
                // Check if already unlocked
                if (await _achievementRepo.HasUserAchievementAsync(userId, achievementId))
                {
                    return false;
                }

                // Get achievement details
                var achievement = await _achievementRepo.GetByIdAsync(achievementId);
                if (achievement == null)
                {
                    _logger.LogWarning("Achievement {AchievementId} not found for user {UserId}", achievementId, userId);
                    return false;
                }

                // Create user achievement record
                var userAchievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementId = achievementId,
                    ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    PointsEarned = achievement.PointsReward ?? 0,
                    RelatedCourseId = relatedCourseId,
                    EnrollmentId = enrollmentId
                };

                // Add to database
                var success = await _achievementRepo.AddUserAchievementAsync(userAchievement);

                if (success)
                {
                    _logger.LogInformation("Achievement {AchievementName} unlocked for user {UserId}",
                        achievement.AchievementName, userId);

                    // Clear cache
                    var cacheKey = $"UserAchievements_{userId}";
                    _cache.Remove(cacheKey);

                    // Send notification
                    try
                    {
                        await _notificationService.SendAchievementNotificationAsync(userId, achievement, relatedCourseId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending achievement notification for user {UserId}, achievement {AchievementId}",
                            userId, achievementId);
                        // Don't fail the achievement unlock if notification fails
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing achievement unlock for user {UserId}, achievement {AchievementId}",
                    userId, achievementId);
                return false;
            }
        }

        #region Helper Methods

        private async Task<(int PassedQuizzesCount, decimal AverageScore)> GetUserQuizStatisticsAsync(string userId, decimal minScore)
        {
            try
            {
                var quizAttempts = await _context.QuizAttempts
                    .Where(qa => qa.UserId == userId && qa.IsPassed == true)
                    .ToListAsync();

                var passedQuizzesCount = quizAttempts.Count;
                var averageScore = passedQuizzesCount > 0 ? quizAttempts.Average(qa => qa.PercentageScore ?? 0) : 0;

                return (passedQuizzesCount, averageScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz statistics for user {UserId}", userId);
                return (0, 0);
            }
        }

        private async Task<int> CalculateLearningStreakAsync(string userId)
        {
            try
            {
                var userActivities = await _context.UserProgresses
                    .Where(up => up.UserId == userId && up.CompletedAt.HasValue)
                    .OrderByDescending(up => up.CompletedAt)
                    .ToListAsync();

                if (!userActivities.Any())
                    return 0;

                var streak = 0;
                var currentDate = DateTime.UtcNow.Date;
                var lastActivityDate = userActivities.First().CompletedAt?.Date;

                if (lastActivityDate == null)
                    return 0;

                // Check if last activity was today or yesterday
                if (lastActivityDate >= currentDate.AddDays(-1))
                {
                    streak = 1;
                    var checkDate = lastActivityDate.Value.AddDays(-1);

                    // Count consecutive days backwards
                    while (userActivities.Any(ua => ua.CompletedAt?.Date == checkDate))
                    {
                        streak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                }

                return streak;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating learning streak for user {UserId}", userId);
                return 0;
            }
        }

        private async Task<(int PublishedCourses, int TotalStudents)> GetInstructorStatisticsAsync(string userId)
        {
            try
            {
                var publishedCourses = await _context.Courses
                    .CountAsync(c => c.AuthorId == userId && c.CourseStatus == 1); // Assuming 1 = published

                var totalStudents = await _context.Enrollments
                    .Where(e => e.Course.AuthorId == userId)
                    .CountAsync();

                return (publishedCourses, totalStudents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor statistics for user {UserId}", userId);
                return (0, 0);
            }
        }

        private async Task<(int FeedbackCount, int TotalLearningTime)> GetEngagementStatisticsAsync(string userId)
        {
            try
            {
                var feedbackCount = await _context.Feedbacks
                    .CountAsync(f => f.UserId == userId);

                var totalLearningTime = await _context.QuizAttempts
                    .Where(qa => qa.UserId == userId)
                    .SumAsync(qa => qa.TimeSpent ?? 0);

                return (feedbackCount, totalLearningTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting engagement statistics for user {UserId}", userId);
                return (0, 0);
            }
        }

        private (int Quizzes, decimal Score)? ParseQuizRequirements(string description)
        {
            try
            {
                // Expected format: "Pass 10 quizzes with 90%+ score"
                var parts = description.Split(' ');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int quizzes))
                {
                    var scorePart = parts.FirstOrDefault(p => p.Contains('%'));
                    if (scorePart != null && decimal.TryParse(scorePart.TrimEnd('%'), out decimal score))
                    {
                        return (quizzes, score);
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private (int Courses, int Students)? ParseInstructorRequirements(string description)
        {
            try
            {
                // Expected format: "Publish 5 courses with 100+ students"
                var parts = description.Split(' ');
                if (parts.Length >= 6 && int.TryParse(parts[1], out int courses))
                {
                    var studentsPart = parts.FirstOrDefault(p => p.Contains('+'));
                    if (studentsPart != null && int.TryParse(studentsPart.TrimEnd('+'), out int students))
                    {
                        return (courses, students);
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private (int Feedback, int Time)? ParseEngagementRequirements(string description)
        {
            try
            {
                // Expected format: "Submit 10 feedback and spend 50+ hours learning"
                var parts = description.Split(' ');
                if (parts.Length >= 4 && int.TryParse(parts[1], out int feedback))
                {
                    var timePart = parts.FirstOrDefault(p => p.Contains("hours"));
                    if (timePart != null)
                    {
                        var timeValue = timePart.Replace("hours", "").Trim();
                        if (int.TryParse(timeValue, out int time))
                        {
                            return (feedback, time);
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}