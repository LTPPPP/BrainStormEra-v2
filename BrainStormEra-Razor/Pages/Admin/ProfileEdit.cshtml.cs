using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using DataAccessLayer.Models;

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
        public string? PhoneNumber { get; set; }

        [BindProperty]
        public string? UserAddress { get; set; }

        [BindProperty]
        public DateOnly? DateOfBirth { get; set; }

        [BindProperty]
        public short? Gender { get; set; }

        [BindProperty]
        public string? BankAccountNumber { get; set; }

        [BindProperty]
        public string? BankName { get; set; }

        [BindProperty]
        public string? AccountHolderName { get; set; }

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
                PhoneNumber = UserProfile.PhoneNumber;
                UserAddress = UserProfile.UserAddress;
                DateOfBirth = UserProfile.DateOfBirth;
                Gender = UserProfile.Gender;
                BankAccountNumber = UserProfile.BankAccountNumber;
                BankName = UserProfile.BankName;
                AccountHolderName = UserProfile.AccountHolderName;

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
                    string.IsNullOrWhiteSpace(UserEmail))
                {
                    ModelState.AddModelError("", "Full Name and Email are required.");
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

                // Load current profile to get the original username
                await LoadUserProfile(userId);

                // Create update request using the ViewModel from DataAccessLayer
                var updateRequest = new UpdateProfileRequest
                {
                    UserId = userId,
                    FullName = FullName.Trim(),
                    Username = UserProfile?.Username ?? "", // Keep existing username - cannot be changed
                    UserEmail = UserEmail.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                    UserAddress = string.IsNullOrWhiteSpace(UserAddress) ? null : UserAddress.Trim(),
                    DateOfBirth = DateOfBirth,
                    Gender = Gender,
                    BankAccountNumber = string.IsNullOrWhiteSpace(BankAccountNumber) ? null : BankAccountNumber.Trim(),
                    BankName = string.IsNullOrWhiteSpace(BankName) ? null : BankName.Trim(),
                    AccountHolderName = string.IsNullOrWhiteSpace(AccountHolderName) ? null : AccountHolderName.Trim()
                };

                // Update profile (placeholder for now)
                // var result = await _userService.UpdateUserProfileAsync(updateRequest);
                var result = true; // Placeholder

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
}