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
        private readonly byte[] _derivedKey;

        public UrlHashServiceImproved(IConfiguration configuration)
        {
            _secretKey = configuration["UrlHashSettings:SecretKey"] ?? "BrainStormEra-DefaultSecretKey-2024";
            _salt = configuration["UrlHashSettings:Salt"] ?? "BrainStorm-Salt-2024";

            // Derive a proper 32-byte key using PBKDF2
            _derivedKey = DeriveKey(_secretKey, _salt);
        }

        /// <summary>
        /// Derive a secure key using PBKDF2
        /// </summary>
        private byte[] DeriveKey(string password, string salt)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32); // AES-256 key length
            }
        }

        /// <summary>
        /// Create encrypted hash from real ID using AES encryption
        /// </summary>
        /// <param name="realId">Real ID to encrypt</param>
        /// <returns>Encrypted string for use in URL</returns>
        public string EncodeId(string realId)
        {
            if (string.IsNullOrEmpty(realId))
                return string.Empty;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _derivedKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.GenerateIV(); // Generate random IV for each encryption

                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(realId);
                        byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                        // Combine IV and cipher for storage
                        byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
                        Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
                        Array.Copy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

                        // Convert to URL-safe string
                        string base64 = Convert.ToBase64String(result)
                            .Replace("+", "-")
                            .Replace("/", "_")
                            .Replace("=", "");

                        // Add prefix for easy recognition
                        return $"h{base64}";
                    }
                }
            }
            catch (Exception)
            {
                // Log the error in real applications
                // For now, fallback to simple hash
                return CreateSimpleHash(realId);
            }
        }

        /// <summary>
        /// Decode encrypted hash back to real ID
        /// </summary>
        /// <param name="hash">Encrypted string from URL</param>
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

                byte[] combinedBytes = Convert.FromBase64String(base64);

                // Extract IV and cipher
                byte[] iv = new byte[16]; // AES block size
                byte[] cipherBytes = new byte[combinedBytes.Length - 16];

                Array.Copy(combinedBytes, 0, iv, 0, 16);
                Array.Copy(combinedBytes, 16, cipherBytes, 0, cipherBytes.Length);

                // Use AES to decrypt
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _derivedKey;
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
            catch (Exception)
            {
                // Log the error in real applications
                // Cannot decode
                return string.Empty;
            }
        }

        /// <summary>
        /// Create simple hash as fallback (only used in case of encryption errors)
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
                return $"s{shortHash}"; // 's' prefix for simple hash (not reversible)
            }
        }

        /// <summary>
        /// Encrypt multiple IDs at once
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
        /// Decrypt multiple hashes at once
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
        /// No cache needed in this version as encryption/decryption is stateless
        /// </summary>
        public void ClearCache()
        {
            // No cache to clear in AES-based implementation
        }

        /// <summary>
        /// Check if string is an encrypted hash or not
        /// </summary>
        public bool IsHash(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Encrypted hash starts with 'h' and has minimum length
            // Simple hash (fallback) starts with 's'
            return (value.StartsWith("h") || value.StartsWith("s")) && value.Length > 5;
        }

        /// <summary>
        /// Get real ID from encrypted hash or return itself if already an ID
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
        /// Get encrypted hash from real ID or return itself if already a hash
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

        /// <summary>
        /// Validate if a hash can be properly decrypted (useful for testing)
        /// </summary>
        public bool ValidateHash(string hash)
        {
            if (!IsHash(hash))
                return false;

            try
            {
                var decoded = DecodeId(hash);
                return !string.IsNullOrEmpty(decoded);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get encryption method used for a hash
        /// </summary>
        public string GetEncryptionMethod(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return "None";

            if (hash.StartsWith("h"))
                return "AES-256-CBC";

            if (hash.StartsWith("s"))
                return "SHA256 (Fallback)";

            return "Plain ID";
        }
    }
}