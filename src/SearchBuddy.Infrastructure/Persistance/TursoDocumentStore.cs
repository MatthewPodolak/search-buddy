using Dapper;
using SearchBuddy.Application.Contracts;
using SearchBuddy.Application.Storage;
using SearchBuddy.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;

namespace SearchBuddy.Infrastructure.Persistance
{
    public class TursoDocumentStore : IDocumentStore
    {
        private readonly TursoConnectionFactory _connFactory;
        public TursoDocumentStore(TursoConnectionFactory connectionFactory)
        {
            _connFactory = connectionFactory;
        }
        public async Task DeleteAsync(string path, CancellationToken ct)
        {
            if(string.IsNullOrWhiteSpace(path)) { return; }

            await DifferExec(async x =>
            {
                return await x.ExecuteAsync("DELETE FROM Documents WHERE Path = @Path", new TursoParams(new { Path = path }));
            }, ct);
        }

        public async Task ChangePath(string path, string newPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(newPath)) { return; }

            await DifferExec(async x =>
            {
                return await x.ExecuteAsync("UPDATE Documents SET Path = @NewPath WHERE Path = @Path", new TursoParams(new { Path = path, NewPath = newPath }));
            }, ct);
        }

        public async Task<string?> GetContentHashAsync(string path, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(path)) { return null; }

            await using var conn = await _connFactory.OpenAsync(ct);

            return await conn.ExecuteScalarAsync<string?>(new CommandDefinition("SELECT ContentHash FROM Documents WHERE Path = @Path", new TursoParams(new { Path = path }), cancellationToken: ct));
        }

        public async Task ReplaceAsync(Document doc, CancellationToken ct)
        {
            var chunkRows = doc.Chunks.Select(c => new
            {
                ChunkIndex = c.Index,
                Text = c.Text,
                Embedding = ToVectorJson(c.Embedding)
            }).ToList();

            await DifferExec(async x =>
            {
                var docId = await x.ExecuteScalarAsync<long>(
                    """
                        INSERT INTO Documents (Path, ContentHash) VALUES (@Path, @ContentHash) ON CONFLICT(Path) DO UPDATE SET ContentHash = excluded.ContentHash
                        RETURNING Id
                    """,
                    new TursoParams(new { doc.Path, doc.ContentHash }));

                await x.ExecuteAsync("DELETE FROM Chunks WHERE DocumentId = @DocId", new TursoParams(new { DocId = docId }));

                return await x.ExecuteAsync(
                    """
                        INSERT INTO Chunks (DocumentId, ChunkIndex, Text, Embedding)
                        VALUES (@DocId, @ChunkIndex, @Text, vector32(@Embedding))
                    """,
                    chunkRows.Select(r => new TursoParams(new { DocId = docId, r.ChunkIndex, r.Text, r.Embedding })));
            }, ct);
        }

        private static string ToVectorJson(ReadOnlyMemory<float> v)
        {
            var sb = new StringBuilder("[");

            foreach (var f in v.Span)
            {
                if (sb.Length > 1) sb.Append(',');
                sb.Append(f.ToString("R", CultureInfo.InvariantCulture));
            }

            return sb.Append(']').ToString();
        }

        private async Task DifferExec<T>(Func<DbConnection, Task<T>> work, CancellationToken ct)
        {
            int attemptCount = 3;

            while (attemptCount > 0)
            {
                attemptCount--;

                await using var conn = await _connFactory.OpenAsync(ct);

                try
                {
                    await conn.ExecuteAsync("BEGIN CONCURRENT");

                    await work(conn);

                    await conn.ExecuteAsync("COMMIT");
                    break;
                }
                catch
                {
                    await conn.ExecuteAsync("ROLLBACK");
                }
            }
        }

        public async Task<IReadOnlyList<SearchHit>> SearchAsync(ReadOnlyMemory<float> queryVector, CancellationToken ct, int limit = 5)
        {
            await using var conn = await _connFactory.OpenAsync(ct);

            var hits = await conn.QueryAsync<SearchHit>(new CommandDefinition(
                """
                    SELECT d.Path, c.Text, vector_distance_cos(c.Embedding, vector32(@Query)) AS Distance FROM Chunks c
                    JOIN Documents d ON d.Id = c.DocumentId ORDER BY Distance LIMIT @Limit
                """,
                new TursoParams(new { Query = ToVectorJson(queryVector), Limit = limit }),
                cancellationToken: ct));

            return hits.AsList();
        }

        public async Task<IReadOnlyDictionary<string, string>> GetPathHashDictAsync(CancellationToken ct)
        {
            await using var conn = await _connFactory.OpenAsync(ct);

            var rows = await conn.QueryAsync<(string Path, string ContentHash)>(new CommandDefinition("SELECT Path, ContentHash FROM Documents", cancellationToken: ct));

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (path, hash) in rows)
            {
                dict[path] = hash;
            }

            return dict;
        }
    }
}
