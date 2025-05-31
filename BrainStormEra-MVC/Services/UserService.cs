using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Services.Interfaces;
using BrainStormEra_MVC.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace BrainStormEra_MVC.Services
{
    public class UserService : IUserService
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IMemoryCache _cache;

        // Increased cache duration for frequently accessed data
        private static readonly TimeSpan CacheExpirationTime = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan SlidingExpirationTime = TimeSpan.FromMinutes(10);

        // Cache keys
        private const string UserByUsernameKey = "User_By_Username_{0}";
        private const string UserByEmailKey = "User_By_Email_{0}";

        // Concurrent cache to avoid lock contention for frequently accessed data
        private static readonly ConcurrentDictionary<string, DateTimeOffset> _usernameLookupCache = new();
        private static readonly ConcurrentDictionary<string, DateTimeOffset> _emailLookupCache = new();

        // Cache cleanup interval (run every 1 hour)
        private static readonly TimeSpan CacheCleanupInterval = TimeSpan.FromHours(1);
        private readonly Timer _cleanupTimer;

        public UserService(BrainStormEraContext context, ILogger<UserService> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;

            // Start cache cleanup timer
            _cleanupTimer = new Timer(CleanupExpiredCache, null, CacheCleanupInterval, CacheCleanupInterval);
        }

        public async Task<Account?> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                return null;

            string cacheKey = string.Format(UserByUsernameKey, username);

            if (!_cache.TryGetValue(cacheKey, out Account? user))
            {
                user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (user != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(CacheExpirationTime)
                        .SetSlidingExpiration(SlidingExpirationTime)
                        .SetPriority(CacheItemPriority.High);

                    _cache.Set(cacheKey, user, cacheOptions);
                    _usernameLookupCache.TryAdd(username, DateTimeOffset.UtcNow.Add(CacheExpirationTime));
                }
            }

            return user;
        }

        public async Task<Account?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            string cacheKey = string.Format(UserByEmailKey, email);

            if (!_cache.TryGetValue(cacheKey, out Account? user))
            {
                user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserEmail == email);

                if (user != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(CacheExpirationTime)
                        .SetSlidingExpiration(SlidingExpirationTime)
                        .SetPriority(CacheItemPriority.High);

                    _cache.Set(cacheKey, user, cacheOptions);
                    _emailLookupCache.TryAdd(email, DateTimeOffset.UtcNow.Add(CacheExpirationTime));
                }
            }

            return user;
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            // Check in-memory cache first
            if (_usernameLookupCache.TryGetValue(username, out var expiryTime))
            {
                if (expiryTime > DateTimeOffset.UtcNow)
                {
                    return true;
                }
                // Remove expired entry
                _usernameLookupCache.TryRemove(username, out _);
            }

            // Check database
            bool exists = await _context.Accounts
                .AsNoTracking()
                .AnyAsync(a => a.Username == username);

            // Cache positive results
            if (exists)
            {
                _usernameLookupCache.TryAdd(username, DateTimeOffset.UtcNow.Add(CacheExpirationTime));
            }

            return exists;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            // Check in-memory cache first
            if (_emailLookupCache.TryGetValue(email, out var expiryTime))
            {
                if (expiryTime > DateTimeOffset.UtcNow)
                {
                    return true;
                }
                // Remove expired entry
                _emailLookupCache.TryRemove(email, out _);
            }

            // Check database
            bool exists = await _context.Accounts
                .AsNoTracking()
                .AnyAsync(a => a.UserEmail == email);

            // Cache positive results
            if (exists)
            {
                _emailLookupCache.TryAdd(email, DateTimeOffset.UtcNow.Add(CacheExpirationTime));
            }

            return exists;
        }

        public async Task<bool> CreateUserAsync(Account user)
        {
            try
            {
                if (user == null)
                    return false;

                _context.Accounts.Add(user);
                await _context.SaveChangesAsync();

                // Update cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(CacheExpirationTime)
                    .SetSlidingExpiration(SlidingExpirationTime)
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(string.Format(UserByUsernameKey, user.Username), user, cacheOptions);
                _cache.Set(string.Format(UserByEmailKey, user.UserEmail), user, cacheOptions);

                _usernameLookupCache.TryAdd(user.Username, DateTimeOffset.UtcNow.Add(CacheExpirationTime));
                _emailLookupCache.TryAdd(user.UserEmail, DateTimeOffset.UtcNow.Add(CacheExpirationTime));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", user?.Username);
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(Account user)
        {
            try
            {
                if (user == null)
                    return false;

                user.AccountUpdatedAt = DateTime.UtcNow;
                _context.Accounts.Update(user);
                await _context.SaveChangesAsync();

                // Update cache
                InvalidateUserCache(user);

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(CacheExpirationTime)
                    .SetSlidingExpiration(SlidingExpirationTime)
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(string.Format(UserByUsernameKey, user.Username), user, cacheOptions);
                _cache.Set(string.Format(UserByEmailKey, user.UserEmail), user, cacheOptions);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", user?.UserId);
                return false;
            }
        }

        public Task<bool> VerifyPasswordAsync(string password, string storedHash)
        {
            // This is a CPU-bound operation, so run it on a ThreadPool thread
            return Task.Run(() => PasswordHasher.VerifyPassword(password, storedHash));
        }

        public async Task UpdateLastLoginAsync(string userId)
        {
            try
            {
                var user = await _context.Accounts.FindAsync(userId);
                if (user != null)
                {
                    user.LastLogin = DateTime.UtcNow;
                    user.AccountUpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Update cache
                    InvalidateUserCache(user);

                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(CacheExpirationTime)
                        .SetSlidingExpiration(SlidingExpirationTime)
                        .SetPriority(CacheItemPriority.High);

                    _cache.Set(string.Format(UserByUsernameKey, user.Username), user, cacheOptions);
                    _cache.Set(string.Format(UserByEmailKey, user.UserEmail), user, cacheOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user {UserId}", userId);
            }
        }

        public async Task<bool> BanUserAsync(string userId, bool isBanned)
        {
            try
            {
                var user = await _context.Accounts.FindAsync(userId);
                if (user != null)
                {
                    user.IsBanned = isBanned;
                    user.AccountUpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Update cache
                    InvalidateUserCache(user);

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning user {UserId}", userId);
                return false;
            }
        }

        private void InvalidateUserCache(Account user)
        {
            if (user == null) return;

            _cache.Remove(string.Format(UserByUsernameKey, user.Username));
            _cache.Remove(string.Format(UserByEmailKey, user.UserEmail));

            _usernameLookupCache.TryRemove(user.Username, out _);
            _emailLookupCache.TryRemove(user.UserEmail, out _);
        }

        private void CleanupExpiredCache(object? state)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;

                // Clean up username cache
                foreach (var item in _usernameLookupCache)
                {
                    if (item.Value < now)
                    {
                        _usernameLookupCache.TryRemove(item.Key, out _);
                        _cache.Remove(string.Format(UserByUsernameKey, item.Key));
                    }
                }

                // Clean up email cache
                foreach (var item in _emailLookupCache)
                {
                    if (item.Value < now)
                    {
                        _emailLookupCache.TryRemove(item.Key, out _);
                        _cache.Remove(string.Format(UserByEmailKey, item.Key));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up user cache");
            }
        }
    }
}
