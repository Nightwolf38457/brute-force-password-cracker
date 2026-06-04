using System;
using System.Security.Cryptography;
using System.Text;

namespace BruteForce
{
    /// <summary>
    /// Responsible for generating a random password and hashing it with SHA256 + static salt.
    /// </summary>
    public class PasswordManager
    {
        // Constant static salt used for all hashing operations
        public const string SALT = "VU_STATIC_SALT_2024";

        private string _plainPassword = string.Empty;
        private string _hashedPassword = string.Empty;

        private static readonly Random _rng = new Random();

        private const string CHARSET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>
        /// Generates a random password with length randomly chosen in [4, 6).
        /// </summary>
        public string GeneratePassword()
        {
            int length = _rng.Next(4, 6); // 4 or 5 characters
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
                sb.Append(CHARSET[_rng.Next(CHARSET.Length)]);

            _plainPassword = sb.ToString();
            _hashedPassword = HashPassword(_plainPassword);
            return _plainPassword;
        }

        /// <summary>
        /// Hashes a plain-text password using SHA256 with the static salt.
        /// </summary>
        public string HashPassword(string plainText)
        {
            string saltedInput = SALT + plainText;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedInput));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public string GetPlainPassword() => _plainPassword;
        public string GetHashedPassword() => _hashedPassword;
    }
}