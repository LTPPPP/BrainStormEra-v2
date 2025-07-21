using Microsoft.Extensions.Logging;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System;
using System.Linq;

namespace BusinessLogicLayer.Services.Implementations
{
    public class AchievementService : IAchievementService
    {
        private readonly IAchievementRepo _achievementRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly IUserRepo _userRepo;
        private readonly IAchievementUnlockService _achievementUnlockService;
        private readonly IAchievementIconService _achievementIconService;
        private readonly IMemoryCache _cache;
        private readonly IUserContextService _userContextService;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(
            IAchievementRepo achievementRepo,
            ICourseRepo courseRepo,
            IUserRepo userRepo,
            IAchievementUnlockService achievementUnlockService,
            IAchievementIconService achievementIconService,
            IMemoryCache cache,
            IUserContextService userContextService,
            ILogger<AchievementService> logger)
        {
            _achievementRepo = achievementRepo;
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _achievementUnlockService = achievementUnlockService;
            _achievementIconService = achievementIconService;
            _cache = cache;
            _userContextService = userContextService;
            _logger = logger;
        }

        public async Task<List<UserAchievement>> GetUserAchievementsAsync(string userId)
        {
            try
            {
                // Check for new achievements before returning
                await _achievementUnlockService.CheckAllAchievementsAsync(userId);
                return await _achievementRepo.GetUserAchievementsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievements for user {UserId}", userId);
                return new List<UserAchievement>();
            }
        }

        public async Task<object?> GetAchievementDetailAsync(string achievementId, string userId)
        {
            try
            {
                var userAchievement = await _achievementRepo.GetUserAchievementAsync(userId, achievementId);
                if (userAchievement == null)
                    return null;

                string? relatedCourseName = null;
                if (!string.IsNullOrEmpty(userAchievement.RelatedCourseId))
                {
                    relatedCourseName = await _achievementRepo.GetCourseNameAsync(userAchievement.RelatedCourseId);
                }

                return new
                {
                    userAchievement.Achievement.AchievementName,
                    userAchievement.Achievement.AchievementDescription,
                    userAchievement.Achievement.AchievementIcon,
                    userAchievement.ReceivedDate,
                    userAchievement.PointsEarned,
                    RelatedCourseName = relatedCourseName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievement detail {AchievementId} for user {UserId}", achievementId, userId);
                return null;
            }
        }

        public async Task AssignAchievementsAsync(string userId)
        {
            try
            {
                // Use the new achievement unlock service
                await _achievementUnlockService.CheckAllAchievementsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning achievements for user {UserId}", userId);
            }
        }

        public async Task BulkAssignAchievementsAsync()
        {
            try
            {
                // Get all users who have enrollments - use repository method
                var userIds = await _userRepo.GetAllActiveUserIdsAsync();

                var tasks = userIds.Select(userId => _achievementUnlockService.CheckAllAchievementsAsync(userId));
                await Task.WhenAll(tasks);

                _logger.LogInformation("Bulk achievement assignment completed for {Count} users", userIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk achievement assignment");
            }
        }

        public async Task<AchievementListViewModel> GetUserAchievementsAsync(string userId, string? search, int page, int pageSize)
        {
            try
            {
                // Check for new achievements before returning
                await _achievementUnlockService.CheckAllAchievementsAsync(userId);

                var userAchievements = await _achievementRepo.GetUserAchievementsAsync(userId, search, page, pageSize);
                var totalCount = await _achievementRepo.GetUserAchievementsCountAsync(userId, search);

                var achievementViewModels = userAchievements.Select(ua => new AchievementSummaryViewModel
                {
                    AchievementId = ua.AchievementId,
                    AchievementName = ua.Achievement.AchievementName ?? "",
                    AchievementDescription = ua.Achievement.AchievementDescription ?? "",
                    AchievementIcon = ua.Achievement.AchievementIcon ?? GetAchievementIcon(ua.Achievement.AchievementType ?? ""),
                    AchievementType = ua.Achievement.AchievementType ?? "",
                    PointsReward = ua.Achievement.PointsReward ?? 0,
                    ReceivedDate = ua.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                    RelatedCourseName = GetCourseNameFromCache(ua.RelatedCourseId)
                }).ToList();

                return new AchievementListViewModel
                {
                    Achievements = achievementViewModels,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalAchievements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    SearchQuery = search
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievements for user {UserId}", userId);
                return new AchievementListViewModel
                {
                    Achievements = new List<AchievementSummaryViewModel>(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalAchievements = 0,
                    TotalPages = 0,
                    SearchQuery = search
                };
            }
        }

        public async Task<List<Achievement>> CheckCourseCompletionAchievementsAsync(string userId, string courseId)
        {
            try
            {
                return await _achievementUnlockService.CheckCourseCompletionAchievementsAsync(userId, courseId);
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
                return await _achievementUnlockService.CheckQuizAchievementsAsync(userId, quizId, score, isPassed);
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
                return await _achievementUnlockService.CheckStreakAchievementsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking streak achievements for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        public async Task<List<Achievement>> GetNewlyUnlockedAchievementsAsync(string userId, TimeSpan timeWindow)
        {
            try
            {
                return await _achievementRepo.GetNewlyUnlockedAchievementsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting newly unlocked achievements for user {UserId}", userId);
                return new List<Achievement>();
            }
        }

        private string? GetCourseNameFromCache(string? courseId)
        {
            if (string.IsNullOrEmpty(courseId))
                return null;

            var cacheKey = $"course_name_{courseId}";
            if (_cache.TryGetValue(cacheKey, out string? courseName))
                return courseName;

            // This would need to be implemented in the repository
            // For now, return null
            return null;
        }

        private static bool ShouldAssignAchievement(Achievement achievement, int completedCourses)
        {
            return achievement.AchievementType == "course_completion" && completedCourses >= 1;
        }

        public async Task<AdminAchievementsViewModel> GetAllAchievementsAsync(string? search = null, string? typeFilter = null, string? pointsFilter = null, int page = 1, int pageSize = 12, string? sortBy = "date_desc")
        {
            try
            {
                var achievements = await _achievementRepo.GetAllAchievementsAsync();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    achievements = achievements.Where(a =>
                        a.AchievementName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true ||
                        a.AchievementDescription?.Contains(search, StringComparison.OrdinalIgnoreCase) == true ||
                        a.AchievementType?.Contains(search, StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                if (!string.IsNullOrEmpty(typeFilter))
                {
                    achievements = achievements.Where(a => a.AchievementType == typeFilter).ToList();
                }

                if (!string.IsNullOrEmpty(pointsFilter))
                {
                    var points = int.Parse(pointsFilter);
                    achievements = achievements.Where(a => a.PointsReward == points).ToList();
                }

                // Apply sorting
                achievements = sortBy switch
                {
                    "name_asc" => achievements.OrderBy(a => a.AchievementName).ToList(),
                    "name_desc" => achievements.OrderByDescending(a => a.AchievementName).ToList(),
                    "type_asc" => achievements.OrderBy(a => a.AchievementType).ToList(),
                    "type_desc" => achievements.OrderByDescending(a => a.AchievementType).ToList(),
                    "points_asc" => achievements.OrderBy(a => a.PointsReward).ToList(),
                    "points_desc" => achievements.OrderByDescending(a => a.PointsReward).ToList(),
                    "date_asc" => achievements.OrderBy(a => a.AchievementCreatedAt).ToList(),
                    "date_desc" => achievements.OrderByDescending(a => a.AchievementCreatedAt).ToList(),
                    _ => achievements.OrderByDescending(a => a.AchievementCreatedAt).ToList()
                };

                var totalCount = achievements.Count;
                var pagedAchievements = achievements
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var achievementViewModels = pagedAchievements.Select(a => new AdminAchievementViewModel
                {
                    AchievementId = a.AchievementId,
                    AchievementName = a.AchievementName ?? "",
                    AchievementDescription = a.AchievementDescription ?? "",
                    AchievementType = a.AchievementType ?? "",
                    AchievementIcon = a.AchievementIcon ?? GetAchievementIcon(a.AchievementType ?? ""),
                    PointsReward = a.PointsReward ?? 0,
                    AchievementCreatedAt = a.AchievementCreatedAt,
                    LastUpdatedAt = a.AchievementCreatedAt, // Use CreatedAt as fallback
                    IsActive = true // Default to active since there's no status field
                }).ToList();

                return new AdminAchievementsViewModel
                {
                    Achievements = achievementViewModels,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalAchievements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    SearchQuery = search,
                    TypeFilter = typeFilter,
                    PointsFilter = pointsFilter,
                    SortBy = sortBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all achievements for admin");
                return new AdminAchievementsViewModel
                {
                    Achievements = new List<AdminAchievementViewModel>(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalAchievements = 0,
                    TotalPages = 0
                };
            }
        }

        public async Task<AdminAchievementViewModel?> GetAchievementByIdAsync(string achievementId)
        {
            try
            {
                var achievement = await _achievementRepo.GetByIdAsync(achievementId);
                if (achievement == null)
                    return null;

                return new AdminAchievementViewModel
                {
                    AchievementId = achievement.AchievementId,
                    AchievementName = achievement.AchievementName ?? "",
                    AchievementDescription = achievement.AchievementDescription ?? "",
                    AchievementType = achievement.AchievementType ?? "",
                    AchievementIcon = achievement.AchievementIcon ?? GetAchievementIcon(achievement.AchievementType ?? ""),
                    PointsReward = achievement.PointsReward ?? 0,
                    AchievementCreatedAt = achievement.AchievementCreatedAt,
                    LastUpdatedAt = achievement.AchievementCreatedAt, // Use CreatedAt as fallback
                    IsActive = true // Default to active since there's no status field
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievement by ID {AchievementId}", achievementId);
                return null;
            }
        }

        public async Task<(bool Success, string? AchievementId)> CreateAchievementAsync(CreateAchievementRequest request, string? adminId = null)
        {
            try
            {
                var achievement = new Achievement
                {
                    AchievementId = Guid.NewGuid().ToString(),
                    AchievementName = request.AchievementName,
                    AchievementDescription = request.AchievementDescription,
                    AchievementType = request.AchievementType,
                    AchievementIcon = request.AchievementIcon,
                    PointsReward = 0, // Default to 0 since PointsReward is not in the request
                    AchievementCreatedAt = DateTime.UtcNow
                };

                var achievementId = await _achievementRepo.CreateAchievementAsync(achievement);
                return (true, achievementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating achievement");
                return (false, null);
            }
        }

        public async Task<bool> UpdateAchievementAsync(UpdateAchievementRequest request, string? adminId = null)
        {
            try
            {
                var achievement = await _achievementRepo.GetByIdAsync(request.AchievementId);
                if (achievement == null)
                    return false;

                achievement.AchievementName = request.AchievementName;
                achievement.AchievementDescription = request.AchievementDescription;
                achievement.AchievementType = request.AchievementType;
                achievement.AchievementIcon = request.AchievementIcon;
                // Note: AchievementCreatedAt is not updated as it's the creation timestamp

                return await _achievementRepo.UpdateAchievementAsync(achievement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating achievement {AchievementId}", request.AchievementId);
                return false;
            }
        }

        public async Task<bool> DeleteAchievementAsync(string achievementId, string? adminId = null)
        {
            try
            {
                return await _achievementRepo.DeleteAchievementAsync(achievementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting achievement {AchievementId}", achievementId);
                return false;
            }
        }

        public async Task<(bool Success, string? IconPath, string? ErrorMessage)> UploadAchievementIconAsync(IFormFile file, string achievementId, string? adminId = null)
        {
            try
            {
                return await _achievementIconService.UploadAchievementIconAsync(file, achievementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading achievement icon for achievement {AchievementId}", achievementId);
                return (false, null, "Error uploading icon");
            }
        }

        public async Task<GetLearnerAchievementsResult> GetLearnerAchievementsAsync(ClaimsPrincipal user, string? search, int page, int pageSize)
        {
            try
            {
                var userId = _userContextService.GetCurrentUserId(user);
                if (string.IsNullOrEmpty(userId))
                {
                    return new GetLearnerAchievementsResult
                    {
                        IsSuccess = false,
                        RedirectToLogin = true,
                        ErrorMessage = "User not authenticated"
                    };
                }

                var achievementList = await GetUserAchievementsAsync(userId, search, page, pageSize);

                return new GetLearnerAchievementsResult
                {
                    IsSuccess = true,
                    AchievementList = achievementList,
                    UserId = userId,
                    HasAchievements = achievementList.Achievements.Any(),
                    TotalAchievements = achievementList.TotalAchievements,
                    SearchQuery = search
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting learner achievements");
                return new GetLearnerAchievementsResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Error retrieving achievements"
                };
            }
        }

        public async Task<GetAchievementDetailsResult> GetAchievementDetailsAsync(string achievementId, string userId)
        {
            try
            {
                var achievement = await GetAchievementDetailAsync(achievementId, userId);

                return new GetAchievementDetailsResult
                {
                    IsSuccess = true,
                    Achievement = achievement
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievement details for achievement {AchievementId}, user {UserId}", achievementId, userId);
                return new GetAchievementDetailsResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Error retrieving achievement details"
                };
            }
        }

        private string GetAchievementIcon(string achievementType)
        {
            return achievementType switch
            {
                "course_completion" => "🎓",
                "quiz_mastery" => "🧠",
                "streak" => "🔥",
                "social" => "👥",
                "first_course" => "🥇",
                "perfect_score" => "💯",
                _ => "🏆"
            };
        }
    }

    public class GetLearnerAchievementsResult
    {
        public bool IsSuccess { get; set; }
        public AchievementListViewModel? AchievementList { get; set; }
        public string? UserId { get; set; }
        public bool HasAchievements { get; set; }
        public int TotalAchievements { get; set; }
        public string? SearchQuery { get; set; }
        public bool RedirectToLogin { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class GetAchievementDetailsResult
    {
        public bool IsSuccess { get; set; }
        public object? Achievement { get; set; }
        public string? ErrorMessage { get; set; }
    }
}








