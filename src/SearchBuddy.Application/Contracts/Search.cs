using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Contracts
{
    public static class SearchContract { public const string PipeName = "SearchBuddy"; }
    public record SearchRequest(string Query, int limit = 5);
    public record SearchHit
    {
        public string Path { get; init; } = "";
        public string Text { get; init; } = "";
        public double Distance { get; init; }
    }

    public record SearchResponse(string Answer, IReadOnlyList<SearchHit> Sources);
}
