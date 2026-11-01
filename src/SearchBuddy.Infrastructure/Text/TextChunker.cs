using Microsoft.ML.Tokenizers;
using SearchBuddy.Application.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Infrastructure.Text
{
    public class TextChunker : ITextChunker
    {
        private readonly SentencePieceTokenizer _tokenizer;
        public TextChunker()
        {
            using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "models", "embedding", "multilingual-e5-small", "sentencepiece.bpe.model"));
            _tokenizer = SentencePieceTokenizer.Create(stream, true, true);
        }

        public Task<List<string>> ChunkText(string text, CancellationToken ct)
        {
            const int maxTokens = 512;

            int overhead = _tokenizer.CountTokens("passage:");

            var chunks = new List<string>();
            var current = new List<string>();

            int currentTokenCount = overhead;

            foreach (var word in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                ct.ThrowIfCancellationRequested();

                int wordTokens = _tokenizer.CountTokens(word);

                if (currentTokenCount + wordTokens > maxTokens && current.Count > 0)
                {
                    chunks.Add(string.Join(' ', current));
                    current.Clear();
                    currentTokenCount = overhead;
                }

                current.Add(word);
                currentTokenCount += wordTokens;
            }

            if (current.Count > 0)
            {
                chunks.Add(string.Join(' ', current));
            }

            return Task.FromResult(chunks);
        }
    }
}
