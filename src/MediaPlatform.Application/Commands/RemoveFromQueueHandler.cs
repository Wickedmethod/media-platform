using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Application.Commands;

public sealed record RemoveFromQueueCommand(string ItemId);

public sealed class RemoveFromQueueHandler(IQueueRepository repository)
{
    public async Task HandleAsync(RemoveFromQueueCommand command, CancellationToken ct = default)
    {
        await repository.RemoveAsync(command.ItemId, ct);
    }
}
