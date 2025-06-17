using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Implementations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using DataAccessLayer.Data;
using Microsoft.EntityFrameworkCore;
using BusinessLogicLayer.Services;

namespace BrainStormEra_Razor.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly BrainStormEraContext _context;
        private readonly UserService _userService;
        private readonly ILogger<LoginModel> _logger;

        [BindProperty]
        public LoginViewModel LoginData { get; set; } = new LoginViewModel
        {
            Username = string.Empty,
            Password = string.Empty
        };

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public LoginModel(BrainStormEraContext context, UserService userService, ILogger<LoginModel> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        public void OnGet(string? returnUrl = null)
        {
            LoginData.ReturnUrl = returnUrl ?? string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                _logger.LogInformation("Admin login attempt for username: {Username}", LoginData.Username);                // Find user by username
                var user = await _context.Accounts
                    .FirstOrDefaultAsync(u => u.Username == LoginData.Username);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {Username}", LoginData.Username);
                    ErrorMessage = "Invalid username or password. Please check your credentials and try again.";
                    return Page();
                }

                // Check if user is banned
                if (user.IsBanned == true)
                {
                    _logger.LogWarning("Banned user attempted login: {Username}", user.Username);
                    ErrorMessage = "Your account has been suspended. Please contact support for assistance.";
                    return Page();
                }

                // Verify password
                if (string.IsNullOrEmpty(LoginData.Password) || !await _userService.VerifyPasswordAsync(LoginData.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password for user: {Username}", user.Username);
                    ErrorMessage = "Invalid username or password. Please check your credentials and try again.";
                    return Page();
                }

                // Check if user is admin
                if (!string.Equals(user.UserRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Non-admin login attempt for user: {Username}, Role: {Role}", user.Username, user.UserRole);
                    ErrorMessage = "Access denied. Admin credentials required.";
                    return Page();
                }

                _logger.LogInformation("Admin login successful for user: {Username}", user.Username);                // Create claims for authentication
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.Username),
                    new(ClaimTypes.NameIdentifier, user.UserId),
                    new(ClaimTypes.Role, user.UserRole),
                    new(ClaimTypes.Email, user.UserEmail ?? ""),
                    new("UserId", user.UserId),
                    new("UserRole", user.UserRole),
                    new("LoginTime", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
                };

                // Add optional user information
                if (!string.IsNullOrEmpty(user.FullName))
                {
                    claims.Add(new("FullName", user.FullName));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = LoginData.RememberMe,
                    ExpiresUtc = LoginData.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                _logger.LogInformation("Admin authentication successful for user: {Username}", user.Username);

                // Redirect to return URL or default admin page
                if (!string.IsNullOrEmpty(LoginData.ReturnUrl) && Url.IsLocalUrl(LoginData.ReturnUrl))
                {
                    return Redirect(LoginData.ReturnUrl);
                }

                return RedirectToPage("/Admin/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during admin login for username: {Username}", LoginData.Username);
                ErrorMessage = "An error occurred during login. Please try again.";
                return Page();
            }
        }
    }
}
