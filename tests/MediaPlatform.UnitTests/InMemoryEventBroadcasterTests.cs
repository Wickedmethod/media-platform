using MediaPlatform.Infrastructure.Events;
using Xunit;

namespace MediaPlatform.UnitTests;

public class InMemoryEventBroadcasterTests
{
    [Fact]
    public async Task Broadcast_DeliversToSubscriber()
    {
        var broadcaster = new InMemoryEventBroadcaster();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        var received = new List<(string EventType, string Json)>();
        var subTask = Task.Run(async () =>
        {
            await foreach (var item in broadcaster.SubscribeAsync(cts.Token))
            {
                received.Add(item);
                if (received.Count >= 1) await cts.CancelAsync();
            }
        }, cts.Token);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        broadcaster.Broadcast("test-event", new { value = 42 });

        try { await subTask; } catch (OperationCanceledException) { }

        Assert.Single(received);
        Assert.Equal("test-event", received[0].EventType);
        Assert.Contains("42", received[0].Json);
    }

    [Fact]
    public async Task Broadcast_DeliversToMultipleSubscribers()
    {
        var broadcaster = new InMemoryEventBroadcaster();
        using var cts1 = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts1.CancelAfter(TimeSpan.FromSeconds(2));
        cts2.CancelAfter(TimeSpan.FromSeconds(2));

        var received1 = new List<string>();
        var received2 = new List<string>();

        var sub1 = Task.Run(async () =>
        {
            await foreach (var item in broadcaster.SubscribeAsync(cts1.Token))
            { received1.Add(item.EventType); await cts1.CancelAsync(); }
        }, cts1.Token);
        var sub2 = Task.Run(async () =>
        {
            await foreach (var item in broadcaster.SubscribeAsync(cts2.Token))
            { received2.Add(item.EventType); await cts2.CancelAsync(); }
        }, cts2.Token);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        broadcaster.Broadcast("ping", "data");

        try { await Task.WhenAll(sub1, sub2); } catch (OperationCanceledException) { }

        Assert.Single(received1);
        Assert.Single(received2);
    }

    [Fact]
    public void Broadcast_NoSubscribers_DoesNotThrow()
    {
        var broadcaster = new InMemoryEventBroadcaster();
        broadcaster.Broadcast("orphan", new { });
    }
}
