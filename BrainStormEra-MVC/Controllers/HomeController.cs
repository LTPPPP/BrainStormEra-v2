using System.Diagnostics;
using DataAccessLayer.Models;
using BrainStormEra_MVC.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Implementations;
using BusinessLogicLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BusinessLogicLayer.Services.Interfaces;

namespace BrainStormEra_MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // Redirect authenticated users to their dashboard
                var userRole = User.FindFirst("UserRole")?.Value;
                if (userRole == "learner")
                {
                    return RedirectToAction("LearnerDashboard");
                }
                else if (userRole == "instructor")
                {
                    return RedirectToAction("InstructorDashboard");
                }
            }

            var result = await _homeService.GetGuestHomePageAsync();
            ViewBag.IsAuthenticated = false;
            return View(result);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize(Roles = "learner")]
        public async Task<IActionResult> LearnerDashboard()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User not authenticated";
                return RedirectToAction("Index", "Login");
            }

            var result = await _homeService.GetLearnerDashboardAsync(userId);
            return View(result);
        }

        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> InstructorDashboard()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User not authenticated";
                return RedirectToAction("Index", "Login");
            }

            var result = await _homeService.GetInstructorDashboardAsync(userId);
            return View(result);
        }

        [HttpGet]
        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> GetIncomeData(int days = 30)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var result = await _homeService.GetIncomeDataAsync(userId, days);
            return Json(result);
        }

        [HttpPost]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> InitializeRecommendations()
        {
            try
            {
                var recommendationHelper = HttpContext.RequestServices.GetRequiredService<RecommendationHelper>();
                var success = await recommendationHelper.EnsureFeaturedCoursesExistAsync();

                if (success)
                {
                    return Json(new { success = true, message = "Recommendation system initialized successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to initialize recommendations - no courses available" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error initializing recommendations: {ex.Message}" });
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin,instructor")]
        public async Task<IActionResult> GetRecommendationStats()
        {
            try
            {
                var recommendationHelper = HttpContext.RequestServices.GetRequiredService<RecommendationHelper>();
                var stats = await recommendationHelper.GetRecommendationStatsAsync();

                return Json(new
                {
                    success = true,
                    stats = new
                    {
                        totalActiveCourses = stats.TotalActiveCourses,
                        featuredCourses = stats.FeaturedCourses,
                        coursesWithEnrollments = stats.CoursesWithEnrollments
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error getting stats: {ex.Message}" });
            }
        }

        private async Task<IActionResult> RedirectToUserDashboard()
        {
            var userRole = User.FindFirst("UserRole")?.Value;
            if (userRole == "learner")
            {
                return RedirectToAction("LearnerDashboard");
            }
            else if (userRole == "instructor")
            {
                return RedirectToAction("InstructorDashboard");
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid user role";
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Index", "Login");
            }
        }
    }
}

