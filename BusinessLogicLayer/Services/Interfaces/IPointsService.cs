using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IPointsService
    {
        /// <summary>
        /// Get current user points from database
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Current points</returns>
        Task<decimal> GetUserPointsAsync(string userId);

        /// <summary>
        /// Update user points in database
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="points">Points to add/subtract</param>
        /// <returns>Success status</returns>
        Task<bool> UpdateUserPointsAsync(string userId, decimal points);

        /// <summary>
        /// Refresh user claims with updated points
        /// </summary>
        /// <param name="httpContext">HTTP context</param>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        Task<bool> RefreshUserPointsClaimAsync(HttpContext httpContext, string userId);
    }
}