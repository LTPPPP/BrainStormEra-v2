using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BrainStormEra_Razor.Pages.Admin
{
    public class ProfileModel : PageModel
    {
        [BindProperty]
        public string AdminName { get; set; } = "Admin User";

        [BindProperty]
        public string Username { get; set; } = "admin";

        [BindProperty]
        [EmailAddress]
        public string Email { get; set; } = "admin@brainstormera.com";

        [BindProperty]
        public string PhoneNumber { get; set; } = "+1234567890";

        [BindProperty]
        public string Bio { get; set; } = "System Administrator for BrainStorm Era platform.";

        [BindProperty]
        public string CurrentPassword { get; set; } = "";

        [BindProperty]
        public string NewPassword { get; set; } = "";

        [BindProperty]
        public string ConfirmPassword { get; set; } = "";

        [BindProperty]
        public bool TwoFactorEnabled { get; set; } = false;

        [BindProperty]
        public bool EmailNotifications { get; set; } = true;

        public string AdminImage { get; set; } = "/img/defaults/default-avatar.svg";
        public int TotalUsers { get; set; } = 150;
        public int TotalCourses { get; set; } = 45;
        public int LastLoginDays { get; set; } = 30;

        public void OnGet()
        {
            // Load admin profile data
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Update profile logic here
            // Save to database

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword))
            {
                ModelState.AddModelError("", "Please fill in all password fields.");
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError("", "New password and confirmation password do not match.");
                return Page();
            }

            // Password change logic here
            // Validate current password
            // Hash and save new password

            TempData["SuccessMessage"] = "Password updated successfully!";
            return RedirectToPage();
        }
    }
}