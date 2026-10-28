using SearchBuddy.Application.TextExtraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Infrastructure.TextExtractors
{
    internal class PlainTextExtractor : ITextExtractor
    {
        public IReadOnlyCollection<string> Extensions { get; } = [".txt", ".cs", ".md"];

        public async Task<string> ExtractAsync(string path, CancellationToken ct)
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 
                bufferSize: 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);

            using var reader = new StreamReader(stream);

            return await reader.ReadToEndAsync(ct);
        }
    }
}
