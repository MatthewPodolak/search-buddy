using Dapper;
using Microsoft.Extensions.Options;
using SearchBuddy.Domain.Entities;
using SearchBuddy.Infrastructure.Persistance;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SearchBuddy.Infrastructure.Tests.Turso
{
    public class TursoDocumentStoreTests : IAsyncLifetime
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "search-buddy-tests", Guid.NewGuid().ToString("N"));
        private readonly CancellationToken ct = TestContext.Current.CancellationToken;

        private TursoConnectionFactory _factory;
        private TursoDocumentStore _store;

        public TursoDocumentStoreTests()
        {

        }

        public async ValueTask InitializeAsync()
        {
            var settings = new DatabaseSettings { Path = Path.Combine(_path, "test.db") };
            _factory = new TursoConnectionFactory(Options.Create(settings));

            await using var conn = await _factory.OpenAsync(ct);

            await conn.ExecuteScalarAsync<string>(new CommandDefinition("PRAGMA journal_mode = 'mvcc'", cancellationToken: ct));

            foreach (var statement in TursoSchema.Statements)
            {
                await conn.ExecuteAsync(new CommandDefinition(statement, cancellationToken: ct));
            }

            _store = new TursoDocumentStore(_factory);
        }
        public async ValueTask DisposeAsync()
        {
            Directory.Delete(_path, recursive: true);
        }

        [Fact]
        public async Task ReplaceAsync_ParallelWritesToDifferentDocuments_NoWriteIsLost()
        {
            const int writers = 50;
            const int chunksPerDoc = 3;

            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var tasks = new Task<Exception?>[writers];

            for (int i = 0; i < writers; i++)
            {
                tasks[i] = ReplaceAfterStart(start.Task, Doc($"doc-{i}.md", $"h{i}", Chunks($"d{i}", chunksPerDoc)));
            }

            start.SetResult();
            var errors = await Task.WhenAll(tasks);

            foreach (var error in errors)
            {
                Assert.Null(error);
            }

            Assert.Equal(writers, await Count("Documents"));
            Assert.Equal(writers * chunksPerDoc, await Count("Chunks"));

            for (int i = 0; i < writers; i++)
            {
                Assert.Equal($"h{i}", await _store.GetContentHashAsync($"doc-{i}.md", ct));
                Assert.Equal(Texts($"d{i}", chunksPerDoc), await ChunkTexts($"doc-{i}.md"));
            }
        }

        [Fact]
        public async Task ReplaceAsync_ParallelWritesToSameDocument_EndStateIsOneConsistentVersion()
        {
            const int writers = 20;
            const int chunksPerVersion = 3;

            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var tasks = new Task<Exception?>[writers];

            for (int i = 0; i < writers; i++)
            {
                tasks[i] = ReplaceAfterStart(start.Task, Doc("same.md", $"v{i}", Chunks($"v{i}", chunksPerVersion)));
            }

            start.SetResult();
            var errors = await Task.WhenAll(tasks);

            int succeeded = 0;

            foreach (var error in errors)
            {
                if (error is null)
                {
                    succeeded++;
                }
            }

            Assert.True(succeeded > 0);
            Assert.Equal(1, await Count("Documents"));

            var hash = await _store.GetContentHashAsync("same.md", ct);
            Assert.NotNull(hash);

            var texts = await ChunkTexts("same.md");
            Assert.Equal(chunksPerVersion, texts.Count);

            foreach (var text in texts)
            {
                Assert.StartsWith($"{hash}-", text);
            }

            Assert.Equal(chunksPerVersion, await Count("Chunks"));
        }

        [Fact]
        public async Task ReplaceAsync_NewDocument_SavesHashAndChunksInOrder()
        {
            await _store.ReplaceAsync(Doc("a.md", "h1", Chunks("a", 3)), ct);

            Assert.Equal("h1", await _store.GetContentHashAsync("a.md", ct));
            Assert.Equal(new[] { "a-c0", "a-c1", "a-c2" }, await ChunkTexts("a.md"));
        }

        [Fact]
        public async Task ReplaceAsync_SamePathTwice_ReplacesHashAndChunks()
        {
            await _store.ReplaceAsync(Doc("a.md", "h1", Chunks("old", 3)), ct);
            await _store.ReplaceAsync(Doc("a.md", "h2", Chunks("new", 1)), ct);

            Assert.Equal("h2", await _store.GetContentHashAsync("a.md", ct));
            Assert.Equal(new[] { "new-c0" }, await ChunkTexts("a.md"));
            Assert.Equal(1, await Count("Documents"));
        }

        [Fact]
        public async Task ReplaceAsync_DoesNotTouchOtherDocuments()
        {
            await _store.ReplaceAsync(Doc("a.md", "ha", Chunks("a", 2)), ct);
            await _store.ReplaceAsync(Doc("b.md", "hb", Chunks("b", 2)), ct);

            await _store.ReplaceAsync(Doc("a.md", "ha2", Chunks("a2", 1)), ct);

            Assert.Equal("hb", await _store.GetContentHashAsync("b.md", ct));
            Assert.Equal(new[] { "b-c0", "b-c1" }, await ChunkTexts("b.md"));
        }

        [Fact]
        public async Task DeleteAsync_ExistingDocument_RemovesDocumentAndItsChunks()
        {
            await _store.ReplaceAsync(Doc("a.md", "h1", Chunks("a", 2)), ct);

            await _store.DeleteAsync("a.md", ct);

            Assert.Null(await _store.GetContentHashAsync("a.md", ct));
            Assert.Equal(0, await Count("Chunks"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteAsync_EmptyPath_DoesNothing(string? path)
        {
            await _store.ReplaceAsync(Doc("a.md", "h1", Chunks("a", 1)), ct);

            await _store.DeleteAsync(path!, ct);

            Assert.Equal("h1", await _store.GetContentHashAsync("a.md", ct));
            Assert.Equal(1, await Count("Chunks"));
        }

        [Fact]
        public async Task DeleteAsync_UnknownPath_DoesNotTouchOtherDocuments()
        {
            await _store.ReplaceAsync(Doc("a.md", "h1", Chunks("a", 1)), ct);

            await _store.DeleteAsync("nie-ma-mnie.md", ct);

            Assert.Equal("h1", await _store.GetContentHashAsync("a.md", ct));
            Assert.Equal(1, await Count("Chunks"));
        }

        [Fact]
        public async Task ChangePath_MovesDocumentWithItsChunks()
        {
            await _store.ReplaceAsync(Doc("a.md", "h1", Chunks("a", 2)), ct);

            await _store.ChangePath("a.md", "b.md", ct);

            Assert.Null(await _store.GetContentHashAsync("a.md", ct));
            Assert.Equal("h1", await _store.GetContentHashAsync("b.md", ct));
            Assert.Equal(new[] { "a-c0", "a-c1" }, await ChunkTexts("b.md"));
        }

        [Theory]
        [InlineData("", "b.md")]
        [InlineData("a.md", "")]
        [InlineData("   ", "   ")]
        public async Task ChangePath_EmptyPath_DoesNothing(string path, string newPath)
        {
            await _store.ReplaceAsync(Doc("a.md", "h1", Chunks("a", 1)), ct);

            await _store.ChangePath(path, newPath, ct);

            Assert.Equal("h1", await _store.GetContentHashAsync("a.md", ct));
            Assert.Equal(1, await Count("Documents"));
        }

        [Fact]
        public async Task GetContentHashAsync_UnknownPath_ReturnsNull()
        {
            Assert.Null(await _store.GetContentHashAsync("nie-ma-mnie.md", ct));
        }

        [Fact]
        public async Task SearchAsync_ReturnsClosestFirst_AndRespectsLimit()
        {
            await _store.ReplaceAsync(Doc("x.md", "h", ("x", Axis(0))), ct);
            await _store.ReplaceAsync(Doc("y.md", "h", ("y", Axis(1))), ct);
            await _store.ReplaceAsync(Doc("z.md", "h", ("z", Axis(2))), ct);

            var hits = await _store.SearchAsync(Axis(1), ct, limit: 2);

            Assert.Equal(2, hits.Count);
            Assert.Equal("y.md", hits[0].Path);
        }

        [Fact]
        public async Task SearchAsync_EmptyDatabase_ReturnsEmptyList()
        {
            Assert.Empty(await _store.SearchAsync(Axis(0), ct));
        }

        private const int Dim = 8;

        private static float[] Axis(int i)
        {
            var v = new float[Dim];
            v[i % Dim] = 1f;
            return v;
        }

        private static List<string> Texts(string prefix, int n)
        {
            var texts = new List<string>();

            for (int j = 0; j < n; j++)
            {
                texts.Add($"{prefix}-c{j}");
            }

            return texts;
        }

        private static (string Text, float[] Embedding)[] Chunks(string prefix, int n)
        {
            var chunks = new (string Text, float[] Embedding)[n];

            for (int j = 0; j < n; j++)
            {
                chunks[j] = ($"{prefix}-c{j}", Axis(j));
            }

            return chunks;
        }

        private static Document Doc(string path, string hash, params (string Text, float[] Embedding)[] chunks)
        {
            var chunkList = new List<Chunk>();

            for (int i = 0; i < chunks.Length; i++)
            {
                chunkList.Add(new Chunk(i, chunks[i].Text, chunks[i].Embedding));
            }

            return new Document(path, hash, chunkList);
        }

        private async Task<Exception?> ReplaceAfterStart(Task start, Document doc)
        {
            await start;

            try
            {
                await _store.ReplaceAsync(doc, ct);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        private async Task<int> Count(string table)
        {
            await using var conn = await _factory.OpenAsync(ct);
            return (int)await conn.ExecuteScalarAsync<long>(new CommandDefinition($"SELECT COUNT(*) FROM {table}", cancellationToken: ct));
        }

        private async Task<List<string>> ChunkTexts(string path)
        {
            await using var conn = await _factory.OpenAsync(ct);
            var texts = await conn.QueryAsync<string>(new CommandDefinition("SELECT c.Text FROM Chunks c JOIN Documents d ON d.Id = c.DocumentId WHERE d.Path = @Path ORDER BY c.ChunkIndex", new TursoParams(new { Path = path }), cancellationToken: ct));
            return texts.AsList();
        }
    }
}