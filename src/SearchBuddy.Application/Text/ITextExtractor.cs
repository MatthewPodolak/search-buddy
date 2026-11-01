using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Text
{
    public interface ITextExtractor
    {
        IReadOnlyCollection<string> Extensions { get; }
        Task<string> ExtractAsync(string path, CancellationToken ct);
    }
}
