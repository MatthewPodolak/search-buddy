using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Text
{
    public interface IHashGenerator
    {
        string GetHashFromText(string text);
        Task<string> GetHashFromFile(string path, CancellationToken ct);
    }
}
