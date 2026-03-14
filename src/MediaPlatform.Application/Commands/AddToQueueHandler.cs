using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.ValueObjects;

namespace MediaPlatform.Application.Commands;

public sealed record AddToQueueCommand(string Url, string Title, double StartAtSeconds = 0);

public sealed class AddToQueueHandler(IQueueRepository repository)
{
    public async Task<QueueItem> HandleAsync(AddToQueueCommand command, CancellationToken ct = default)
    {
        var videoUrl = VideoUrl.Create(command.Url);
        var item = new QueueItem(Guid.NewGuid().ToString("N"), videoUrl, command.Title, command.StartAtSeconds);
        await repository.AddAsync(item, ct);
        return item;
    }
}
