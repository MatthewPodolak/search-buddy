using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Indexer.FileWatcher
{
    internal class ChangeListenerWorker(ChangeQueue changeQueue, IOptions<WatchSettings> settings) : BackgroundService
    {
        private Dictionary<ulong, string> processes = new Dictionary<ulong, string>(100_000);
        private readonly HashSet<string> extensions = new(settings.Value.Extensions, StringComparer.OrdinalIgnoreCase);


        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            using var session = new TraceEventSession(KernelTraceEventParser.KernelSessionName);

            session.EnableKernelProvider(KernelTraceEventParser.Keywords.FileIOInit | KernelTraceEventParser.Keywords.DiskFileIO);

            session.Source.Kernel.FileIOCreate += data =>
            {
                if (extensions.Contains(Path.GetExtension(data.FileName)))
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

            await Task.Run(() => session.Source.Process(), ct);
        }
    }
}
