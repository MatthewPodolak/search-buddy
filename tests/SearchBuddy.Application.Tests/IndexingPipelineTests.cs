using NSubstitute;
using SearchBuddy.Application.Indexing;
using SearchBuddy.Application.LLM;
using SearchBuddy.Application.Storage;
using SearchBuddy.Application.Text;

namespace SearchBuddy.Application.Tests;

public class IndexingPipelineTests
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly IDocumentStore _docStore = Substitute.For<IDocumentStore>();
    private readonly ITextChunker _textChunker = Substitute.For<ITextChunker>();
    private readonly IEmbedder _embedder = Substitute.For<IEmbedder>();
    private readonly IHashGenerator _hashGen = Substitute.For<IHashGenerator>();

    public IndexingPipelineTests()
    {
        _hashGen.GetHashFromFile(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("hash-new");
        _docStore.GetContentHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        _textChunker.ChunkText(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(["chunk0"]);
        _embedder.EmbeddAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(new ReadOnlyMemory<float>([0.1f, 0.2f, 0.3f]));
    }

    [Fact]
    public async Task ProcessFileAsync_UsesExtractorMatchingExtension()
    {
        var pdf = new FakeExtractor(".pdf");
        var txt = new FakeExtractor(".txt");
        var pipeline = new IndexingPipeline([pdf, txt], _docStore, _textChunker, _embedder, _hashGen);

        await pipeline.ProcessFileAsync(@"C:\a.pdf", _ct);

        Assert.Equal([@"C:\a.pdf"], pdf.CalledWith);
        Assert.Empty(txt.CalledWith);
    }

    [Fact]
    public async Task ProcessFileAsync_UnknownExtension_CallsNoExtractor()
    {
        var txt = new FakeExtractor(".txt");
        var pipeline = new IndexingPipeline([txt], _docStore, _textChunker, _embedder, _hashGen);

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