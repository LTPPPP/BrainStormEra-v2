using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BusinessLogicLayer.Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BrainStormEra_MVC.Filters
{
    /// <summary>
    /// Filter to check if the current user is banned. If banned, logs out and redirects to login page.
    /// </summary>
    public class CheckBanStatusAttribute : TypeFilterAttribute
    {
        public CheckBanStatusAttribute() : base(typeof(CheckBanStatusFilterImpl)) { }

        private class CheckBanStatusFilterImpl : IAsyncActionFilter
        {
            private readonly IUserService _userService;
            private readonly IAuthService _authService;

            public CheckBanStatusFilterImpl(IUserService userService, IAuthService authService)
            {
                _userService = userService;
                _authService = authService;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var user = context.HttpContext.User;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    var userId = user.FindFirst("UserId")?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var account = await _userService.GetUserByUsernameAsync(user.Identity.Name);
                        if (account != null && account.IsBanned == true)
                        {
                            // Logout user
                            await _authService.LogoutUserAsync(context.HttpContext, user);
                            // Redirect to login with banned message
                            context.Result = new RedirectToActionResult("Login", "Auth", new { banned = true });
                            return;
                        }
                    }
                }
                await next();
            }
        }
    }
}