using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BusinessLogicLayer.Services.Interfaces;
using System.Net;

namespace BrainStormEra_Razor.Middlewares
{
    /// <summary>
    /// Middleware to handle security checks and rate limiting for all requests
    /// </summary>
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityMiddleware> _logger;

        public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISecurityService securityService)
        {
            try
            {
                var request = context.Request;
                var ipAddress = GetClientIpAddress(context);

                // Only check security for login endpoints
                if (IsLoginEndpoint(request.Path))
                {
                    // For POST requests to login endpoints, check if IP is blocked
                    if (HttpMethods.IsPost(request.Method))
                    {
                        var isBlocked = await securityService.IsBlockedAsync(null, ipAddress);
                        if (isBlocked)
                        {
                            _logger.LogWarning("Blocked IP {IpAddress} attempted to access login endpoint", ipAddress);

                            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                            await context.Response.WriteAsync("Too many failed login attempts. Please try again later.");
                            return;
                        }
                    }
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SecurityMiddleware");
                // Continue processing even if security check fails
                await _next(context);
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            // Try to get IP from various headers (for proxy scenarios)
            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }

            // Default to localhost if nothing is found
            return ipAddress ?? "127.0.0.1";
        }

        private static bool IsLoginEndpoint(PathString path)
        {
            var pathValue = path.Value?.ToLowerInvariant();
            return pathValue != null && (
                pathValue.Contains("/auth/login") ||
                pathValue.Contains("/login") ||
                pathValue.Contains("/account/login") ||
                pathValue.Contains("/user/login")
            );
        }
    }

    /// <summary>
    /// Extension method to register the security middleware
    /// </summary>
    public static class SecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityMiddleware>();
        }
    }
}
