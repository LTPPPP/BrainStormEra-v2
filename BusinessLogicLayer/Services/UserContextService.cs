using BusinessLogicLayer.Services.Interfaces;
using System.Security.Claims;

namespace BusinessLogicLayer.Services
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







