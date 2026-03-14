namespace MediaPlatform.Application.Abstractions;

public interface IAuditLog
{
    void Record(AuditEntry entry);
    IReadOnlyList<AuditEntry> GetRecent(int count = 50);
}

public record AuditEntry(
    string Action,
    string? UserId,
    string? IpAddress,
    string? Detail,
    DateTimeOffset Timestamp);
