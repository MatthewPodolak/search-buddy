using SearchBuddy.Indexer.FileWatcher;

namespace SearchBuddy.Indexer.Tests;

public class ChangeQueueTests
{
    private readonly ChangeQueue _queue = new();
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public void Enqueue_SamePathTwice_QueuesOnce()
    {
        _queue.Enqueue(@"C:\a.txt");
        _queue.Enqueue(@"C:\a.txt");

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
}
