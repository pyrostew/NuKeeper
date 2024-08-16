using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace NuKeeper.Abstractions.Formats
{
    public static class Hasher
    {
#pragma warning disable CA5351
        private static readonly MD5 md5 = MD5.Create();

        public static string Hash(string input)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            return BytesToString(hash);
        }

        private static string BytesToString(byte[] bytes)
        {
            StringBuilder result = new();

            foreach (byte b in bytes)
            {
                _ = result.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }
    }
}
