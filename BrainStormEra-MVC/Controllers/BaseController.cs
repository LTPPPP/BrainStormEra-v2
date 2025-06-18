using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BusinessLogicLayer.Services.Interfaces;

namespace BrainStormEra_MVC.Controllers
{
    /// <summary>
    /// Base controller to provide common functionality for accessing user information from authentication cookies
    /// and URL hash handling
    /// </summary>
    public class BaseController : Controller
    {
        protected readonly IUrlHashService? _urlHashService;

        public BaseController()
        {
            // Default constructor for controllers that don't need URL hash
        }

        public BaseController(IUrlHashService urlHashService)
        {
            _urlHashService = urlHashService;
        }

        /// <summary>
        /// Get the current logged-in user's ID from claims
        /// </summary>
        protected string? CurrentUserId => User?.FindFirst("UserId")?.Value;

        /// <summary>
        /// Get the current logged-in user's username from claims
        /// </summary>
        protected string? CurrentUsername => User?.Identity?.Name;

        /// <summary>
        /// Get the current logged-in user's role from claims
        /// </summary>
        protected string? CurrentUserRole => User?.FindFirst("UserRole")?.Value;

        /// <summary>
        /// Get the current logged-in user's full name from claims
        /// </summary>
        protected string? CurrentUserFullName => User?.FindFirst("FullName")?.Value;

        /// <summary>
        /// Get the current logged-in user's email from claims
        /// </summary>
        protected string? CurrentUserEmail => User?.FindFirst(ClaimTypes.Email)?.Value;

        /// <summary>
        /// Get the current logged-in user's phone number from claims
        /// </summary>
        protected string? CurrentUserPhoneNumber => User?.FindFirst("PhoneNumber")?.Value;

        /// <summary>
        /// Get the current logged-in user's image from claims
        /// </summary>
        protected string? CurrentUserImage => User?.FindFirst("UserImage")?.Value;

        /// <summary>
        /// Get the current logged-in user's address from claims
        /// </summary>
        protected string? CurrentUserAddress => User?.FindFirst("UserAddress")?.Value;

        /// <summary>
        /// Get the current logged-in user's gender from claims
        /// </summary>
        protected string? CurrentUserGender => User?.FindFirst("Gender")?.Value;

        /// <summary>
        /// Get the current logged-in user's date of birth from claims
        /// </summary>
        protected string? CurrentUserDateOfBirth => User?.FindFirst("DateOfBirth")?.Value;

        /// <summary>
        /// Get the current logged-in user's payment points from claims
        /// </summary>
        protected string? CurrentUserPaymentPoint => User?.FindFirst("PaymentPoint")?.Value;

        /// <summary>
        /// Get the login time from claims
        /// </summary>
        protected string? CurrentUserLoginTime => User?.FindFirst("LoginTime")?.Value;

        /// <summary>
        /// Check if the current user has a specific role
        /// </summary>
        /// <param name="role">Role to check</param>
        /// <returns>True if user has the role, false otherwise</returns>
        protected bool IsInRole(string role)
        {
            return User?.IsInRole(role) ?? false;
        }

        /// <summary>
        /// Check if the current user is authenticated
        /// </summary>
        protected bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        /// <summary>
        /// Check if the current user is an instructor
        /// </summary>
        protected bool IsInstructor => IsInRole("instructor");

        /// <summary>
        /// Check if the current user is a learner
        /// </summary>
        protected bool IsLearner => IsInRole("learner");

        /// <summary>
        /// Get user information as a dictionary for easy access
        /// </summary>
        /// <returns>Dictionary containing user information from claims</returns>
        protected Dictionary<string, string?> GetCurrentUserInfo()
        {
            return new Dictionary<string, string?>
            {
                ["UserId"] = CurrentUserId,
                ["Username"] = CurrentUsername,
                ["Role"] = CurrentUserRole,
                ["FullName"] = CurrentUserFullName,
                ["Email"] = CurrentUserEmail,
                ["PhoneNumber"] = CurrentUserPhoneNumber,
                ["UserImage"] = CurrentUserImage,
                ["UserAddress"] = CurrentUserAddress,
                ["Gender"] = CurrentUserGender,
                ["DateOfBirth"] = CurrentUserDateOfBirth,
                ["PaymentPoint"] = CurrentUserPaymentPoint,
                ["LoginTime"] = CurrentUserLoginTime
            };
        }

        /// <summary>
        /// Get display name for the current user (Full Name if available, otherwise Username)
        /// </summary>
        protected string GetCurrentUserDisplayName()
        {
            return CurrentUserFullName ?? CurrentUsername ?? "Unknown User";
        }

        #region URL Hash Helper Methods

        /// <summary>
        /// Decode hash ID from URL parameter to real ID
        /// </summary>
        /// <param name="hashId">Hash ID from URL</param>
        /// <returns>Real ID</returns>
        protected string DecodeHashId(string hashId)
        {
            if (_urlHashService == null || string.IsNullOrEmpty(hashId))
                return hashId;

            return _urlHashService.GetRealId(hashId);
        }

        /// <summary>
        /// Encode real ID to hash for use in URL
        /// </summary>
        /// <param name="realId">Real ID</param>
        /// <returns>Hash ID</returns>
        protected string EncodeToHash(string realId)
        {
            if (_urlHashService == null || string.IsNullOrEmpty(realId))
                return realId;

            return _urlHashService.GetHash(realId);
        }

        /// <summary>
        /// Encode multiple IDs to hash
        /// </summary>
        /// <param name="realIds">List of real IDs</param>
        /// <returns>Dictionary mapping real ID to hash</returns>
        protected Dictionary<string, string> EncodeIdsToHashes(IEnumerable<string> realIds)
        {
            if (_urlHashService == null)
                return realIds.ToDictionary(id => id, id => id);

            return _urlHashService.EncodeIds(realIds);
        }

        /// <summary>
        /// Redirect to action with hash ID
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="realId">Real ID</param>
        /// <returns>RedirectToActionResult</returns>
        protected RedirectToActionResult RedirectToActionWithHash(string actionName, string realId)
        {
            var hashId = EncodeToHash(realId);
            return RedirectToAction(actionName, new { id = hashId });
        }

        /// <summary>
        /// Redirect to action with hash ID and controller
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="realId">Real ID</param>
        /// <returns>RedirectToActionResult</returns>
        protected RedirectToActionResult RedirectToActionWithHash(string actionName, string controllerName, string realId)
        {
            var hashId = EncodeToHash(realId);
            return RedirectToAction(actionName, controllerName, new { id = hashId });
        }

        /// <summary>
        /// Redirect to action with hash ID and route values
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="realId">Real ID</param>
        /// <param name="routeValues">Other route values</param>
        /// <returns>RedirectToActionResult</returns>
        protected RedirectToActionResult RedirectToActionWithHash(string actionName, string controllerName, string realId, object routeValues)
        {
            var hashId = EncodeToHash(realId);
            var routes = new Dictionary<string, object> { ["id"] = hashId };

            // Merge with other route values
            if (routeValues != null)
            {
                var properties = routeValues.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    routes[prop.Name] = prop.GetValue(routeValues) ?? "";
                }
            }

            return RedirectToAction(actionName, controllerName, routes);
        }

        #endregion
    }
}

