using DataAccessLayer.Models;
using DataAccessLayer.Data;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BrainStormEra_MVC.Controllers
{
    public class LoginController : Controller
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        // Cache for OTP codes (in a production environment, use a more robust solution like Redis)
        private static Dictionary<string, (string otp, DateTime expiry)> _otpCache = new();

        public LoginController(BrainStormEraContext context, ILogger<LoginController> logger, IConfiguration configuration, IUserService userService, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _userService = userService;
            _emailService = emailService;
        }
        [HttpGet]
        public IActionResult Index(string? returnUrl = null)
        {            // Clear any existing authentication
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View("~/Views/Auth/Login.cshtml", new LoginViewModel
            {
                ReturnUrl = returnUrl ?? string.Empty,
                Username = string.Empty,
                Password = string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            ViewData["ReturnUrl"] = model.ReturnUrl;

            if (!ModelState.IsValid)
            {
                return View("~/Views/Auth/Login.cshtml", model);
            }

            try
            {
                _logger.LogInformation("Login attempt for username: {Username}", model.Username);

                // Find user by username using service
                var user = await _userService.GetUserByUsernameAsync(model.Username);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {Username}", model.Username);
                    TempData["ErrorMessage"] = "Invalid username or password. Please check your credentials and try again.";
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View("~/Views/Auth/Login.cshtml", model);
                }

                // Check if user is banned
                if (user.IsBanned == true)
                {
                    _logger.LogWarning("Banned user attempted login: {Username}", user.Username);
                    TempData["ErrorMessage"] = "Your account has been suspended. Please contact support for assistance.";
                    ModelState.AddModelError(string.Empty, "This account has been suspended.");
                    return View("~/Views/Auth/Login.cshtml", model);
                }

                // Verify password using service
                if (string.IsNullOrEmpty(model.Password) || !await _userService.VerifyPasswordAsync(model.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password for user: {Username}", user.Username);
                    TempData["ErrorMessage"] = "Invalid username or password. Please check your credentials and try again.";
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View("~/Views/Auth/Login.cshtml", model);
                }

                _logger.LogInformation("Password verified successfully for user: {Username}", user.Username);

                // Validate user role (ensure it's one of the allowed roles)
                var validRoles = new[] { "instructor", "learner" };
                if (!validRoles.Contains(user.UserRole, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid role for user: {Username}, Role: {Role}", user.Username, user.UserRole);
                    TempData["ErrorMessage"] = "Invalid login attempt.";
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View("~/Views/Auth/Login.cshtml", model);
                }

                _logger.LogInformation("Creating claims for user: {Username}", user.Username);

                // Create claims for authentication with comprehensive user information
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId),
                    new Claim(ClaimTypes.Role, user.UserRole),
                    new Claim(ClaimTypes.Email, user.UserEmail ?? ""),
                    new Claim("UserId", user.UserId),
                    new Claim("UserRole", user.UserRole),
                    new Claim("LoginTime", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
                };

                // Add optional user information
                if (!string.IsNullOrEmpty(user.FullName))
                {
                    claims.Add(new Claim("FullName", user.FullName));
                }
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    claims.Add(new Claim("PhoneNumber", user.PhoneNumber));
                }
                if (user.DateOfBirth.HasValue)
                {
                    claims.Add(new Claim("DateOfBirth", user.DateOfBirth.Value.ToString("yyyy-MM-dd")));
                }
                if (!string.IsNullOrEmpty(user.UserImage))
                {
                    claims.Add(new Claim("UserImage", user.UserImage));
                }
                if (!string.IsNullOrEmpty(user.UserAddress))
                {
                    claims.Add(new Claim("UserAddress", user.UserAddress));
                }
                if (user.Gender.HasValue)
                {
                    claims.Add(new Claim("Gender", user.Gender.Value.ToString()));
                }
                if (user.PaymentPoint.HasValue)
                {
                    claims.Add(new Claim("PaymentPoint", user.PaymentPoint.Value.ToString()));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1),
                    AllowRefresh = true
                };

                _logger.LogInformation("Signing in user: {Username}", user.Username);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User signed in successfully: {Username}", user.Username);

                // Update last login time using service
                await _userService.UpdateLastLoginAsync(user.UserId);

                _logger.LogInformation("User {Username} logged in at {Time}.", user.Username, DateTime.UtcNow);

                // Set success message for toast notification
                TempData["SuccessMessage"] = $"Welcome back, {user.FullName ?? user.Username}! Login successful.";

                _logger.LogInformation("Determining redirect for user: {Username}, Role: {Role}", user.Username, user.UserRole);

                // Add cache control headers to prevent caching of login page
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                // Use helper method for redirect logic
                return GetPostLoginRedirect(user.UserRole, model.ReturnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt for user {Username}", model.Username);
                TempData["ErrorMessage"] = "An unexpected error occurred during login. Please try again later.";
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View("~/Views/Auth/ForgotPassword.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Auth/ForgotPassword.cshtml", model);
            }

            try
            {
                // Find user by email using service
                var user = await _userService.GetUserByEmailAsync(model.Email);

                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // Generate OTP code
                string otp = GenerateOtp();

                // Store OTP in cache with 10-minute expiry
                _otpCache[model.Email] = (otp, DateTime.UtcNow.AddMinutes(10));

                // Send email with OTP using EmailService
                var emailResult = await _emailService.SendForgotPasswordEmailAsync(
                    model.Email,
                    user.FullName ?? user.Username,
                    otp,
                    10);

                if (!emailResult.IsSuccess)
                {
                    _logger.LogError("Failed to send forgot password email to {Email}: {Error}",
                        model.Email, emailResult.Message);

                    // Remove OTP from cache since email failed
                    _otpCache.Remove(model.Email);

                    ModelState.AddModelError(string.Empty, "Failed to send verification email. Please try again later.");
                    return View("~/Views/Auth/ForgotPassword.cshtml", model);
                }

                _logger.LogInformation("Forgot password email sent successfully to {Email}", model.Email);

                // Redirect to OTP verification page
                return RedirectToAction(nameof(VerifyOtp), new { email = model.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for email {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
                return View("~/Views/Auth/ForgotPassword.cshtml", model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View("~/Views/Auth/ForgotPasswordConfirmation.cshtml");
        }

        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View("~/Views/Auth/VerifyOtp.cshtml", new OtpVerificationViewModel
            {
                Email = email,
                OtpCode = string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(OtpVerificationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Auth/VerifyOtp.cshtml", model);
            }

            // Check if OTP exists and is valid
            if (!_otpCache.TryGetValue(model.Email, out var otpData) ||
                otpData.otp != model.OtpCode ||
                otpData.expiry < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired OTP code.");
                return View("~/Views/Auth/VerifyOtp.cshtml", model);
            }

            // Remove OTP from cache
            _otpCache.Remove(model.Email);

            // Generate password reset token
            string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            // In a real application, store token in database with expiry
            // For demo, we'll use the same cache
            _otpCache[model.Email] = (token, DateTime.UtcNow.AddHours(1));

            // Redirect to reset password page
            return RedirectToAction(nameof(ResetPassword), new { email = model.Email, token });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Verify token is valid
            if (!_otpCache.TryGetValue(email, out var tokenData) ||
                tokenData.otp != token ||
                tokenData.expiry < DateTime.UtcNow)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View("~/Views/Auth/ResetPassword.cshtml", new ResetPasswordViewModel
            {
                Email = email,
                Token = token,
                NewPassword = string.Empty,
                ConfirmPassword = string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Auth/ResetPassword.cshtml", model);
            }

            // Verify token is valid
            if (!_otpCache.TryGetValue(model.Email, out var tokenData) ||
                tokenData.otp != model.Token ||
                tokenData.expiry < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired token.");
                return View("~/Views/Auth/ResetPassword.cshtml", model);
            }

            try
            {
                var user = await _userService.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return RedirectToAction(nameof(ResetPasswordConfirmation));
                }

                // Update password
                user.PasswordHash = PasswordHasher.HashPassword(model.NewPassword);
                user.AccountUpdatedAt = DateTime.UtcNow;

                await _userService.UpdateUserAsync(user);

                // Remove token from cache
                _otpCache.Remove(model.Email);

                // Send password reset confirmation email
                var resetTime = DateTime.UtcNow;
                var emailResult = await _emailService.SendPasswordResetConfirmationEmailAsync(
                    model.Email,
                    user.FullName ?? user.Username,
                    resetTime);

                if (!emailResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to send password reset confirmation email to {Email}: {Error}",
                        model.Email, emailResult.Message);
                    // Don't fail the password reset if email fails, just log it
                }
                else
                {
                    _logger.LogInformation("Password reset confirmation email sent successfully to {Email}", model.Email);
                }

                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset for email {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
                return View("~/Views/Auth/ResetPassword.cshtml", model);
            }
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View("~/Views/Auth/ResetPasswordConfirmation.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name;

            // Sign out of the authentication scheme
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear all session data
            HttpContext.Session.Clear();

            // Clear all authentication cookies
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            // Explicitly delete the authentication cookie
            Response.Cookies.Delete("BrainStormEraAuth");

            // Clear any authentication tokens from the response
            Response.Headers["Authorization"] = "";

            // Set cache control headers to prevent caching of sensitive information
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            _logger.LogInformation("User {Username} logged out at {Time}", username, DateTime.UtcNow);

            // Clear any success messages from previous login
            TempData.Remove("SuccessMessage");
            TempData["InfoMessage"] = "You have been logged out successfully.";

            // Redirect to guest homepage to ensure user sees the guest view
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> LogoutComplete()
        {
            var username = User.Identity?.Name;

            // Sign out of the authentication scheme
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear all session data
            HttpContext.Session.Clear();

            // Clear all authentication cookies
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            // Explicitly delete the authentication cookie
            Response.Cookies.Delete("BrainStormEraAuth");

            // Clear any authentication tokens from the response
            Response.Headers["Authorization"] = "";

            // Set cache control headers to prevent caching of sensitive information
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            _logger.LogInformation("User {Username} logged out at {Time}", username, DateTime.UtcNow);

            // Clear any success messages from previous login
            TempData.Remove("SuccessMessage");

            // Return JSON result for AJAX requests
            return Json(new { success = true, message = "Logout successful" });
        }

        #region Helper Methods
        private string GenerateOtp()
        {
            // Generate a 6-digit OTP
            return Random.Shared.Next(100000, 999999).ToString();
        }

        /// <summary>
        /// Helper method to determine the appropriate redirect after successful login
        /// </summary>
        private IActionResult GetPostLoginRedirect(string userRole, string? returnUrl = null)
        {
            _logger.LogInformation("Determining post-login redirect for role: {Role}, ReturnUrl: {ReturnUrl}", userRole, returnUrl);

            // Check return URL first and validate it's safe
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                _logger.LogInformation("Redirecting to return URL: {ReturnUrl}", returnUrl);
                return Redirect(returnUrl);
            }

            // Role-based redirect with explicit URLs for better reliability (case-insensitive)
            var redirectResult = userRole.ToLower() switch
            {
                "instructor" => RedirectToAction("InstructorDashboard", "Home"),
                "learner" => RedirectToAction("LearnerDashboard", "Home"),
                _ => RedirectToAction("Index", "Home")
            };

            _logger.LogInformation("Redirecting user with role {Role} to {Controller}/{Action}",
                userRole,
                redirectResult is RedirectToActionResult redirectAction ? redirectAction.ControllerName : "Unknown",
                redirectResult is RedirectToActionResult redirectAction2 ? redirectAction2.ActionName : "Unknown");

            return redirectResult;
        }
        #endregion
    }
}

