using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using System.Security.Claims;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class AchievementController : BaseController
    {
        private readonly IAchievementService _achievementService;
        private readonly IAchievementUnlockService _achievementUnlockService;
        private readonly ILogger<AchievementController> _logger;

        public AchievementController(
            IAchievementService achievementService,
            IAchievementUnlockService achievementUnlockService,
            ILogger<AchievementController> logger)
        {
            _achievementService = achievementService;
            _achievementUnlockService = achievementUnlockService;
            _logger = logger;
        }

        /// <summary>
        /// Display user's achievements page
        /// </summary>
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Check for new achievements
                await _achievementUnlockService.CheckAllAchievementsAsync(userId);

                var pageSize = 9;
                var achievementList = await _achievementService.GetUserAchievementsAsync(userId, search, page, pageSize);

                ViewBag.SearchQuery = search;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = achievementList.TotalPages;
                ViewBag.TotalAchievements = achievementList.TotalAchievements;

                return View(achievementList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading achievements for user");
                TempData["Error"] = "An error occurred while loading your achievements.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Display learner's achievements page (alias for Index)
        /// </summary>
        public async Task<IActionResult> LearnerAchievements(string? search, int page = 1)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Check for new achievements
                await _achievementUnlockService.CheckAllAchievementsAsync(userId);

                var pageSize = 9;
                var achievementList = await _achievementService.GetUserAchievementsAsync(userId, search, page, pageSize);

                ViewBag.SearchQuery = search;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = achievementList.TotalPages;
                ViewBag.TotalAchievements = achievementList.TotalAchievements;

                return View("LearnerAchievements", achievementList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading learner achievements for user");
                TempData["Error"] = "An error occurred while loading your achievements.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Get achievement details as JSON for modal display
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAchievementDetails(string achievementId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var achievementDetails = await _achievementService.GetAchievementDetailAsync(achievementId, userId);
                if (achievementDetails == null)
                {
                    return Json(new { success = false, message = "Achievement not found" });
                }

                return Json(new { success = true, data = achievementDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievement details for achievement {AchievementId}", achievementId);
                return Json(new { success = false, message = "An error occurred while loading achievement details" });
            }
        }

        /// <summary>
        /// Get newly unlocked achievements for notification
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNewAchievements()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Get achievements unlocked in the last hour
                var timeWindow = TimeSpan.FromHours(1);
                var newAchievements = await _achievementUnlockService.GetNewlyUnlockedAchievementsAsync(userId, timeWindow);

                var achievementData = newAchievements.Select(a => new
                {
                    a.AchievementId,
                    a.AchievementName,
                    a.AchievementDescription,
                    a.AchievementIcon,
                    a.PointsReward
                }).ToList();

                return Json(new { success = true, achievements = achievementData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting new achievements for user");
                return Json(new { success = false, message = "An error occurred while loading new achievements" });
            }
        }

        /// <summary>
        /// Force check for new achievements (for testing purposes)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ForceCheckAchievements(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User ID is required" });
                }

                var unlockedAchievements = await _achievementUnlockService.CheckAllAchievementsAsync(userId);

                return Json(new
                {
                    success = true,
                    message = $"Checked achievements for user {userId}",
                    unlockedCount = unlockedAchievements.Count,
                    achievements = unlockedAchievements.Select(a => new { a.AchievementId, a.AchievementName })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force checking achievements for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while checking achievements" });
            }
        }

        /// <summary>
        /// Get achievement statistics for dashboard
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAchievementStats()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Get user's achievements
                var userAchievements = await _achievementService.GetUserAchievementsAsync(userId);
                var totalPoints = userAchievements.Sum(ua => ua.PointsEarned ?? 0);
                var achievementCount = userAchievements.Count;

                // Get newly unlocked achievements (last 24 hours)
                var recentAchievements = await _achievementUnlockService.GetNewlyUnlockedAchievementsAsync(userId, TimeSpan.FromHours(24));

                return Json(new
                {
                    success = true,
                    totalPoints,
                    achievementCount,
                    recentAchievementsCount = recentAchievements.Count,
                    recentAchievements = recentAchievements.Select(a => new { a.AchievementName, a.PointsReward })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievement stats for user");
                return Json(new { success = false, message = "An error occurred while loading achievement statistics" });
            }
        }
    }
}

