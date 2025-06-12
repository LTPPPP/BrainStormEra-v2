using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BusinessLogicLayer.Services.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class AvatarService : IAvatarService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<AvatarService> _logger;
        private const string AvatarDirectory = "img/profiles";
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public AvatarService(IWebHostEnvironment webHostEnvironment, ILogger<AvatarService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<(bool Success, string? ImagePath, string? ErrorMessage)> UploadAvatarAsync(IFormFile file, string userId)
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
                    return (false, null, "File size cannot exceed 5MB.");
                }

                // Check file type
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!Array.Exists(AllowedExtensions, ext => ext == fileExtension))
                {
                    return (false, null, "Only JPG, JPEG, PNG, GIF, and WEBP files are allowed.");
                }

                // Create upload directory if it doesn't exist
                var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, AvatarDirectory);
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                // Generate unique filename
                var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Avatar uploaded successfully for user {UserId}: {FileName}", userId, fileName);
                return (true, fileName, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for user: {UserId}", userId);
                return (false, null, "An error occurred while uploading the avatar.");
            }
        }
        public Task<bool> DeleteAvatarAsync(string? imageFileName)
        {
            try
            {
                if (string.IsNullOrEmpty(imageFileName))
                    return Task.FromResult(true);

                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, AvatarDirectory, imageFileName);
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    _logger.LogInformation("Avatar deleted successfully: {ImageFileName}", imageFileName);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete avatar image: {ImageFileName}", imageFileName);
                return Task.FromResult(false);
            }
        }

        public string GetAvatarUrl(string? imageFileName)
        {
            if (string.IsNullOrEmpty(imageFileName))
                return "/Profile/GetAvatar"; // Return default avatar URL

            return $"/{AvatarDirectory}/{imageFileName}";
        }

        public bool AvatarExists(string? imageFileName)
        {
            if (string.IsNullOrEmpty(imageFileName))
                return false;

            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, AvatarDirectory, imageFileName);
            return File.Exists(imagePath);
        }

        public byte[] GetDefaultAvatarBytes()
        {
            var svg = @"<svg xmlns='http://www.w3.org/2000/svg' width='120' height='120' viewBox='0 0 120 120'>
                <defs>
                    <linearGradient id='grad1' x1='0%' y1='0%' x2='100%' y2='100%'>
                        <stop offset='0%' style='stop-color:#667eea;stop-opacity:1' />
                        <stop offset='100%' style='stop-color:#764ba2;stop-opacity:1' />
                    </linearGradient>
                </defs>
                <circle cx='60' cy='60' r='60' fill='url(#grad1)'/>
                <circle cx='60' cy='45' r='18' fill='white' opacity='0.9'/>
                <ellipse cx='60' cy='85' rx='28' ry='18' fill='white' opacity='0.9'/>
            </svg>";
            return System.Text.Encoding.UTF8.GetBytes(svg);
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
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
    }
}







