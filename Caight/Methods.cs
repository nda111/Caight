using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Resources;

namespace Caight
{
    public static class Methods
    {
        private const string HexDigits = "0123456789ABCDEF";
        private const string HashDigits = "0AD3EHI6MPQ7TU9Z";

        public static byte[] IntToByteArray(int i)
        {
            return new byte[]
            {
                (byte)(i),
                (byte)(i >> 8),
                (byte)(i >> 16),
                (byte)(i >> 24),
            };
        }

        public static int ByteArrayToInt(byte[] b)
        {
            return b[0] | (b[1] << 8) | (b[1] << 16) | (b[1] << 24);
        }

        internal static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] buffer = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "steak"));
            return Byte256ToString(buffer, HexDigits);
        }

        internal static string CreateVertificationHash(string email)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] buffer = sha.ComputeHash(Encoding.UTF8.GetBytes(email + DateTime.Now.Ticks));
            return Byte256ToString(buffer, HashDigits);
        }

        private static string Byte256ToString(byte[] bytes, string digits)
        {
            StringBuilder builder = new StringBuilder();

            foreach (var bt in bytes)
            {
                builder.Append(digits[bt >> 4]);
                builder.Append(digits[bt & 0xF]);
            }

            return builder.ToString();
        }
    }
}
