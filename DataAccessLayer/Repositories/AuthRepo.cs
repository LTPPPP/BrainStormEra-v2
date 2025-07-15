using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace DataAccessLayer.Repositories
{
    public class AuthRepo : BaseRepo<Account>, IAuthRepo
    {
        private readonly ILogger<AuthRepo>? _logger;

        public AuthRepo(BrainStormEraContext context, ILogger<AuthRepo>? logger = null)
            : base(context)
        {
            _logger = logger;
        }

        // Core authentication methods matching UserService
        public async Task<Account?> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _dbSet.FirstOrDefaultAsync(a => a.Username == username);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user by username: {Username}", username);
                throw;
            }
        }

        public async Task<Account?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _dbSet.FirstOrDefaultAsync(a => a.UserEmail == email);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user by email: {Email}", email);
                throw;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                return await _dbSet.AnyAsync(a => a.Username == username);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking username existence: {Username}", username);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                return await _dbSet.AnyAsync(a => a.UserEmail == email);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking email existence: {Email}", email);
                throw;
            }
        }

        public async Task<bool> CreateUserAsync(Account user)
        {
            try
            {
                await AddAsync(user);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(Account user)
        {
            try
            {
                await UpdateAsync(user);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user");
                throw;
            }
        }

        public async Task<bool> VerifyPasswordAsync(string password, string passwordHash)
        {
            return await Task.FromResult(VerifyPassword(password, passwordHash));
        }

        public async Task<bool> UpdateLastLoginAsync(string userId)
        {
            try
            {
                var account = await GetByIdAsync(userId);
                if (account == null)
                    return false;

                account.LastLogin = DateTime.UtcNow;
                account.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating last login: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> BanUserAsync(string userId, bool isBanned)
        {
            try
            {
                var account = await GetByIdAsync(userId);
                if (account == null)
                    return false;

                account.IsBanned = isBanned;
                account.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating ban status: {UserId}", userId);
                throw;
            }
        }

        // Authentication methods
        public async Task<Account?> AuthenticateAsync(string username, string password)
        {
            try
            {
                var account = await _dbSet
                    .FirstOrDefaultAsync(a => a.Username == username && a.IsBanned != true);

                if (account != null && VerifyPassword(password, account.PasswordHash))
                {
                    await UpdateLastLoginAsync(account.UserId);
                    return account;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error authenticating user: {Username}", username);
                throw;
            }
        }

        public async Task<Account?> AuthenticateByEmailAsync(string email, string password)
        {
            try
            {
                var account = await _dbSet
                    .FirstOrDefaultAsync(a => a.UserEmail == email && a.IsBanned != true);

                if (account != null && VerifyPassword(password, account.PasswordHash))
                {
                    await UpdateLastLoginAsync(account.UserId);
                    return account;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error authenticating user by email: {Email}", email);
                throw;
            }
        }

        public async Task<bool> ValidatePasswordAsync(string userId, string password)
        {
            try
            {
                var account = await GetByIdAsync(userId);
                return account != null && VerifyPassword(password, account.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error validating password for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            try
            {
                var account = await GetByIdAsync(userId);
                if (account == null || !VerifyPassword(oldPassword, account.PasswordHash))
                    return false;

                account.PasswordHash = HashPassword(newPassword);
                account.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error changing password for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            try
            {
                var account = await _dbSet.FirstOrDefaultAsync(a => a.UserEmail == email);
                if (account == null)
                    return false;

                account.PasswordHash = HashPassword(newPassword);
                account.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error resetting password for email: {Email}", email);
                throw;
            }
        }

        // Account security
        public async Task<bool> IsAccountBannedAsync(string userId)
        {
            try
            {
                var account = await GetByIdAsync(userId);
                return account?.IsBanned == true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if account is banned: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> LockAccountAsync(string userId, DateTime? lockUntil = null)
        {
            try
            {
                return await BanUserAsync(userId, true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error locking account: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UnlockAccountAsync(string userId)
        {
            try
            {
                return await BanUserAsync(userId, false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error unlocking account: {UserId}", userId);
                throw;
            }
        }

        // Account status and metadata
        public async Task<DateTime?> GetLastLoginAsync(string userId)
        {
            try
            {
                var account = await GetByIdAsync(userId);
                return account?.LastLogin;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting last login: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> IsAccountActiveAsync(string userId)
        {
            try
            {
                var account = await GetByIdAsync(userId);
                return account?.IsBanned != true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if account is active: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ActivateAccountAsync(string userId)
        {
            try
            {
                return await BanUserAsync(userId, false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error activating account: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeactivateAccountAsync(string userId)
        {
            try
            {
                return await BanUserAsync(userId, true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deactivating account: {UserId}", userId);
                throw;
            }
        }

        // User role management
        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            try
            {
                var account = await GetByIdAsync(userId);
                return account != null ? new List<string> { account.UserRole } : new List<string>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user roles: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> IsUserInRoleAsync(string userId, string roleName)
        {
            try
            {
                var account = await GetByIdAsync(userId);
                return account?.UserRole?.Equals(roleName, StringComparison.OrdinalIgnoreCase) == true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking user role: {UserId}", userId);
                throw;
            }
        }

        // Cleanup and maintenance
        public async Task<bool> CleanupInactiveAccountsAsync(int daysInactive = 365)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysInactive);
                var inactiveAccounts = await _dbSet
                    .Where(a => a.LastLogin < cutoffDate || (!a.LastLogin.HasValue && a.AccountCreatedAt < cutoffDate))
                    .ToListAsync();

                if (inactiveAccounts.Any())
                {
                    foreach (var account in inactiveAccounts)
                    {
                        account.IsBanned = true;
                        account.AccountUpdatedAt = DateTime.UtcNow;
                    }

                    var result = await SaveChangesAsync();
                    return result > 0;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error cleaning up inactive accounts");
                throw;
            }
        }

        // Helper methods for password hashing using SHA1 algorithm
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password cannot be null or empty", nameof(password));
            }

            using var sha1 = SHA1.Create();
            var hashedBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
            // Convert to uppercase hex string to match PasswordHasher utility
            var hexString = Convert.ToHexString(hashedBytes).ToUpperInvariant();
            return hexString;
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            {
                return false;
            }

            string hashedInput = HashPassword(password);
            return hashedInput.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase);
        }
    }
}
