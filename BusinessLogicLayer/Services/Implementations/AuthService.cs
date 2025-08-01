using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using DataAccessLayer.Models;
using DataAccessLayer.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BusinessLogicLayer.Utilities;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using BusinessLogicLayer.DTOs.Security;
using BusinessLogicLayer.DTOs.Auth;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Validation result for username and email availability checks
    /// </summary>


    /// <summary>
    /// Implementation class that handles complex business logic for Authentication operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// authorization, validation, and error handling.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthService> _logger;
        private readonly BrainStormEraContext _context;
        private readonly IAvatarService _avatarService;
        private readonly IMediaPathService _mediaPathService;
        private readonly IEmailService _emailService;
        private readonly ISecurityService _securityService;
        // Cache for OTP codes (in a production environment, use a more robust solution like Redis)
        private static Dictionary<string, (string otp, DateTime expiry)> _otpCache = new();

        public AuthService(
            IUserService userService,
            ILogger<AuthService> logger,
            BrainStormEraContext context,
            IAvatarService avatarService,
            IMediaPathService mediaPathService,
            IEmailService emailService,
            ISecurityService securityService)
        {
            _userService = userService;
            _logger = logger;
            _context = context;
            _avatarService = avatarService;
            _mediaPathService = mediaPathService;
            _emailService = emailService;
            _securityService = securityService;
        }

        #region Login Operations        /// <summary>
        /// Get login view model
        /// </summary>
        public Task<LoginResult> GetLoginViewModelAsync(string? returnUrl = null)
        {
            try
            {
                return Task.FromResult(new LoginResult
                {
                    Success = true,
                    ViewModel = new LoginViewModel
                    {
                        ReturnUrl = returnUrl ?? string.Empty,
                        Username = string.Empty,
                        Password = string.Empty
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading login page");
                return Task.FromResult(new LoginResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the login page.",
                    ViewModel = new LoginViewModel
                    {
                        ReturnUrl = returnUrl ?? string.Empty,
                        Username = string.Empty,
                        Password = string.Empty
                    }
                });
            }
        }

        /// <summary>
        /// Authenticate user with comprehensive validation and security checks
        /// </summary>
        public async Task<LoginResult> AuthenticateUserAsync(HttpContext httpContext, LoginViewModel model)
        {
            var ipAddress = GetClientIpAddress(httpContext);

            try
            {
                _logger.LogInformation("Login attempt for username: {Username} from IP: {IpAddress}", model.Username, ipAddress);

                // SECURITY CHECK: Verify if this login attempt should be allowed
                var securityCheck = await _securityService.CheckLoginAttemptAsync(model.Username, ipAddress);
                if (!securityCheck.IsAllowed)
                {
                    _logger.LogWarning("Login attempt blocked by security service - Username: {Username}, IP: {IpAddress}, Reason: {Reason}",
                        model.Username, ipAddress, securityCheck.BlockReason);

                    // Log the blocked attempt
                    await _securityService.LogLoginAttemptAsync(new LoginAttemptRequest
                    {
                        Username = model.Username,
                        IpAddress = ipAddress,
                        UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                        IsSuccessful = false,
                        FailureReason = "Blocked by security service: " + securityCheck.BlockReason
                    });

                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = securityCheck.ErrorMessage ?? "Too many failed login attempts. Please try again later.",
                        ViewModel = model
                    };
                }

                // Find user by username using service
                var user = await _userService.GetUserByUsernameAsync(model.Username);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {Username}", model.Username);

                    // Log failed attempt
                    await _securityService.LogLoginAttemptAsync(new LoginAttemptRequest
                    {
                        Username = model.Username,
                        IpAddress = ipAddress,
                        UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                        IsSuccessful = false,
                        FailureReason = "User not found"
                    });

                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid username or password. Please check your credentials and try again.",
                        ViewModel = model
                    };
                }

                // Check if user is banned
                if (user.IsBanned == true)
                {
                    _logger.LogWarning("Banned user attempted login: {Username}", user.Username);

                    // Log failed attempt
                    await _securityService.LogLoginAttemptAsync(new LoginAttemptRequest
                    {
                        Username = model.Username,
                        IpAddress = ipAddress,
                        UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                        IsSuccessful = false,
                        FailureReason = "User account is banned"
                    });

                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Your account has been suspended. Please contact support for assistance.",
                        ViewModel = model
                    };
                }

                // Verify password using service
                if (string.IsNullOrEmpty(model.Password) || !await _userService.VerifyPasswordAsync(model.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password for user: {Username}", user.Username);

                    // Log failed attempt
                    await _securityService.LogLoginAttemptAsync(new LoginAttemptRequest
                    {
                        Username = model.Username,
                        IpAddress = ipAddress,
                        UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                        IsSuccessful = false,
                        FailureReason = "Invalid password"
                    });

                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid username or password. Please check your credentials and try again.",
                        ViewModel = model
                    };
                }

                _logger.LogInformation("Password verified successfully for user: {Username}", user.Username);

                // Block admin role from logging in
                if (string.Equals(user.UserRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Admin login attempt blocked for user: {Username}", user.Username);

                    // Log failed attempt
                    await _securityService.LogLoginAttemptAsync(new LoginAttemptRequest
                    {
                        Username = model.Username,
                        IpAddress = ipAddress,
                        UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                        IsSuccessful = false,
                        FailureReason = "Admin login blocked"
                    });

                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid login attempt.",
                        ViewModel = model
                    };
                }

                // Validate user role (ensure it's one of the allowed roles)
                var validRoles = new[] { "instructor", "learner" };
                if (!validRoles.Contains(user.UserRole, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid role for user: {Username}, Role: {Role}", user.Username, user.UserRole);
                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid login attempt.",
                        ViewModel = model
                    };
                }

                _logger.LogInformation("Creating claims for user: {Username}", user.Username);

                // Create claims for authentication with comprehensive user information
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId),
                    new Claim(ClaimTypes.Role, user.UserRole),
                    new Claim(ClaimTypes.Email, user.UserEmail ?? ""),
                    new Claim("UserId", user.UserId),
                    new Claim("UserRole", user.UserRole),
                    new Claim("LoginTime", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
                };

                // Add optional user information
                if (!string.IsNullOrEmpty(user.FullName))
                {
                    claims.Add(new Claim("FullName", user.FullName));
                }
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    claims.Add(new Claim("PhoneNumber", user.PhoneNumber));
                }
                if (user.DateOfBirth.HasValue)
                {
                    claims.Add(new Claim("DateOfBirth", user.DateOfBirth.Value.ToString("yyyy-MM-dd")));
                }
                if (!string.IsNullOrEmpty(user.UserImage))
                {
                    claims.Add(new Claim("UserImage", user.UserImage));
                }
                if (!string.IsNullOrEmpty(user.UserAddress))
                {
                    claims.Add(new Claim("UserAddress", user.UserAddress));
                }
                if (user.Gender.HasValue)
                {
                    claims.Add(new Claim("Gender", user.Gender.Value.ToString()));
                }
                if (user.PaymentPoint.HasValue)
                {
                    claims.Add(new Claim("PaymentPoint", user.PaymentPoint.Value.ToString()));
                }

                // Add points last refresh time claim
                claims.Add(new Claim("PointsLastRefresh", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1),
                    AllowRefresh = true
                };

                _logger.LogInformation("Signing in user: {Username}", user.Username);

                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User signed in successfully: {Username}", user.Username);

                // Update last login time using service
                await _userService.UpdateLastLoginAsync(user.UserId);

                // Log successful login attempt
                await _securityService.LogLoginAttemptAsync(new LoginAttemptRequest
                {
                    Username = model.Username,
                    IpAddress = ipAddress,
                    UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                    IsSuccessful = true,
                    FailureReason = null
                });

                _logger.LogInformation("User {Username} logged in at {Time}.", user.Username, DateTime.UtcNow);

                return new LoginResult
                {
                    Success = true,
                    SuccessMessage = $"Welcome back, {user.FullName ?? user.Username}! Login successful.",
                    UserRole = user.UserRole,
                    RedirectAction = GetPostLoginRedirect(user.UserRole, model.ReturnUrl)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt for user {Username}", model.Username);

                // Log error attempt
                try
                {
                    await _securityService.LogLoginAttemptAsync(new LoginAttemptRequest
                    {
                        Username = model.Username,
                        IpAddress = ipAddress,
                        UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                        IsSuccessful = false,
                        FailureReason = "System error: " + ex.Message
                    });
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log error attempt for user {Username}", model.Username);
                }

                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred during login. Please try again later.",
                    ViewModel = model
                };
            }
        }

        #endregion

        #region Register Operations        /// <summary>
        /// Get register view model
        /// </summary>
        public Task<RegisterResult> GetRegisterViewModelAsync()
        {
            try
            {
                return Task.FromResult(new RegisterResult
                {
                    Success = true,
                    ViewModel = new RegisterViewModel
                    {
                        Username = string.Empty,
                        Email = string.Empty,
                        Password = string.Empty,
                        ConfirmPassword = string.Empty
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading register page");
                return Task.FromResult(new RegisterResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the register page."
                });
            }
        }

        /// <summary>
        /// Register new user with comprehensive validation
        /// </summary>
        public async Task<RegisterResult> RegisterUserAsync(RegisterViewModel model)
        {
            try
            {
                // Check if username already exists
                bool usernameExists = await _userService.UsernameExistsAsync(model.Username);
                if (usernameExists)
                {
                    return new RegisterResult
                    {
                        Success = false,
                        ErrorMessage = "Username is already taken. Please choose a different username.",
                        ViewModel = model,
                        ValidationError = "Username"
                    };
                }

                // Check if email already exists
                bool emailExists = await _userService.EmailExistsAsync(model.Email);
                if (emailExists)
                {
                    return new RegisterResult
                    {
                        Success = false,
                        ErrorMessage = "Email is already registered. Please use a different email or try logging in.",
                        ViewModel = model,
                        ValidationError = "Email"
                    };
                }                // Create new account
                var account = new Account
                {
                    UserId = Guid.NewGuid().ToString(),
                    UserRole = "learner", // Default role for new registrations
                    Username = model.Username,
                    PasswordHash = PasswordHasher.HashPassword(model.Password),
                    UserEmail = model.Email,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth.HasValue ? DateOnly.FromDateTime(model.DateOfBirth.Value) : null,
                    Gender = model.Gender,
                    PhoneNumber = model.PhoneNumber,
                    UserAddress = model.Address,
                    IsBanned = false,
                    AccountCreatedAt = DateTime.UtcNow,
                    AccountUpdatedAt = DateTime.UtcNow
                };

                // Use service to create user
                bool success = await _userService.CreateUserAsync(account);

                if (!success)
                {
                    return new RegisterResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create account. Please try again later.",
                        ViewModel = model
                    };
                }

                // Send welcome email
                if (_emailService != null)
                {
                    var emailResult = await _emailService.SendWelcomeEmailAsync(
                        model.Email ?? "",
                        model.FullName ?? "User",
                        model.Username ?? model.Email ?? "User");

                    if (!emailResult.IsSuccess)
                    {
                        _logger.LogWarning("Failed to send welcome email to {Email}: {Error}",
                            model.Email, emailResult.Message);
                    }
                }

                return new RegisterResult
                {
                    Success = true,
                    SuccessMessage = "Your account has been created successfully. You can now log in.",
                    RedirectAction = "Login"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                return new RegisterResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred during registration. Please try again later.",
                    ViewModel = model
                };
            }
        }

        /// <summary>
        /// Check if username is available
        /// </summary>
        public async Task<ValidationResult> CheckUsernameAvailabilityAsync(string username)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || !System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$"))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Invalid username format"
                    };
                }

                bool exists = await _userService.UsernameExistsAsync(username);
                return new ValidationResult
                {
                    IsValid = !exists,
                    Message = exists ? "Username is already taken" : ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username availability: {Username}", username);
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Error checking username availability"
                };
            }
        }

        /// <summary>
        /// Check if email is available
        /// </summary>
        public async Task<ValidationResult> CheckEmailAvailabilityAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Invalid email format"
                    };
                }

                bool exists = await _userService.EmailExistsAsync(email);
                return new ValidationResult
                {
                    IsValid = !exists,
                    Message = exists ? "Email is already registered" : ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email availability: {Email}", email);
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Error checking email availability"
                };
            }
        }

        #endregion

        #region Password Reset Operations        /// <summary>
        /// Get forgot password view model
        /// </summary>
        public Task<ForgotPasswordResult> GetForgotPasswordViewModelAsync()
        {
            try
            {
                return Task.FromResult(new ForgotPasswordResult
                {
                    Success = true,
                    ViewModel = new ForgotPasswordViewModel
                    {
                        Email = string.Empty
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading forgot password page");
                return Task.FromResult(new ForgotPasswordResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the forgot password page."
                });
            }
        }

        /// <summary>
        /// Process forgot password request
        /// </summary>
        public async Task<ForgotPasswordResult> ProcessForgotPasswordAsync(ForgotPasswordViewModel model)
        {
            try
            {
                // Find user by email using service
                var user = await _userService.GetUserByEmailAsync(model.Email);

                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return new ForgotPasswordResult
                    {
                        Success = true,
                        RedirectAction = "ForgotPasswordConfirmation"
                    };
                }

                // Generate OTP code
                string otp = GenerateOtp();

                // Store OTP in cache with 10-minute expiry
                _otpCache[model.Email] = (otp, DateTime.UtcNow.AddMinutes(10));

                _logger.LogInformation("Generated OTP {OTP} for email {Email}", otp, model.Email);

                // Send email with OTP using EmailService
                var emailResult = await _emailService.SendForgotPasswordEmailAsync(
                    model.Email,
                    user.FullName ?? user.Username,
                    otp,
                    10);

                if (!emailResult.IsSuccess)
                {
                    _logger.LogError("Failed to send forgot password email to {Email}: {Error}",
                        model.Email, emailResult.Message);

                    // Remove OTP from cache since email failed
                    _otpCache.Remove(model.Email);

                    return new ForgotPasswordResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to send verification email. Please try again later.",
                        ViewModel = model
                    };
                }

                _logger.LogInformation("Forgot password email sent successfully to {Email}", model.Email);

                return new ForgotPasswordResult
                {
                    Success = true,
                    RedirectAction = "VerifyOtp",
                    Email = model.Email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for email {Email}", model.Email);
                return new ForgotPasswordResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred. Please try again.",
                    ViewModel = model
                };
            }
        }        /// <summary>
                 /// Get verify OTP view model
                 /// </summary>
        public Task<VerifyOtpResult> GetVerifyOtpViewModelAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return Task.FromResult(new VerifyOtpResult
                    {
                        Success = false,
                        RedirectAction = "ForgotPassword"
                    });
                }

                return Task.FromResult(new VerifyOtpResult
                {
                    Success = true,
                    ViewModel = new OtpVerificationViewModel
                    {
                        Email = email,
                        OtpCode = string.Empty
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading verify OTP page");
                return Task.FromResult(new VerifyOtpResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the verification page.",
                    RedirectAction = "ForgotPassword"
                });
            }
        }        /// <summary>
                 /// Verify OTP code
                 /// </summary>
        public Task<VerifyOtpResult> VerifyOtpAsync(OtpVerificationViewModel model)
        {
            try
            {
                _logger.LogInformation("Verifying OTP {OTP} for email {Email}", model.OtpCode, model.Email);

                // Check if OTP exists and is valid
                var cacheFound = _otpCache.TryGetValue(model.Email, out var otpData);
                _logger.LogInformation("Cache lookup for {Email}: Found={Found}, StoredOTP={StoredOTP}, InputOTP={InputOTP}, Expiry={Expiry}, Now={Now}",
                    model.Email, cacheFound, cacheFound ? otpData.otp : "null", model.OtpCode,
                    cacheFound ? otpData.expiry.ToString("yyyy-MM-dd HH:mm:ss") : "null", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                if (!cacheFound ||
                    otpData.otp != model.OtpCode ||
                    otpData.expiry < DateTime.UtcNow)
                {
                    return Task.FromResult(new VerifyOtpResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid or expired OTP code.",
                        ViewModel = model
                    });
                }

                // Remove OTP from cache
                _otpCache.Remove(model.Email);

                // Generate password reset token
                string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

                // Store token in cache with 1-hour expiry
                _otpCache[model.Email] = (token, DateTime.UtcNow.AddHours(1));

                return Task.FromResult(new VerifyOtpResult
                {
                    Success = true,
                    RedirectAction = "ResetPassword",
                    Email = model.Email,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OTP verification for email {Email}", model.Email);
                return Task.FromResult(new VerifyOtpResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred during verification. Please try again.",
                    ViewModel = model
                });
            }
        }        /// <summary>
                 /// Get reset password view model
                 /// </summary>
        public Task<ResetPasswordResult> GetResetPasswordViewModelAsync(string email, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                {
                    return Task.FromResult(new ResetPasswordResult
                    {
                        Success = false,
                        RedirectAction = "ForgotPassword"
                    });
                }

                // Verify token is valid
                if (!_otpCache.TryGetValue(email, out var tokenData) ||
                    tokenData.otp != token ||
                    tokenData.expiry < DateTime.UtcNow)
                {
                    return Task.FromResult(new ResetPasswordResult
                    {
                        Success = false,
                        RedirectAction = "ForgotPassword"
                    });
                }

                return Task.FromResult(new ResetPasswordResult
                {
                    Success = true,
                    ViewModel = new ResetPasswordViewModel
                    {
                        Email = email,
                        Token = token,
                        NewPassword = string.Empty,
                        ConfirmPassword = string.Empty
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading reset password page");
                return Task.FromResult(new ResetPasswordResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the reset password page.",
                    RedirectAction = "ForgotPassword"
                });
            }
        }

        /// <summary>
        /// Reset user password
        /// </summary>
        public async Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            try
            {
                // Verify token is valid
                if (!_otpCache.TryGetValue(model.Email, out var tokenData) ||
                    tokenData.otp != model.Token ||
                    tokenData.expiry < DateTime.UtcNow)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid or expired token.",
                        ViewModel = model
                    };
                }

                var user = await _userService.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return new ResetPasswordResult
                    {
                        Success = true,
                        RedirectAction = "ResetPasswordConfirmation"
                    };
                }

                // Update password
                user.PasswordHash = PasswordHasher.HashPassword(model.NewPassword);
                user.AccountUpdatedAt = DateTime.UtcNow;

                await _userService.UpdateUserAsync(user);

                // Remove token from cache
                _otpCache.Remove(model.Email);

                // Send password reset confirmation email
                var resetTime = DateTime.UtcNow;
                var emailResult = await _emailService.SendPasswordResetConfirmationEmailAsync(
                    model.Email,
                    user.FullName ?? user.Username,
                    resetTime);

                if (!emailResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to send password reset confirmation email to {Email}: {Error}",
                        model.Email, emailResult.Message);
                    // Don't fail the password reset if email fails, just log it
                }
                else
                {
                    _logger.LogInformation("Password reset confirmation email sent successfully to {Email}", model.Email);
                }

                return new ResetPasswordResult
                {
                    Success = true,
                    RedirectAction = "ResetPasswordConfirmation"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset for email {Email}", model.Email);
                return new ResetPasswordResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred. Please try again.",
                    ViewModel = model
                };
            }
        }

        #endregion

        #region Logout Operations

        /// <summary>
        /// Logout user
        /// </summary>
        public async Task<LogoutResult> LogoutUserAsync(HttpContext httpContext, ClaimsPrincipal user)
        {
            try
            {
                var username = user.Identity?.Name;

                // Sign out of the authentication scheme
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // Clear all session data
                httpContext.Session.Clear();

                // Clear all authentication cookies
                foreach (var cookie in httpContext.Request.Cookies.Keys)
                {
                    httpContext.Response.Cookies.Delete(cookie);
                }

                // Explicitly delete the authentication cookie
                httpContext.Response.Cookies.Delete("BrainStormEraAuth");

                // Clear any authentication tokens from the response
                httpContext.Response.Headers["Authorization"] = "";

                // Set cache control headers to prevent caching of sensitive information
                httpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                httpContext.Response.Headers["Pragma"] = "no-cache";
                httpContext.Response.Headers["Expires"] = "0";

                _logger.LogInformation("User {Username} logged out at {Time}", username, DateTime.UtcNow);

                return new LogoutResult
                {
                    Success = true,
                    Message = "You have been logged out successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return new LogoutResult
                {
                    Success = false,
                    Message = "An error occurred during logout."
                };
            }
        }

        #endregion

        #region Profile Operations

        /// <summary>
        /// Get profile view model with user statistics
        /// </summary>
        public async Task<ProfileResult> GetProfileViewModelAsync(string userId, string userRole)
        {
            try
            {
                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (user == null)
                {
                    return new ProfileResult
                    {
                        Success = false,
                        ErrorMessage = "User profile not found."
                    };
                }                // Get user statistics based on role
                var enrolledCoursesCount = 0;
                var completedCoursesCount = 0;
                var createdCoursesCount = 0;
                var totalStudentsCount = 0;

                if (userRole?.Equals("learner", StringComparison.OrdinalIgnoreCase) == true)
                {
                    enrolledCoursesCount = await _context.Enrollments
                        .CountAsync(e => e.UserId == userId);

                    completedCoursesCount = await _context.Enrollments
                        .CountAsync(e => e.UserId == userId && e.ProgressPercentage >= 100);
                }
                else if (userRole?.Equals("instructor", StringComparison.OrdinalIgnoreCase) == true)
                {
                    createdCoursesCount = await _context.Courses
                        .CountAsync(c => c.AuthorId == userId);

                    totalStudentsCount = await _context.Enrollments
                        .Where(e => e.Course.AuthorId == userId)
                        .CountAsync();
                }

                // Get additional statistics
                var certificatesCount = await _context.Certificates
                    .CountAsync(c => c.UserId == userId);

                var achievementsCount = await _context.UserAchievements
                    .CountAsync(ua => ua.UserId == userId);

                var inProgressCoursesCount = await _context.Enrollments
                    .CountAsync(e => e.UserId == userId && e.ProgressPercentage > 0 && e.ProgressPercentage < 100);                // Convert gender short to string
                string? genderString = user.Gender switch
                {
                    1 => "Male",
                    2 => "Female",
                    3 => "Other",
                    _ => null
                };

                var viewModel = new UserProfileViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username ?? "",
                    FullName = user.FullName ?? "",
                    Email = user.UserEmail ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    UserAddress = user.UserAddress ?? "",
                    DateOfBirth = user.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
                    Gender = genderString ?? "",
                    UserImage = user.UserImage ?? "",
                    Role = userRole ?? "",
                    CreatedAt = user.AccountCreatedAt,

                    // Bank Information
                    BankAccountNumber = user.BankAccountNumber ?? "",
                    BankName = user.BankName ?? "",
                    AccountHolderName = user.AccountHolderName ?? "",

                    // Statistics
                    TotalCourses = userRole?.Equals("learner", StringComparison.OrdinalIgnoreCase) == true ? enrolledCoursesCount : createdCoursesCount,
                    CompletedCourses = completedCoursesCount,
                    InProgressCourses = inProgressCoursesCount,
                    CertificatesEarned = certificatesCount,
                    TotalAchievements = achievementsCount
                };

                return new ProfileResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for user: {UserId}", userId);
                return new ProfileResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading your profile."
                };
            }
        }

        /// <summary>
        /// Get edit profile view model
        /// </summary>
        public async Task<EditProfileResult> GetEditProfileViewModelAsync(string userId)
        {
            try
            {
                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (user == null)
                {
                    return new EditProfileResult
                    {
                        Success = false,
                        ErrorMessage = "User profile not found."
                    };
                }                // Convert gender short to string
                string? genderString = user.Gender switch
                {
                    1 => "Nam",
                    2 => "Nữ",
                    3 => "Khác",
                    _ => null
                };

                var viewModel = new EditProfileViewModel
                {
                    FullName = user.FullName ?? "",
                    Email = user.UserEmail ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    UserAddress = user.UserAddress ?? "",
                    DateOfBirth = user.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
                    Gender = genderString ?? "",
                    BankAccountNumber = user.BankAccountNumber ?? "",
                    BankName = user.BankName ?? "",
                    AccountHolderName = user.AccountHolderName ?? "",
                    CurrentImagePath = user.UserImage ?? ""
                };

                return new EditProfileResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit profile for user: {UserId}", userId);
                return new EditProfileResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the edit profile page."
                };
            }
        }

        /// <summary>
        /// Update user profile with comprehensive validation
        /// </summary>
        public async Task<EditProfileResult> UpdateProfileAsync(string userId, EditProfileViewModel model)
        {
            try
            {
                var user = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (user == null)
                {
                    return new EditProfileResult
                    {
                        Success = false,
                        ErrorMessage = "User profile not found.",
                        ViewModel = model
                    };
                }

                // Update user information
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.UserAddress = model.UserAddress;
                user.DateOfBirth = model.DateOfBirth.HasValue ? DateOnly.FromDateTime(model.DateOfBirth.Value) : null;

                // Update bank information
                user.BankAccountNumber = model.BankAccountNumber;
                user.BankName = model.BankName;
                user.AccountHolderName = model.AccountHolderName;

                // Convert gender string to short
                user.Gender = model.Gender switch
                {
                    "Nam" => 1,
                    "Nữ" => 2,
                    "Khác" => 3,
                    _ => null
                };

                user.AccountUpdatedAt = DateTime.UtcNow;

                // Handle profile image upload if provided
                string? warningMessage = null;
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    _logger.LogInformation("Starting avatar upload for user {UserId}", userId);

                    // Delete old avatar before uploading new one
                    if (!string.IsNullOrEmpty(user.UserImage))
                    {
                        _logger.LogInformation("Deleting old avatar: {OldAvatar}", user.UserImage);
                        await _avatarService.DeleteAvatarAsync(user.UserImage);
                    }

                    var uploadResult = await _avatarService.UploadAvatarAsync(model.ProfileImage, userId);
                    if (uploadResult.Success)
                    {
                        _logger.LogInformation("Avatar upload successful. Setting UserImage to: {ImagePath}", uploadResult.ImagePath);
                        user.UserImage = uploadResult.ImagePath;
                    }
                    else
                    {
                        _logger.LogError("Avatar upload failed: {Error}", uploadResult.ErrorMessage);
                        warningMessage = uploadResult.ErrorMessage ?? "Failed to upload profile image.";
                    }
                }

                _logger.LogInformation("Saving changes to database for user {UserId}", userId);
                var saveResult = await _context.SaveChangesAsync();
                _logger.LogInformation("Database save completed. Rows affected: {RowsAffected}", saveResult);

                return new EditProfileResult
                {
                    Success = true,
                    SuccessMessage = "Your profile has been updated successfully.",
                    WarningMessage = warningMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user: {UserId}", userId);
                return new EditProfileResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while updating your profile.",
                    ViewModel = model
                };
            }
        }

        /// <summary>
        /// Change user password with current password verification
        /// </summary>
        public async Task<ChangePasswordResult> ChangePasswordAsync(string userId, ChangePasswordViewModel model)
        {
            try
            {
                var user = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (user == null)
                {
                    return new ChangePasswordResult
                    {
                        Success = false,
                        ErrorMessage = "User account not found.",
                        ViewModel = model
                    };
                }

                // Verify current password
                if (!await _userService.VerifyPasswordAsync(model.CurrentPassword, user.PasswordHash))
                {
                    return new ChangePasswordResult
                    {
                        Success = false,
                        ErrorMessage = "Current password is incorrect.",
                        ValidationError = "CurrentPassword",
                        ViewModel = model
                    };
                }

                // Update password
                user.PasswordHash = Utilities.PasswordHasher.HashPassword(model.NewPassword);
                user.AccountUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new ChangePasswordResult
                {
                    Success = true,
                    SuccessMessage = "Your password has been changed successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return new ChangePasswordResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while changing your password.",
                    ViewModel = model
                };
            }
        }

        /// <summary>
        /// Get user avatar with fallback to default
        /// </summary>
        public async Task<AvatarResult> GetUserAvatarAsync(string? userId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return new AvatarResult
                    {
                        Success = true,
                        ImageBytes = _avatarService.GetDefaultAvatarBytes(),
                        ContentType = "image/svg+xml"
                    };
                }

                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (user?.UserImage == null || !_avatarService.AvatarExists(user.UserImage))
                {
                    return new AvatarResult
                    {
                        Success = true,
                        ImageBytes = _avatarService.GetDefaultAvatarBytes(),
                        ContentType = "image/svg+xml"
                    };
                }

                var imagePath = Path.Combine(_mediaPathService.GetPhysicalPath("avatars"), user.UserImage);
                var contentType = _avatarService.GetImageContentType(user.UserImage);
                var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);

                return new AvatarResult
                {
                    Success = true,
                    ImageBytes = imageBytes,
                    ContentType = contentType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving avatar for user: {UserId}", userId);
                return new AvatarResult
                {
                    Success = true,
                    ImageBytes = _avatarService.GetDefaultAvatarBytes(),
                    ContentType = "image/svg+xml"
                };
            }
        }

        /// <summary>
        /// Delete user avatar
        /// </summary>
        public async Task<DeleteAvatarResult> DeleteUserAvatarAsync(string userId)
        {
            try
            {
                var user = await _context.Accounts.FindAsync(userId);
                if (user == null)
                {
                    return new DeleteAvatarResult
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                if (!string.IsNullOrEmpty(user.UserImage))
                {
                    await _avatarService.DeleteAvatarAsync(user.UserImage);
                    user.UserImage = null;
                    user.AccountUpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return new DeleteAvatarResult
                {
                    Success = true,
                    Message = "Avatar deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar for user: {UserId}", userId);
                return new DeleteAvatarResult
                {
                    Success = false,
                    Message = "An error occurred while deleting avatar"
                };
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Generate OTP code
        /// </summary>
        private string GenerateOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var otpBytes = new byte[4];
            rng.GetBytes(otpBytes);
            var otp = Math.Abs(BitConverter.ToInt32(otpBytes, 0)) % 1000000;
            return otp.ToString("D6");
        }

        /// <summary>
        /// Helper method to determine the appropriate redirect after successful login
        /// </summary>
        private string GetPostLoginRedirect(string userRole, string? returnUrl = null)
        {
            _logger.LogInformation("Determining post-login redirect for role: {Role}, ReturnUrl: {ReturnUrl}", userRole, returnUrl);

            // Check return URL first and validate it's safe
            if (!string.IsNullOrEmpty(returnUrl))
            {
                _logger.LogInformation("Using return URL: {ReturnUrl}", returnUrl);
                return returnUrl;
            }

            // Role-based redirect (case-insensitive)
            return userRole.ToLower() switch
            {
                "instructor" => "/Home/InstructorDashboard",
                "learner" => "/Home/LearnerDashboard",
                _ => "/Home/Index"
            };
        }

        /// <summary>
        /// Get user information by userId
        /// </summary>
        public async Task<Account?> GetUserInfoAsync(string userId)
        {
            try
            {
                var user = await _context.Accounts
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info for userId: {UserId}", userId);
                return null;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get client IP address from HttpContext with proxy support
        /// </summary>
        private static string GetClientIpAddress(HttpContext httpContext)
        {
            // Try to get IP from various headers (for proxy scenarios)
            var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            }

            // Default to localhost if nothing is found
            return ipAddress ?? "127.0.0.1";
        }

        #endregion
    }


}









