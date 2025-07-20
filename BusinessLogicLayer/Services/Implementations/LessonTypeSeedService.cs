using Microsoft.Extensions.Logging;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using BusinessLogicLayer.Services.Interfaces;

namespace BusinessLogicLayer.Services.Implementations
{
    public class LessonTypeSeedService : ILessonTypeSeedService
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<LessonTypeSeedService> _logger;

        public LessonTypeSeedService(BrainStormEraContext context, ILogger<LessonTypeSeedService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedLessonTypesAsync()
        {
            try
            {
                // Check if lesson types already exist
                if (await _context.LessonTypes.AnyAsync())
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

                await _context.LessonTypes.AddRangeAsync(lessonTypes);
                await _context.SaveChangesAsync();

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