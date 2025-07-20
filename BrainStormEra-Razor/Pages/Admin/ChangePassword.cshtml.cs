using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using System.Security.Claims;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class ChangePasswordModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(IAuthService authService, ILogger<ChangePasswordModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        public ChangePasswordViewModel ChangePasswordData { get; set; } = new ChangePasswordViewModel();

        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                // Get current user ID
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to change your password.";
                    return RedirectToPage("/Login");
                }

                // Create the view model from bound properties
                var changePasswordViewModel = new ChangePasswordViewModel
                {
                    CurrentPassword = Request.Form["CurrentPassword"].ToString(),
                    NewPassword = Request.Form["NewPassword"].ToString(),
                    ConfirmPassword = Request.Form["ConfirmPassword"].ToString()
                };

                // Validate the model
                if (string.IsNullOrEmpty(changePasswordViewModel.CurrentPassword))
                {
                    ModelState.AddModelError(nameof(CurrentPassword), "Current password is required.");
                }

                if (string.IsNullOrEmpty(changePasswordViewModel.NewPassword))
                {
                    ModelState.AddModelError(nameof(NewPassword), "New password is required.");
                }

                if (string.IsNullOrEmpty(changePasswordViewModel.ConfirmPassword))
                {
                    ModelState.AddModelError(nameof(ConfirmPassword), "Confirm password is required.");
                }

                if (changePasswordViewModel.NewPassword != changePasswordViewModel.ConfirmPassword)
                {
                    ModelState.AddModelError(nameof(ConfirmPassword), "New password and confirmation password do not match.");
                }

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                // Call the auth service to change password
                var result = await _authService.ChangePasswordAsync(userId, changePasswordViewModel);

                if (!result.Success)
                {
                    if (!string.IsNullOrEmpty(result.ValidationError) && !string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        ModelState.AddModelError(result.ValidationError, result.ErrorMessage);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to change password.";
                    }

                    // Preserve the form data but clear passwords for security
                    ChangePasswordData = result.ViewModel ?? new ChangePasswordViewModel();
                    ChangePasswordData.CurrentPassword = "";
                    ChangePasswordData.NewPassword = "";
                    ChangePasswordData.ConfirmPassword = "";

                    return Page();
                }

                TempData["SuccessMessage"] = result.SuccessMessage ?? "Your password has been changed successfully.";
                return RedirectToPage("/Admin/Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for admin user: {UserId}", User.FindFirst("UserId")?.Value);
                TempData["ErrorMessage"] = "An error occurred while changing your password. Please try again.";

                // Clear password fields for security
                ChangePasswordData = new ChangePasswordViewModel();
                return Page();
            }
        }
    }
}
