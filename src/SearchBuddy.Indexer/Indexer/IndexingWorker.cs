using SearchBuddy.Application.Indexing;
using SearchBuddy.Application.Settings;
using SearchBuddy.Indexer.FileWatcher;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Indexer.Indexer
{
    internal class IndexingWorker(ChangeQueue _changeQueue, IIndexingPipeline _pipeline, ILogger<IndexingWorker> _logger, IndexerState _state) : BackgroundService
    {
        private readonly SemaphoreSlim _slots = new(2, 2);
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (true)
            {
                await _state.WaitUntilEnabledAsync(ct);
                await _slots.WaitAsync(ct);

                var path = await _changeQueue.DequeueAsync(ct);

                _ = Task.Run(() => ProcessAsync(path, ct), ct);
            }
        }

        private async Task ProcessAsync(string filePath, CancellationToken ct)
        {
            try
            {
                await _pipeline.ProcessFileAsync(filePath, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Indexing failed for {Path}", filePath);
            }
            finally
            {
                _slots.Release();
            }
        }
    }
}
