using DataAccessLayer.Data;
using DataAccessLayer.Models;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface IEnrollmentService
    {
        Task<bool> EnrollAsync(string userId, string courseId);
        Task<bool> IsEnrolledAsync(string userId, string courseId);
        Task<List<Enrollment>> GetUserEnrollmentsAsync(string userId);
        Task<int> GetCourseEnrollmentCountAsync(string courseId);
    }
}
