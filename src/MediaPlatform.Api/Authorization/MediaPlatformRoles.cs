namespace MediaPlatform.Api.Authorization;

public static class MediaPlatformRoles
{
    public const string Admin = "media-admin";
    public const string Operator = "media-operator";
    public const string Viewer = "media-viewer";
}

public static class AuthPolicies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string OperatorOrAdmin = nameof(OperatorOrAdmin);
    public const string ViewerOrAbove = nameof(ViewerOrAbove);
    public const string ReadAccess = nameof(ReadAccess);
    public const string QueueAdd = nameof(QueueAdd);
    public const string QueueOwner = nameof(QueueOwner);
    public const string WorkerOnly = nameof(WorkerOnly);
}
