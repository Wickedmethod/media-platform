using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain.Entities;

namespace MediaPlatform.Application.Commands;

public sealed record ReportPositionCommand(double PositionSeconds);

public sealed class ReportPositionHandler(IQueueRepository repository)
{
    public async Task<PlaybackState> HandleAsync(ReportPositionCommand command, CancellationToken ct = default)
    {
        var state = await repository.GetPlaybackStateAsync(ct);
        state.UpdatePosition(command.PositionSeconds);
        await repository.SavePlaybackStateAsync(state, ct);
        return state;
    }
}
