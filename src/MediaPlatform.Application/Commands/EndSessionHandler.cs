using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Application.Commands;

public sealed record EndSessionCommand(string SessionId, string UserId);

public sealed class EndSessionHandler(ISessionRepository sessions)
{
    public async Task HandleAsync(EndSessionCommand command, CancellationToken ct = default)
    {
        var session = await sessions.GetSessionAsync(command.SessionId, ct);
        if (session is null)
            return;

        // Only the session owner can end their session
        if (session.UserId != command.UserId)
            throw new UnauthorizedAccessException("Cannot end another user's session");

        await sessions.DeleteSessionAsync(command.SessionId, ct);
    }
}
