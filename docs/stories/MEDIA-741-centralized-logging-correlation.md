# MEDIA-741: Centralized Logging & Correlation IDs

## Story

**Epic:** Infrastructure & Security  
**Priority:** Medium  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** None (existing API)

---

## Summary

Add correlation IDs to every request flowing through the API so that a single user action can be traced across all log entries. Implement structured JSON logging and prepare for centralized log aggregation (Grafana Loki, Seq, or similar).

**Absorbs:** MEDIA-757 (Structured JSON Logging Format) — structured JSON output is fully covered in this story's logging setup.

---

## Architecture

```
Client Request
    │ X-Correlation-Id: abc-123 (or auto-generated)
    ▼
API Middleware (CorrelationIdMiddleware)
    │ Sets HttpContext.TraceIdentifier = correlationId
    ▼
All log entries include correlationId
    │
    ├── Console (structured JSON)
    ├── Redis audit log (existing, enriched)
    └── Future: Grafana Loki / Seq sink
```

---

## Correlation ID Middleware

```csharp
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..12];

        context.TraceIdentifier = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
```

---

## Structured Logging Setup

```csharp
// Program.cs
builder.Host.UseSerilog((context, config) =>
{
    config
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "MediaPlatform")
        .WriteTo.Console(new JsonFormatter())
        .MinimumLevel.Information();
});
```

### Log Output Format

```json
{
  "timestamp": "2026-03-16T14:32:00.123Z",
  "level": "Information",
  "message": "Queue item added",
  "correlationId": "a1b2c3d4e5f6",
  "application": "MediaPlatform",
  "properties": {
    "itemId": "abc-123",
    "userId": "jonas",
    "endpoint": "POST /queue/add"
  }
}
```

---

## Propagation

The correlation ID propagates to:

| Component | How |
|-----------|-----|
| API log entries | Serilog `LogContext.PushProperty` |
| Redis audit log | Include `correlationId` field in audit entries |
| SSE events | Include `correlationId` in event metadata |
| Webhook deliveries | `X-Correlation-Id` header on outgoing webhooks |
| Player heartbeats | Pi includes API's correlation ID in heartbeat |

---

## NuGet Packages

```xml
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
<PackageReference Include="Serilog.Expressions" Version="5.0.0" />
```

---

## Tasks

- [ ] Add Serilog NuGet packages
- [ ] Configure structured JSON logging in `Program.cs`
- [ ] Create `CorrelationIdMiddleware`
- [ ] Register middleware early in pipeline
- [ ] Propagate correlation ID to audit log entries
- [ ] Propagate correlation ID to outgoing webhook headers
- [ ] Add `X-Correlation-Id` response header
- [ ] Unit tests for correlation ID generation and propagation

---

## Acceptance Criteria

- [ ] Every request gets a correlation ID (auto-generated or from header)
- [ ] All log entries include `correlationId` property
- [ ] Response includes `X-Correlation-Id` header
- [ ] Logs are structured JSON on console
- [ ] Audit log entries include correlation ID
