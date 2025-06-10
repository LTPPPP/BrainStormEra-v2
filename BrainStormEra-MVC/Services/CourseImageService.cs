using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BrainStormEra_MVC.Services
{
    public class CourseImageService : ICourseImageService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<CourseImageService> _logger;
        private const string CourseImageDirectory = "img/courses";
        private const long MaxFileSize = 10 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public CourseImageService(IWebHostEnvironment webHostEnvironment, ILogger<CourseImageService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<(bool Success, string? ImagePath, string? ErrorMessage)> UploadCourseImageAsync(IFormFile file, string courseId)
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
                var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, CourseImageDirectory);
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                // Generate unique filename
                var fileName = $"{courseId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Course image uploaded successfully for course {CourseId}: {FileName}", courseId, fileName);
                return (true, $"/{CourseImageDirectory}/{fileName}", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading course image for course: {CourseId}", courseId);
                return (false, null, "An error occurred while uploading the course image.");
            }
        }

        public Task<bool> DeleteCourseImageAsync(string? imageFileName)
        {
            try
            {
                if (string.IsNullOrEmpty(imageFileName))
                    return Task.FromResult(true);

                // Extract filename from path if needed
                var fileName = Path.GetFileName(imageFileName);
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, CourseImageDirectory, fileName);

                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    _logger.LogInformation("Course image deleted successfully: {ImageFileName}", fileName);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete course image: {ImageFileName}", imageFileName);
                return Task.FromResult(false);
            }
        }
        public string GetCourseImageUrl(string? imageFileName)
        {
            if (string.IsNullOrEmpty(imageFileName))
                return "/img/defaults/default-course.svg"; // Return default course image URL

            // If imageFileName is already a full path, return as is
            if (imageFileName.StartsWith("/"))
                return imageFileName;

            return $"/{CourseImageDirectory}/{imageFileName}";
        }

        public bool CourseImageExists(string? imageFileName)
        {
            if (string.IsNullOrEmpty(imageFileName))
                return false;

            var fileName = Path.GetFileName(imageFileName);
            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, CourseImageDirectory, fileName);
            return File.Exists(imagePath);
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
