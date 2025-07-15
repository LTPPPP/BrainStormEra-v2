using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class CreateAchievementModel : PageModel
    {
        private readonly ILogger<CreateAchievementModel> _logger;
        private readonly IAchievementService _achievementService;

        public string? AdminName { get; set; }
        public string? UserId { get; set; }

        [BindProperty]
        public CreateAchievementRequest Achievement { get; set; } = new CreateAchievementRequest
        {
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

        public CreateAchievementModel(ILogger<CreateAchievementModel> logger, IAchievementService achievementService)
        {
            _logger = logger;
            _achievementService = achievementService;
        }

        public void OnGet()
        {
            try
            {
                AdminName = HttpContext.User?.Identity?.Name ?? "Admin";
                UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";

                _logger.LogInformation("Create achievement page accessed by admin: {AdminName} at {AccessTime}", AdminName, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create achievement page for admin: {UserId}", UserId);
                ErrorMessage = "An error occurred while loading the page.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Set UserId from claims
                UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";
                AdminName = HttpContext.User?.Identity?.Name ?? "Admin";

                _logger.LogInformation("Create achievement form submitted by admin: {AdminName}, Request: {@Request}", AdminName, Achievement);

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

                // Normalize achievement type to lowercase
                if (!string.IsNullOrEmpty(Achievement.AchievementType))
                {
                    Achievement.AchievementType = Achievement.AchievementType.ToLower();
                }

                // Set default icon if empty
                if (string.IsNullOrWhiteSpace(Achievement.AchievementIcon))
                {
                    Achievement.AchievementIcon = "fas fa-trophy";
                }

                // Handle file upload if present
                if (IconFile != null && Achievement.AchievementIcon.StartsWith("CUSTOM_UPLOAD:"))
                {
                    var uploadResult = await HandleIconUploadAsync(IconFile);
                    if (uploadResult.Success)
                    {
                        Achievement.AchievementIcon = uploadResult.IconPath ?? "fas fa-trophy";
                        _logger.LogInformation("Icon uploaded successfully: {IconPath}", uploadResult.IconPath);
                    }
                    else
                    {
                        ModelState.AddModelError("IconFile", uploadResult.ErrorMessage ?? "Upload failed");
                        ErrorMessage = uploadResult.ErrorMessage ?? "Upload failed";
                        return Page();
                    }
                }

                var (success, achievementId) = await _achievementService.CreateAchievementAsync(Achievement, UserId);

                if (success)
                {
                    _logger.LogInformation("Achievement created successfully by admin {AdminName}: {AchievementName}, ID: {AchievementId}",
                        AdminName, Achievement.AchievementName, achievementId);

                    SuccessMessage = $"Achievement '{Achievement.AchievementName}' has been created successfully!";

                    // Redirect to achievements list page
                    return RedirectToPage("/Admin/Achievements");
                }
                else
                {
                    _logger.LogError("Failed to create achievement: {AchievementName}", Achievement.AchievementName);
                    ErrorMessage = "Failed to create achievement. Please try again.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating achievement: {AchievementName}", Achievement?.AchievementName);
                ErrorMessage = "An error occurred while creating the achievement. Please try again.";
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
    }
}
