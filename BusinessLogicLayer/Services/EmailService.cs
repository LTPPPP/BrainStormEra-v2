using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.DTOs.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Email Service implementation that handles sending emails with HTML templates
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Get SMTP configuration from appsettings
            _smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.TryParse(_configuration["EmailSettings:SmtpPort"], out int port) ? port : 587;
            _smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? "";
            _smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? "";
            _fromEmail = _configuration["EmailSettings:FromEmail"] ?? _smtpUsername;
            _fromName = _configuration["EmailSettings:FromName"] ?? "BrainStormEra";
            _enableSsl = bool.TryParse(_configuration["EmailSettings:EnableSsl"], out bool ssl) ? ssl : true;
        }

        /// <summary>
        /// Send email with HTML template
        /// </summary>
        public async Task<ServiceResult<bool>> SendEmailAsync(string to, string subject, string htmlBody, bool isHtml = true)
        {
            try
            {
                if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
                {
                    _logger.LogWarning("SMTP credentials not configured. Email will not be sent.");
                    return ServiceResult<bool>.Failure("Email service not configured");
                }

                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = _enableSsl,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = isHtml,
                    Priority = MailPriority.Normal
                };

                message.To.Add(new MailAddress(to));

                await client.SendMailAsync(message);

                _logger.LogInformation("Email sent successfully to {Email}", to);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", to);
                return ServiceResult<bool>.Failure($"Failed to send email: {ex.Message}");
            }
        }

        /// <summary>
        /// Send forgot password email with OTP
        /// </summary>
        public async Task<ServiceResult<bool>> SendForgotPasswordEmailAsync(string to, string userName, string otp, int expiryMinutes = 10)
        {
            try
            {
                var subject = "Password Reset Request - BrainStormEra";
                var htmlBody = GetForgotPasswordEmailTemplate(userName, otp, expiryMinutes);

                return await SendEmailAsync(to, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending forgot password email to {Email}", to);
                return ServiceResult<bool>.Failure($"Failed to send forgot password email: {ex.Message}");
            }
        }

        /// <summary>
        /// Send password reset confirmation email
        /// </summary>
        public async Task<ServiceResult<bool>> SendPasswordResetConfirmationEmailAsync(string to, string userName, DateTime resetTime)
        {
            try
            {
                var subject = "Password Reset Successful - BrainStormEra";
                var htmlBody = GetPasswordResetConfirmationEmailTemplate(userName, resetTime);

                return await SendEmailAsync(to, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset confirmation email to {Email}", to);
                return ServiceResult<bool>.Failure($"Failed to send password reset confirmation email: {ex.Message}");
            }
        }

        /// <summary>
        /// Send welcome email for new user registration
        /// </summary>
        public async Task<ServiceResult<bool>> SendWelcomeEmailAsync(string to, string userName, string username)
        {
            try
            {
                var subject = "Welcome to BrainStormEra!";
                var htmlBody = GetWelcomeEmailTemplate(userName, username);

                return await SendEmailAsync(to, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to {Email}", to);
                return ServiceResult<bool>.Failure($"Failed to send welcome email: {ex.Message}");
            }
        }

        /// <summary>
        /// Test email configuration
        /// </summary>
        public async Task<ServiceResult<bool>> TestEmailConfigurationAsync(string to)
        {
            try
            {
                var subject = "Email Configuration Test - BrainStormEra";
                var htmlBody = GetTestEmailTemplate();

                return await SendEmailAsync(to, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email to {Email}", to);
                return ServiceResult<bool>.Failure($"Failed to send test email: {ex.Message}");
            }
        }

        #region Email Templates

        /// <summary>
        /// Get forgot password email template
        /// </summary>
        private string GetForgotPasswordEmailTemplate(string userName, string otp, int expiryMinutes)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset Request</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 300;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .otp-container {{
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
            color: white;
            padding: 20px;
            border-radius: 10px;
            text-align: center;
            margin: 30px 0;
        }}
        .otp-code {{
            font-size: 32px;
            font-weight: bold;
            letter-spacing: 5px;
            margin: 10px 0;
        }}
        .warning {{
            background-color: #fff3cd;
            border: 1px solid #ffeaa7;
            color: #856404;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px 30px;
            text-align: center;
            color: #6c757d;
            font-size: 14px;
        }}
        .btn {{
            display: inline-block;
            padding: 12px 24px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 10px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Password Reset Request</h1>
            <p>BrainStormEra Learning Platform</p>
        </div>
        
        <div class='content'>
            <h2>Hello {userName}!</h2>
            
            <p>We received a request to reset your password for your BrainStormEra account. If you didn't make this request, you can safely ignore this email.</p>
            
            <div class='otp-container'>
                <h3>Your Verification Code</h3>
                <div class='otp-code'>{otp}</div>
                <p>This code will expire in {expiryMinutes} minutes</p>
            </div>
            
            <div class='warning'>
                <strong>⚠️ Security Notice:</strong>
                <ul>
                    <li>Never share this code with anyone</li>
                    <li>Our team will never ask for this code</li>
                    <li>If you didn't request this, please ignore this email</li>
                </ul>
            </div>
            
            <p><strong>Next Steps:</strong></p>
            <ol>
                <li>Copy the verification code above</li>
                <li>Return to the BrainStormEra website</li>
                <li>Enter the code in the verification form</li>
                <li>Create your new password</li>
            </ol>
            
            <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
            
            <p>Best regards,<br>
            <strong>The BrainStormEra</strong></p>
        </div>
        
        <div class='footer'>
            <p>This email was sent to you because you requested a password reset for your BrainStormEra account.</p>
            <p>© 2024 BrainStormEra. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Get password reset confirmation email template
        /// </summary>
        private string GetPasswordResetConfirmationEmailTemplate(string userName, DateTime resetTime)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset Successful</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 300;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .success-container {{
            background: linear-gradient(135deg, #a8edea 0%, #fed6e3 100%);
            padding: 20px;
            border-radius: 10px;
            text-align: center;
            margin: 30px 0;
        }}
        .success-icon {{
            font-size: 48px;
            margin-bottom: 10px;
        }}
        .info-box {{
            background-color: #e3f2fd;
            border: 1px solid #bbdefb;
            color: #1565c0;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px 30px;
            text-align: center;
            color: #6c757d;
            font-size: 14px;
        }}
        .btn {{
            display: inline-block;
            padding: 12px 24px;
            background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 10px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Password Reset Successful</h1>
            <p>BrainStormEra Learning Platform</p>
        </div>
        
        <div class='content'>
            <h2>Hello {userName}!</h2>
            
            <div class='success-container'>
                <div class='success-icon'>🎉</div>
                <h3>Your password has been successfully reset!</h3>
                <p>You can now log in to your BrainStormEra account with your new password.</p>
            </div>
            
            <div class='info-box'>
                <strong>📅 Reset Details:</strong><br>
                <strong>Time:</strong> {resetTime:dddd, MMMM dd, yyyy 'at' HH:mm:ss UTC}<br>
                <strong>Account:</strong> {userName}
            </div>
            
            <p><strong>What happens next?</strong></p>
            <ul>
                <li>Your new password is now active</li>
                <li>You can log in to your account immediately</li>
                <li>All your data and progress remain unchanged</li>
                <li>Your account security has been maintained</li>
            </ul>
            
            <p><strong>Security Tips:</strong></p>
            <ul>
                <li>Use a strong, unique password</li>
                <li>Never share your password with anyone</li>
                <li>Enable two-factor authentication if available</li>
                <li>Log out from shared devices</li>
            </ul>
            
            <p>If you didn't reset your password, please contact our support team immediately.</p>
            
            <p>Best regards,<br>
            <strong>The BrainStormEra Team</strong></p>
        </div>
        
        <div class='footer'>
            <p>This email confirms that your password was successfully reset on {resetTime:dddd, MMMM dd, yyyy}.</p>
            <p>© 2024 BrainStormEra. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Get welcome email template
        /// </summary>
        private string GetWelcomeEmailTemplate(string userName, string username)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welcome to BrainStormEra!</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 300;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .welcome-container {{
            background: linear-gradient(135deg, #ffecd2 0%, #fcb69f 100%);
            padding: 20px;
            border-radius: 10px;
            text-align: center;
            margin: 30px 0;
        }}
        .welcome-icon {{
            font-size: 48px;
            margin-bottom: 10px;
        }}
        .features {{
            background-color: #f8f9fa;
            padding: 20px;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px 30px;
            text-align: center;
            color: #6c757d;
            font-size: 14px;
        }}
        .btn {{
            display: inline-block;
            padding: 12px 24px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 10px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎓 Welcome to BrainStormEra!</h1>
            <p>Your Learning Journey Begins Here</p>
        </div>
        
        <div class='content'>
            <h2>Hello {userName}!</h2>
            
            <div class='welcome-container'>
                <div class='welcome-icon'>🌟</div>
                <h3>Welcome to the BrainStormEra Community!</h3>
                <p>We're excited to have you join our learning platform.</p>
            </div>
            
            <p>Your account has been successfully created with the username: <strong>{username}</strong></p>
            
            <div class='features'>
                <h3>🚀 What you can do now:</h3>
                <ul>
                    <li>Browse our extensive course catalog</li>
                    <li>Enroll in courses that interest you</li>
                    <li>Track your learning progress</li>
                    <li>Earn certificates and achievements</li>
                    <li>Connect with other learners</li>
                    <li>Access interactive learning materials</li>
                </ul>
            </div>
            
            <p><strong>Getting Started:</strong></p>
            <ol>
                <li>Complete your profile with additional information</li>
                <li>Explore our course categories</li>
                <li>Take a skills assessment to get personalized recommendations</li>
                <li>Start your first course!</li>
            </ol>
            
            <p>If you have any questions or need help getting started, our support team is here to help!</p>
            
            <p>Happy Learning!<br>
            <strong>The BrainStormEra Team</strong></p>
        </div>
        
        <div class='footer'>
            <p>Thank you for choosing BrainStormEra for your learning journey.</p>
            <p>© 2024 BrainStormEra. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Get test email template
        /// </summary>
        private string GetTestEmailTemplate()
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Configuration Test</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 300;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .test-container {{
            background: linear-gradient(135deg, #a8edea 0%, #fed6e3 100%);
            padding: 20px;
            border-radius: 10px;
            text-align: center;
            margin: 30px 0;
        }}
        .test-icon {{
            font-size: 48px;
            margin-bottom: 10px;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px 30px;
            text-align: center;
            color: #6c757d;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Email Configuration Test</h1>
            <p>BrainStormEra Learning Platform</p>
        </div>
        
        <div class='content'>
            <div class='test-container'>
                <div class='test-icon'>📧</div>
                <h3>Email Service is Working!</h3>
                <p>This is a test email to verify that the email configuration is working correctly.</p>
            </div>
            
            <p><strong>Test Details:</strong></p>
            <ul>
                <li><strong>Time:</strong> {DateTime.UtcNow:dddd, MMMM dd, yyyy 'at' HH:mm:ss UTC}</li>
                <li><strong>Status:</strong> Email service is operational</li>
                <li><strong>SMTP Host:</strong> {_smtpHost}</li>
                <li><strong>SMTP Port:</strong> {_smtpPort}</li>
            </ul>
            
            <p>If you received this email, it means:</p>
            <ul>
                <li>✅ SMTP configuration is correct</li>
                <li>✅ Email credentials are valid</li>
                <li>✅ Network connectivity is working</li>
                <li>✅ Email templates are functioning</li>
            </ul>
            
            <p>You can now proceed with using the email features in BrainStormEra!</p>
        </div>
        
        <div class='footer'>
            <p>This is an automated test email from BrainStormEra.</p>
            <p>© 2024 BrainStormEra. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        #endregion
    }
}