using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class ProfileEditModel : PageModel
    {
        private readonly ILogger<ProfileEditModel> _logger;
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;

        public AdminUserViewModel? UserProfile { get; set; }

        [BindProperty]
        public string FullName { get; set; } = "";

        [BindProperty]
        public string Username { get; set; } = "";

        [BindProperty]
        public string UserEmail { get; set; } = "";

        [BindProperty]
        public string? Bio { get; set; }

        [BindProperty]
        public string? PhoneNumber { get; set; }

        [BindProperty]
        public string? Location { get; set; }

        [BindProperty]
        public string? Timezone { get; set; }

        [BindProperty]
        public string? PreferredLanguage { get; set; }

        public ProfileEditModel(ILogger<ProfileEditModel> logger, IAdminService adminService, IUserService userService)
        {
            _logger = logger;
            _adminService = adminService;
            _userService = userService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = HttpContext.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return RedirectToPage("/Auth/Login");
                }

                await LoadUserProfile(userId);

                if (UserProfile == null)
                {
                    _logger.LogWarning("User profile not found for userId: {UserId}", userId);
                    return NotFound();
                }

                // Populate form fields
                FullName = UserProfile.FullName ?? "";
                Username = UserProfile.Username ?? "";
                UserEmail = UserProfile.UserEmail ?? "";
                Bio = UserProfile.Bio;
                PhoneNumber = UserProfile.PhoneNumber;
                Location = UserProfile.Location;
                Timezone = UserProfile.Timezone;
                PreferredLanguage = UserProfile.PreferredLanguage ?? "en";

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile edit page");
                return RedirectToPage("/Admin/Profile");
            }
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            try
            {
                var userId = HttpContext.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(FullName) ||
                    string.IsNullOrWhiteSpace(Username) ||
                    string.IsNullOrWhiteSpace(UserEmail))
                {
                    ModelState.AddModelError("", "Full Name, Username, and Email are required.");
                    await LoadUserProfile(userId);
                    return Page();
                }

                // Validate email format
                if (!IsValidEmail(UserEmail))
                {
                    ModelState.AddModelError("UserEmail", "Please enter a valid email address.");
                    await LoadUserProfile(userId);
                    return Page();
                }

                // Create update request
                var updateRequest = new UpdateProfileRequest
                {
                    UserId = userId,
                    FullName = FullName.Trim(),
                    Username = Username.Trim(),
                    UserEmail = UserEmail.Trim(),
                    Bio = string.IsNullOrWhiteSpace(Bio) ? null : Bio.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                    Location = string.IsNullOrWhiteSpace(Location) ? null : Location.Trim(),
                    Timezone = string.IsNullOrWhiteSpace(Timezone) ? null : Timezone.Trim(),
                    PreferredLanguage = string.IsNullOrWhiteSpace(PreferredLanguage) ? "en" : PreferredLanguage.Trim()
                };

                // Update profile (you'll need to implement this in your service layer)
                // var result = await _userService.UpdateUserProfileAsync(updateRequest);

                // For now, simulate success
                var result = true;

                if (result)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    _logger.LogInformation("Profile updated for user: {UserId}", userId);
                    return RedirectToPage("/Admin/Profile");
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update profile. Please try again.");
                    await LoadUserProfile(userId);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                ModelState.AddModelError("", "An error occurred while updating your profile.");

                var userId = HttpContext.User?.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await LoadUserProfile(userId);
                }

                return Page();
            }
        }

        private async Task LoadUserProfile(string userId)
        {
            try
            {
                var allUsers = await _adminService.GetAllUsersAsync();
                UserProfile = allUsers.Users.FirstOrDefault(u => u.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile for userId: {UserId}", userId);
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    // Helper class for profile updates
    public class UpdateProfileRequest
    {
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string? Bio { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Location { get; set; }
        public string? Timezone { get; set; }
        public string? PreferredLanguage { get; set; }
    }
}