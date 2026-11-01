using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace SearchBuddy.Indexer.FileWatcher
{
    public class DeleteQueue
    {
        private Channel<string> delete_queue = Channel.CreateUnbounded<string>();
        private ConcurrentDictionary<string, byte> queue_set = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        public DeleteQueue() { }

        public int Count => delete_queue.Reader.Count;

        public bool Contains(string path)
        {
            return queue_set.TryGetValue(path, out _);
        }

        public void Enqueue(string path)
        {
            if (queue_set.TryAdd(path, 0))
            {
                delete_queue.Writer.TryWrite(path);
            }
        }
        public async ValueTask<string> DequeueAsync(CancellationToken ct)
        {
            while (true)
            {
                var path = await delete_queue.Reader.ReadAsync(ct);
                if (queue_set.TryRemove(path, out _))
                {
                    return path;
                }
            }
        }

        public bool Remove(string path)
        {
            return queue_set.TryRemove(path, out _);
        }
    }
}
