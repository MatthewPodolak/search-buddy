using SearchBuddy.Application.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.LLM
{
    public interface ILlmGen
    {
        Task<string> AskAsync(string question, IReadOnlyList<SearchHit> FilePaths, CancellationToken ct);
    }
}
