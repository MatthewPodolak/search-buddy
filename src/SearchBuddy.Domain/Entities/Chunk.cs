using SearchBuddy.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Domain.Entities
{
    public class Chunk
    {
        public int Index { get; private set; }
        public string Text { get; private set; }
        public ReadOnlyMemory<float> Embedding { get; private set; }

        public Chunk(int index, string text, ReadOnlyMemory<float> embedding)
        {
            if (int.IsNegative(index)) { throw new DomainException($"Can't assign chunk with negative document index. Check {nameof(Chunk)} mapping."); }
            if (string.IsNullOrEmpty(text)) { throw new DomainException($"Can't assign chunk with empty text. Check {nameof(Chunk)} mapping."); }
            if (embedding.IsEmpty) { throw new DomainException($"Can't assign empty chunk embedding. Check {nameof(Chunk)} mapping."); }

            Index = index;
            Text = text;
            Embedding = embedding;
        }
    }
}
