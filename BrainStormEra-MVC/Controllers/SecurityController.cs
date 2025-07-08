using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.SecurityModels;
using BusinessLogicLayer.DTOs.Security;

namespace BrainStormEra_MVC.Controllers
{
    /// <summary>
    /// Controller for managing security settings and monitoring
    /// </summary>
    [Authorize(Roles = "admin")]
    public class SecurityController : BaseController
    {
        private readonly ISecurityService _securityService;
        private readonly ILogger<SecurityController> _logger;

        public SecurityController(
            ISecurityService securityService,
            ILogger<SecurityController> logger)
        {
            _securityService = securityService;
            _logger = logger;
        }

        /// <summary>
        /// Display security dashboard
        /// </summary>
        public IActionResult Dashboard()
        {
            try
            {
                // This would normally fetch security statistics
                // For now, return basic view
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security dashboard");
                ViewBag.Error = "Unable to load security dashboard.";
                return View();
            }
        }

        /// <summary>
        /// Reset login attempts for a user or IP
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAttempts(string? username, string? ipAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(ipAddress))
                {
                    return Json(new { success = false, message = "Username or IP address is required." });
                }

                await _securityService.ResetAttemptsAsync(username, ipAddress);

                _logger.LogInformation("Reset security attempts for username: {Username}, IP: {IpAddress} by admin", username, ipAddress);

                return Json(new { success = true, message = "Security attempts reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting security attempts for username: {Username}, IP: {IpAddress}", username, ipAddress);
                return Json(new { success = false, message = "Failed to reset security attempts." });
            }
        }

        /// <summary>
        /// Block a user or IP address
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUserOrIp(string? username, string? ipAddress, string reason, int durationMinutes)
        {
            try
            {
                if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(ipAddress))
                {
                    return Json(new { success = false, message = "Username or IP address is required." });
                }

                if (string.IsNullOrEmpty(reason))
                {
                    return Json(new { success = false, message = "Reason is required." });
                }

                if (durationMinutes <= 0)
                {
                    return Json(new { success = false, message = "Duration must be greater than 0." });
                }

                await _securityService.BlockUserOrIpAsync(username, ipAddress, reason, durationMinutes);

                _logger.LogWarning("Admin blocked - Username: {Username}, IP: {IpAddress}, Duration: {Duration} minutes, Reason: {Reason}",
                    username, ipAddress, durationMinutes, reason);

                return Json(new { success = true, message = "User/IP blocked successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking username: {Username}, IP: {IpAddress}", username, ipAddress);
                return Json(new { success = false, message = "Failed to block user/IP." });
            }
        }

        /// <summary>
        /// Check if a user or IP is blocked
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckBlockStatus(string? username, string? ipAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(ipAddress))
                {
                    return Json(new { success = false, message = "Username or IP address is required." });
                }

                var blockInfo = await _securityService.GetBlockInfoAsync(username, ipAddress);
                var failedAttempts = await _securityService.GetFailedAttemptsCountAsync(username, ipAddress, TimeSpan.FromHours(24));

                return Json(new
                {
                    success = true,
                    isBlocked = blockInfo.IsBlocked,
                    blockReason = blockInfo.BlockReason,
                    blockExpiresAt = blockInfo.BlockExpiresAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                    failedAttemptsLast24Hours = failedAttempts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking block status for username: {Username}, IP: {IpAddress}", username, ipAddress);
                return Json(new { success = false, message = "Failed to check block status." });
            }
        }

        /// <summary>
        /// Trigger manual cleanup of expired records
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CleanupExpiredRecords()
        {
            try
            {
                await _securityService.CleanupExpiredRecordsAsync();

                _logger.LogInformation("Manual security cleanup triggered by admin");

                return Json(new { success = true, message = "Cleanup completed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual security cleanup");
                return Json(new { success = false, message = "Failed to perform cleanup." });
            }
        }
    }
}
