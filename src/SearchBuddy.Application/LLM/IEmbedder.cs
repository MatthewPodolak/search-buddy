using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.LLM
{
    public interface IEmbedder
    {
        Task<ReadOnlyMemory<float>> EmbeddAsync(string text, bool isQuest, CancellationToken ct);
    }
}
