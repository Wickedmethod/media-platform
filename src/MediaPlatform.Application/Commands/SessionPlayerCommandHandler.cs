using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;

namespace MediaPlatform.Application.Commands;

public sealed record SessionPlayerCommand(string SessionId, CommandType Action);

public sealed class SessionPlayerCommandHandler(ISessionRepository sessions)
{
    public async Task<PlaybackState> HandleAsync(SessionPlayerCommand command, CancellationToken ct = default)
    {
        var session = await sessions.GetSessionAsync(command.SessionId, ct)
            ?? throw new InvalidOperationException("Session not found");

        var state = await sessions.GetSessionPlaybackStateAsync(command.SessionId, ct);

        var playerEvent = command.Action switch
        {
            CommandType.Play => PlayerEvent.PlayRequested,
            CommandType.Pause => PlayerEvent.PauseRequested,
            CommandType.Skip => PlayerEvent.SkipRequested,
            CommandType.Stop => PlayerEvent.StopRequested,
            _ => throw new ArgumentOutOfRangeException(nameof(command))
        };

        QueueItem? nextItem = null;

        if (command.Action is CommandType.Play && state.State is PlayerState.Idle or PlayerState.Stopped)
        {
            nextItem = await sessions.DequeueNextFromSessionAsync(command.SessionId, ct);
            if (nextItem is null)
                return state;
        }

        if (command.Action is CommandType.Skip &&
            state.State is PlayerState.Playing or PlayerState.Paused or PlayerState.Buffering)
        {
            PlaybackStateMachine.Apply(state, playerEvent, nextItem);

            var next = await sessions.DequeueNextFromSessionAsync(command.SessionId, ct);
            if (next is not null)
            {
                PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, next);
                PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);
            }

            await sessions.SaveSessionPlaybackStateAsync(command.SessionId, state, ct);
            session.Touch();
            await sessions.SaveSessionAsync(session, ct);
            return state;
        }

        PlaybackStateMachine.Apply(state, playerEvent, nextItem);

        if (state.State is PlayerState.Buffering)
            PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);

        await sessions.SaveSessionPlaybackStateAsync(command.SessionId, state, ct);

        session.Touch();
        await sessions.SaveSessionAsync(session, ct);

        return state;
    }
}
