using BrainStormEra_MVC.Models;
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
        public async Task<IActionResult> LearnerAchievements()
        {
            var userId = User?.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Login");
            }

            var userAchievements = await _achievementService.GetUserAchievementsAsync(userId);

            var learnerAchievements = userAchievements.Select(ua => new
            {
                AchievementId = ua.AchievementId,
                AchievementName = ua.Achievement.AchievementName,
                AchievementDescription = ua.Achievement.AchievementDescription,
                AchievementIcon = ua.Achievement.AchievementIcon,
                ReceivedDate = ua.ReceivedDate
            }).ToList();

            ViewData["UserId"] = userId;
            ViewData["Achievements"] = learnerAchievements;

            return View("~/Views/Achievements/LearnerAchievements.cshtml");
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
