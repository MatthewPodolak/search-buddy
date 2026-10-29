using SearchBuddy.Application.Indexing;
using SearchBuddy.Application.TextExtraction;

namespace SearchBuddy.Application.Tests;

public class IndexingPipelineTests
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessFileAsync_UsesExtractorMatchingExtension()
    {
        var pdf = new FakeExtractor(".pdf");
        var txt = new FakeExtractor(".txt");
        var pipeline = new IndexingPipeline([pdf, txt]);

        await pipeline.ProcessFileAsync(@"C:\a.pdf", _ct);

        Assert.Equal([@"C:\a.pdf"], pdf.CalledWith);
        Assert.Empty(txt.CalledWith);
    }

    [Fact]
    public async Task ProcessFileAsync_UnknownExtension_CallsNoExtractor()
    {
        var txt = new FakeExtractor(".txt");
        var pipeline = new IndexingPipeline([txt]);

        await pipeline.ProcessFileAsync(@"C:\a.exe", _ct);

        Assert.Empty(txt.CalledWith);
    }
}

internal class FakeExtractor(params string[] extensions) : ITextExtractor
{
    public IReadOnlyCollection<string> Extensions { get; } = extensions;
    public List<string> CalledWith { get; } = [];

    public Task<string> ExtractAsync(string path, CancellationToken ct)
    {
        CalledWith.Add(path);
        return Task.FromResult("text");
    }
}