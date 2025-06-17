using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace BusinessLogicLayer.Services
{
    public class MediaPathService : IMediaPathService
    {
        private readonly ILogger<MediaPathService> _logger;
        private readonly string _mediaRootPath;

        public MediaPathService(ILogger<MediaPathService> logger)
        {
            _logger = logger;
            // SharedMedia folder is at the solution root level
            _mediaRootPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SharedMedia");
        }

        public string GetPhysicalPath(string category)
        {
            return Path.Combine(_mediaRootPath, category);
        }

        public string GetWebUrl(string category, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            return $"/SharedMedia/{category}/{fileName}";
        }

        public void EnsureDirectoryExists(string category)
        {
            try
            {
                var directoryPath = GetPhysicalPath(category);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    _logger.LogInformation("Created media directory: {DirectoryPath}", directoryPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create media directory for category: {Category}", category);
                throw;
            }
        }
    }
}