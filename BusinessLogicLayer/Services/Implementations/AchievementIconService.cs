using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Constants;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Implementations
{
    public class AchievementIconService : IAchievementIconService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<AchievementIconService> _logger;
        private readonly IMediaPathService _mediaPathService;
        private const string MediaCategory = MediaConstants.Categories.Icons;
        private const long MaxFileSize = 2 * 1024 * 1024; // 2MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };

        public AchievementIconService(IWebHostEnvironment webHostEnvironment, ILogger<AchievementIconService> logger, IMediaPathService mediaPathService)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _mediaPathService = mediaPathService;
        }

        public async Task<(bool Success, string? IconPath, string? ErrorMessage)> UploadAchievementIconAsync(IFormFile file, string achievementId)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return (false, null, "No file selected.");
                }

                // Check file size
                if (file.Length > MaxFileSize)
                {
                    return (false, null, "File size cannot exceed 2MB.");
                }

                // Check file type
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!Array.Exists(AllowedExtensions, ext => ext == fileExtension))
                {
                    return (false, null, "Only JPG, JPEG, PNG, GIF, WEBP, and SVG files are allowed.");
                }

                // Create upload directory if it doesn't exist
                _mediaPathService.EnsureDirectoryExists(MediaCategory);
                var uploadDir = _mediaPathService.GetPhysicalPath(MediaCategory);

                // Generate unique filename
                var fileName = $"achievement_{achievementId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Achievement icon uploaded successfully for achievement {AchievementId}: {FileName}", achievementId, fileName);
                return (true, _mediaPathService.GetWebUrl(MediaCategory, fileName), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading achievement icon for achievement: {AchievementId}", achievementId);
                return (false, null, "An error occurred while uploading the achievement icon.");
            }
        }

        public Task<bool> DeleteAchievementIconAsync(string? iconFileName)
        {
            try
            {
                if (string.IsNullOrEmpty(iconFileName))
                    return Task.FromResult(true);

                // Extract filename from path if needed
                var fileName = Path.GetFileName(iconFileName);
                var iconPath = Path.Combine(_mediaPathService.GetPhysicalPath(MediaCategory), fileName);

                if (File.Exists(iconPath))
                {
                    File.Delete(iconPath);
                    _logger.LogInformation("Achievement icon deleted successfully: {IconFileName}", fileName);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete achievement icon: {IconFileName}", iconFileName);
                return Task.FromResult(false);
            }
        }

        public string GetAchievementIconUrl(string? iconFileName)
        {
            if (string.IsNullOrEmpty(iconFileName))
                return MediaConstants.Defaults.DefaultAchievementPath; // Return default achievement icon URL

            // If iconFileName is already a full path, return as is
            if (iconFileName.StartsWith("/"))
                return iconFileName;

            // If it's a FontAwesome class, return as is
            if (iconFileName.StartsWith("fas ") || iconFileName.StartsWith("far ") || iconFileName.StartsWith("fab "))
                return iconFileName;

            return _mediaPathService.GetWebUrl(MediaCategory, iconFileName);
        }

        public bool AchievementIconExists(string? iconFileName)
        {
            if (string.IsNullOrEmpty(iconFileName))
                return false;

            // FontAwesome classes always "exist"
            if (iconFileName.StartsWith("fas ") || iconFileName.StartsWith("far ") || iconFileName.StartsWith("fab "))
                return true;

            var fileName = Path.GetFileName(iconFileName);
            var iconPath = Path.Combine(_mediaPathService.GetPhysicalPath(MediaCategory), fileName);
            return File.Exists(iconPath);
        }

        public string GetImageContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
    }
}