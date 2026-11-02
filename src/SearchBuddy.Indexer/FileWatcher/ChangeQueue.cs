using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace SearchBuddy.Indexer.FileWatcher
{
    public class ChangeQueue
    {
        private Channel<string> _update_queue = Channel.CreateUnbounded<string>();
        private ConcurrentDictionary<string, byte> _queue_set = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        public ChangeQueue() { }

        public int Count => _update_queue.Reader.Count;

        public bool Contains(string path)
        {
            return _queue_set.TryGetValue(path, out _);
        }

        public void Enqueue(string path)
        {
            if (_queue_set.TryAdd(path, 0))
            {
                _update_queue.Writer.TryWrite(path);
            }
        }
        public async ValueTask<string> DequeueAsync(CancellationToken ct)
        {
            while (true)
            {
                var path = await _update_queue.Reader.ReadAsync(ct);
                if(_queue_set.TryRemove(path, out _))
                {
                    return path;
                }
            }
        }

        public bool Remove(string path)
        {
            return _queue_set.TryRemove(path, out _);
        }
    }
}
