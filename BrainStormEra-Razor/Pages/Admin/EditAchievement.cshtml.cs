using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class EditAchievementModel : PageModel
    {
        private readonly ILogger<EditAchievementModel> _logger;
        private readonly IAchievementService _achievementService;

        public string? AdminName { get; set; }
        public string? UserId { get; set; }

        [BindProperty]
        public UpdateAchievementRequest Achievement { get; set; } = new UpdateAchievementRequest
        {
            AchievementId = "",
            AchievementName = "",
            AchievementType = "",
            AchievementIcon = "fas fa-trophy",
            AchievementDescription = ""
        };

        [BindProperty]
        public IFormFile? IconFile { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public bool AchievementNotFound { get; set; } = false;

        public EditAchievementModel(ILogger<EditAchievementModel> logger, IAchievementService achievementService)
        {
            _logger = logger;
            _achievementService = achievementService;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                AdminName = HttpContext.User?.Identity?.Name ?? "Admin";
                UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Edit achievement accessed without ID by admin: {AdminName}", AdminName);
                    return RedirectToPage("/Admin/Achievements");
                }

                // Load existing achievement data
                var existingAchievement = await _achievementService.GetAchievementByIdAsync(id);
                if (existingAchievement == null)
                {
                    _logger.LogWarning("Achievement not found: {AchievementId} by admin: {AdminName}", id, AdminName);
                    AchievementNotFound = true;
                    return Page();
                }

                // Map to edit model
                Achievement = new UpdateAchievementRequest
                {
                    AchievementId = existingAchievement.AchievementId,
                    AchievementName = existingAchievement.AchievementName,
                    AchievementDescription = existingAchievement.AchievementDescription,
                    AchievementIcon = existingAchievement.AchievementIcon,
                    AchievementType = existingAchievement.AchievementType
                };

                _logger.LogInformation("Edit achievement page accessed by admin: {AdminName} for achievement: {AchievementId} at {AccessTime}",
                    AdminName, id, DateTime.UtcNow);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit achievement page for admin: {UserId}, AchievementId: {AchievementId}", UserId, id);
                ErrorMessage = "An error occurred while loading the achievement data.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Set UserId from claims
                UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";
                AdminName = HttpContext.User?.Identity?.Name ?? "Admin";

                _logger.LogInformation("Edit achievement form submitted by admin: {AdminName}, Request: {@Request}", AdminName, Achievement);

                if (string.IsNullOrEmpty(UserId))
                {
                    _logger.LogWarning("UserId not found in claims");
                    ErrorMessage = "User authentication failed. Please log in again.";
                    return Page();
                }

                // Validate achievement type against allowed values
                var allowedTypes = new[] { "course_completion", "quiz_master", "streak", "first_course", "instructor", "student_engagement" };
                if (!allowedTypes.Contains(Achievement.AchievementType?.ToLower()))
                {
                    _logger.LogWarning("Invalid achievement type: {Type}. Allowed types: {AllowedTypes}",
                        Achievement.AchievementType, string.Join(", ", allowedTypes));
                    ModelState.AddModelError("Achievement.AchievementType",
                        $"Invalid achievement type. Allowed types: {string.Join(", ", allowedTypes)}");
                }

                // Additional validation
                if (string.IsNullOrWhiteSpace(Achievement.AchievementName))
                {
                    ModelState.AddModelError("Achievement.AchievementName", "Achievement name is required.");
                }

                if (string.IsNullOrWhiteSpace(Achievement.AchievementType))
                {
                    ModelState.AddModelError("Achievement.AchievementType", "Achievement type is required.");
                }

                if (Achievement.AchievementName?.Length > 100)
                {
                    ModelState.AddModelError("Achievement.AchievementName", "Achievement name cannot exceed 100 characters.");
                }

                if (Achievement.AchievementDescription?.Length > 500)
                {
                    ModelState.AddModelError("Achievement.AchievementDescription", "Achievement description cannot exceed 500 characters.");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));

                    ErrorMessage = "Please correct the errors and try again.";
                    return Page();
                }

                // Get current achievement to preserve existing icon if no new upload
                var currentAchievement = await _achievementService.GetAchievementByIdAsync(Achievement.AchievementId);
                if (currentAchievement == null)
                {
                    _logger.LogError("Achievement not found during update: {AchievementId}", Achievement.AchievementId);
                    ErrorMessage = "Achievement not found. It may have been deleted.";
                    return Page();
                }

                string iconToUse = Achievement.AchievementIcon ?? currentAchievement.AchievementIcon;
                string? oldCustomIconPath = null;

                // Handle file upload if present and icon indicates custom upload
                if (IconFile != null && Achievement.AchievementIcon?.StartsWith("CUSTOM_UPLOAD:") == true)
                {
                    // Store old custom icon path for cleanup if it's a custom upload
                    if (currentAchievement.AchievementIcon?.StartsWith("/uploads/") == true)
                    {
                        oldCustomIconPath = currentAchievement.AchievementIcon;
                    }

                    var uploadResult = await HandleIconUploadAsync(IconFile);
                    if (uploadResult.Success)
                    {
                        iconToUse = uploadResult.IconPath ?? currentAchievement?.AchievementIcon ?? "fas fa-trophy";
                        _logger.LogInformation("Icon uploaded successfully: {IconPath}", uploadResult.IconPath);
                    }
                    else
                    {
                        ModelState.AddModelError("IconFile", uploadResult.ErrorMessage ?? "Upload failed");
                        ErrorMessage = uploadResult.ErrorMessage ?? "Upload failed";
                        return Page();
                    }
                }

                // Normalize achievement type to lowercase
                if (!string.IsNullOrEmpty(Achievement.AchievementType))
                {
                    Achievement.AchievementType = Achievement.AchievementType.ToLower();
                }

                // Create update request
                var updateRequest = new UpdateAchievementRequest
                {
                    AchievementId = Achievement.AchievementId ?? "",
                    AchievementName = Achievement.AchievementName ?? "",
                    AchievementDescription = Achievement.AchievementDescription ?? "",
                    AchievementIcon = iconToUse ?? "fas fa-trophy",
                    AchievementType = Achievement.AchievementType ?? ""
                };

                var success = await _achievementService.UpdateAchievementAsync(updateRequest, UserId);

                if (success)
                {
                    // Clean up old custom icon if a new one was uploaded
                    if (!string.IsNullOrEmpty(oldCustomIconPath) && iconToUse != oldCustomIconPath)
                    {
                        await CleanupOldIconAsync(oldCustomIconPath);
                    }

                    _logger.LogInformation("Achievement updated successfully by admin {AdminName}: {AchievementName}, ID: {AchievementId}",
                        AdminName, Achievement.AchievementName, Achievement.AchievementId);

                    SuccessMessage = $"Achievement '{Achievement.AchievementName}' has been updated successfully!";

                    // Redirect to achievements list page
                    return RedirectToPage("/Admin/Achievements");
                }
                else
                {
                    _logger.LogError("Failed to update achievement: {AchievementName}", Achievement.AchievementName);
                    ErrorMessage = "Failed to update achievement. Please try again.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating achievement: {AchievementName}", Achievement?.AchievementName);
                ErrorMessage = "An error occurred while updating the achievement. Please try again.";
                return Page();
            }
        }

        public IActionResult OnPostCancel()
        {
            return RedirectToPage("/Admin/Achievements");
        }

        private async Task<IconUploadResult> HandleIconUploadAsync(IFormFile iconFile)
        {
            try
            {
                // Validate file
                if (iconFile == null || iconFile.Length == 0)
                {
                    return new IconUploadResult { Success = false, ErrorMessage = "No file selected" };
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/svg+xml", "image/webp" };
                if (!allowedTypes.Contains(iconFile.ContentType.ToLower()))
                {
                    return new IconUploadResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid file type. Only PNG, JPG, SVG, and WEBP files are allowed."
                    };
                }

                // Validate file size (2MB)
                if (iconFile.Length > 2 * 1024 * 1024)
                {
                    return new IconUploadResult
                    {
                        Success = false,
                        ErrorMessage = "File size must be less than 2MB."
                    };
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine("wwwroot", "uploads", "achievements", "icons");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(iconFile.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await iconFile.CopyToAsync(stream);
                }

                // Return web path
                var webPath = $"/uploads/achievements/icons/{fileName}";
                return new IconUploadResult { Success = true, IconPath = webPath };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading achievement icon");
                return new IconUploadResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while uploading the icon."
                };
            }
        }

        private async Task CleanupOldIconAsync(string iconPath)
        {
            try
            {
                if (string.IsNullOrEmpty(iconPath) || !iconPath.StartsWith("/uploads/"))
                    return;

                var fullPath = Path.Combine("wwwroot", iconPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    await Task.Run(() => System.IO.File.Delete(fullPath));
                    _logger.LogInformation("Cleaned up old achievement icon: {IconPath}", iconPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup old achievement icon: {IconPath}", iconPath);
                // Don't fail the operation for cleanup issues
            }
        }
    }

    public class EditAchievementRequest
    {
        [Required]
        public string AchievementId { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string AchievementName { get; set; } = "";

        [StringLength(500)]
        public string? AchievementDescription { get; set; }

        [StringLength(255)]
        public string AchievementIcon { get; set; } = "fas fa-trophy";

        [Required]
        [StringLength(50)]
        public string AchievementType { get; set; } = "";
    }

}
