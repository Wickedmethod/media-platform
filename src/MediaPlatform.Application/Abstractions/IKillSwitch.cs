namespace MediaPlatform.Application.Abstractions;

/// <summary>
/// Emergency kill switch — blocks all write/command operations when active.
/// </summary>
public interface IKillSwitch
{
    bool IsActive { get; }
    string? ActivatedBy { get; }
    DateTimeOffset? ActivatedAt { get; }
    void Activate(string reason, string? userId = null);
    void Deactivate(string? userId = null);
}
