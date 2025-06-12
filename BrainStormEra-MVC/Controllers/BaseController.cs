using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BrainStormEra_MVC.Controllers
{
    /// <summary>
    /// Base controller to provide common functionality for accessing user information from authentication cookies
    /// </summary>
    public class BaseController : Controller
    {
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
        protected bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;        /// <summary>
                                                                                           /// Check if the current user is an admin
                                                                                           /// </summary>
        protected bool IsAdmin => IsInRole("admin");

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
    }
}

