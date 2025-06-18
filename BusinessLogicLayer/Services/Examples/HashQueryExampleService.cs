using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services.Examples
{
    /// <summary>
    /// Example service demonstrating how to use AES-based URL hash service for database queries
    /// This service shows best practices for working with encrypted IDs
    /// </summary>
    public class HashQueryExampleService
    {
        private readonly QueryHashService _queryHashService;
        private readonly ICourseRepo _courseRepo;
        private readonly IUserRepo _userRepo;
        private readonly ILogger<HashQueryExampleService> _logger;

        public HashQueryExampleService(
            QueryHashService queryHashService,
            ICourseRepo courseRepo,
            IUserRepo userRepo,
            ILogger<HashQueryExampleService> logger)
        {
            _queryHashService = queryHashService;
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _logger = logger;
        }

        /// <summary>
        /// Example: Get course by ID or hash - accepts both formats
        /// </summary>
        /// <param name="courseIdOrHash">Course ID or encrypted hash</param>
        /// <returns>Course entity or null</returns>
        public async Task<DataAccessLayer.Models.Course?> GetCourseByIdOrHashAsync(string courseIdOrHash)
        {
            // Validate the input first
            var validation = _queryHashService.ValidateIdForQuery(courseIdOrHash);
            if (!validation.IsValid)
            {
                _logger.LogWarning($"Invalid course ID/hash: {validation.ErrorMessage}");
                return null;
            }

            // Use the smart query method
            return await _queryHashService.ExecuteQueryWithId(courseIdOrHash, async (realId) =>
            {
                // This function receives the real ID, ready for database query
                _logger.LogInformation($"Querying course with real ID: {realId}");
                return await _courseRepo.GetByIdAsync(realId);
            });
        }

        /// <summary>
        /// Example: Get multiple courses by IDs or hashes
        /// </summary>
        /// <param name="courseIdsOrHashes">Collection of course IDs or encrypted hashes</param>
        /// <returns>List of course entities</returns>
        public async Task<List<DataAccessLayer.Models.Course>> GetCoursesByIdsOrHashesAsync(IEnumerable<string> courseIdsOrHashes)
        {
            return await _queryHashService.ExecuteQueryWithIds(courseIdsOrHashes, async (realIds) =>
            {
                _logger.LogInformation($"Querying {realIds.Count()} courses with real IDs");

                var courses = new List<DataAccessLayer.Models.Course>();
                foreach (var realId in realIds)
                {
                    var course = await _courseRepo.GetByIdAsync(realId);
                    if (course != null)
                        courses.Add(course);
                }
                return courses;
            }) ?? new List<DataAccessLayer.Models.Course>();
        }

        /// <summary>
        /// Example: Prepare course data for display with encrypted IDs
        /// </summary>
        /// <param name="courses">Courses from database</param>
        /// <returns>Courses with encrypted IDs for URLs</returns>
        public List<CourseDisplayModel> PrepareCourseDisplayData(List<DataAccessLayer.Models.Course> courses)
        {
            var displayData = new List<CourseDisplayModel>();

            foreach (var course in courses)
            {
                if (course?.CourseId != null)
                {
                    var encryptedId = _queryHashService.PrepareIdForDisplay(course.CourseId);
                    displayData.Add(new CourseDisplayModel
                    {
                        CourseId = course.CourseId, // Keep real ID for internal use
                        EncryptedId = encryptedId,  // Use encrypted ID for URLs
                        Title = course.CourseName,
                        Description = course.CourseDescription ?? string.Empty,
                        // ... other properties
                    });
                }
            }

            return displayData;
        }

        /// <summary>
        /// Example: Complex query with ID validation and error handling
        /// </summary>
        /// <param name="userIdOrHash">User ID or hash</param>
        /// <param name="courseIdOrHash">Course ID or hash</param>
        /// <returns>Enrollment status</returns>
        public async Task<EnrollmentResult> CheckEnrollmentStatusAsync(string userIdOrHash, string courseIdOrHash)
        {
            try
            {
                // Analyze the IDs for debugging
                var userAnalysis = _queryHashService.AnalyzeId(userIdOrHash);
                var courseAnalysis = _queryHashService.AnalyzeId(courseIdOrHash);

                _logger.LogInformation($"User ID Analysis: {userAnalysis.EncryptionMethod}, Valid: {userAnalysis.IsValid}");
                _logger.LogInformation($"Course ID Analysis: {courseAnalysis.EncryptionMethod}, Valid: {courseAnalysis.IsValid}");

                // Validate both IDs
                if (!userAnalysis.IsValid)
                    return new EnrollmentResult { Success = false, Message = $"Invalid user ID: {userAnalysis.ErrorMessage}" };

                if (!courseAnalysis.IsValid)
                    return new EnrollmentResult { Success = false, Message = $"Invalid course ID: {courseAnalysis.ErrorMessage}" };

                // Prepare real IDs for query
                var realUserId = _queryHashService.PrepareIdForQuery(userIdOrHash);
                var realCourseId = _queryHashService.PrepareIdForQuery(courseIdOrHash);

                // Perform the actual database queries
                var user = await _userRepo.GetByIdAsync(realUserId);
                var course = await _courseRepo.GetByIdAsync(realCourseId);

                if (user == null)
                    return new EnrollmentResult { Success = false, Message = "User not found" };

                if (course == null)
                    return new EnrollmentResult { Success = false, Message = "Course not found" };

                // Check enrollment (this would use your enrollment repository)
                // var enrollment = await _enrollmentRepo.GetEnrollmentAsync(realUserId, realCourseId);

                return new EnrollmentResult
                {
                    Success = true,
                    Message = "Enrollment check completed",
                    UserRealId = realUserId,
                    CourseRealId = realCourseId,
                    UserDisplayId = _queryHashService.PrepareIdForDisplay(realUserId),
                    CourseDisplayId = _queryHashService.PrepareIdForDisplay(realCourseId)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking enrollment status: {ex.Message}");
                return new EnrollmentResult { Success = false, Message = "Internal error occurred" };
            }
        }

        /// <summary>
        /// Example: Bulk operation with ID conversion
        /// </summary>
        /// <param name="courseIdsOrHashes">Course IDs or hashes to process</param>
        /// <returns>Processing result</returns>
        public async Task<BulkOperationResult> ProcessCoursesAsync(IEnumerable<string> courseIdsOrHashes)
        {
            var result = new BulkOperationResult();

            // Prepare all IDs for queries
            var realIds = _queryHashService.PrepareIdsForQuery(courseIdsOrHashes);
            result.TotalRequested = courseIdsOrHashes.Count();
            result.ValidIds = realIds.Count();

            // Process each course
            foreach (var realId in realIds)
            {
                try
                {
                    var course = await _courseRepo.GetByIdAsync(realId);
                    if (course != null)
                    {
                        // Process the course...
                        result.SuccessfullyProcessed++;
                        result.ProcessedCourses.Add(new ProcessedCourse
                        {
                            RealId = realId,
                            DisplayId = _queryHashService.PrepareIdForDisplay(realId),
                            Title = course.CourseName,
                            Status = "Processed"
                        });
                    }
                    else
                    {
                        result.Failed++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing course {realId}: {ex.Message}");
                    result.Failed++;
                }
            }

            return result;
        }
    }

    // Supporting classes for examples
    public class CourseDisplayModel
    {
        public string CourseId { get; set; } = string.Empty;
        public string EncryptedId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class EnrollmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserRealId { get; set; } = string.Empty;
        public string CourseRealId { get; set; } = string.Empty;
        public string UserDisplayId { get; set; } = string.Empty;
        public string CourseDisplayId { get; set; } = string.Empty;
    }

    public class BulkOperationResult
    {
        public int TotalRequested { get; set; }
        public int ValidIds { get; set; }
        public int SuccessfullyProcessed { get; set; }
        public int Failed { get; set; }
        public List<ProcessedCourse> ProcessedCourses { get; set; } = new();
    }

    public class ProcessedCourse
    {
        public string RealId { get; set; } = string.Empty;
        public string DisplayId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}