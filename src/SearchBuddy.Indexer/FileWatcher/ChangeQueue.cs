using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace SearchBuddy.Indexer.FileWatcher
{
    internal class ChangeQueue
    {
        private Channel<string> update_queue = Channel.CreateUnbounded<string>();
        private ConcurrentDictionary<string, byte> queue_set = new ConcurrentDictionary<string, byte>();
        public ChangeQueue() { }

        public int Count => update_queue.Reader.Count;

        public bool Contains(string path)
        {
            return queue_set.TryGetValue(path, out _);
        }

        public void Enqueue(string path)
        {
            if (queue_set.TryAdd(path, 0))
            {
                update_queue.Writer.TryWrite(path);
            }
        }
        public async ValueTask<string> DequeueAsync(CancellationToken ct)
        {
            var path = await update_queue.Reader.ReadAsync(ct);
            queue_set.TryRemove(path, out _);

            return path;
        }
    }
}
