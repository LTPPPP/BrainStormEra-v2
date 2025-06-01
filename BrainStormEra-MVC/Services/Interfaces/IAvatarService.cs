using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface IAvatarService
    {
        Task<(bool Success, string? ImagePath, string? ErrorMessage)> UploadAvatarAsync(IFormFile file, string userId);
        Task<bool> DeleteAvatarAsync(string? imageFileName);
        string GetAvatarUrl(string? imageFileName);
        bool AvatarExists(string? imageFileName);
        byte[] GetDefaultAvatarBytes();
        string GetImageContentType(string fileName);
    }
}
