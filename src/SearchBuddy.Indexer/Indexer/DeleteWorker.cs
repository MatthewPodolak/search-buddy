using DocumentFormat.OpenXml.Office2013.Excel;
using SearchBuddy.Application.Indexing;
using SearchBuddy.Application.Storage;
using SearchBuddy.Indexer.FileWatcher;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Indexer.Indexer
{
    public class DeleteWorker(DeleteQueue deleteQueue, IDocumentStore docStore, ILogger<DeleteWorker> logger) : BackgroundService
    {
        private readonly SemaphoreSlim _slots = new(2, 2);

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (true)
            {
                await _slots.WaitAsync(ct);
                var path = await deleteQueue.DequeueAsync(ct);

                _ = Task.Run(() => ProcessAsync(path, ct), ct);
            }
        }

        private async Task ProcessAsync(string path, CancellationToken ct)
        {
            try
            {
                await docStore.DeleteAsync(path, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Deleting failed for {Path}", path);
            }
            finally
            {
                _slots.Release();
            }
        }
    }
}
