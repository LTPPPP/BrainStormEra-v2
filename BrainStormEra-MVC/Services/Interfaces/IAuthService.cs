using DataAccessLayer.Models.ViewModels;
using BrainStormEra_MVC.Services.Implementations;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BrainStormEra_MVC.Services.Interfaces
{
    /// <summary>
    /// Interface for Authentication Service Implementation
    /// This interface defines the contract for AuthServiceImpl that handles 
    /// complex business logic for Authentication operations
    /// </summary>
    public interface IAuthService
    {
        #region Login Operations
        Task<LoginResult> GetLoginViewModelAsync(string? returnUrl = null);
        Task<LoginResult> AuthenticateUserAsync(HttpContext httpContext, LoginViewModel model);
        #endregion

        #region Register Operations
        Task<RegisterResult> GetRegisterViewModelAsync();
        Task<RegisterResult> RegisterUserAsync(RegisterViewModel model);
        Task<ValidationResult> CheckUsernameAvailabilityAsync(string username);
        Task<ValidationResult> CheckEmailAvailabilityAsync(string email);
        #endregion

        #region Password Reset Operations
        Task<ForgotPasswordResult> GetForgotPasswordViewModelAsync();
        Task<ForgotPasswordResult> ProcessForgotPasswordAsync(ForgotPasswordViewModel model);
        Task<VerifyOtpResult> GetVerifyOtpViewModelAsync(string email);
        Task<VerifyOtpResult> VerifyOtpAsync(OtpVerificationViewModel model);
        Task<ResetPasswordResult> GetResetPasswordViewModelAsync(string email, string token); Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordViewModel model);
        #endregion

        #region Logout Operations
        Task<LogoutResult> LogoutUserAsync(HttpContext httpContext, ClaimsPrincipal user);
        #endregion

        #region Profile Operations
        Task<ProfileResult> GetProfileViewModelAsync(string userId, string userRole);
        Task<EditProfileResult> GetEditProfileViewModelAsync(string userId);
        Task<EditProfileResult> UpdateProfileAsync(string userId, EditProfileViewModel model);
        Task<ChangePasswordResult> ChangePasswordAsync(string userId, ChangePasswordViewModel model);
        Task<AvatarResult> GetUserAvatarAsync(string? userId = null);
        Task<DeleteAvatarResult> DeleteUserAvatarAsync(string userId);
        #endregion
    }
}
