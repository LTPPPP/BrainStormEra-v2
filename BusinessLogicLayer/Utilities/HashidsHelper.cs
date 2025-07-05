using BusinessLogicLayer.Services.Interfaces;

namespace BusinessLogicLayer.Utilities
{
    /// <summary>
    /// Utility helper for working with Hashids
    /// </summary>
    public static class HashidsHelper
    {
        /// <summary>
        /// Safely encode an ID with error handling
        /// </summary>
        /// <param name="hashidsService">The Hashids service instance</param>
        /// <param name="id">ID to encode</param>
        /// <param name="fallback">Fallback value if encoding fails</param>
        /// <returns>Encoded hash or fallback value</returns>
        public static string SafeEncode(IHashidsService hashidsService, int id, string fallback = "")
        {
            try
            {
                return hashidsService?.Encode(id) ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// Safely decode a hash with error handling
        /// </summary>
        /// <param name="hashidsService">The Hashids service instance</param>
        /// <param name="hash">Hash to decode</param>
        /// <param name="fallback">Fallback value if decoding fails</param>
        /// <returns>Decoded ID or fallback value</returns>
        public static int SafeDecode(IHashidsService hashidsService, string hash, int fallback = 0)
        {
            try
            {
                return hashidsService?.DecodeSingle(hash) ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// Safely encode a long ID with error handling
        /// </summary>
        /// <param name="hashidsService">The Hashids service instance</param>
        /// <param name="id">Long ID to encode</param>
        /// <param name="fallback">Fallback value if encoding fails</param>
        /// <returns>Encoded hash or fallback value</returns>
        public static string SafeEncodeLong(IHashidsService hashidsService, long id, string fallback = "")
        {
            try
            {
                return hashidsService?.EncodeLong(id) ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// Safely decode a hash to long with error handling
        /// </summary>
        /// <param name="hashidsService">The Hashids service instance</param>
        /// <param name="hash">Hash to decode</param>
        /// <param name="fallback">Fallback value if decoding fails</param>
        /// <returns>Decoded long ID or fallback value</returns>
        public static long SafeDecodeLong(IHashidsService hashidsService, string hash, long fallback = 0)
        {
            try
            {
                return hashidsService?.DecodeLong(hash) ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// Create URL-friendly hash from ID
        /// </summary>
        /// <param name="hashidsService">The Hashids service instance</param>
        /// <param name="id">ID to encode</param>
        /// <param name="prefix">Optional prefix for the URL</param>
        /// <returns>URL-friendly hash</returns>
        public static string CreateUrlHash(IHashidsService hashidsService, int id, string prefix = "")
        {
            var hash = SafeEncode(hashidsService, id);
            return string.IsNullOrEmpty(prefix) ? hash : $"{prefix}-{hash}";
        }

        /// <summary>
        /// Extract ID from URL-friendly hash
        /// </summary>
        /// <param name="hashidsService">The Hashids service instance</param>
        /// <param name="urlHash">URL hash to decode</param>
        /// <param name="prefix">Optional prefix to remove</param>
        /// <returns>Decoded ID</returns>
        public static int ExtractIdFromUrlHash(IHashidsService hashidsService, string urlHash, string prefix = "")
        {
            if (string.IsNullOrEmpty(urlHash))
                return 0;

            var hash = urlHash;
            if (!string.IsNullOrEmpty(prefix) && urlHash.StartsWith($"{prefix}-"))
            {
                hash = urlHash.Substring(prefix.Length + 1);
            }

            return SafeDecode(hashidsService, hash);
        }

        /// <summary>
        /// Validate and decode hash in one operation
        /// </summary>
        /// <param name="hashidsService">The Hashids service instance</param>
        /// <param name="hash">Hash to validate and decode</param>
        /// <param name="decodedId">Output decoded ID</param>
        /// <returns>True if valid and decoded successfully</returns>
        public static bool TryDecode(IHashidsService hashidsService, string hash, out int decodedId)
        {
            decodedId = 0;
            if (hashidsService?.IsValidHash(hash) == true)
            {
                decodedId = hashidsService.DecodeSingle(hash);
                return decodedId > 0;
            }
            return false;
        }
    }
}
