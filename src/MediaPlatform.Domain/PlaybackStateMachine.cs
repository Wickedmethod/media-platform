using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.Errors;

namespace MediaPlatform.Domain;

/// <summary>
/// Centralized state machine for playback transitions.
/// All state changes MUST go through this class.
/// </summary>
public static class PlaybackStateMachine
{
    public static void Apply(PlaybackState state, PlayerEvent playerEvent, QueueItem? item = null)
    {
        var current = state.State;

        switch (playerEvent)
        {
            case PlayerEvent.PlayRequested when current is PlayerState.Idle or PlayerState.Stopped:
                if (item is null)
                    throw new InvalidStateTransitionException("Cannot play without a queue item.");
                state.SetBuffering(item);
                break;

            case PlayerEvent.PlayRequested when current is PlayerState.Paused:
                state.SetPlaying(state.CurrentItem!);
                break;

            case PlayerEvent.PlayRequested when current is PlayerState.Playing:
                // Idempotent — already playing, no-op
                break;

            case PlayerEvent.PauseRequested when current is PlayerState.Playing:
                state.SetPaused();
                break;

            case PlayerEvent.PauseRequested when current is PlayerState.Paused:
                // Idempotent — already paused, no-op
                break;

            case PlayerEvent.SkipRequested when current is PlayerState.Playing or PlayerState.Paused or PlayerState.Buffering:
                state.SetIdle();
                break;

            case PlayerEvent.SkipRequested when current is PlayerState.Idle:
                // Idempotent — nothing to skip, no-op
                break;

            case PlayerEvent.StopRequested when current is not PlayerState.Stopped:
                state.SetStopped();
                break;

            case PlayerEvent.StopRequested when current is PlayerState.Stopped:
                // Idempotent — already stopped, no-op
                break;

            case PlayerEvent.BufferingStarted when current is PlayerState.Idle or PlayerState.Buffering:
                if (item is null)
                    throw new InvalidStateTransitionException("Cannot buffer without a queue item.");
                state.SetBuffering(item);
                break;

            case PlayerEvent.PlaybackStarted when current is PlayerState.Buffering:
                state.SetPlaying(state.CurrentItem!);
                break;

            case PlayerEvent.TrackEnded when current is PlayerState.Playing:
                state.SetIdle();
                break;

            case PlayerEvent.ErrorOccurred when current is not PlayerState.Stopped:
                state.SetError();
                break;

            default:
                throw new InvalidStateTransitionException(
                    $"Invalid transition: cannot apply {playerEvent} in state {current}.");
        }
    }
}
