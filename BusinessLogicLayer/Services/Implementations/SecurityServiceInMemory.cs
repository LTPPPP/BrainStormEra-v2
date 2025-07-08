using BusinessLogicLayer.DTOs.Security;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.SecurityModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Implementation of security services for brute force protection using in-memory storage
    /// </summary>
    public class SecurityServiceInMemory : ISecurityService
    {
        private readonly ILogger<SecurityServiceInMemory> _logger;
        private readonly IMemoryCache _cache;
        private readonly SecurityConfig _config;

        // In-memory storage for login attempts and blocks
        private static readonly ConcurrentBag<LoginAttempt> _loginAttempts = new();
        private static readonly ConcurrentBag<SecurityBlock> _securityBlocks = new();

        private const string CACHE_PREFIX_ATTEMPTS = "security_attempts_";
        private const string CACHE_PREFIX_BLOCK = "security_block_";

        public SecurityServiceInMemory(
            ILogger<SecurityServiceInMemory> logger,
            IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;

            // Initialize security configuration
            _config = new SecurityConfig
            {
                MaxAttemptsPerMinute = 5,
                MaxAttemptsPerHour = 10,
                MaxAttemptsPerDay = 50,
                BlockDurationMinutes = 15,
                ExtendedBlockDurationHours = 24,
                MaxFailuresBeforeExtendedBlock = 20,
                EnableIpBlocking = true,
                EnableUserBlocking = true
            };
        }

        public async Task<SecurityCheckResult> CheckLoginAttemptAsync(string username, string ipAddress)
        {
            try
            {
                _logger.LogInformation("Checking login attempt for username: {Username}, IP: {IpAddress}", username, ipAddress);

                // First check if user or IP is currently blocked
                var blockInfo = await GetBlockInfoAsync(username, ipAddress);
                if (blockInfo.IsBlocked)
                {
                    _logger.LogWarning("Login attempt blocked - User: {Username}, IP: {IpAddress}, Reason: {Reason}",
                        username, ipAddress, blockInfo.BlockReason);
                    return blockInfo;
                }

                var now = DateTime.UtcNow;

                // Check rate limits for different time windows
                var attemptsLastMinute = await GetFailedAttemptsCountAsync(username, ipAddress, TimeSpan.FromMinutes(1));
                var attemptsLastHour = await GetFailedAttemptsCountAsync(username, ipAddress, TimeSpan.FromHours(1));
                var attemptsLastDay = await GetFailedAttemptsCountAsync(username, ipAddress, TimeSpan.FromDays(1));

                // Check if any rate limits are exceeded
                if (attemptsLastMinute >= _config.MaxAttemptsPerMinute)
                {
                    await BlockUserOrIpAsync(username, ipAddress, "Too many attempts in 1 minute", _config.BlockDurationMinutes);
                    return new SecurityCheckResult
                    {
                        IsAllowed = false,
                        IsBlocked = true,
                        BlockReason = "Too many login attempts in a short time. Please wait before trying again.",
                        BlockExpiresAt = now.AddMinutes(_config.BlockDurationMinutes),
                        ErrorMessage = $"Account temporarily locked due to too many failed attempts. Try again in {_config.BlockDurationMinutes} minutes."
                    };
                }

                if (attemptsLastHour >= _config.MaxAttemptsPerHour)
                {
                    await BlockUserOrIpAsync(username, ipAddress, "Too many attempts in 1 hour", _config.BlockDurationMinutes * 2);
                    return new SecurityCheckResult
                    {
                        IsAllowed = false,
                        IsBlocked = true,
                        BlockReason = "Too many attempts in the last hour",
                        BlockExpiresAt = now.AddMinutes(_config.BlockDurationMinutes * 2),
                        ErrorMessage = $"Account temporarily locked. Try again in {_config.BlockDurationMinutes * 2} minutes."
                    };
                }

                if (attemptsLastDay >= _config.MaxAttemptsPerDay)
                {
                    await BlockUserOrIpAsync(username, ipAddress, "Too many attempts in 1 day", _config.ExtendedBlockDurationHours * 60);
                    return new SecurityCheckResult
                    {
                        IsAllowed = false,
                        IsBlocked = true,
                        BlockReason = "Daily attempt limit exceeded",
                        BlockExpiresAt = now.AddHours(_config.ExtendedBlockDurationHours),
                        ErrorMessage = $"Account locked for {_config.ExtendedBlockDurationHours} hours due to excessive failed login attempts."
                    };
                }

                // Calculate remaining attempts for user feedback
                var remainingAttempts = Math.Min(
                    _config.MaxAttemptsPerMinute - attemptsLastMinute,
                    Math.Min(_config.MaxAttemptsPerHour - attemptsLastHour, _config.MaxAttemptsPerDay - attemptsLastDay)
                );

                return new SecurityCheckResult
                {
                    IsAllowed = true,
                    RemainingAttempts = remainingAttempts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking login attempt for username: {Username}, IP: {IpAddress}", username, ipAddress);
                // In case of error, allow the attempt but log it
                return new SecurityCheckResult { IsAllowed = true };
            }
        }

        public Task LogLoginAttemptAsync(LoginAttemptRequest request)
        {
            try
            {
                var loginAttempt = new LoginAttempt
                {
                    Username = request.Username,
                    IpAddress = request.IpAddress,
                    AttemptTime = DateTime.UtcNow,
                    IsSuccessful = request.IsSuccessful,
                    UserAgent = request.UserAgent,
                    FailureReason = request.FailureReason
                };

                _loginAttempts.Add(loginAttempt);

                // Clear cache for this user/IP combination to force refresh
                var cacheKey = $"{CACHE_PREFIX_ATTEMPTS}{request.Username}_{request.IpAddress}";
                _cache.Remove(cacheKey);

                _logger.LogInformation("Login attempt logged - Username: {Username}, IP: {IpAddress}, Success: {Success}",
                    request.Username, request.IpAddress, request.IsSuccessful);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging login attempt for username: {Username}, IP: {IpAddress}",
                    request.Username, request.IpAddress);
            }

            return Task.CompletedTask;
        }

        public Task BlockUserOrIpAsync(string? username, string? ipAddress, string reason, int durationMinutes)
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiresAt = now.AddMinutes(durationMinutes);

                var securityBlock = new SecurityBlock
                {
                    Username = username,
                    IpAddress = ipAddress,
                    BlockedAt = now,
                    ExpiresAt = expiresAt,
                    BlockType = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(ipAddress) ? "UserAndIP" :
                                !string.IsNullOrEmpty(username) ? "User" : "IP",
                    Reason = reason,
                    IsActive = true
                };

                _securityBlocks.Add(securityBlock);

                // Update cache
                var cacheKey = $"{CACHE_PREFIX_BLOCK}{username}_{ipAddress}";
                _cache.Set(cacheKey, securityBlock, TimeSpan.FromMinutes(durationMinutes + 5));

                _logger.LogWarning("Security block created - Username: {Username}, IP: {IpAddress}, Duration: {Duration} minutes, Reason: {Reason}",
                    username, ipAddress, durationMinutes, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating security block for username: {Username}, IP: {IpAddress}", username, ipAddress);
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsBlockedAsync(string? username, string? ipAddress)
        {
            try
            {
                var now = DateTime.UtcNow;

                var isBlocked = _securityBlocks.Any(b => b.IsActive &&
                                                        b.ExpiresAt > now &&
                                                        ((b.Username == username && !string.IsNullOrEmpty(username)) ||
                                                         (b.IpAddress == ipAddress && !string.IsNullOrEmpty(ipAddress))));

                return Task.FromResult(isBlocked);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if blocked for username: {Username}, IP: {IpAddress}", username, ipAddress);
                return Task.FromResult(false);
            }
        }

        public Task<SecurityCheckResult> GetBlockInfoAsync(string? username, string? ipAddress)
        {
            try
            {
                // Check cache first
                var cacheKey = $"{CACHE_PREFIX_BLOCK}{username}_{ipAddress}";
                if (_cache.TryGetValue(cacheKey, out SecurityBlock? cachedBlock) && cachedBlock != null)
                {
                    if (cachedBlock.ExpiresAt > DateTime.UtcNow && cachedBlock.IsActive)
                    {
                        return Task.FromResult(new SecurityCheckResult
                        {
                            IsAllowed = false,
                            IsBlocked = true,
                            BlockReason = cachedBlock.Reason,
                            BlockExpiresAt = cachedBlock.ExpiresAt,
                            ErrorMessage = $"Access blocked: {cachedBlock.Reason}. Block expires at {cachedBlock.ExpiresAt:yyyy-MM-dd HH:mm} UTC."
                        });
                    }
                }

                var now = DateTime.UtcNow;
                var activeBlock = _securityBlocks
                    .Where(b => b.IsActive &&
                                b.ExpiresAt > now &&
                                ((b.Username == username && !string.IsNullOrEmpty(username)) ||
                                 (b.IpAddress == ipAddress && !string.IsNullOrEmpty(ipAddress))))
                    .OrderByDescending(b => b.ExpiresAt)
                    .FirstOrDefault();

                if (activeBlock != null)
                {
                    // Cache the result
                    var cacheExpiry = activeBlock.ExpiresAt.Subtract(now).Add(TimeSpan.FromMinutes(5));
                    _cache.Set(cacheKey, activeBlock, cacheExpiry);

                    return Task.FromResult(new SecurityCheckResult
                    {
                        IsAllowed = false,
                        IsBlocked = true,
                        BlockReason = activeBlock.Reason,
                        BlockExpiresAt = activeBlock.ExpiresAt,
                        ErrorMessage = $"Access blocked: {activeBlock.Reason}. Block expires at {activeBlock.ExpiresAt:yyyy-MM-dd HH:mm} UTC."
                    });
                }

                return Task.FromResult(new SecurityCheckResult { IsAllowed = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting block info for username: {Username}, IP: {IpAddress}", username, ipAddress);
                return Task.FromResult(new SecurityCheckResult { IsAllowed = true });
            }
        }

        public Task<int> GetFailedAttemptsCountAsync(string? username, string? ipAddress, TimeSpan timeWindow)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);

                var count = _loginAttempts.Count(la => !la.IsSuccessful &&
                                                      la.AttemptTime >= cutoffTime &&
                                                      ((la.Username == username && !string.IsNullOrEmpty(username)) ||
                                                       (la.IpAddress == ipAddress && !string.IsNullOrEmpty(ipAddress))));

                return Task.FromResult(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting failed attempts count for username: {Username}, IP: {IpAddress}", username, ipAddress);
                return Task.FromResult(0);
            }
        }

        public Task CleanupExpiredRecordsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var cutoffTime = now.AddDays(-7); // Keep records for 7 days

                // Clean up expired blocks - mark as inactive
                var expiredBlocks = _securityBlocks.Where(b => b.ExpiresAt < now).ToList();
                foreach (var block in expiredBlocks)
                {
                    block.IsActive = false;
                }

                // Clean up old login attempts by recreating the collection without old items
                var validAttempts = _loginAttempts.Where(la => la.AttemptTime >= cutoffTime).ToList();

                // Clear and re-add valid attempts
                while (_loginAttempts.TryTake(out _)) { }
                foreach (var attempt in validAttempts)
                {
                    _loginAttempts.Add(attempt);
                }

                // Remove inactive blocks
                var activeBlocks = _securityBlocks.Where(b => b.IsActive).ToList();
                while (_securityBlocks.TryTake(out _)) { }
                foreach (var block in activeBlocks)
                {
                    _securityBlocks.Add(block);
                }

                _logger.LogInformation("Cleaned up expired security records - {ExpiredBlocks} blocks, kept {ValidAttempts} attempts",
                    expiredBlocks.Count, validAttempts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during security records cleanup");
            }

            return Task.CompletedTask;
        }

        public Task ResetAttemptsAsync(string? username, string? ipAddress)
        {
            try
            {
                // Deactivate existing blocks
                var blocksToDeactivate = _securityBlocks.Where(b => b.IsActive &&
                                                                    ((b.Username == username && !string.IsNullOrEmpty(username)) ||
                                                                     (b.IpAddress == ipAddress && !string.IsNullOrEmpty(ipAddress)))).ToList();

                foreach (var block in blocksToDeactivate)
                {
                    block.IsActive = false;
                }

                // Clear cache
                var cacheKey = $"{CACHE_PREFIX_BLOCK}{username}_{ipAddress}";
                _cache.Remove(cacheKey);

                var attemptsCacheKey = $"{CACHE_PREFIX_ATTEMPTS}{username}_{ipAddress}";
                _cache.Remove(attemptsCacheKey);

                _logger.LogInformation("Reset security attempts for username: {Username}, IP: {IpAddress}", username, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting attempts for username: {Username}, IP: {IpAddress}", username, ipAddress);
            }

            return Task.CompletedTask;
        }
    }
}
