using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;

namespace MediaPlatform.Application.Commands;

public sealed record PlayerCommand(CommandType Command);

public sealed class PlayerCommandHandler(IQueueRepository repository)
{
    public async Task<PlaybackState> HandleAsync(PlayerCommand command, CancellationToken ct = default)
    {
        var state = await repository.GetPlaybackStateAsync(ct);

        var playerEvent = command.Command switch
        {
            CommandType.Play => PlayerEvent.PlayRequested,
            CommandType.Pause => PlayerEvent.PauseRequested,
            CommandType.Skip => PlayerEvent.SkipRequested,
            CommandType.Stop => PlayerEvent.StopRequested,
            _ => throw new ArgumentOutOfRangeException(nameof(command))
        };

        QueueItem? nextItem = null;

        if (command.Command is CommandType.Play && state.State is PlayerState.Idle or PlayerState.Stopped)
        {
            nextItem = await repository.DequeueNextAsync(ct);
            if (nextItem is null)
                return state; // Nothing to play
        }

        PlaybackStateMachine.Apply(state, playerEvent, nextItem);

        // Auto-transition Buffering → Playing (no real buffering delay in this service)
        if (state.State is PlayerState.Buffering)
            PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);

        await repository.SavePlaybackStateAsync(state, ct);

        return state;
    }
}
