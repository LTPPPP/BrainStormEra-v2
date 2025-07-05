using BusinessLogicLayer.Constants;
using BusinessLogicLayer.Services.Interfaces;
using HashidsNet;
using Microsoft.Extensions.Configuration;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Service for encoding and decoding IDs using Hashids
    /// </summary>
    public class HashidsService : IHashidsService
    {
        private readonly IHashids _hashids;
        private readonly IConfiguration _configuration;

        public HashidsService(IConfiguration configuration)
        {
            _configuration = configuration;

            // Get configuration values or use defaults
            var salt = _configuration[HashidsConstants.ConfigKeys.Salt] ?? HashidsConstants.Defaults.Salt;
            var minHashLength = int.Parse(_configuration[HashidsConstants.ConfigKeys.MinLength] ?? HashidsConstants.Defaults.MinLength.ToString());
            var alphabet = _configuration[HashidsConstants.ConfigKeys.Alphabet] ?? HashidsConstants.Defaults.Alphabet;

            _hashids = new Hashids(salt, minHashLength, alphabet);
        }

        /// <summary>
        /// Encode an integer ID to a hash string
        /// </summary>
        /// <param name="id">The integer ID to encode</param>
        /// <returns>Encoded hash string</returns>
        public string Encode(int id)
        {
            if (id <= 0)
                return string.Empty;

            return _hashids.Encode(id);
        }

        /// <summary>
        /// Encode multiple integer IDs to a hash string
        /// </summary>
        /// <param name="ids">Array of integer IDs to encode</param>
        /// <returns>Encoded hash string</returns>
        public string Encode(params int[] ids)
        {
            if (ids == null || ids.Length == 0 || ids.Any(id => id <= 0))
                return string.Empty;

            return _hashids.Encode(ids);
        }

        /// <summary>
        /// Decode a hash string to an array of integer IDs
        /// </summary>
        /// <param name="hash">The hash string to decode</param>
        /// <returns>Array of decoded integer IDs</returns>
        public int[] Decode(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return Array.Empty<int>();

            try
            {
                return _hashids.Decode(hash);
            }
            catch
            {
                return Array.Empty<int>();
            }
        }

        /// <summary>
        /// Decode a hash string to a single integer ID
        /// </summary>
        /// <param name="hash">The hash string to decode</param>
        /// <returns>Decoded integer ID, or 0 if invalid</returns>
        public int DecodeSingle(string hash)
        {
            var decoded = Decode(hash);
            return decoded.Length > 0 ? decoded[0] : 0;
        }

        /// <summary>
        /// Check if a hash string is valid
        /// </summary>
        /// <param name="hash">The hash string to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValidHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            var decoded = Decode(hash);
            return decoded.Length > 0;
        }

        /// <summary>
        /// Encode a long ID to a hash string
        /// </summary>
        /// <param name="id">The long ID to encode</param>
        /// <returns>Encoded hash string</returns>
        public string EncodeLong(long id)
        {
            if (id <= 0)
                return string.Empty;

            return _hashids.EncodeLong(id);
        }

        /// <summary>
        /// Decode a hash string to a single long ID
        /// </summary>
        /// <param name="hash">The hash string to decode</param>
        /// <returns>Decoded long ID, or 0 if invalid</returns>
        public long DecodeLong(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return 0;

            try
            {
                var decoded = _hashids.DecodeLong(hash);
                return decoded.Length > 0 ? decoded[0] : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
