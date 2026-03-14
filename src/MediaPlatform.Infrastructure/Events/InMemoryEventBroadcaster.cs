using System.Text.Json;
using System.Threading.Channels;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Infrastructure.Events;

public sealed class InMemoryEventBroadcaster : IEventBroadcaster
{
    private readonly List<Channel<(string, string)>> _subscribers = [];
    private readonly Lock _lock = new();

    public void Broadcast(string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data);
        lock (_lock)
        {
            _subscribers.RemoveAll(ch => ch.Reader.Completion.IsCompleted);
            foreach (var ch in _subscribers)
                ch.Writer.TryWrite((eventType, json));
        }
    }

    public async IAsyncEnumerable<(string EventType, string Json)> SubscribeAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateBounded<(string, string)>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        lock (_lock) { _subscribers.Add(channel); }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(ct))
                yield return item;
        }
        finally
        {
            channel.Writer.TryComplete();
            lock (_lock) { _subscribers.Remove(channel); }
        }
    }
}
