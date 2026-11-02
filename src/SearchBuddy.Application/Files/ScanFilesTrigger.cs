using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace SearchBuddy.Application.Files
{
    public class ScanFilesTrigger
    {
        private readonly Channel<bool> channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
        public void Request()
        {
            channel.Writer.TryWrite(true);
        }
        public IAsyncEnumerable<bool> WaitAllAsync(CancellationToken ct)
        {
            return channel.Reader.ReadAllAsync(ct);
        }
    }
}
