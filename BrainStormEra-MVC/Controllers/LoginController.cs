using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Utilities;
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
        // Cache for OTP codes (in a production environment, use a more robust solution like Redis)
        private static Dictionary<string, (string otp, DateTime expiry)> _otpCache = new();

        public LoginController(BrainStormEraContext context, ILogger<LoginController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }
        [HttpGet]
        public IActionResult Index(string? returnUrl = null)
        {            // Clear any existing authentication
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel
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
                return View(model);
            }

            try
            {
                _logger.LogInformation("Login attempt for username: {Username}", model.Username);

                // Find user by username
                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Username == model.Username);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {Username}", model.Username);
                    TempData["ErrorMessage"] = "Invalid username or password. Please check your credentials and try again.";
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }

                _logger.LogInformation("User found: {Username}, Role: {Role}", user.Username, user.UserRole);

                // Check if user is banned
                if (user.IsBanned == true)
                {
                    _logger.LogWarning("Banned user attempted login: {Username}", user.Username);
                    TempData["ErrorMessage"] = "Your account has been suspended. Please contact support for assistance.";
                    ModelState.AddModelError(string.Empty, "This account has been suspended.");
                    return View(model);
                }

                // Verify password
                _logger.LogInformation("Verifying password for user: {Username}", user.Username);

                // Debug: Log hash information
                var inputPasswordHash = PasswordHasher.HashPassword(model.Password ?? "");
                _logger.LogInformation("Input password hash: {InputHash}", inputPasswordHash);
                _logger.LogInformation("Stored password hash: {StoredHash}", user.PasswordHash);

                if (string.IsNullOrEmpty(model.Password) || !PasswordHasher.VerifyPassword(model.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password for user: {Username}", user.Username);
                    TempData["ErrorMessage"] = "Invalid username or password. Please check your credentials and try again.";
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }

                _logger.LogInformation("Password verified successfully for user: {Username}", user.Username);

                // Validate user role (ensure it's one of the allowed roles)
                var validRoles = new[] { "Admin", "Instructor", "Learner", "admin", "instructor", "learner" };
                if (!validRoles.Contains(user.UserRole, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid role for user: {Username}, Role: {Role}", user.Username, user.UserRole);
                    TempData["ErrorMessage"] = "Invalid user role. Please contact system administrator.";
                    ModelState.AddModelError(string.Empty, "Account configuration error.");
                    return View(model);
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

                // Update last login time synchronously in the same context
                try
                {
                    var userToUpdate = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == user.UserId);
                    if (userToUpdate != null)
                    {
                        userToUpdate.LastLogin = DateTime.UtcNow;
                        userToUpdate.AccountUpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Updated last login time for user {UserId}", user.UserId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating last login time for user {UserId}", user.UserId);
                    // Don't fail the login process for this error
                }

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
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserEmail == model.Email);

                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // Generate OTP code
                string otp = GenerateOtp();

                // Store OTP in cache with 10-minute expiry
                _otpCache[model.Email] = (otp, DateTime.UtcNow.AddMinutes(10));

                // In a real application, send email with OTP
                // For demo purposes, we'll just log it
                _logger.LogInformation("OTP for {Email}: {OTP}", model.Email, otp);

                // Redirect to OTP verification page
                return RedirectToAction(nameof(VerifyOtp), new { email = model.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for email {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new OtpVerificationViewModel
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
                return View(model);
            }

            // Check if OTP exists and is valid
            if (!_otpCache.TryGetValue(model.Email, out var otpData) ||
                otpData.otp != model.OtpCode ||
                otpData.expiry < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired OTP code.");
                return View(model);
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

            return View(new ResetPasswordViewModel
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
                return View(model);
            }

            // Verify token is valid
            if (!_otpCache.TryGetValue(model.Email, out var tokenData) ||
                tokenData.otp != model.Token ||
                tokenData.expiry < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired token.");
                return View(model);
            }

            try
            {
                var user = await _context.Accounts.FirstOrDefaultAsync(a => a.UserEmail == model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return RedirectToAction(nameof(ResetPasswordConfirmation));
                }

                // Update password
                user.PasswordHash = PasswordHasher.HashPassword(model.NewPassword);
                user.AccountUpdatedAt = DateTime.UtcNow;

                _context.Accounts.Update(user);
                await _context.SaveChangesAsync();

                // Remove token from cache
                _otpCache.Remove(model.Email);

                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset for email {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name;

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("User {Username} logged out at {Time}", username, DateTime.UtcNow);

            TempData["InfoMessage"] = "You have been logged out successfully.";

            return RedirectToAction("Index", "Home");
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
                "admin" => RedirectToAction("AdminDashboard", "Admin"),
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
