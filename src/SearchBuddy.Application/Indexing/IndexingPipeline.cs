using SearchBuddy.Application.LLM;
using SearchBuddy.Application.Storage;
using SearchBuddy.Application.Text;
using SearchBuddy.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Indexing
{
    internal class IndexingPipeline : IIndexingPipeline
    {
        private readonly Dictionary<string, ITextExtractor> _extractors = new(StringComparer.OrdinalIgnoreCase);
        private readonly IDocumentStore _docStore;
        private readonly ITextChunker _textChunker;
        private readonly IEmbedder _embedder;
        private readonly IHashGenerator _hashGenerator;

        public IndexingPipeline(IEnumerable<ITextExtractor> extractors, IDocumentStore docStore, ITextChunker textChunker, IEmbedder embedder, IHashGenerator hashGenerator) 
        {
            foreach (var extractor in extractors)
            {
                foreach (var extension in extractor.Extensions)
                {
                    _extractors.Add(extension, extractor);
                }
            }

            _docStore = docStore;
            _textChunker = textChunker;
            _embedder = embedder;
            _hashGenerator = hashGenerator;
        }

        public async Task ProcessFileAsync(string path, CancellationToken ct)
        {
            if (!_extractors.TryGetValue(Path.GetExtension(path), out var extractor)) { return; }

            var hash = await _hashGenerator.GetHashFromFile(path, ct);
            if (string.IsNullOrWhiteSpace(hash)) { return; }

            var prevHash = await _docStore.GetContentHashAsync(path, ct);
            if(prevHash == hash) { return; }

            var text = await extractor.ExtractAsync(path, ct);
            if (String.IsNullOrEmpty(text)) { return; }

            var chunked = await _textChunker.ChunkText(text, ct);
            if (chunked.Count == 0) { return; }

            var chunks = new List<Chunk>(chunked.Count);
            for (int i = 0; i < chunked.Count; i++)
            {
                var vector = await _embedder.EmbeddAsync(chunked[i], false, ct);
                chunks.Add(new Chunk(i, chunked[i], vector));
            }

            await _docStore.ReplaceAsync(new Document(path, hash, chunks), ct);
        }
    }
}
