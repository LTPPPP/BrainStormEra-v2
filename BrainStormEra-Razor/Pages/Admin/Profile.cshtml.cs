using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using System.IO;

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

                // Save the file
                var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{avatarFile.FileName}";
                var filePath = Path.Combine("wwwroot/SharedMedia/avatars", fileName);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                var avatarUrl = $"/SharedMedia/avatars/{fileName}";

                // For now, just return success (implement UpdateUserAvatarAsync later)
                // var updateResult = await _userService.UpdateUserAvatarAsync(userId, avatarUrl);
                var updateResult = true; // Placeholder

                if (updateResult)
                {
                    _logger.LogInformation("Avatar updated for user: {UserId}", userId);
                    return new JsonResult(new { success = true, message = "Avatar updated successfully", avatarUrl = avatarUrl });
                }
                else
                {
                    // Delete the uploaded file if database update failed
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    return new JsonResult(new { success = false, message = "Failed to update avatar in database" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return new JsonResult(new { success = false, message = "An error occurred while uploading the avatar" });
            }
        }

        public async Task<IActionResult> OnPostGenerateQRCodeAsync([FromBody] VNPayQRRequest request)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new JsonResult(new { success = false, message = "User not authenticated" });
                }

                // Load user profile to verify bank account info
                await LoadUserProfile(userId);

                if (UserProfile == null || string.IsNullOrEmpty(UserProfile.BankAccountNumber))
                {
                    return new JsonResult(new { success = false, message = "Bank account information not found" });
                }

                // Generate VNPay QR Code data
                var qrData = GenerateVNPayQRData(request);

                // Generate QR Code using a QR code library (you'll need to install QRCoder or similar)
                var qrCodeBase64 = GenerateQRCodeImage(qrData);

                _logger.LogInformation("QR Code generated for user: {UserId}", userId);

                return new JsonResult(new
                {
                    success = true,
                    qrCodeData = qrCodeBase64,
                    bankInfo = new
                    {
                        bankName = UserProfile.BankName,
                        accountHolder = UserProfile.AccountHolderName,
                        accountNumber = UserProfile.BankAccountNumber,
                        amount = request.Amount,
                        description = request.Description
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code");
                return new JsonResult(new { success = false, message = "An error occurred while generating QR code" });
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

        private string GenerateVNPayQRData(VNPayQRRequest request)
        {
            // VNPay QR Code format: Bank Code|Account Number|Template|Amount|Description|Terminal ID
            // This is a simplified format - you should use the official VNPay QR format
            var qrContent = $"BANK|{UserProfile?.BankAccountNumber}|{UserProfile?.BankName}|{request.Amount}|{request.Description ?? "Payment"}|VNPAY";
            return qrContent;
        }

        private string GenerateQRCodeImage(string qrData)
        {
            try
            {
                // You'll need to install QRCoder NuGet package: Install-Package QRCoder
                // For now, returning a placeholder
                // In real implementation:
                /*
                using QRCoder;
                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);
                return Convert.ToBase64String(qrCodeBytes);
                */

                // Placeholder base64 QR code image
                return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code image");
                throw;
            }
        }

        private int GetRandomStat(int min, int max)
        {
            var random = new Random();
            return random.Next(min, max + 1);
        }
    }
}