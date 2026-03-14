using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain.Enums;

namespace MediaPlatform.Application.Commands;

public sealed record SetQueueModeCommand(QueueMode Mode);

public sealed class SetQueueModeHandler(IQueueRepository repository)
{
    public async Task<QueueMode> HandleAsync(SetQueueModeCommand command, CancellationToken ct = default)
    {
        await repository.SetQueueModeAsync(command.Mode, ct);
        return command.Mode;
    }
}
