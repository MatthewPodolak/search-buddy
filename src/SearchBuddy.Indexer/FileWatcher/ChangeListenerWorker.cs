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
    internal class ChangeListenerWorker(ChangeQueue changeQueue, DeleteQueue deleteQueue, ScanFilesTrigger scanTrigger, PathFilter filter) : BackgroundService
    {
        private Dictionary<ulong, string> processes = new Dictionary<ulong, string>(100_000);

        private readonly int upd_trehsold = 5;
        private int upd_counter = 0;
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            using var session = new TraceEventSession(KernelTraceEventParser.KernelSessionName);

            session.EnableKernelProvider(KernelTraceEventParser.Keywords.FileIOInit | KernelTraceEventParser.Keywords.DiskFileIO);

            session.Source.Kernel.FileIOCreate += data =>
            {
                if (filter.ShouldIndex(data.FileName))
                {
                    processes.TryAdd(data.FileObject, data.FileName);
                }
            };

            session.Source.Kernel.FileIOWrite += data =>
            {
                if (processes.TryGetValue(data.FileObject, out var path))
                {
                    changeQueue.Enqueue(path);
                }
            };

            session.Source.Kernel.FileIOClose += data =>
            {
                processes.Remove(data.FileObject);
            };

            session.Source.Kernel.FileIODelete += data =>
            {
                if (!filter.ShouldIndex(data.FileName)) { return; }

                changeQueue.Remove(data.FileName);
                deleteQueue.Enqueue(data.FileName);
            };

            session.Source.Kernel.FileIORename += data =>
            {
                if (!filter.ShouldIndex(data.FileName)) { return; }

                upd_counter++;
                if(upd_counter >= upd_trehsold)
                {
                    scanTrigger.Request();
                    upd_counter = 0;
                }
            };

            await Task.Run(() => session.Source.Process(), ct);
        }
    }
}
