using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Infrastructure.Security;

public sealed class InMemoryKillSwitch : IKillSwitch
{
    private volatile bool _active;
    private string? _activatedBy;
    private DateTimeOffset? _activatedAt;
    private readonly object _lock = new();

    public bool IsActive => _active;
    public string? ActivatedBy => _activatedBy;
    public DateTimeOffset? ActivatedAt => _activatedAt;

    public void Activate(string reason, string? userId = null)
    {
        lock (_lock)
        {
            _active = true;
            _activatedBy = $"{userId ?? "system"}: {reason}";
            _activatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void Deactivate(string? userId = null)
    {
        lock (_lock)
        {
            _active = false;
            _activatedBy = null;
            _activatedAt = null;
        }
    }
}
