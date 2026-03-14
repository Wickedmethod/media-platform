namespace MediaPlatform.Api.Endpoints;

public sealed record AddToQueueRequest(string Url, string Title, double StartAtSeconds = 0);

public sealed record QueueItemResponse(
    string Id, string Url, string Title, string Status, DateTimeOffset AddedAt, double StartAtSeconds);

public sealed record PlaybackStateResponse(
    string State, QueueItemResponse? CurrentItem, DateTimeOffset? StartedAt,
    double PositionSeconds, int RetryCount, string? LastError);

public sealed record ReportPositionRequest(double PositionSeconds);
public sealed record ReportErrorRequest(string Reason);
public sealed record SetQueueModeRequest(string Mode);

public sealed record QueueModeResponse(string Mode);

public sealed record ApiError(string Error, string? Detail = null);
