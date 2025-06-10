using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BrainStormEra_MVC.Services.Implementations
{
    public class AchievementServiceImpl
    {
        private readonly IAchievementService _achievementService;
        private readonly IUserContextService _userContextService;
        private readonly ILogger<AchievementServiceImpl> _logger;

        public AchievementServiceImpl(
            IAchievementService achievementService,
            IUserContextService userContextService,
            ILogger<AchievementServiceImpl> logger)
        {
            _achievementService = achievementService;
            _userContextService = userContextService;
            _logger = logger;
        }

        public async Task<GetLearnerAchievementsResult> GetLearnerAchievementsAsync(ClaimsPrincipal user, string? search, int page, int pageSize)
        {
            try
            {
                // Authentication check
                if (!_userContextService.IsAuthenticated(user))
                {
                    _logger.LogWarning("Unauthenticated user attempted to access learner achievements");
                    return new GetLearnerAchievementsResult
                    {
                        IsSuccess = false,
                        RedirectToLogin = true,
                        ErrorMessage = "User not authenticated"
                    };
                }

                var userId = user?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims for learner achievements request");
                    return new GetLearnerAchievementsResult
                    {
                        IsSuccess = false,
                        RedirectToLogin = true,
                        ErrorMessage = "User ID not found"
                    };
                }

                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 9;

                // Get achievements
                var achievementList = await _achievementService.GetUserAchievementsAsync(userId, search, page, pageSize);

                return new GetLearnerAchievementsResult
                {
                    IsSuccess = true,
                    AchievementList = achievementList,
                    UserId = userId,
                    HasAchievements = achievementList.HasAchievements,
                    TotalAchievements = achievementList.TotalAchievements,
                    SearchQuery = search
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting learner achievements for user search: {Search}, page: {Page}", search, page);
                return new GetLearnerAchievementsResult
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while loading achievements"
                };
            }
        }

        public async Task<GetAchievementDetailsResult> GetAchievementDetailsAsync(string achievementId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(achievementId) || string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Invalid achievement ID or user ID provided for achievement details: AchievementId={AchievementId}, UserId={UserId}", achievementId, userId);
                    return new GetAchievementDetailsResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid achievement or user ID"
                    };
                }

                var achievement = await _achievementService.GetAchievementDetailAsync(achievementId, userId);

                if (achievement == null)
                {
                    _logger.LogWarning("Achievement not found: AchievementId={AchievementId}, UserId={UserId}", achievementId, userId);
                    return new GetAchievementDetailsResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Achievement not found"
                    };
                }

                return new GetAchievementDetailsResult
                {
                    IsSuccess = true,
                    Achievement = achievement
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievement details: AchievementId={AchievementId}, UserId={UserId}", achievementId, userId);
                return new GetAchievementDetailsResult
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while loading achievement details"
                };
            }
        }
    }

    // Result classes for structured returns
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
