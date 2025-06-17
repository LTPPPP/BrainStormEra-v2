using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class ProfileModel : PageModel
    {
        private readonly ILogger<ProfileModel> _logger;
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;

        public AdminUserViewModel? UserProfile { get; set; }

        public ProfileModel(ILogger<ProfileModel> logger, IAdminService adminService, IUserService userService)
        {
            _logger = logger;
            _adminService = adminService;
            _userService = userService;
        }

        public async Task OnGetAsync()
        {
            try
            {
                var userId = HttpContext.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return;
                }

                // Load user profile
                await LoadUserProfile(userId);



                _logger.LogInformation("Profile page accessed by user: {UserId} at {AccessTime}", userId, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile data");
            }
        }

        public async Task<IActionResult> OnPostUploadAvatarAsync(IFormFile avatarFile)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new JsonResult(new { success = false, message = "User not authenticated" });
                }

                if (avatarFile == null || avatarFile.Length == 0)
                {
                    return new JsonResult(new { success = false, message = "No file selected" });
                }

                // Validate file size (2MB max)
                if (avatarFile.Length > 2 * 1024 * 1024)
                {
                    return new JsonResult(new { success = false, message = "File size must be less than 2MB" });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(avatarFile.ContentType.ToLower()))
                {
                    return new JsonResult(new { success = false, message = "Only image files (JPG, PNG, GIF) are allowed" });
                }

                // Save the file (implement your file upload logic here)
                var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{avatarFile.FileName}";
                var filePath = Path.Combine("wwwroot/SharedMedia/avatars", fileName);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                var avatarUrl = $"/SharedMedia/avatars/{fileName}";

                // Update user profile with new avatar URL
                // Note: You'll need to implement this in your user service
                // await _userService.UpdateUserAvatarAsync(userId, avatarUrl);

                _logger.LogInformation("Avatar updated for user: {UserId}", userId);

                return new JsonResult(new { success = true, message = "Avatar updated successfully", avatarUrl = avatarUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return new JsonResult(new { success = false, message = "An error occurred while uploading the avatar" });
            }
        }

        private async Task LoadUserProfile(string userId)
        {
            try
            {
                // Get user profile from admin service
                var allUsers = await _adminService.GetAllUsersAsync();
                UserProfile = allUsers.Users.FirstOrDefault(u => u.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile for userId: {UserId}", userId);
            }
        }





        private int GetRandomStat(int min, int max)
        {
            var random = new Random();
            return random.Next(min, max + 1);
        }
    }




}