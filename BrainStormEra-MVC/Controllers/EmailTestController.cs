using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_MVC.Controllers
{
    /// <summary>
    /// Controller for testing email functionality
    /// This controller should only be available in development environment
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailTestController> _logger;

        public EmailTestController(IEmailService emailService, ILogger<EmailTestController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Test email configuration by sending a test email
        /// </summary>
        /// <param name="email">Email address to send test email to</param>
        /// <returns>Result of the email test</returns>
        [HttpPost("test")]
        public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { success = false, message = "Email address is required" });
                }

                var result = await _emailService.TestEmailConfigurationAsync(request.Email);

                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Test email sent successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing email configuration");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Test forgot password email template
        /// </summary>
        /// <param name="request">Test request with email and user details</param>
        /// <returns>Result of the email test</returns>
        [HttpPost("test-forgot-password")]
        public async Task<IActionResult> TestForgotPasswordEmail([FromBody] TestForgotPasswordRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { success = false, message = "Email address is required" });
                }

                var result = await _emailService.SendForgotPasswordEmailAsync(
                    request.Email,
                    request.UserName ?? "Test User",
                    request.Otp ?? "123456",
                    10);

                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Forgot password email sent successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing forgot password email");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Test welcome email template
        /// </summary>
        /// <param name="request">Test request with email and user details</param>
        /// <returns>Result of the email test</returns>
        [HttpPost("test-welcome")]
        public async Task<IActionResult> TestWelcomeEmail([FromBody] TestWelcomeRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { success = false, message = "Email address is required" });
                }

                var result = await _emailService.SendWelcomeEmailAsync(
                    request.Email,
                    request.UserName ?? "Test User",
                    request.Username ?? "testuser");

                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Welcome email sent successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing welcome email");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Test password reset confirmation email template
        /// </summary>
        /// <param name="request">Test request with email and user details</param>
        /// <returns>Result of the email test</returns>
        [HttpPost("test-reset-confirmation")]
        public async Task<IActionResult> TestResetConfirmationEmail([FromBody] TestResetConfirmationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { success = false, message = "Email address is required" });
                }

                var result = await _emailService.SendPasswordResetConfirmationEmailAsync(
                    request.Email,
                    request.UserName ?? "Test User",
                    DateTime.UtcNow);

                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Password reset confirmation email sent successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing password reset confirmation email");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    #region Request Models

    public class TestEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class TestForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Otp { get; set; }
    }

    public class TestWelcomeRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Username { get; set; }
    }

    public class TestResetConfirmationRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }
    }

    #endregion
}