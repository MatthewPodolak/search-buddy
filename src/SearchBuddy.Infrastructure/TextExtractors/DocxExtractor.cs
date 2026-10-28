using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SearchBuddy.Application.TextExtraction;

using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Infrastructure.TextExtractors
{
    internal class DocxExtractor : ITextExtractor
    {
        public IReadOnlyCollection<string> Extensions { get; } = [".docx"];

        public async Task<string> ExtractAsync(string path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096);

            using var document = WordprocessingDocument.Open(stream, isEditable: false);

            var body = document.MainDocumentPart?.Document?.Body;
            if (body is null) { return string.Empty; }

            var sb = new StringBuilder();

            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                ct.ThrowIfCancellationRequested();
                sb.AppendLine(paragraph.InnerText);
            }

            return sb.ToString();
        }
    }
}
