using DocumentFormat.OpenXml.Office2013.Excel;
using SearchBuddy.Application.Indexing;
using SearchBuddy.Application.Settings;
using SearchBuddy.Application.Storage;
using SearchBuddy.Indexer.FileWatcher;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Indexer.Indexer
{
    public class DeleteWorker(DeleteQueue _deleteQueue, IDocumentStore _docStore, ILogger<DeleteWorker> _logger, IndexerState _state) : BackgroundService
    {
        private readonly SemaphoreSlim _slots = new(2, 2);

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (true)
            {
                await _state.WaitUntilEnabledAsync(ct);
                await _slots.WaitAsync(ct);

                var path = await _deleteQueue.DequeueAsync(ct);

                _ = Task.Run(() => ProcessAsync(path, ct), ct);
            }
        }

        private async Task ProcessAsync(string path, CancellationToken ct)
        {
            try
            {
                await _docStore.DeleteAsync(path, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deleting failed for {Path}", path);
            }
            finally
            {
                _slots.Release();
            }
        }
    }
}
