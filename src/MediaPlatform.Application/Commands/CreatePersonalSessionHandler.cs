using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain.Entities;

namespace MediaPlatform.Application.Commands;

public sealed record CreatePersonalSessionCommand(string UserId, string DeviceId);

public sealed class CreatePersonalSessionHandler(ISessionRepository sessions)
{
    public async Task<PlaybackSession> HandleAsync(CreatePersonalSessionCommand command, CancellationToken ct = default)
    {
        // Check for existing session for this user — reuse if found
        var existing = await sessions.GetPersonalSessionAsync(command.UserId, ct);
        if (existing is not null)
        {
            existing.Touch();
            await sessions.SaveSessionAsync(existing, ct);
            return existing;
        }

        var session = PlaybackSession.CreatePersonal(command.UserId, command.DeviceId);
        await sessions.SaveSessionAsync(session, ct);
        return session;
    }
}
