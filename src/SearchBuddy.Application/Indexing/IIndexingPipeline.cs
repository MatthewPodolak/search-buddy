using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Indexing
{
    public interface IIndexingPipeline
    {
        Task ProcessFileAsync(string fileName, CancellationToken ct);
    }
}
