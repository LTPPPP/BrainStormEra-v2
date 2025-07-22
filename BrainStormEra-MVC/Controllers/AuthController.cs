using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
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
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        #region Login Operations

        /// <summary>
        /// Display login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            var result = await _authService.GetLoginViewModelAsync(returnUrl);

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

            var result = await _authService.AuthenticateUserAsync(HttpContext, model);

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
            var result = await _authService.GetRegisterViewModelAsync();

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

            var result = await _authService.RegisterUserAsync(model);

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
            var result = await _authService.CheckUsernameAvailabilityAsync(username);

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
            var result = await _authService.CheckEmailAvailabilityAsync(email);

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
            var result = await _authService.GetForgotPasswordViewModelAsync();

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

            var result = await _authService.ProcessForgotPasswordAsync(model);

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
            var result = await _authService.GetVerifyOtpViewModelAsync(email);

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
            _logger.LogInformation("AuthController.VerifyOtp called with Email={Email}, OtpCode={OtpCode}", model?.Email, model?.OtpCode);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid for VerifyOtp");
                return View("~/Views/Auth/VerifyOtp.cshtml", model);
            }

            var result = await _authService.VerifyOtpAsync(model);

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
            var result = await _authService.GetResetPasswordViewModelAsync(email, token);

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

            var result = await _authService.ResetPasswordAsync(model);

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
            var result = await _authService.LogoutUserAsync(HttpContext, User);

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
            var result = await _authService.LogoutUserAsync(HttpContext, User);

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

                var result = await _authService.GetProfileViewModelAsync(userId, userRole ?? "");

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

                var result = await _authService.GetEditProfileViewModelAsync(userId);

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

                var result = await _authService.UpdateProfileAsync(userId, model);

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

                var result = await _authService.ChangePasswordAsync(userId, model); if (!result.Success)
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
                var result = await _authService.GetUserAvatarAsync(targetUserId);

                return File(result.ImageBytes, result.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving avatar for user: {UserId}", userId);
                var defaultResult = await _authService.GetUserAvatarAsync(null);
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

                var result = await _authService.DeleteUserAvatarAsync(userId);

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar for user: {UserId}", CurrentUserId);
                return Json(new { success = false, message = "An error occurred while deleting avatar" });
            }
        }

        /// <summary>
        /// Get user information (for AJAX calls)
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var userInfo = await _authService.GetUserInfoAsync(userId);
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                return Json(new
                {
                    success = true,
                    userId = userInfo.UserId,
                    fullName = userInfo.FullName,
                    email = userInfo.UserEmail,
                    paymentPoint = userInfo.PaymentPoint ?? 0,
                    userRole = userInfo.UserRole
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                return Json(new { success = false, message = "Error getting user info" });
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

