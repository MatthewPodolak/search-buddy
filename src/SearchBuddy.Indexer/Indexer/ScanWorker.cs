using DocumentFormat.OpenXml.Bibliography;
using SearchBuddy.Application.Files;
using SearchBuddy.Application.Storage;
using SearchBuddy.Application.Text;
using SearchBuddy.Indexer.FileWatcher;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Indexer.Indexer
{
    public class ScanWorker(ScanFilesTrigger _trigger, IFileEnumerator _fileEnumerator, IDocumentStore _docStore, ChangeQueue _changeQue, DeleteQueue _delQue, IHashGenerator _hashGen, ILogger<ScanWorker> _logger) : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await foreach (var _ in _trigger.WaitAllAsync(ct))
            {
                try
                {
                    await Task.Run(() => ScanAsync(ct), ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SCAN DUMPED");
                }
            }
        }

        private async Task ScanAsync(CancellationToken ct)
        {
            var filesDict = await _docStore.GetPathHashDictAsync(ct);
            var missing = new HashSet<string>(filesDict.Keys);

            try
            {
                foreach (var path in _fileEnumerator.Enumerate(ct))
                {
                    missing.Remove(path);

                    if (filesDict.TryGetValue(path, out string fileHash))
                    {
                        var hash = await _hashGen.GetHashFromFile(path, ct);
                        if (hash != fileHash) { _changeQue.Enqueue(path); }
                    }
                    else
                    {
                        _changeQue.Enqueue(path);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogWarning("SCAN FAILED {Message}", ex.Message);
            }

            foreach (var path in missing)
            {
                _changeQue.Remove(path);
                _delQue.Enqueue(path);
            }
        }
    }
}
