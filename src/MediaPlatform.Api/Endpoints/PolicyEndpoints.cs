using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Endpoints;

public static class PolicyEndpoints
{
    public static void MapPolicyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/policies").WithTags("Policies");

        group.MapGet("/", (IPolicyEngine engine) =>
        {
            return Results.Ok(engine.GetPolicies());
        });

        group.MapPost("/", (AddPolicyRequest request, IPolicyEngine engine, IAuditLog auditLog, HttpContext http) =>
        {
            if (!Enum.TryParse<PolicyType>(request.Type, true, out var type))
                return Results.BadRequest(new ApiError($"Invalid policy type: {request.Type}"));

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Value))
                return Results.BadRequest(new ApiError("Name and Value are required"));

            var policy = new PlaybackPolicy(
                Id: Guid.NewGuid().ToString("N")[..8],
                Name: request.Name,
                Type: type,
                Value: request.Value,
                Enabled: request.Enabled);

            engine.AddPolicy(policy);

            auditLog.Record(new AuditEntry(
                "POLICY_ADDED",
                null,
                http.Connection.RemoteIpAddress?.ToString(),
                $"{policy.Type}: {policy.Name} = {policy.Value}",
                DateTimeOffset.UtcNow));

            return Results.Created($"/policies/{policy.Id}", policy);
        });

        group.MapDelete("/{id}", (string id, IPolicyEngine engine, IAuditLog auditLog, HttpContext http) =>
        {
            engine.RemovePolicy(id);
            auditLog.Record(new AuditEntry(
                "POLICY_REMOVED",
                null,
                http.Connection.RemoteIpAddress?.ToString(),
                $"Policy {id}",
                DateTimeOffset.UtcNow));
            return Results.NoContent();
        });

        group.MapPost("/{id}/toggle", (string id, TogglePolicyRequest request, IPolicyEngine engine) =>
        {
            engine.SetEnabled(id, request.Enabled);
            return Results.Ok(new { id, enabled = request.Enabled });
        });

        // Evaluate a policy check without executing
        group.MapPost("/evaluate", (EvaluatePolicyRequest request, IPolicyEngine engine) =>
        {
            var context = new PolicyContext(
                Action: request.Action,
                VideoUrl: request.VideoUrl,
                UserId: null,
                Timestamp: DateTimeOffset.UtcNow);

            var result = engine.Evaluate(context);
            return Results.Ok(result);
        });
    }
}

public record AddPolicyRequest(string Name, string Type, string Value, bool Enabled = true);
public record TogglePolicyRequest(bool Enabled);
public record EvaluatePolicyRequest(string Action, string? VideoUrl);
