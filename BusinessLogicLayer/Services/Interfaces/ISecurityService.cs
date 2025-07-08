using BusinessLogicLayer.DTOs.Security;

namespace BusinessLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Interface for security services including rate limiting and brute force protection
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Check if a login attempt should be allowed based on rate limiting rules
        /// </summary>
        Task<SecurityCheckResult> CheckLoginAttemptAsync(string username, string ipAddress);

        /// <summary>
        /// Log a login attempt (successful or failed)
        /// </summary>
        Task LogLoginAttemptAsync(LoginAttemptRequest request);

        /// <summary>
        /// Block a user or IP address
        /// </summary>
        Task BlockUserOrIpAsync(string? username, string? ipAddress, string reason, int durationMinutes);

        /// <summary>
        /// Check if a user or IP is currently blocked
        /// </summary>
        Task<bool> IsBlockedAsync(string? username, string? ipAddress);

        /// <summary>
        /// Get security block information for a user or IP
        /// </summary>
        Task<SecurityCheckResult> GetBlockInfoAsync(string? username, string? ipAddress);

        /// <summary>
        /// Clean up expired blocks and old login attempts
        /// </summary>
        Task CleanupExpiredRecordsAsync();

        /// <summary>
        /// Get failed login attempts count for a user/IP in a specific time window
        /// </summary>
        Task<int> GetFailedAttemptsCountAsync(string? username, string? ipAddress, TimeSpan timeWindow);

        /// <summary>
        /// Reset login attempts for a user or IP (admin function)
        /// </summary>
        Task ResetAttemptsAsync(string? username, string? ipAddress);
    }
}
