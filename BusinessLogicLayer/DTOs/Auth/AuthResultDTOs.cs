using DataAccessLayer.Models.ViewModels;

namespace BusinessLogicLayer.DTOs.Auth
{
    /// <summary>
    /// Result class for login operations
    /// </summary>
    public class LoginResult
    {
        public bool Success { get; set; }
        public LoginViewModel? ViewModel { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UserRole { get; set; }
        public string? RedirectAction { get; set; }
    }

    /// <summary>
    /// Result class for register operations
    /// </summary>
    public class RegisterResult
    {
        public bool Success { get; set; }
        public RegisterViewModel? ViewModel { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ValidationError { get; set; }
        public string? RedirectAction { get; set; }
    }

    /// <summary>
    /// Result class for validation operations
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result class for forgot password operations
    /// </summary>
    public class ForgotPasswordResult
    {
        public bool Success { get; set; }
        public ForgotPasswordViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// Result class for verify OTP operations
    /// </summary>
    public class VerifyOtpResult
    {
        public bool Success { get; set; }
        public OtpVerificationViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? Email { get; set; }
        public string? Token { get; set; }
    }

    /// <summary>
    /// Result class for reset password operations
    /// </summary>
    public class ResetPasswordResult
    {
        public bool Success { get; set; }
        public ResetPasswordViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
    }

    /// <summary>
    /// Result class for logout operations
    /// </summary>
    public class LogoutResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result class for profile operations
    /// </summary>
    public class ProfileResult
    {
        public bool Success { get; set; }
        public UserProfileViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result class for edit profile operations
    /// </summary>
    public class EditProfileResult
    {
        public bool Success { get; set; }
        public EditProfileViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public string? WarningMessage { get; set; }
    }

    /// <summary>
    /// Result class for change password operations
    /// </summary>
    public class ChangePasswordResult
    {
        public bool Success { get; set; }
        public ChangePasswordViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ValidationError { get; set; }
    }

    /// <summary>
    /// Result class for avatar operations
    /// </summary>
    public class AvatarResult
    {
        public bool Success { get; set; }
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "";
    }

    /// <summary>
    /// Result class for delete avatar operations
    /// </summary>
    public class DeleteAvatarResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}