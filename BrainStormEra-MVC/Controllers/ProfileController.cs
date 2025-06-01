using System.Security.Claims;
using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class ProfileController : BaseController
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<ProfileController> _logger;
        private readonly IUserService _userService;

        public ProfileController(
            BrainStormEraContext context,
            ILogger<ProfileController> logger,
            IUserService userService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to view your profile.";
                    return RedirectToAction("Index", "Login");
                }

                // Get user data from database
                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User profile not found.";
                    return RedirectToAction("Index", "Login");
                }

                // Get user statistics based on role
                var enrolledCoursesCount = 0;
                var completedCoursesCount = 0;
                var createdCoursesCount = 0;
                var totalStudentsCount = 0;

                if (IsLearner)
                {
                    enrolledCoursesCount = await _context.Enrollments
                        .CountAsync(e => e.UserId == userId);

                    completedCoursesCount = await _context.Enrollments
                        .CountAsync(e => e.UserId == userId && e.ProgressPercentage >= 100);
                }
                else if (IsInstructor)
                {
                    createdCoursesCount = await _context.Courses
                        .CountAsync(c => c.AuthorId == userId);

                    totalStudentsCount = await _context.Enrollments
                        .Where(e => e.Course.AuthorId == userId)
                        .CountAsync();
                }                // Get additional statistics
                var certificatesCount = await _context.Certificates
                    .CountAsync(c => c.UserId == userId);

                var achievementsCount = await _context.UserAchievements
                    .CountAsync(ua => ua.UserId == userId);

                var inProgressCoursesCount = await _context.Enrollments
                    .CountAsync(e => e.UserId == userId && e.ProgressPercentage > 0 && e.ProgressPercentage < 100);

                // Convert gender short to string
                string genderString = user.Gender switch
                {
                    1 => "Nam",
                    2 => "Nữ",
                    3 => "Khác",
                    _ => null
                }; var viewModel = new UserProfileViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username ?? "",
                    FullName = user.FullName ?? "",
                    Email = user.UserEmail ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    UserAddress = user.UserAddress ?? "",
                    DateOfBirth = user.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
                    Gender = genderString ?? "",
                    UserImage = user.UserImage ?? "",
                    Role = CurrentUserRole ?? "",
                    CreatedAt = user.AccountCreatedAt,

                    // Statistics
                    TotalCourses = IsLearner ? enrolledCoursesCount : createdCoursesCount,
                    CompletedCourses = completedCoursesCount,
                    InProgressCourses = inProgressCoursesCount,
                    CertificatesEarned = certificatesCount,
                    TotalAchievements = achievementsCount
                };

                return View("~/Views/Profile/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while loading your profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to edit your profile.";
                    return RedirectToAction("Index", "Login");
                }

                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User profile not found.";
                    return RedirectToAction("Index", "Login");
                }                // Convert gender short to string
                string genderString = user.Gender switch
                {
                    1 => "Nam",
                    2 => "Nữ",
                    3 => "Khác",
                    _ => null
                }; var viewModel = new EditProfileViewModel
                {
                    FullName = user.FullName ?? "",
                    Email = user.UserEmail ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    UserAddress = user.UserAddress ?? "",
                    DateOfBirth = user.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
                    Gender = genderString ?? "",
                    CurrentImagePath = user.UserImage ?? ""
                };

                return View("~/Views/Profile/Edit.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit profile for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while loading the edit profile page.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Profile/Edit.cshtml", model);
            }

            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to edit your profile.";
                    return RedirectToAction("Index", "Login");
                }

                var user = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User profile not found.";
                    return RedirectToAction("Index", "Login");
                }                // Update user information
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.UserAddress = model.UserAddress;
                user.DateOfBirth = model.DateOfBirth.HasValue ? DateOnly.FromDateTime(model.DateOfBirth.Value) : null;

                // Convert gender string to short
                user.Gender = model.Gender switch
                {
                    "Nam" => 1,
                    "Nữ" => 2,
                    "Khác" => 3,
                    _ => null
                };

                user.AccountUpdatedAt = DateTime.UtcNow;

                // Handle profile image upload if provided
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var uploadResult = await UploadProfileImageAsync(model.ProfileImage, userId);
                    if (uploadResult.Success)
                    {
                        user.UserImage = uploadResult.ImagePath;
                    }
                    else
                    {
                        TempData["WarningMessage"] = uploadResult.ErrorMessage ?? "Failed to upload profile image.";
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while updating your profile.";
                return View("~/Views/Profile/Edit.cshtml", model);
            }
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View("~/Views/Profile/ChangePassword.cshtml", new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Profile/ChangePassword.cshtml", model);
            }

            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to change your password.";
                    return RedirectToAction("Index", "Login");
                }

                var user = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User account not found.";
                    return RedirectToAction("Index", "Login");
                }

                // Verify current password
                if (!await _userService.VerifyPasswordAsync(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                    return View("~/Views/Profile/ChangePassword.cshtml", model);
                }

                // Update password
                user.PasswordHash = Utilities.PasswordHasher.HashPassword(model.NewPassword);
                user.AccountUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while changing your password.";
                return View("~/Views/Profile/ChangePassword.cshtml", model);
            }
        }

        private async Task<(bool Success, string? ImagePath, string? ErrorMessage)> UploadProfileImageAsync(IFormFile file, string userId)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return (false, null, "No file selected.");
                }

                // Check file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return (false, null, "File size cannot exceed 5MB.");
                }

                // Check file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return (false, null, "Only JPG, JPEG, PNG, and GIF files are allowed.");
                }

                // Create upload directory if it doesn't exist
                var uploadDir = Path.Combine("wwwroot", "img", "profiles");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                // Generate unique filename
                var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = $"/img/profiles/{fileName}";
                return (true, relativePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile image for user: {UserId}", userId);
                return (false, null, "An error occurred while uploading the image.");
            }
        }
    }
}
