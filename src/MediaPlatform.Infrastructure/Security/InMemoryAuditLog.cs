using System.Collections.Concurrent;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Infrastructure.Security;

public sealed class InMemoryAuditLog : IAuditLog
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();
    private const int MaxEntries = 1000;

    public void Record(AuditEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries)
            _entries.TryDequeue(out _);
    }

    public IReadOnlyList<AuditEntry> GetRecent(int count = 50)
    {
        return _entries.Reverse().Take(count).ToList();
    }
}
