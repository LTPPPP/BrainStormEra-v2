using System.Security.Cryptography;
using System.Text;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BusinessLogicLayer.Services
{
    public class UrlHashService : IUrlHashService
    {
        private readonly string _secretKey;
        private readonly Dictionary<string, string> _hashToIdCache;
        private readonly Dictionary<string, string> _idToHashCache;

        public UrlHashService(IConfiguration configuration)
        {
            _secretKey = configuration["UrlHashSettings:SecretKey"] ?? "BrainStormEra-DefaultSecretKey-2024";
            _hashToIdCache = new Dictionary<string, string>();
            _idToHashCache = new Dictionary<string, string>();
        }

        /// <summary>
        /// Create hash from real ID
        /// </summary>
        /// <param name="realId">Real ID to hash</param>
        /// <returns>Hash string for use in URL</returns>
        public string EncodeId(string realId)
        {
            if (string.IsNullOrEmpty(realId))
                return string.Empty;

            // Check cache first
            if (_idToHashCache.ContainsKey(realId))
                return _idToHashCache[realId];

            // Create hash from ID and secret key
            string input = $"{realId}_{_secretKey}";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                string fullHash = Convert.ToBase64String(hashBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

                // Take first 12 characters for short hash
                string shortHash = fullHash.Substring(0, 12);

                // Add prefix for easy recognition and avoid duplicates
                string finalHash = $"h{shortHash}";

                // Save to cache
                _idToHashCache[realId] = finalHash;
                _hashToIdCache[finalHash] = realId;

                return finalHash;
            }
        }

        /// <summary>
        /// Decode hash back to real ID
        /// </summary>
        /// <param name="hash">Hash string from URL</param>
        /// <returns>Real ID</returns>
        public string DecodeId(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return string.Empty;

            // Check cache first
            if (_hashToIdCache.ContainsKey(hash))
                return _hashToIdCache[hash];

            // If not in cache, might be hash created from previous request
            // Need to rebuild cache from database or return empty
            return string.Empty;
        }

        /// <summary>
        /// Create hash for multiple IDs at once
        /// </summary>
        /// <param name="realIds">List of real IDs</param>
        /// <returns>Dictionary mapping real ID to hash</returns>
        public Dictionary<string, string> EncodeIds(IEnumerable<string> realIds)
        {
            var result = new Dictionary<string, string>();

            foreach (var id in realIds.Where(id => !string.IsNullOrEmpty(id)))
            {
                result[id] = EncodeId(id);
            }

            return result;
        }

        /// <summary>
        /// Decode multiple hashes at once
        /// </summary>
        /// <param name="hashes">List of hashes</param>
        /// <returns>Dictionary mapping hash to real ID</returns>
        public Dictionary<string, string> DecodeIds(IEnumerable<string> hashes)
        {
            var result = new Dictionary<string, string>();

            foreach (var hash in hashes.Where(h => !string.IsNullOrEmpty(h)))
            {
                var realId = DecodeId(hash);
                if (!string.IsNullOrEmpty(realId))
                {
                    result[hash] = realId;
                }
            }

            return result;
        }

        /// <summary>
        /// Clear cache (use when refresh is needed)
        /// </summary>
        public void ClearCache()
        {
            _hashToIdCache.Clear();
            _idToHashCache.Clear();
        }

        /// <summary>
        /// Check if string is a hash or not
        /// </summary>
        /// <param name="value">String to check</param>
        /// <returns>True if it's a hash, False if it's a real ID</returns>
        public bool IsHash(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Our hash starts with 'h' and has length of 13 characters
            return value.StartsWith("h") && value.Length == 13;
        }

        /// <summary>
        /// Get real ID from hash or return itself if already an ID
        /// </summary>
        /// <param name="hashOrId">Hash or ID</param>
        /// <returns>Real ID</returns>
        public string GetRealId(string hashOrId)
        {
            if (string.IsNullOrEmpty(hashOrId))
                return string.Empty;

            if (IsHash(hashOrId))
            {
                return DecodeId(hashOrId);
            }

            return hashOrId;
        }

        /// <summary>
        /// Get hash from real ID or return itself if already a hash
        /// </summary>
        /// <param name="idOrHash">Real ID or hash</param>
        /// <returns>Hash</returns>
        public string GetHash(string idOrHash)
        {
            if (string.IsNullOrEmpty(idOrHash))
                return string.Empty;

            if (IsHash(idOrHash))
            {
                return idOrHash;
            }

            return EncodeId(idOrHash);
        }
    }
}