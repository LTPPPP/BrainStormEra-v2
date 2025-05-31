using System.Security.Claims;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface IUserContextService
    {
        string? GetCurrentUserId(ClaimsPrincipal user);
        bool IsAuthenticated(ClaimsPrincipal user);
    }
}
