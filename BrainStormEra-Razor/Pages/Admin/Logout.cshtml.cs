using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize]
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var username = User.Identity?.Name;
                _logger.LogInformation("Admin logout initiated for user: {Username}", username);

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("Admin {Username} successfully logged out", username);

                return RedirectToPage("/Admin/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin logout");
                return RedirectToPage("/Admin/Login");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return await OnGetAsync();
        }
    }
}