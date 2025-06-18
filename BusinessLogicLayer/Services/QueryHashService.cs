using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Service for performing queries with encrypted/hashed IDs
    /// Provides smart conversion between real IDs and encrypted hashes for database operations
    /// </summary>
    public class QueryHashService
    {
        private readonly IUrlHashService _urlHashService;
        private readonly ILogger<QueryHashService> _logger;

        public QueryHashService(IUrlHashService urlHashService, ILogger<QueryHashService> logger)
        {
            _urlHashService = urlHashService;
            _logger = logger;
        }

        /// <summary>
        /// Prepare a single ID for database query (converts hash to real ID if needed)
        /// </summary>
        /// <param name="idOrHash">ID or hash from URL/request</param>
        /// <returns>Real ID for database query</returns>
        public string PrepareIdForQuery(string idOrHash)
        {
            if (string.IsNullOrEmpty(idOrHash))
                return string.Empty;

            try
            {
                return _urlHashService.GetRealId(idOrHash);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to prepare ID for query: {ex.Message}");
                return idOrHash; // Return original if conversion fails
            }
        }

        /// <summary>
        /// Prepare multiple IDs for database query
        /// </summary>
        /// <param name="idsOrHashes">Collection of IDs or hashes</param>
        /// <returns>Collection of real IDs for database queries</returns>
        public IEnumerable<string> PrepareIdsForQuery(IEnumerable<string> idsOrHashes)
        {
            if (idsOrHashes == null)
                return Enumerable.Empty<string>();

            var realIds = new List<string>();

            foreach (var idOrHash in idsOrHashes.Where(x => !string.IsNullOrEmpty(x)))
            {
                try
                {
                    var realId = _urlHashService.GetRealId(idOrHash);
                    if (!string.IsNullOrEmpty(realId))
                        realIds.Add(realId);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Failed to prepare ID '{idOrHash}' for query: {ex.Message}");
                    realIds.Add(idOrHash); // Add original if conversion fails
                }
            }

            return realIds;
        }

        /// <summary>
        /// Prepare results for display (converts real IDs to hashes for URLs)
        /// </summary>
        /// <param name="realId">Real ID from database</param>
        /// <returns>Hash for URL display</returns>
        public string PrepareIdForDisplay(string realId)
        {
            if (string.IsNullOrEmpty(realId))
                return string.Empty;

            try
            {
                return _urlHashService.GetHash(realId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to prepare ID for display: {ex.Message}");
                return realId; // Return original if conversion fails
            }
        }

        /// <summary>
        /// Prepare multiple results for display
        /// </summary>
        /// <param name="realIds">Collection of real IDs from database</param>
        /// <returns>Dictionary mapping real ID to hash for display</returns>
        public Dictionary<string, string> PrepareIdsForDisplay(IEnumerable<string> realIds)
        {
            if (realIds == null)
                return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();

            foreach (var realId in realIds.Where(x => !string.IsNullOrEmpty(x)))
            {
                try
                {
                    var hash = _urlHashService.GetHash(realId);
                    result[realId] = hash;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Failed to prepare ID '{realId}' for display: {ex.Message}");
                    result[realId] = realId; // Use original if conversion fails
                }
            }

            return result;
        }

        /// <summary>
        /// Smart query method that handles both hashed and real IDs
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="idOrHash">ID or hash parameter</param>
        /// <param name="queryFunc">Function that performs the actual query with real ID</param>
        /// <returns>Query result</returns>
        public async Task<T?> ExecuteQueryWithId<T>(string idOrHash, Func<string, Task<T?>> queryFunc)
        {
            if (string.IsNullOrEmpty(idOrHash) || queryFunc == null)
                return default(T);

            try
            {
                var realId = PrepareIdForQuery(idOrHash);
                if (string.IsNullOrEmpty(realId))
                    return default(T);

                return await queryFunc(realId);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error executing query with ID '{idOrHash}': {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Smart query method for multiple IDs
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="idsOrHashes">Collection of IDs or hashes</param>
        /// <param name="queryFunc">Function that performs the actual query with real IDs</param>
        /// <returns>Query result</returns>
        public async Task<T?> ExecuteQueryWithIds<T>(IEnumerable<string> idsOrHashes, Func<IEnumerable<string>, Task<T?>> queryFunc)
        {
            if (idsOrHashes == null || !idsOrHashes.Any() || queryFunc == null)
                return default(T);

            try
            {
                var realIds = PrepareIdsForQuery(idsOrHashes);
                if (!realIds.Any())
                    return default(T);

                return await queryFunc(realIds);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error executing query with multiple IDs: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Validate if an ID/hash is valid for querying
        /// </summary>
        /// <param name="idOrHash">ID or hash to validate</param>
        /// <returns>Validation result with details</returns>
        public (bool IsValid, string RealId, string ErrorMessage) ValidateIdForQuery(string idOrHash)
        {
            if (string.IsNullOrEmpty(idOrHash))
                return (false, string.Empty, "ID is null or empty");

            try
            {
                if (_urlHashService.IsHash(idOrHash))
                {
                    if (!_urlHashService.ValidateHash(idOrHash))
                        return (false, string.Empty, "Invalid hash format or cannot be decrypted");

                    var realId = _urlHashService.DecodeId(idOrHash);
                    if (string.IsNullOrEmpty(realId))
                        return (false, string.Empty, "Hash cannot be decoded to real ID");

                    return (true, realId, string.Empty);
                }
                else
                {
                    // Assume it's a real ID if it's not a hash
                    return (true, idOrHash, string.Empty);
                }
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get information about an ID/hash for debugging
        /// </summary>
        /// <param name="idOrHash">ID or hash to analyze</param>
        /// <returns>Analysis information</returns>
        public IdAnalysisResult AnalyzeId(string idOrHash)
        {
            var result = new IdAnalysisResult
            {
                OriginalValue = idOrHash,
                IsEmpty = string.IsNullOrEmpty(idOrHash)
            };

            if (result.IsEmpty)
                return result;

            try
            {
                result.IsHash = _urlHashService.IsHash(idOrHash);
                result.EncryptionMethod = _urlHashService.GetEncryptionMethod(idOrHash);

                if (result.IsHash)
                {
                    result.CanBeDecoded = _urlHashService.ValidateHash(idOrHash);
                    if (result.CanBeDecoded)
                    {
                        result.RealId = _urlHashService.DecodeId(idOrHash);
                    }
                }
                else
                {
                    result.RealId = idOrHash;
                    result.CanBeDecoded = true;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }

    /// <summary>
    /// Result of ID analysis
    /// </summary>
    public class IdAnalysisResult
    {
        public string OriginalValue { get; set; } = string.Empty;
        public bool IsEmpty { get; set; }
        public bool IsHash { get; set; }
        public bool CanBeDecoded { get; set; }
        public string RealId { get; set; } = string.Empty;
        public string EncryptionMethod { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public bool IsValid => !IsEmpty && CanBeDecoded && !string.IsNullOrEmpty(RealId);
    }
}