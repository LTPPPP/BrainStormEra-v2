using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace BrainStormEra_MVC.Middlewares
{
    public class PointsRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PointsRefreshMiddleware> _logger;

        public PointsRefreshMiddleware(
            RequestDelegate next,
            ILogger<PointsRefreshMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Check if user is authenticated
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var userId = context.User.FindFirst("UserId")?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        // Check if it's time to refresh points (every 5 minutes)
                        var lastRefreshTime = context.User.FindFirst("PointsLastRefresh")?.Value;
                        var shouldRefresh = ShouldRefreshPoints(lastRefreshTime);

                        if (shouldRefresh)
                        {
                            _logger.LogDebug("Refreshing points for user: {UserId}", userId);

                            // Get scoped service from the request scope
                            var pointsService = context.RequestServices.GetRequiredService<IPointsService>();
                            await pointsService.RefreshUserPointsClaimAsync(context, userId);
                        }
                    }
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PointsRefreshMiddleware");
                await _next(context);
            }
        }

        private bool ShouldRefreshPoints(string? lastRefreshTime)
        {
            if (string.IsNullOrEmpty(lastRefreshTime))
                return true;

            if (DateTime.TryParse(lastRefreshTime, out var lastRefresh))
            {
                var timeSinceLastRefresh = DateTime.UtcNow - lastRefresh;
                return timeSinceLastRefresh.TotalMinutes >= 5; // Refresh every 5 minutes
            }

            return true;
        }
    }

    // Extension method for easy registration
    public static class PointsRefreshMiddlewareExtensions
    {
        public static IApplicationBuilder UsePointsRefresh(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PointsRefreshMiddleware>();
        }
    }
}