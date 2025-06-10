using BrainStormEra_MVC.Services.Implementations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_MVC.Controllers
{
    public class AchievementController : Controller
    {
        private readonly AchievementServiceImpl _achievementServiceImpl;
        private readonly ILogger<AchievementController> _logger;

        public AchievementController(
            AchievementServiceImpl achievementServiceImpl,
            ILogger<AchievementController> logger)
        {
            _achievementServiceImpl = achievementServiceImpl;
            _logger = logger;
        }
        [Authorize(Roles = "learner")]
        public async Task<IActionResult> LearnerAchievements(string? search, int page = 1, int pageSize = 9)
        {
            var result = await _achievementServiceImpl.GetLearnerAchievementsAsync(User, search, page, pageSize);

            if (!result.IsSuccess)
            {
                if (result.RedirectToLogin)
                {
                    return RedirectToAction("Index", "Login");
                }

                // Handle other errors
                TempData["ErrorMessage"] = result.ErrorMessage;
                return View("~/Views/Achievements/LearnerAchievements.cshtml");
            }

            ViewData["UserId"] = result.UserId;
            ViewData["HasAchievements"] = result.HasAchievements;
            ViewData["TotalAchievements"] = result.TotalAchievements;
            ViewData["SearchQuery"] = result.SearchQuery;

            return View("~/Views/Achievements/LearnerAchievements.cshtml", result.AchievementList);
        }
        [HttpGet]
        public async Task<IActionResult> GetAchievementDetails(string achievementId, string userId)
        {
            var result = await _achievementServiceImpl.GetAchievementDetailsAsync(achievementId, userId);

            if (!result.IsSuccess)
            {
                return PartialView("_AchievementDetailError", result.ErrorMessage);
            }

            return PartialView("_AchievementDetail", result.Achievement);
        }
    }
}
