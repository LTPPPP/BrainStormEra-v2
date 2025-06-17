using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrainStormEra_Razor.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            // If user is authenticated and is admin, redirect to admin dashboard
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("admin"))
            {
                return RedirectToPage("/Admin/Index");
            }

            // Otherwise redirect to login
            return RedirectToPage("/Auth/Login");
        }
    }
}
