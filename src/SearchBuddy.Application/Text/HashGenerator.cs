using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace SearchBuddy.Application.Text
{
    public class HashGenerator : IHashGenerator
    {
        public HashGenerator()
        {

        }

        public string GetHashFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { return ""; }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        }

        public async Task<string> GetHashFromFile(string path, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(path)) { return ""; }

            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 81920, useAsync: true);
            return Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
        }
    }
}
