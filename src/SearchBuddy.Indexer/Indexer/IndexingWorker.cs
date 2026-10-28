using SearchBuddy.Application.Indexing;
using SearchBuddy.Indexer.FileWatcher;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Indexer.Indexer
{
    internal class IndexingWorker(ChangeQueue changeQueue, IIndexingPipeline pipeline, ILogger<IndexingWorker> logger) : BackgroundService
    {
        private readonly SemaphoreSlim _slots = new(10, 10);
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (true)
            {
                await _slots.WaitAsync(ct);
                var path = await changeQueue.DequeueAsync(ct);

                _ = ProcessAsync(path, ct);
            }
        }

        private async Task ProcessAsync(string filePath, CancellationToken ct)
        {
            try
            {
                await pipeline.ProcessFileAsync(filePath, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Indexing failed for {Path}", filePath);
            }
            finally
            {
                _slots.Release();
            }
        }
    }
}
