using SearchBuddy.Application.Contracts;
using SearchBuddy.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Storage
{
    public interface IDocumentStore
    {
        Task<string?> GetContentHashAsync(string path, CancellationToken ct);
        Task ChangePath(string path, string newPath, CancellationToken ct);
        Task ReplaceAsync(Document doc, CancellationToken ct);
        Task DeleteAsync(string path, CancellationToken ct);
        Task<IReadOnlyList<SearchHit>> SearchAsync(ReadOnlyMemory<float> queryVector, CancellationToken ct, int limit = 5);
        Task<IReadOnlyDictionary<string, string>> GetPathHashDictAsync(CancellationToken ct);
    }
}
