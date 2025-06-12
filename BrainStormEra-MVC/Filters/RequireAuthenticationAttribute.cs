using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BrainStormEra_MVC.Filters
{    /// <summary>
     /// Filter to check authentication for ViewDetail pages
     /// Shows notification if user is not logged in
     /// </summary>
    public class RequireAuthenticationAttribute : ActionFilterAttribute
    {
        private readonly string _errorMessage;

        public RequireAuthenticationAttribute(string errorMessage = "")
        {
            _errorMessage = errorMessage;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;

            // Check if user is authenticated
            if (user?.Identity?.IsAuthenticated != true)
            {
                // Save error message to TempData
                var tempData = context.Controller as Controller;
                if (tempData != null)
                {
                    var message = !string.IsNullOrEmpty(_errorMessage)
                        ? _errorMessage
                        : "You need to login to view details. Please login to continue.";

                    tempData.TempData["ErrorMessage"] = message;
                }

                // Save current URL to redirect back after login
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;

                // Redirect to login page with return URL
                context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl = returnUrl });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
