using BusinessLogicLayer.DTOs.Common;

namespace BusinessLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Interface for Email Service that handles sending emails with templates
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send email with HTML template
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="htmlBody">HTML content of the email</param>
        /// <param name="isHtml">Whether the content is HTML (default: true)</param>
        /// <returns>Service result indicating success or failure</returns>
        Task<ServiceResult<bool>> SendEmailAsync(string to, string subject, string htmlBody, bool isHtml = true);

        /// <summary>
        /// Send forgot password email with OTP
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="userName">User's name</param>
        /// <param name="otp">OTP code</param>
        /// <param name="expiryMinutes">OTP expiry time in minutes</param>
        /// <returns>Service result indicating success or failure</returns>
        Task<ServiceResult<bool>> SendForgotPasswordEmailAsync(string to, string userName, string otp, int expiryMinutes = 10);

        /// <summary>
        /// Send password reset confirmation email
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="userName">User's name</param>
        /// <param name="resetTime">Time when password was reset</param>
        /// <returns>Service result indicating success or failure</returns>
        Task<ServiceResult<bool>> SendPasswordResetConfirmationEmailAsync(string to, string userName, DateTime resetTime);

        /// <summary>
        /// Send welcome email for new user registration
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="userName">User's name</param>
        /// <param name="username">User's username</param>
        /// <returns>Service result indicating success or failure</returns>
        Task<ServiceResult<bool>> SendWelcomeEmailAsync(string to, string userName, string username);

        /// <summary>
        /// Test email configuration
        /// </summary>
        /// <param name="to">Test recipient email address</param>
        /// <returns>Service result indicating success or failure</returns>
        Task<ServiceResult<bool>> TestEmailConfigurationAsync(string to);
    }
}