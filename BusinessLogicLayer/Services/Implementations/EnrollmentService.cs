using Microsoft.Extensions.Logging;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using BusinessLogicLayer.Services.Interfaces;

namespace BusinessLogicLayer.Services.Implementations
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly ICourseRepo _courseRepo;
        private readonly IUserRepo _userRepo;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(
            ICourseRepo courseRepo,
            IUserRepo userRepo,
            ILogger<EnrollmentService> logger)
        {
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<bool> EnrollAsync(string userId, string courseId)
        {
            try
            {
                var result = await _courseRepo.EnrollUserAsync(userId, courseId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling user {UserId} in course {CourseId}", userId, courseId);
                return false;
            }
        }

        public async Task<bool> IsEnrolledAsync(string userId, string courseId)
        {
            return await _courseRepo.IsUserEnrolledAsync(userId, courseId);
        }

        public async Task<List<Enrollment>> GetUserEnrollmentsAsync(string userId)
        {
            return await _courseRepo.GetUserEnrollmentsAsync(userId);
        }

        public async Task<int> GetCourseEnrollmentCountAsync(string courseId)
        {
            return await _courseRepo.GetCourseEnrollmentCountAsync(courseId);
        }
    }
}