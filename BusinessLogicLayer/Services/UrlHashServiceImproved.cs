using System.Security.Cryptography;
using System.Text;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BusinessLogicLayer.Services
{
    public class UrlHashServiceImproved : IUrlHashService
    {
        private readonly string _secretKey;
        private readonly string _salt;

        public UrlHashServiceImproved(IConfiguration configuration)
        {
            _secretKey = configuration["UrlHashSettings:SecretKey"] ?? "BrainStormEra-DefaultSecretKey-2024";
            _salt = configuration["UrlHashSettings:Salt"] ?? "BrainStorm-Salt-2024";
        }

        /// <summary>
        /// Create hash from real ID using reversible algorithm
        /// </summary>
        /// <param name="realId">Real ID to hash</param>
        /// <returns>Hash string for use in URL</returns>
        public string EncodeId(string realId)
        {
            if (string.IsNullOrEmpty(realId))
                return string.Empty;

            try
            {
                // Create key from secret key and salt
                byte[] key = Encoding.UTF8.GetBytes(_secretKey + _salt);
                byte[] iv = new byte[16]; // Fixed IV to enable decryption

                // Use AES to encrypt ID
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key.Take(32).ToArray(); // AES-256 needs 32 bytes
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(realId);
                        byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                        // Convert to URL-safe string
                        string base64 = Convert.ToBase64String(cipherBytes)
                            .Replace("+", "-")
                            .Replace("/", "_")
                            .Replace("=", "");

                        // Add prefix for easy recognition
                        return $"h{base64}";
                    }
                }
            }
            catch
            {
                // Fallback to simple hash if error occurs
                return CreateSimpleHash(realId);
            }
        }

        /// <summary>
        /// Decode hash back to real ID
        /// </summary>
        /// <param name="hash">Hash string from URL</param>
        /// <returns>Real ID</returns>
        public string DecodeId(string hash)
        {
            if (string.IsNullOrEmpty(hash) || !hash.StartsWith("h"))
                return string.Empty;

            try
            {
                // Remove prefix 'h'
                string base64 = hash.Substring(1);

                // Restore padding for base64
                while (base64.Length % 4 != 0)
                {
                    base64 += "=";
                }

                // Restore original base64 characters
                base64 = base64.Replace("-", "+").Replace("_", "/");

                byte[] cipherBytes = Convert.FromBase64String(base64);

                // Create key from secret key and salt
                byte[] key = Encoding.UTF8.GetBytes(_secretKey + _salt);
                byte[] iv = new byte[16]; // Fixed IV

                // Use AES to decrypt
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key.Take(32).ToArray(); // AES-256 needs 32 bytes
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                        return Encoding.UTF8.GetString(plainBytes);
                    }
                }
            }
            catch
            {
                // Cannot decode
                return string.Empty;
            }
        }

        /// <summary>
        /// Create simple hash as fallback
        /// </summary>
        private string CreateSimpleHash(string realId)
        {
            string input = $"{realId}_{_secretKey}_{_salt}";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                string fullHash = Convert.ToBase64String(hashBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

                // Take first 12 characters
                string shortHash = fullHash.Substring(0, 12);
                return $"s{shortHash}"; // 's' prefix for simple hash
            }
        }

        /// <summary>
        /// Create hash for multiple IDs at once
        /// </summary>
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
        /// No cache needed in this version
        /// </summary>
        public void ClearCache()
        {
            // No cache to clear
        }

        /// <summary>
        /// Check if string is a hash or not
        /// </summary>
        public bool IsHash(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Hash starts with 'h' or 's' and has minimum length
            return (value.StartsWith("h") || value.StartsWith("s")) && value.Length > 5;
        }

        /// <summary>
        /// Get real ID from hash or return itself if already an ID
        /// </summary>
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