using System.Diagnostics;
using DataAccessLayer.Models;
using BrainStormEra_MVC.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Implementations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BrainStormEra_MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly HomeServiceImpl _homeServiceImpl;

        public HomeController(HomeServiceImpl homeServiceImpl)
        {
            _homeServiceImpl = homeServiceImpl;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _homeServiceImpl.HandleIndexAsync(User);

            if (result.ShouldRedirect)
            {
                if (!string.IsNullOrEmpty(result.TempDataMessage))
                {
                    TempData["ErrorMessage"] = result.TempDataMessage;
                }
                return RedirectToAction(result.RedirectAction, result.RedirectController);
            }

            if (!string.IsNullOrEmpty(result.ViewBagError))
            {
                ViewBag.Error = result.ViewBagError;
            }

            ViewBag.IsAuthenticated = result.ViewBagIsAuthenticated;
            return View(result.ViewModel);
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
            var result = await _homeServiceImpl.HandleLearnerDashboardAsync(User);

            if (!result.IsSuccess)
            {
                if (!string.IsNullOrEmpty(result.TempDataErrorMessage))
                {
                    TempData["ErrorMessage"] = result.TempDataErrorMessage;
                }

                if (result.ShouldRedirect && !string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                {
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }

                if (result.ShouldRedirectToUserDashboard)
                {
                    return await RedirectToUserDashboard();
                }

                return RedirectToAction("Index", "Home");
            }

            return View(result.ViewModel);
        }

        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> InstructorDashboard()
        {
            var result = await _homeServiceImpl.HandleInstructorDashboardAsync(User);

            if (!result.IsSuccess)
            {
                if (!string.IsNullOrEmpty(result.TempDataErrorMessage))
                {
                    TempData["ErrorMessage"] = result.TempDataErrorMessage;
                }

                if (result.ShouldRedirect && !string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                {
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }

                if (result.ShouldRedirectToUserDashboard)
                {
                    return await RedirectToUserDashboard();
                }

                return RedirectToAction("Index", "Home");
            }

            return View(result.ViewModel);
        }

        [HttpGet]
        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> GetIncomeData(int days = 30)
        {
            var result = await _homeServiceImpl.HandleGetIncomeDataAsync(User, days);
            return Json(result.JsonResponse);
        }

        private async Task<IActionResult> RedirectToUserDashboard()
        {
            var result = _homeServiceImpl.GetUserDashboardRedirect(User);

            if (result.HasError)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Index", "Login");
            }

            return RedirectToAction(result.RedirectAction, result.RedirectController);
        }
    }
}

