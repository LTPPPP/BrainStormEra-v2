using System.Security.Claims;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IUserContextService
    {
        string? GetCurrentUserId(ClaimsPrincipal user);
        bool IsAuthenticated(ClaimsPrincipal user);
    }
}







