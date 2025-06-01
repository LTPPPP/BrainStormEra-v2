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
        private readonly IAvatarService _avatarService;

        public ProfileController(
            BrainStormEraContext context,
            ILogger<ProfileController> logger,
            IUserService userService,
            IAvatarService avatarService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
            _avatarService = avatarService;
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
                    // Delete old avatar before uploading new one
                    if (!string.IsNullOrEmpty(user.UserImage))
                    {
                        await _avatarService.DeleteAvatarAsync(user.UserImage);
                    }

                    var uploadResult = await _avatarService.UploadAvatarAsync(model.ProfileImage, userId);
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

        [HttpGet]
        public async Task<IActionResult> GetAvatar(string? userId = null)
        {
            try
            {
                var targetUserId = userId ?? CurrentUserId;
                if (string.IsNullOrEmpty(targetUserId))
                {
                    return File(_avatarService.GetDefaultAvatarBytes(), "image/svg+xml");
                }

                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserId == targetUserId);

                if (user?.UserImage == null || !_avatarService.AvatarExists(user.UserImage))
                {
                    return File(_avatarService.GetDefaultAvatarBytes(), "image/svg+xml");
                }

                var imagePath = Path.Combine("wwwroot", "images", "users", user.UserImage);
                var contentType = _avatarService.GetImageContentType(user.UserImage);
                var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);

                return File(imageBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving avatar for user: {UserId}", userId);
                return File(_avatarService.GetDefaultAvatarBytes(), "image/svg+xml");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvatar()
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var user = await _context.Accounts.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                if (!string.IsNullOrEmpty(user.UserImage))
                {
                    await _avatarService.DeleteAvatarAsync(user.UserImage);
                    user.UserImage = null;
                    user.AccountUpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Avatar deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar for user: {UserId}", CurrentUserId);
                return Json(new { success = false, message = "An error occurred while deleting avatar" });
            }
        }
    }
}
