using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IAuthRepo : IBaseRepo<Account>
    {
        // Core authentication methods matching the actual Account model
        Task<Account?> GetUserByUsernameAsync(string username);
        Task<Account?> GetUserByEmailAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> CreateUserAsync(Account user);
        Task<bool> UpdateUserAsync(Account user);
        Task<bool> VerifyPasswordAsync(string password, string passwordHash);
        Task<bool> UpdateLastLoginAsync(string userId);
        Task<bool> BanUserAsync(string userId, bool isBanned);

        // Password management
        Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
        Task<bool> ResetPasswordAsync(string email, string newPassword);

        // Account security
        Task<bool> IsAccountBannedAsync(string userId);
        Task<bool> LockAccountAsync(string userId, DateTime? lockUntil = null);
        Task<bool> UnlockAccountAsync(string userId);

        // Account status and metadata
        Task<DateTime?> GetLastLoginAsync(string userId);
        Task<bool> IsAccountActiveAsync(string userId);
        Task<bool> ActivateAccountAsync(string userId);
        Task<bool> DeactivateAccountAsync(string userId);

        // Authentication helpers
        Task<Account?> AuthenticateAsync(string username, string password);
        Task<Account?> AuthenticateByEmailAsync(string email, string password);
        Task<bool> ValidatePasswordAsync(string userId, string password);

        // User role management
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<bool> IsUserInRoleAsync(string userId, string roleName);

        // Cleanup and maintenance
        Task<bool> CleanupInactiveAccountsAsync(int daysInactive = 365);
    }
}

// Supporting models for Auth operations matching the existing implementation
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AuthenticationResult
{
    public bool Success { get; set; }
    public Account? User { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Token { get; set; }
}
