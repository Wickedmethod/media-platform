using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;

namespace MediaPlatform.Application.Commands;

public sealed record ReportErrorCommand(string Reason);

public sealed class ReportErrorHandler(IQueueRepository repository)
{
    private const int MaxRetries = 3;

    public async Task<PlaybackState> HandleAsync(ReportErrorCommand command, CancellationToken ct = default)
    {
        var state = await repository.GetPlaybackStateAsync(ct);

        if (state.State is PlayerState.Stopped or PlayerState.Idle)
            return state;

        state.IncrementRetry();

        if (state.RetryCount >= MaxRetries)
        {
            // Permanent failure — skip to next
            PlaybackStateMachine.Apply(state, PlayerEvent.ErrorOccurred, errorReason: command.Reason);
            state.SetIdle();

            // Try to play next item
            var next = await repository.DequeueNextAsync(ct);
            if (next is not null)
            {
                PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, next);
                PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);
            }
        }
        else
        {
            // Transient — mark error but allow retry (UI will retry)
            state.SetError(command.Reason);
        }

        await repository.SavePlaybackStateAsync(state, ct);
        return state;
    }
}
