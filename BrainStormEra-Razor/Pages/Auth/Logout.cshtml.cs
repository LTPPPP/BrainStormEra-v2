using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrainStormEra_Razor.Pages.Auth
{
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> OnGet()
        {
            return await OnPost();
        }

        public async Task<IActionResult> OnPost()
        {
            try
            {
                var username = HttpContext.User?.Identity?.Name ?? "Unknown";
                _logger.LogInformation("Admin logout for user: {Username}", username);

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("Admin logout successful for user: {Username}", username);

                return RedirectToPage("/Auth/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during admin logout");
                return RedirectToPage("/Auth/Login");
            }
        }
    }
}
