using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface ICourseImageService
    {
        Task<(bool Success, string? ImagePath, string? ErrorMessage)> UploadCourseImageAsync(IFormFile file, string courseId);
        Task<bool> DeleteCourseImageAsync(string? imageFileName);
        string GetCourseImageUrl(string? imageFileName);
        bool CourseImageExists(string? imageFileName);
        string GetImageContentType(string fileName);
    }
}







