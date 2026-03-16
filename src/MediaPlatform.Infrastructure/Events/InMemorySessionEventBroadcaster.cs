using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Infrastructure.Events;

public sealed class InMemorySessionEventBroadcaster : ISessionEventBroadcaster
{
    private readonly ConcurrentDictionary<string, List<Channel<(string, string)>>> _sessions = new();
    private readonly Lock _lock = new();

    public void Broadcast(string sessionId, string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data);
        if (!_sessions.TryGetValue(sessionId, out var subscribers)) return;

        lock (_lock)
        {
            subscribers.RemoveAll(ch => ch.Reader.Completion.IsCompleted);
            foreach (var ch in subscribers)
                ch.Writer.TryWrite((eventType, json));
        }
    }

    public async IAsyncEnumerable<(string EventType, string Json)> SubscribeAsync(
        string sessionId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateBounded<(string, string)>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        var subscribers = _sessions.GetOrAdd(sessionId, _ => []);
        lock (_lock) { subscribers.Add(channel); }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(ct))
                yield return item;
        }
        finally
        {
            channel.Writer.TryComplete();
            lock (_lock)
            {
                subscribers.Remove(channel);
                if (subscribers.Count == 0)
                    _sessions.TryRemove(sessionId, out _);
            }
        }
    }
}
