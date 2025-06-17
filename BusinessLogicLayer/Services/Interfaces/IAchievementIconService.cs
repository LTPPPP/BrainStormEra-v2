using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IAchievementIconService
    {
        Task<(bool Success, string? IconPath, string? ErrorMessage)> UploadAchievementIconAsync(IFormFile file, string achievementId);
        Task<bool> DeleteAchievementIconAsync(string? iconFileName);
        string GetAchievementIconUrl(string? iconFileName);
        bool AchievementIconExists(string? iconFileName);
        string GetImageContentType(string fileName);
    }
}