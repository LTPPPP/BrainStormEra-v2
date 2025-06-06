using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_MVC.Controllers
{
    public class AchievementController : Controller
    {
        private readonly IAchievementService _achievementService;

        public AchievementController(IAchievementService achievementService)
        {
            _achievementService = achievementService;
        }
        [Authorize(Roles = "Learner,learner")]
        public async Task<IActionResult> LearnerAchievements(string? search, int page = 1, int pageSize = 9)
        {
            var userId = User?.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Login");
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 9;

            var achievementList = await _achievementService.GetUserAchievementsAsync(userId, search, page, pageSize);

            ViewData["UserId"] = userId;
            ViewData["HasAchievements"] = achievementList.HasAchievements;
            ViewData["TotalAchievements"] = achievementList.TotalAchievements;
            ViewData["SearchQuery"] = search;

            return View("~/Views/Achievements/LearnerAchievements.cshtml", achievementList);
        }

        [HttpGet]
        public async Task<IActionResult> GetAchievementDetails(string achievementId, string userId)
        {
            if (string.IsNullOrEmpty(achievementId) || string.IsNullOrEmpty(userId))
            {
                return PartialView("_AchievementDetailError", "Invalid achievement or user ID");
            }

            var achievement = await _achievementService.GetAchievementDetailAsync(achievementId, userId);

            if (achievement == null)
            {
                return PartialView("_AchievementDetailError", "Achievement not found");
            }

            return PartialView("_AchievementDetail", achievement);
        }
    }
}
