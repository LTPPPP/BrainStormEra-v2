using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PointsController : BaseController
    {
        private readonly IPointsService _pointsService;
        private readonly ILogger<PointsController> _logger;

        public PointsController(IPointsService pointsService, ILogger<PointsController> logger)
        {
            _pointsService = pointsService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user points
        /// </summary>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentPoints()
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var points = await _pointsService.GetUserPointsAsync(userId);
                return Ok(new { success = true, points = points });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user points");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Refresh user points claim
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshPoints()
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var success = await _pointsService.RefreshUserPointsClaimAsync(HttpContext, userId);
                if (success)
                {
                    var points = await _pointsService.GetUserPointsAsync(userId);
                    return Ok(new { success = true, points = points, message = "Points refreshed successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to refresh points" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing user points");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}