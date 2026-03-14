using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;

namespace MediaPlatform.Application.Abstractions;

public interface IPlayerClient
{
    Task SendCommandAsync(CommandType command, QueueItem? item = null, CancellationToken ct = default);
}
