using BrainStormEra_MVC.Services.Interfaces;
using System.Security.Claims;

namespace BrainStormEra_MVC.Services
{
    public class UserContextService : IUserContextService
    {
        public string? GetCurrentUserId(ClaimsPrincipal user)
        {
            return user.FindFirst("UserId")?.Value;
        }

        public bool IsAuthenticated(ClaimsPrincipal user)
        {
            return !string.IsNullOrEmpty(GetCurrentUserId(user));
        }
    }
}
