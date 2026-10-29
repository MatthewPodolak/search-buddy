using SearchBuddy.Application.TextExtraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Indexing
{
    internal class IndexingPipeline : IIndexingPipeline
    {
        private readonly Dictionary<string, ITextExtractor> _extractors = new(StringComparer.OrdinalIgnoreCase);

        public IndexingPipeline(IEnumerable<ITextExtractor> extractors) 
        {
            foreach (var extractor in extractors)
            {
                foreach (var extension in extractor.Extensions)
                {
                    _extractors.Add(extension, extractor);
                }
            }
        }

        public async Task ProcessFileAsync(string path, CancellationToken ct)
        {
            if (!_extractors.TryGetValue(Path.GetExtension(path), out var extractor)) { return; }

            var text = await extractor.ExtractAsync(path, ct);
            if (String.IsNullOrEmpty(text)) { return; }


        }
    }
}
