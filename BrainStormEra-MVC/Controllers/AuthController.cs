using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Implementations;
using DataAccessLayer.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BrainStormEra_MVC.Controllers
{
    /// <summary>
    /// Unified Authentication Controller that handles all authentication operations
    /// Uses AuthServiceImpl for complex business logic, following the established pattern
    /// </summary>
    public class AuthController : BaseController
    {
        private readonly AuthServiceImpl _authServiceImpl;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AuthServiceImpl authServiceImpl,
            ILogger<AuthController> logger)
        {
            _authServiceImpl = authServiceImpl;
            _logger = logger;
        }

        #region Login Operations

        /// <summary>
        /// Display login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            var result = await _authServiceImpl.GetLoginViewModelAsync(returnUrl);

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
            }

            return View("~/Views/Auth/Login.cshtml", result.ViewModel);
        }

        /// <summary>
        /// Process login form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Auth/Login.cshtml", model);
            }

            var result = await _authServiceImpl.AuthenticateUserAsync(HttpContext, model);

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
                return View("~/Views/Auth/Login.cshtml", result.ViewModel);
            }

            ViewBag.Success = result.SuccessMessage;

            // Redirect based on user role or return URL
            if (!string.IsNullOrEmpty(result.RedirectAction))
            {
                if (result.RedirectAction.StartsWith("/"))
                {
                    return Redirect(result.RedirectAction);
                }
                return RedirectToAction(result.RedirectAction);
            }

            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Register Operations

        /// <summary>
        /// Display registration page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var result = await _authServiceImpl.GetRegisterViewModelAsync();

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
            }

            return View("~/Views/Auth/Register.cshtml", result.ViewModel);
        }

        /// <summary>
        /// Process registration form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Auth/Register.cshtml", model);
            }

            var result = await _authServiceImpl.RegisterUserAsync(model);

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
                // Add specific validation errors
                if (!string.IsNullOrEmpty(result.ValidationError) && !string.IsNullOrEmpty(result.ErrorMessage))
                {
                    ModelState.AddModelError(result.ValidationError, result.ErrorMessage);
                }

                return View("~/Views/Auth/Register.cshtml", result.ViewModel);
            }

            ViewBag.Success = result.SuccessMessage;

            // Redirect to login page after successful registration
            if (!string.IsNullOrEmpty(result.RedirectAction))
            {
                return RedirectToAction(result.RedirectAction);
            }

            return RedirectToAction("Login");
        }

        /// <summary>
        /// Check username availability via AJAX
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> CheckUsernameAvailability(string username)
        {
            var result = await _authServiceImpl.CheckUsernameAvailabilityAsync(username);

            return Json(new
            {
                isValid = result.IsValid,
                message = result.Message
            });
        }

        /// <summary>
        /// Check email availability via AJAX
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> CheckEmailAvailability(string email)
        {
            var result = await _authServiceImpl.CheckEmailAvailabilityAsync(email);

            return Json(new
            {
                isValid = result.IsValid,
                message = result.Message
            });
        }

        #endregion

        #region Password Reset Operations

        /// <summary>
        /// Display forgot password page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ForgotPassword()
        {
            var result = await _authServiceImpl.GetForgotPasswordViewModelAsync();

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
            }

            return View("~/Views/Auth/ForgotPassword.cshtml", result.ViewModel);
        }

        /// <summary>
        /// Process forgot password form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Auth/ForgotPassword.cshtml", model);
            }

            var result = await _authServiceImpl.ProcessForgotPasswordAsync(model);

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
                return View("~/Views/Auth/ForgotPassword.cshtml", result.ViewModel);
            }

            // Redirect to OTP verification or confirmation page
            if (!string.IsNullOrEmpty(result.RedirectAction))
            {
                if (!string.IsNullOrEmpty(result.Email))
                {
                    return RedirectToAction(result.RedirectAction, new { email = result.Email });
                }
                return RedirectToAction(result.RedirectAction);
            }

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        /// <summary>
        /// Display forgot password confirmation page
        /// </summary>
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View("~/Views/Auth/ForgotPasswordConfirmation.cshtml");
        }

        /// <summary>
        /// Display OTP verification page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VerifyOtp(string email)
        {
            var result = await _authServiceImpl.GetVerifyOtpViewModelAsync(email);

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;

                if (!string.IsNullOrEmpty(result.RedirectAction))
                {
                    return RedirectToAction(result.RedirectAction);
                }
            }

            return View("~/Views/Auth/VerifyOtp.cshtml", result.ViewModel);
        }

        /// <summary>
        /// Process OTP verification form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(OtpVerificationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Auth/VerifyOtp.cshtml", model);
            }

            var result = await _authServiceImpl.VerifyOtpAsync(model);

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
                return View("~/Views/Auth/VerifyOtp.cshtml", result.ViewModel);
            }

            // Redirect to reset password page with token
            if (!string.IsNullOrEmpty(result.RedirectAction))
            {
                return RedirectToAction(result.RedirectAction, new
                {
                    email = result.Email,
                    token = result.Token
                });
            }

            return RedirectToAction("Login");
        }

        /// <summary>
        /// Display reset password page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string email, string token)
        {
            var result = await _authServiceImpl.GetResetPasswordViewModelAsync(email, token);

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;

                if (!string.IsNullOrEmpty(result.RedirectAction))
                {
                    return RedirectToAction(result.RedirectAction);
                }
            }

            return View("~/Views/Auth/ResetPassword.cshtml", result.ViewModel);
        }

        /// <summary>
        /// Process reset password form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Auth/ResetPassword.cshtml", model);
            }

            var result = await _authServiceImpl.ResetPasswordAsync(model);

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
                return View("~/Views/Auth/ResetPassword.cshtml", result.ViewModel);
            }

            ViewBag.Success = "Your password has been reset successfully. You can now log in with your new password.";

            // Redirect to confirmation page or login
            if (!string.IsNullOrEmpty(result.RedirectAction))
            {
                return RedirectToAction(result.RedirectAction);
            }

            return RedirectToAction("Login");
        }

        /// <summary>
        /// Display reset password confirmation page
        /// </summary>
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View("~/Views/Auth/ResetPasswordConfirmation.cshtml");
        }

        #endregion

        #region Logout Operations        /// <summary>
        /// Process user logout
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var result = await _authServiceImpl.LogoutUserAsync(HttpContext, User);

            if (!result.Success)
            {
                _logger.LogError("Logout failed: {Message}", result.Message);
                ViewBag.Error = result.Message;
            }
            else
            {
                TempData["InfoMessage"] = "You have been logged out successfully.";
            }

            // Redirect to guest homepage to ensure user sees the guest view
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Alternative logout endpoint (GET) for logout links
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> LogoutConfirm()
        {
            var result = await _authServiceImpl.LogoutUserAsync(HttpContext, User);

            if (!result.Success)
            {
                _logger.LogError("Logout failed: {Message}", result.Message);
                ViewBag.Error = result.Message;
            }
            else
            {
                TempData["InfoMessage"] = "You have been logged out successfully.";
            }

            // Redirect to guest homepage to ensure user sees the guest view
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Profile Operations

        /// <summary>
        /// Display user profile page
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = CurrentUserId;
                var userRole = CurrentUserRole;

                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to view your profile.";
                    return RedirectToAction("Login");
                }

                var result = await _authServiceImpl.GetProfileViewModelAsync(userId, userRole ?? "");

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Login");
                }

                return View("~/Views/Profile/Index.cshtml", result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while loading your profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Display edit profile page
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to edit your profile.";
                    return RedirectToAction("Login");
                }

                var result = await _authServiceImpl.GetEditProfileViewModelAsync(userId);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Login");
                }

                return View("~/Views/Profile/Edit.cshtml", result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit profile for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while loading the edit profile page.";
                return RedirectToAction("Profile");
            }
        }

        /// <summary>
        /// Process edit profile form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Profile/Edit.cshtml", model);
            }

            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to edit your profile.";
                    return RedirectToAction("Login");
                }

                var result = await _authServiceImpl.UpdateProfileAsync(userId, model);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return View("~/Views/Profile/Edit.cshtml", result.ViewModel);
                }

                TempData["SuccessMessage"] = result.SuccessMessage;
                if (!string.IsNullOrEmpty(result.WarningMessage))
                {
                    TempData["WarningMessage"] = result.WarningMessage;
                }

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while updating your profile.";
                return View("~/Views/Profile/Edit.cshtml", model);
            }
        }

        /// <summary>
        /// Display change password page
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View("~/Views/Profile/ChangePassword.cshtml", new ChangePasswordViewModel());
        }

        /// <summary>
        /// Process change password form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Profile/ChangePassword.cshtml", model);
            }

            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to change your password.";
                    return RedirectToAction("Login");
                }

                var result = await _authServiceImpl.ChangePasswordAsync(userId, model); if (!result.Success)
                {
                    if (!string.IsNullOrEmpty(result.ValidationError) && !string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        ModelState.AddModelError(result.ValidationError, result.ErrorMessage);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }
                    return View("~/Views/Profile/ChangePassword.cshtml", result.ViewModel);
                }

                TempData["SuccessMessage"] = result.SuccessMessage;
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while changing your password.";
                return View("~/Views/Profile/ChangePassword.cshtml", model);
            }
        }

        /// <summary>
        /// Get user avatar image
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAvatar(string? userId = null)
        {
            try
            {
                var targetUserId = userId ?? CurrentUserId;
                var result = await _authServiceImpl.GetUserAvatarAsync(targetUserId);

                return File(result.ImageBytes, result.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving avatar for user: {UserId}", userId);
                var defaultResult = await _authServiceImpl.GetUserAvatarAsync(null);
                return File(defaultResult.ImageBytes, defaultResult.ContentType);
            }
        }

        /// <summary>
        /// Delete user avatar
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteAvatar()
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = await _authServiceImpl.DeleteUserAvatarAsync(userId);

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar for user: {UserId}", CurrentUserId);
                return Json(new { success = false, message = "An error occurred while deleting avatar" });
            }
        }

        #endregion

        #region Access Denied

        /// <summary>
        /// Display access denied page
        /// </summary>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View("~/Views/Auth/AccessDenied.cshtml");
        }

        #endregion
    }
}

