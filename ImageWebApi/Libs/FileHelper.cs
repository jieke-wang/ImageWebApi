using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImageWebApi.Libs
{
    public class FileHelper
    {
        public static string ToValidFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidReStr = string.Format(@"[{0}]+", invalidChars);
            string str = Regex.Replace(name, invalidReStr, "-");
            str = Regex.Replace(str, @"-+", "-").Trim().ToLower();
            return str;
        }

        public static string ToValidFilePath(string path)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidPathChars()));
            string invalidReStr = string.Format(@"[{0}]+", invalidChars);
            string str = Regex.Replace(path, invalidReStr, "-");
            str = Regex.Replace(str, @"-+", "-").Trim().ToLower(); ;
            return str;
        }

        public static string AddRandomToFilename(string filename)
        {
            Random random = new Random(Environment.TickCount);
            //Span<byte> randomDate = stackalloc byte[2];
            byte[] randomDate = new byte[2];
            random.NextBytes(randomDate);

            return $"{Path.GetFileNameWithoutExtension(filename)}-{BitConverter.ToString(randomDate).Replace("-", string.Empty).ToLower()}{Path.GetExtension(filename)}";
        }

        public static class Algorithms
        {
            public static readonly HashAlgorithm MD5 = new MD5CryptoServiceProvider();
            public static readonly HashAlgorithm SHA1 = new SHA1Managed();
            public static readonly HashAlgorithm SHA256 = new SHA256Managed();
            public static readonly HashAlgorithm SHA384 = new SHA384Managed();
            public static readonly HashAlgorithm SHA512 = new SHA512Managed();
        }

        public static async Task<string> GetHashFromFileAsync(string fileName, HashAlgorithm algorithm)
        {
            using (var stream = new BufferedStream(File.OpenRead(fileName), 100000))
            {
                return BitConverter.ToString(await algorithm.ComputeHashAsync(stream)).Replace("-", string.Empty);
            }
        }

        public static async Task<string> GetHashFromStreamAsync(Stream stream, HashAlgorithm algorithm)
        {
            return BitConverter.ToString(await algorithm.ComputeHashAsync(stream)).Replace("-", string.Empty);
        }

        public static string GetHashFromByteArray(byte[] buffer, HashAlgorithm algorithm)
        {
            return BitConverter.ToString(algorithm.ComputeHash(buffer)).Replace("-", string.Empty);
        }
    }
}
