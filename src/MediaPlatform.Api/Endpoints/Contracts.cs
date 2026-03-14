namespace MediaPlatform.Api.Endpoints;

public sealed record AddToQueueRequest(string Url, string Title);

public sealed record QueueItemResponse(
    string Id, string Url, string Title, string Status, DateTimeOffset AddedAt);

public sealed record PlaybackStateResponse(
    string State, QueueItemResponse? CurrentItem, DateTimeOffset? StartedAt);
