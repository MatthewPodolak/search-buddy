using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Extensions.Options;
using SearchBuddy.Application.Files;
using SearchBuddy.Application.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Indexer.FileWatcher
{
    internal class ChangeListenerWorker(ChangeQueue _changeQueue, DeleteQueue _deleteQueue, ScanFilesTrigger _scanTrigger, PathFilter _filter, IndexerState _state) : BackgroundService
    {
        private Dictionary<ulong, string> processes = new Dictionary<ulong, string>(100_000);

        private readonly int upd_trehsold = 5;
        private int upd_counter = 0;
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await _state.WaitUntilEnabledAsync(ct);

                using var run = CancellationTokenSource.CreateLinkedTokenSource(ct, _state.DisabledToken);
                try
                {
                    await RunSessionAsync(run.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
            }
        }
        private async Task RunSessionAsync(CancellationToken ct)
        {
            processes.Clear();
            upd_counter = 0;

            using var session = new TraceEventSession(KernelTraceEventParser.KernelSessionName);
            using var stopReg = ct.Register(() => session.Stop());

            session.EnableKernelProvider(KernelTraceEventParser.Keywords.FileIOInit | KernelTraceEventParser.Keywords.DiskFileIO);

            session.Source.Kernel.FileIOCreate += data =>
            {
                if (_filter.ShouldIndex(data.FileName))
                {
                    processes.TryAdd(data.FileObject, data.FileName);
                }
            };

            session.Source.Kernel.FileIOWrite += data =>
            {
                if (processes.TryGetValue(data.FileObject, out var path))
                {
                    _changeQueue.Enqueue(path);
                }
            };

            session.Source.Kernel.FileIOClose += data =>
            {
                processes.Remove(data.FileObject);
            };

            session.Source.Kernel.FileIODelete += data =>
            {
                if (!_filter.ShouldIndex(data.FileName)) { return; }

                _changeQueue.Remove(data.FileName);
                _deleteQueue.Enqueue(data.FileName);
            };

            session.Source.Kernel.FileIORename += data =>
            {
                if (!_filter.ShouldIndex(data.FileName)) { return; }

                upd_counter++;
                if (upd_counter >= upd_trehsold)
                {
                    _scanTrigger.Request();
                    upd_counter = 0;
                }
            };

            await Task.Run(() => session.Source.Process());
        }
    }
}