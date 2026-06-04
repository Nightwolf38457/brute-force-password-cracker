using System.Security.Cryptography;
using System.Text;

namespace BruteForce
{
    /// <summary>
    /// Independently validates a candidate password against a target SHA256 hash.
    /// Completely separate from BruteForceGenerator – only responsible for hash comparison.
    /// </summary>
    public class PasswordValidator
    {
        private const string SALT = PasswordManager.SALT;

        /// <summary>
        /// Returns true if the candidate, when hashed, matches the target hash.
        /// </summary>
        public bool Validate(string candidate, string targetHash)
        {
            string candidateHash = HashCandidate(candidate);
            return candidateHash == targetHash;
        }

        /// <summary>
        /// Hashes a candidate string using SHA256 + static salt.
        /// </summary>
        public string HashCandidate(string candidate)
        {
            return ComputeSHA256(SALT + candidate);
        }

        /// <summary>
        /// Core SHA256 computation. Returns lowercase hex string.
        /// </summary>
        public string ComputeSHA256(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}