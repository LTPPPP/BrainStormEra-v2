using DataAccessLayer.Data;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Services
{
    public class StatusSeedService
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<StatusSeedService> _logger;

        public StatusSeedService(BrainStormEraContext context, ILogger<StatusSeedService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedStatusesAsync()
        {
            try
            {
                // Check if statuses already exist
                var existingStatuses = await _context.Statuses.CountAsync();
                if (existingStatuses > 0)
                {
                    _logger.LogInformation("Statuses already exist in database: {Count}", existingStatuses);
                    return;
                }

                var statuses = new List<Status>
                {
                    new Status
                    {
                        StatusId = 0,
                        StatusName = "Draft"
                    },
                    new Status
                    {
                        StatusId = 1,
                        StatusName = "Published"
                    },
                    new Status
                    {
                        StatusId = 2,
                        StatusName = "Active"
                    },
                    new Status
                    {
                        StatusId = 3,
                        StatusName = "Inactive"
                    },
                    new Status
                    {
                        StatusId = 4,
                        StatusName = "Archived"
                    },
                    new Status
                    {
                        StatusId = 5,
                        StatusName = "Suspended"
                    },
                    new Status
                    {
                        StatusId = 6,
                        StatusName = "Completed"
                    },
                    new Status
                    {
                        StatusId = 7,
                        StatusName = "In Progress"
                    }
                };

                await _context.Statuses.AddRangeAsync(statuses);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} statuses successfully", statuses.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding statuses");
                throw;
            }
        }
    }
}
