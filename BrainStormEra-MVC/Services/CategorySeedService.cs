using DataAccessLayer.Data;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Services
{
    public class CategorySeedService
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<CategorySeedService> _logger;

        public CategorySeedService(BrainStormEraContext context, ILogger<CategorySeedService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedCategoriesAsync()
        {
            try
            {
                // Check if categories already exist
                var existingCategories = await _context.CourseCategories.CountAsync();
                if (existingCategories > 0)
                {
                    _logger.LogInformation("Categories already exist in database: {Count}", existingCategories);
                    return;
                }

                var categories = new List<CourseCategory>
                {
                    new CourseCategory
                    {
                        CourseCategoryId = Guid.NewGuid().ToString(),
                        CourseCategoryName = "Programming",
                        CategoryDescription = "Learn programming languages and software development",
                        CategoryIcon = "fas fa-code",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new CourseCategory
                    {
                        CourseCategoryId = Guid.NewGuid().ToString(),
                        CourseCategoryName = "Web Development",
                        CategoryDescription = "Frontend and backend web development",
                        CategoryIcon = "fas fa-globe",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new CourseCategory
                    {
                        CourseCategoryId = Guid.NewGuid().ToString(),
                        CourseCategoryName = "Design",
                        CategoryDescription = "UI/UX design and graphic design",
                        CategoryIcon = "fas fa-paint-brush",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new CourseCategory
                    {
                        CourseCategoryId = Guid.NewGuid().ToString(),
                        CourseCategoryName = "Business",
                        CategoryDescription = "Business skills and entrepreneurship",
                        CategoryIcon = "fas fa-briefcase",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new CourseCategory
                    {
                        CourseCategoryId = Guid.NewGuid().ToString(),
                        CourseCategoryName = "Marketing",
                        CategoryDescription = "Digital marketing and promotion strategies",
                        CategoryIcon = "fas fa-bullhorn",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new CourseCategory
                    {
                        CourseCategoryId = Guid.NewGuid().ToString(),
                        CourseCategoryName = "Data Science",
                        CategoryDescription = "Data analysis and machine learning",
                        CategoryIcon = "fas fa-chart-line",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new CourseCategory
                    {
                        CourseCategoryId = Guid.NewGuid().ToString(),
                        CourseCategoryName = "Mobile Development",
                        CategoryDescription = "iOS and Android app development",
                        CategoryIcon = "fas fa-mobile-alt",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await _context.CourseCategories.AddRangeAsync(categories);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} categories successfully", categories.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding categories");
                throw;
            }
        }
    }
}
