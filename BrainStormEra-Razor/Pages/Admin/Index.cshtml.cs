using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public string? AdminName { get; set; }
        public string? UserId { get; set; }
        public DateTime LoginTime { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            AdminName = HttpContext.User?.Identity?.Name ?? "Admin";
            UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";

            if (DateTime.TryParse(HttpContext.User?.FindFirst("LoginTime")?.Value, out var loginTime))
            {
                LoginTime = loginTime;
            }
            else
            {
                LoginTime = DateTime.UtcNow;
            }

            _logger.LogInformation("Admin dashboard accessed by: {AdminName}", AdminName);
        }
    }
}
