using Microsoft.Extensions.Logging;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models;
using BusinessLogicLayer.Services.Interfaces;

namespace BusinessLogicLayer.Services.Implementations
{
    public class LessonTypeSeedService : ILessonTypeSeedService
    {
        private readonly IBaseRepo<LessonType> _lessonTypeRepo;
        private readonly ILogger<LessonTypeSeedService> _logger;

        public LessonTypeSeedService(IBaseRepo<LessonType> lessonTypeRepo, ILogger<LessonTypeSeedService> logger)
        {
            _lessonTypeRepo = lessonTypeRepo;
            _logger = logger;
        }

        public async Task SeedLessonTypesAsync()
        {
            try
            {
                // Check if lesson types already exist
                if (await _lessonTypeRepo.AnyAsync(lt => true))
                {
                    return;
                }

                var lessonTypes = new List<LessonType>
                {
                    new LessonType
                    {
                        LessonTypeName = "Video Lesson"
                    },
                    new LessonType
                    {
                        LessonTypeName = "Text Lesson"
                    },
                    new LessonType
                    {
                        LessonTypeName = "Document Lesson"
                    },
                    new LessonType
                    {
                        LessonTypeName = "Interactive Lesson"
                    },
                    new LessonType
                    {
                        LessonTypeName = "Quiz Lesson"
                    },
                    new LessonType
                    {
                        LessonTypeName = "Assignment Lesson"
                    }
                };

                await _lessonTypeRepo.AddRangeAsync(lessonTypes);
                await _lessonTypeRepo.SaveChangesAsync();

                _logger.LogInformation("Successfully seeded {Count} lesson types", lessonTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding lesson types");
                throw;
            }
        }
    }
}