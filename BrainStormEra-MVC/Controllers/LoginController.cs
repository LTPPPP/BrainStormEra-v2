using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
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
                // Find user by username
                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Username == model.Username);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }

                // Check if user is banned
                if (user.IsBanned == true)
                {
                    ModelState.AddModelError(string.Empty, "This account has been suspended.");
                    return View(model);
                }

                // Verify password
                if (!VerifyPassword(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }

                // Create claims for authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId),
                    new Claim(ClaimTypes.Role, user.UserRole),
                    new Claim(ClaimTypes.Email, user.UserEmail)
                };

                if (!string.IsNullOrEmpty(user.FullName))
                {
                    claims.Add(new Claim("FullName", user.FullName));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Update last login time
                user.LastLogin = DateTime.UtcNow;
                _context.Accounts.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Username} logged in at {Time}.", user.Username, DateTime.UtcNow);

                // Redirect to return URL or learner homepage based on role
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                else
                {
                    // Redirect to appropriate dashboard based on user role
                    if (user.UserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }
                    else if (user.UserRole.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("InstructorDashboard", "Home");
                    }
                    else
                    {
                        // Default learner dashboard
                        return RedirectToAction("LearnerDashboard", "Home");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt for user {Username}", model.Username);
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
                user.PasswordHash = HashPassword(model.NewPassword);
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
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        #region Helper Methods
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            string hashedPassword = HashPassword(password);
            return hashedPassword == storedHash;
        }

        private string GenerateOtp()
        {
            // Generate a 6-digit OTP
            return Random.Shared.Next(100000, 999999).ToString();
        }
        #endregion
    }
}
