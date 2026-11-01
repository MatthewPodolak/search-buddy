using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Text
{
    public interface ITextChunker
    {
        Task<List<string>> ChunkText(string text, CancellationToken ct); 
    }
}
