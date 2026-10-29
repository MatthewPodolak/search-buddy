using SearchBuddy.Application.TextExtraction;
using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace SearchBuddy.Infrastructure.TextExtractors
{
    internal class PdfExtractor : ITextExtractor
    {
        public IReadOnlyCollection<string> Extensions { get; } = [".pdf"];

        public async Task<string> ExtractAsync(string path, CancellationToken ct)
        {
            var sb = new StringBuilder();

            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096);

            using (PdfDocument document = PdfDocument.Open(stream))
            {
                foreach (Page page in document.GetPages())
                {
                    ct.ThrowIfCancellationRequested();

                    sb.Append(page.Text);
                }
            }

            return sb.ToString();
        }
    }
}
