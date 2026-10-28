using System.Text;
using DocumentFormat.OpenXml.Packaging;
using SearchBuddy.Application.TextExtraction;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace SearchBuddy.Infrastructure.TextExtractors;

internal sealed class PptxExtractor : ITextExtractor
{
    public IReadOnlyCollection<string> Extensions { get; } = [".pptx"];

    public async Task<string> ExtractAsync(string path, CancellationToken ct)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096);

        using var document = PresentationDocument.Open(stream, isEditable: false);
        var presentationPart = document.PresentationPart!;

        var sb = new StringBuilder();

        foreach (var slideId in presentationPart.Presentation.SlideIdList?.Elements<P.SlideId>() ?? [])
        {
            ct.ThrowIfCancellationRequested();

            var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!.Value!);

            foreach (var paragraph in slidePart.Slide.Descendants<A.Paragraph>())
            {
                sb.AppendLine(paragraph.InnerText);
            }
        }

        return sb.ToString();
    }
}