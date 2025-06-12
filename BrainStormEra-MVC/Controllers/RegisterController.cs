using DataAccessLayer.Models;
using DataAccessLayer.Data;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Controllers
{
    public class RegisterController : Controller
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<RegisterController> _logger;
        private readonly IUserService _userService;
        private readonly AuthServiceImpl _authServiceImpl;

        public RegisterController(BrainStormEraContext context, ILogger<RegisterController> logger, IUserService userService, AuthServiceImpl authServiceImpl)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
            _authServiceImpl = authServiceImpl;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await _authServiceImpl.GetRegisterViewModelAsync();

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return View("~/Views/Auth/Register.cshtml");
            }

            return View("~/Views/Auth/Register.cshtml", result.ViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors below and try again.";
                return View("~/Views/Auth/Register.cshtml", model);
            }

            try
            {
                var result = await _authServiceImpl.RegisterUserAsync(model);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;

                    // Add specific validation errors to ModelState
                    if (!string.IsNullOrEmpty(result.ValidationError))
                    {
                        if (result.ValidationError == "Username")
                        {
                            ModelState.AddModelError("Username", "Username is already taken");
                        }
                        else if (result.ValidationError == "Email")
                        {
                            ModelState.AddModelError("Email", "Email is already registered");
                        }
                    }

                    return View("~/Views/Auth/Register.cshtml", result.ViewModel ?? model);
                }

                // Redirect to success page or login
                TempData["SuccessMessage"] = result.SuccessMessage;
                return RedirectToAction("Index", "Login");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while registering user");
                TempData["ErrorMessage"] = "A database error occurred while creating your account. Please try again.";
                ModelState.AddModelError(string.Empty, "An error occurred while registering your account. Please try again.");
                return View("~/Views/Auth/Register.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                TempData["ErrorMessage"] = "An unexpected error occurred during registration. Please try again later.";
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                return View("~/Views/Auth/Register.cshtml", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckUsername(string username)
        {
            var result = await _authServiceImpl.CheckUsernameAvailabilityAsync(username);
            return Json(new { valid = result.IsValid, message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            var result = await _authServiceImpl.CheckEmailAvailabilityAsync(email);
            return Json(new { valid = result.IsValid, message = result.Message });
        }
    }
}

