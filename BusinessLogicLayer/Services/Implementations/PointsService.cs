using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BusinessLogicLayer.Services.Implementations
{
    public class PointsService : IPointsService
    {
        private readonly IUserRepo _userRepo;
        private readonly ILogger<PointsService> _logger;

        public PointsService(IUserRepo userRepo, ILogger<PointsService> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<decimal> GetUserPointsAsync(string userId)
        {
            try
            {
                var user = await _userRepo.GetUserWithPaymentPointAsync(userId);
                return user?.PaymentPoint ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user points for user: {UserId}", userId);
                return 0;
            }
        }

        public async Task<bool> UpdateUserPointsAsync(string userId, decimal points)
        {
            try
            {
                _logger.LogInformation("Updating points for user: {UserId}, points change: {Points}", userId, points);

                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                decimal currentPoints = user.PaymentPoint ?? 0;
                user.PaymentPoint = currentPoints + points;
                user.AccountUpdatedAt = DateTime.Now;

                _logger.LogInformation("User points updated: {UserId}, old: {OldPoints}, new: {NewPoints}",
                    userId, currentPoints, user.PaymentPoint);

                var result = await _userRepo.UpdateUserAsync(user);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user points for user: {UserId}, points: {Points}", userId, points);
                return false;
            }
        }

        public async Task<bool> RefreshUserPointsClaimAsync(HttpContext httpContext, string userId)
        {
            try
            {
                if (!httpContext.User.Identity?.IsAuthenticated == true)
                {
                    return false;
                }

                var currentPoints = await GetUserPointsAsync(userId);

                // Get current claims
                var currentClaims = httpContext.User.Claims.ToList();

                // Remove existing PaymentPoint claim
                var claimsToRemove = currentClaims.Where(c => c.Type == "PaymentPoint").ToList();
                foreach (var claim in claimsToRemove)
                {
                    currentClaims.Remove(claim);
                }

                // Add updated PaymentPoint claim
                currentClaims.Add(new Claim("PaymentPoint", currentPoints.ToString()));

                // Add points last refresh time claim
                currentClaims.Add(new Claim("PointsLastRefresh", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));

                // Create new claims identity
                var claimsIdentity = new ClaimsIdentity(currentClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                    AllowRefresh = true
                };

                // Sign in with updated claims
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User points claim refreshed for user: {UserId}, points: {Points}", userId, currentPoints);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing user points claim for user: {UserId}", userId);
                return false;
            }
        }
    }
}