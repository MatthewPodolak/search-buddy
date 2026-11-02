using SearchBuddy.Indexer.FileWatcher;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Indexer.Tests
{
    public class DelQueueTests
    {
        private readonly DeleteQueue _queue = new();
        private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

        [Fact]
        public void Enqueue_SamePathTwice_QueuesOnce()
        {
            _queue.Enqueue(@"C:\a.txt");
            _queue.Enqueue(@"C:\a.txt");

            Assert.Equal(1, _queue.Count);
        }

        [Fact]
        public void Enqueue_SamePathDifferentCase_QueuesOnce()
        {
            _queue.Enqueue(@"C:\Users\a.txt");
            _queue.Enqueue(@"C:\USERS\A.TXT");

            Assert.Equal(1, _queue.Count);
        }

        [Fact]
        public async Task DequeueAsync_ReturnsEnqueuedPath()
        {
            _queue.Enqueue(@"C:\a.txt");

            var path = await _queue.DequeueAsync(_ct);

            Assert.Equal(@"C:\a.txt", path);
            Assert.Equal(0, _queue.Count);
        }

        [Fact]
        public async Task Enqueue_AfterDequeue_QueuesSamePathAgain()
        {
            _queue.Enqueue(@"C:\a.txt");
            await _queue.DequeueAsync(_ct);

            _queue.Enqueue(@"C:\a.txt");

            Assert.Equal(1, _queue.Count);
        }

        [Fact]
        public async Task DequeueAsync_SkipsRemovedPath()
        {
            _queue.Enqueue(@"C:\a.txt");
            _queue.Enqueue(@"C:\b.txt");

            _queue.Remove(@"C:\a.txt");

            Assert.Equal(@"C:\b.txt", await _queue.DequeueAsync(_ct));
        }
    }
}
