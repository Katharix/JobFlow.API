using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Utilities
{
    public static class TokenGenerator
    {
        /// <summary>
        /// Generates a cryptographically secure, URL-safe token string.
        /// </summary>
        /// <param name="byteLength">The number of random bytes to use (default 32 = 256-bit)</param>
        public static string GenerateInviteToken(int byteLength = 32)
        {
            // Generate secure random bytes
            var bytes = RandomNumberGenerator.GetBytes(byteLength);

            // Convert to Base64 and make it URL safe
            var token = Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');

            return token;
        }
    }
}
