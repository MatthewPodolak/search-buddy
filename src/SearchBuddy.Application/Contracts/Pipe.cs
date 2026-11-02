using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Contracts
{
    public static class PipeCommands
    {
        public const string Search = "search";
        public const string Update = "update";
    }
    public record PipeRequest(string Command, string? Query = null, int Limit = 5);
    public record CommandResponse(bool Ok, string Message);
}
