namespace MediaPlatform.Api.Middleware;

/// <summary>
/// Assigns a correlation ID to every request for end-to-end tracing.
/// Uses the incoming X-Correlation-Id header if present, otherwise generates one.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..12];

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
