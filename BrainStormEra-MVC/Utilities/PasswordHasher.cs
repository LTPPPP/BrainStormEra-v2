using System.Security.Cryptography;
using System.Text;

namespace BrainStormEra_MVC.Utilities
{
    /// <summary>
    /// Utility class for password hashing and verification using MD5 algorithm.
    /// Note: MD5 is used here for compatibility reasons. For production applications,
    /// consider using more secure hashing algorithms like bcrypt, scrypt, or Argon2.
    /// </summary>
    public static class PasswordHasher
    {        /// <summary>
             /// Hashes a password using MD5 algorithm (compatible with SQL Server HASHBYTES)
             /// </summary>
             /// <param name="password">The plain text password to hash</param>
             /// <returns>The hashed password as a 32-character uppercase hexadecimal string</returns>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password cannot be null or empty", nameof(password));
            }

            using var md5 = MD5.Create();
            var hashedBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            // Convert to uppercase hex string
            var hexString = Convert.ToHexString(hashedBytes).ToUpperInvariant();
            return hexString.Length > 32 ? hexString.Substring(0, 32) : hexString;
        }

        /// <summary>
        /// Verifies a password against a stored hash
        /// </summary>
        /// <param name="password">The plain text password to verify</param>
        /// <param name="storedHash">The stored hash to compare against</param>
        /// <returns>True if the password matches the hash, false otherwise</returns>
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            string hashedPassword = HashPassword(password);
            return hashedPassword.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Generates a random salt (for future use if needed)
        /// </summary>
        /// <param name="size">The size of the salt in bytes (default: 32)</param>
        /// <returns>A random salt as base64 string</returns>
        public static string GenerateSalt(int size = 32)
        {
            var saltBytes = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// Hashes a password with a salt (for future enhanced security)
        /// </summary>
        /// <param name="password">The plain text password</param>
        /// <param name="salt">The salt to use</param>
        /// <returns>The hashed password with salt</returns>
        public static string HashPasswordWithSalt(string password, string salt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(salt))
            {
                throw new ArgumentException("Password and salt cannot be null or empty");
            }

            string saltedPassword = password + salt;
            return HashPassword(saltedPassword);
        }
    }
}