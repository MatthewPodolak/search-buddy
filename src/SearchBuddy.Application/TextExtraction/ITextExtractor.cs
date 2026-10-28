using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.TextExtraction
{
    public interface ITextExtractor
    {
        IReadOnlyCollection<string> Extensions { get; }
        Task<string> ExtractAsync(string path, CancellationToken ct);
    }
}
