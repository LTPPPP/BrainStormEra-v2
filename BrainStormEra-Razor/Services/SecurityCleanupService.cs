using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BrainStormEra_Razor.Services
{
    /// <summary>
    /// Background service to periodically clean up expired security records
    /// </summary>
    public class SecurityCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SecurityCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Run every hour

        public SecurityCleanupService(
            IServiceProvider serviceProvider,
            ILogger<SecurityCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Security cleanup service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanupAsync();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during security cleanup");
                    // Wait a bit before retrying
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Security cleanup service stopped");
        }

        private async Task PerformCleanupAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var securityService = scope.ServiceProvider.GetRequiredService<ISecurityService>();

                await securityService.CleanupExpiredRecordsAsync();

                _logger.LogDebug("Security cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform security cleanup");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Security cleanup service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}
