using Microsoft.Extensions.Logging;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models;
using BusinessLogicLayer.Services.Interfaces;

namespace BusinessLogicLayer.Services.Implementations
{
    public class StatusSeedService : IStatusSeedService
    {
        private readonly IBaseRepo<Status> _statusRepo;
        private readonly ILogger<StatusSeedService> _logger;

        public StatusSeedService(IBaseRepo<Status> statusRepo, ILogger<StatusSeedService> logger)
        {
            _statusRepo = statusRepo;
            _logger = logger;
        }

        public async Task SeedStatusesAsync()
        {
            try
            {
                // Check if statuses already exist
                var existingStatuses = await _statusRepo.CountAsync();
                if (existingStatuses > 0)
                {
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

                await _statusRepo.AddRangeAsync(statuses);
                await _statusRepo.SaveChangesAsync();

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