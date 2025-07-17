using Microsoft.Extensions.Logging;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly IAchievementRepo _achievementRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly IAchievementUnlockService _achievementUnlockService;
        private readonly IAchievementIconService _achievementIconService;
        private readonly BrainStormEraContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(
            IAchievementRepo achievementRepo,
            ICourseRepo courseRepo,
            IAchievementUnlockService achievementUnlockService,
            IAchievementIconService achievementIconService,
            BrainStormEraContext context,
            IMemoryCache cache,
            ILogger<AchievementService> logger)
        {
            _achievementRepo = achievementRepo;
            _courseRepo = courseRepo;
            _achievementUnlockService = achievementUnlockService;
            _achievementIconService = achievementIconService;
            _context = context;
            _cache = cache;
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
                // Get all users who have enrollments
                var userIds = await _context.Enrollments
                    .Select(e => e.UserId)
                    .Distinct()
                    .ToListAsync();

                var tasks = userIds.Select(userId => _achievementUnlockService.CheckAllAchievementsAsync(userId));
                await Task.WhenAll(tasks);

                _logger.LogInformation("Bulk achievement assignment completed for {UserCount} users", userIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk assigning achievements");
            }
        }

        public async Task<AchievementListViewModel> GetUserAchievementsAsync(string userId, string? search, int page, int pageSize)
        {
            try
            {
                // Check for new achievements before getting the list
                await _achievementUnlockService.CheckAllAchievementsAsync(userId);

                var cacheKey = $"UserAchievementsList_{userId}_{search}_{page}_{pageSize}";
                if (_cache.TryGetValue(cacheKey, out AchievementListViewModel? cached))
                    return cached!;

                var userAchievements = await _achievementRepo.GetUserAchievementsAsync(userId, search, page, pageSize);
                var totalCount = await _achievementRepo.GetUserAchievementsCountAsync(userId, search);

                var achievements = userAchievements.Select(ua => new AchievementSummaryViewModel
                {
                    AchievementId = ua.AchievementId,
                    AchievementName = ua.Achievement.AchievementName,
                    AchievementDescription = ua.Achievement.AchievementDescription ?? "",
                    AchievementIcon = ua.Achievement.AchievementIcon ?? MediaConstants.Defaults.DefaultAchievementPath,
                    AchievementType = ua.Achievement.AchievementType ?? "",
                    PointsReward = ua.Achievement.PointsReward,
                    ReceivedDate = ua.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                    RelatedCourseName = ua.RelatedCourseId != null ? GetCourseNameFromCache(ua.RelatedCourseId) : null
                }).ToList();

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var result = new AchievementListViewModel
                {
                    Achievements = achievements,
                    SearchQuery = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalAchievements = totalCount,
                    TotalPages = totalPages
                };

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    Size = 1 // Each cache entry takes 1 unit of size
                };
                _cache.Set(cacheKey, result, cacheOptions);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated achievements for user {UserId}", userId);
                return new AchievementListViewModel
                {
                    Achievements = new List<AchievementSummaryViewModel>(),
                    SearchQuery = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalAchievements = 0,
                    TotalPages = 0
                };
            }
        }

        /// <summary>
        /// Check and unlock achievements for course completion
        /// </summary>
        public async Task<List<Achievement>> CheckCourseCompletionAchievementsAsync(string userId, string courseId)
        {
            return await _achievementUnlockService.CheckCourseCompletionAchievementsAsync(userId, courseId);
        }

        /// <summary>
        /// Check and unlock achievements for quiz performance
        /// </summary>
        public async Task<List<Achievement>> CheckQuizAchievementsAsync(string userId, string quizId, decimal score, bool isPassed)
        {
            return await _achievementUnlockService.CheckQuizAchievementsAsync(userId, quizId, score, isPassed);
        }

        /// <summary>
        /// Check and unlock achievements for learning streak
        /// </summary>
        public async Task<List<Achievement>> CheckStreakAchievementsAsync(string userId)
        {
            return await _achievementUnlockService.CheckStreakAchievementsAsync(userId);
        }

        /// <summary>
        /// Get newly unlocked achievements for a user
        /// </summary>
        public async Task<List<Achievement>> GetNewlyUnlockedAchievementsAsync(string userId, TimeSpan timeWindow)
        {
            return await _achievementUnlockService.GetNewlyUnlockedAchievementsAsync(userId, timeWindow);
        }

        private string? GetCourseNameFromCache(string courseId)
        {
            var cacheKey = $"CourseName_{courseId}";
            return _cache.TryGetValue(cacheKey, out string? courseName) ? courseName : null;
        }

        private static bool ShouldAssignAchievement(Achievement achievement, int completedCourses)
        {
            return int.TryParse(achievement.AchievementDescription, out int required) && completedCourses >= required;
        }

        // Admin Achievement Management Methods
        public async Task<AdminAchievementsViewModel> GetAllAchievementsAsync(string? search = null, string? typeFilter = null, string? pointsFilter = null, int page = 1, int pageSize = 12, string? sortBy = "date_desc")
        {
            try
            {
                var allAchievements = await _achievementRepo.GetAllAchievementsAsync();

                // Apply filters
                var filteredAchievements = allAchievements.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    filteredAchievements = filteredAchievements.Where(a =>
                        a.AchievementName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (a.AchievementDescription != null && a.AchievementDescription.Contains(search, StringComparison.OrdinalIgnoreCase)));
                }

                if (!string.IsNullOrEmpty(typeFilter))
                {
                    filteredAchievements = filteredAchievements.Where(a =>
                        a.AchievementType != null && a.AchievementType.Equals(typeFilter, StringComparison.OrdinalIgnoreCase));
                }

                var achievements = filteredAchievements.ToList();
                var totalAchievements = achievements.Count;

                // Apply sorting
                var sortedAchievements = sortBy?.ToLower() switch
                {
                    "name_asc" => achievements.OrderBy(a => a.AchievementName),
                    "name_desc" => achievements.OrderByDescending(a => a.AchievementName),
                    "date_asc" => achievements.OrderBy(a => a.AchievementCreatedAt),
                    "date_desc" => achievements.OrderByDescending(a => a.AchievementCreatedAt),
                    "type_asc" => achievements.OrderBy(a => a.AchievementType),
                    "type_desc" => achievements.OrderByDescending(a => a.AchievementType),
                    "awarded_desc" => achievements.OrderByDescending(a => a.UserAchievements?.Count ?? 0),
                    "awarded_asc" => achievements.OrderBy(a => a.UserAchievements?.Count ?? 0),
                    _ => achievements.OrderByDescending(a => a.AchievementCreatedAt) // default to newest first
                };

                // Apply pagination
                var paginatedAchievements = sortedAchievements
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Calculate statistics
                var courseAchievements = allAchievements.Count(a => a.AchievementType?.Equals("course_completion", StringComparison.OrdinalIgnoreCase) == true || a.AchievementType?.Equals("first_course", StringComparison.OrdinalIgnoreCase) == true);
                var quizAchievements = allAchievements.Count(a => a.AchievementType?.Equals("quiz_master", StringComparison.OrdinalIgnoreCase) == true);
                var specialAchievements = allAchievements.Count(a => a.AchievementType?.Equals("instructor", StringComparison.OrdinalIgnoreCase) == true || a.AchievementType?.Equals("student_engagement", StringComparison.OrdinalIgnoreCase) == true);
                var milestoneAchievements = allAchievements.Count(a => a.AchievementType?.Equals("streak", StringComparison.OrdinalIgnoreCase) == true);

                // Calculate total times awarded (would need user achievements data, using placeholder for now)
                var totalAwarded = 0; // This would require joining with UserAchievement table

                return new AdminAchievementsViewModel
                {
                    Achievements = paginatedAchievements.Select(a => new AdminAchievementViewModel
                    {
                        AchievementId = a.AchievementId,
                        AchievementName = a.AchievementName,
                        AchievementDescription = a.AchievementDescription ?? "",
                        AchievementIcon = a.AchievementIcon ?? "fas fa-trophy",
                        AchievementType = a.AchievementType ?? "general",
                        PointsReward = a.PointsReward ?? 0,
                        AchievementCreatedAt = a.AchievementCreatedAt,
                        IsActive = true, // Add this field to Achievement model if needed
                        TimesAwarded = a.UserAchievements?.Count ?? 0 // Get actual count from UserAchievements
                    }).ToList(),
                    SearchQuery = search,
                    TypeFilter = typeFilter,
                    PointsFilter = pointsFilter,
                    SortBy = sortBy,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalAchievements / pageSize),
                    TotalAchievements = allAchievements.Count,
                    CourseAchievements = courseAchievements,
                    QuizAchievements = quizAchievements,
                    SpecialAchievements = specialAchievements,
                    MilestoneAchievements = milestoneAchievements,
                    TotalAwarded = totalAwarded
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all achievements with search: {Search}, typeFilter: {TypeFilter}, pointsFilter: {PointsFilter}, page: {Page}, pageSize: {PageSize}",
                    search, typeFilter, pointsFilter, page, pageSize);
                throw;
            }
        }

        public async Task<AdminAchievementViewModel?> GetAchievementByIdAsync(string achievementId)
        {
            try
            {
                var achievement = await _achievementRepo.GetByIdAsync(achievementId);
                if (achievement == null) return null;

                return new AdminAchievementViewModel
                {
                    AchievementId = achievement.AchievementId,
                    AchievementName = achievement.AchievementName,
                    AchievementDescription = achievement.AchievementDescription ?? "",
                    AchievementIcon = achievement.AchievementIcon ?? "fas fa-trophy",
                    AchievementType = achievement.AchievementType ?? "general",
                    PointsReward = achievement.PointsReward ?? 0,
                    AchievementCreatedAt = achievement.AchievementCreatedAt,
                    IsActive = true,
                    TimesAwarded = achievement.UserAchievements?.Count ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievement by ID: {AchievementId}", achievementId);
                throw;
            }
        }

        public async Task<(bool Success, string? AchievementId)> CreateAchievementAsync(CreateAchievementRequest request, string? adminId = null)
        {
            try
            {
                var achievementId = Guid.NewGuid().ToString();
                var achievement = new Achievement
                {
                    AchievementId = achievementId,
                    AchievementName = request.AchievementName,
                    AchievementDescription = request.AchievementDescription,
                    AchievementIcon = request.AchievementIcon,
                    AchievementType = request.AchievementType,
                    AchievementCreatedAt = DateTime.UtcNow
                };

                var result = await _achievementRepo.CreateAchievementAsync(achievement);
                var success = !string.IsNullOrEmpty(result);
                return (success, success ? achievementId : null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating achievement: {AchievementName}", request.AchievementName);
                throw;
            }
        }

        public async Task<bool> UpdateAchievementAsync(UpdateAchievementRequest request, string? adminId = null)
        {
            try
            {
                var existingAchievement = await _achievementRepo.GetByIdAsync(request.AchievementId);
                if (existingAchievement == null) return false;

                existingAchievement.AchievementName = request.AchievementName;
                existingAchievement.AchievementDescription = request.AchievementDescription;
                existingAchievement.AchievementIcon = request.AchievementIcon;
                existingAchievement.AchievementType = request.AchievementType;

                return await _achievementRepo.UpdateAchievementAsync(existingAchievement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating achievement: {AchievementId}", request.AchievementId);
                throw;
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
                _logger.LogError(ex, "Error deleting achievement: {AchievementId}", achievementId);
                throw;
            }
        }

        public async Task<(bool Success, string? IconPath, string? ErrorMessage)> UploadAchievementIconAsync(IFormFile file, string achievementId, string? adminId = null)
        {
            try
            {
                // Check if achievement exists
                var achievement = await _achievementRepo.GetByIdAsync(achievementId);
                if (achievement == null)
                {
                    return (false, null, "Achievement not found.");
                }

                // Upload the icon
                var uploadResult = await _achievementIconService.UploadAchievementIconAsync(file, achievementId);
                if (!uploadResult.Success)
                {
                    return uploadResult;
                }

                // Update achievement with new icon path
                var updateResult = await _achievementRepo.UpdateAchievementIconAsync(achievementId, uploadResult.IconPath!);
                if (!updateResult)
                {
                    // Clean up uploaded file if database update fails
                    await _achievementIconService.DeleteAchievementIconAsync(uploadResult.IconPath);
                    return (false, null, "Failed to update achievement icon in database.");
                }

                _logger.LogInformation("Achievement icon uploaded successfully: {AchievementId} by admin {AdminId}", achievementId, adminId);
                return (true, uploadResult.IconPath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading achievement icon: {AchievementId}", achievementId);
                return (false, null, "An error occurred while uploading the achievement icon.");
            }
        }
    }
}








