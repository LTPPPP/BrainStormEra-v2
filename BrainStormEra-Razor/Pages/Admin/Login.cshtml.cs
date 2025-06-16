using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_Razor.Pages.Admin
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IUserService userService, ILogger<LoginModel> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; } = string.Empty;

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

        public class LoginInputModel
        {
            [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
            [Display(Name = "Tên đăng nhập")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Ghi nhớ đăng nhập")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/Admin/Dashboard");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Admin/Dashboard");
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                _logger.LogInformation("Admin login attempt for username: {Username}", Input.Username);

                // Find user by username
                var user = await _userService.GetUserByUsernameAsync(Input.Username);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {Username}", Input.Username);
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                    return Page();
                }

                // Check if user is admin
                if (!string.Equals(user.UserRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Non-admin user attempted admin login: {Username}, Role: {Role}", user.Username, user.UserRole);
                    ModelState.AddModelError(string.Empty, "Bạn không có quyền truy cập vào khu vực này.");
                    return Page();
                }

                // Check if user is banned
                if (user.IsBanned == true)
                {
                    _logger.LogWarning("Banned admin attempted login: {Username}", user.Username);
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị tạm khóa.");
                    return Page();
                }

                // Verify password
                if (!await _userService.VerifyPasswordAsync(Input.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password for admin: {Username}", user.Username);
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                    return Page();
                }

                _logger.LogInformation("Admin login successful: {Username}", user.Username);

                // Create claims for authentication
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

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = Input.RememberMe,
                    ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                // Update last login
                await _userService.UpdateLastLoginAsync(user.UserId);

                _logger.LogInformation("Admin {Username} successfully logged in", user.Username);

                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin login for username: {Username}", Input.Username);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại.");
                return Page();
            }
        }
    }
}