# MEDIA-712: API — OpenAPI Spec Generation & SSE Event Contract

## Story

**Epic:** MEDIA-BE-MULTI — Multi-User Backend Support  
**Priority:** High  
**Effort:** 2 points  
**Status:** ✅ Done  
**Depends on:** None (existing API)  
**Absorbs:** MEDIA-717 (SSE Event Contract Specification)

---

## Summary

Generate an OpenAPI 3.1 specification from the existing minimal API endpoints using `Microsoft.AspNetCore.OpenApi`. This spec is consumed by **Orval** in the frontend project to auto-generate a fully typed TypeScript API client.

Additionally, define a formal **SSE event contract** documenting all event types, their payload schemas, and delivery semantics. The SSE contract is included in the OpenAPI spec as webhook definitions.

---

## Why

Without an OpenAPI spec, the frontend would need hand-written API types and fetch calls. Using Orval + generated spec:

- **Zero hand-written API types** — all DTOs generated
- **TanStack Query hooks generated** — `useGetQueue()`, `useAddToQueue()`, etc.
- **Automatic re-sync** — regenerate when API changes
- **Contract-verified** — types match real API

---

## Implementation

### 1. NuGet Packages

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
<PackageReference Include="Scalar.AspNetCore" Version="2.3.0" />
```

### 2. Program.cs Registration

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, ctx, ct) =>
    {
        doc.Info = new()
        {
            Title = "Media Platform API",
            Version = "v1",
            Description = "Queue-based media playback controller"
        };
        return Task.CompletedTask;
    });
});

// After app.Build()
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
```

### 3. Endpoint Metadata

Add `.WithName()`, `.WithTags()`, `.Produces<T>()` to all endpoints:

```csharp
group.MapPost("/add", handler)
    .WithName("AddToQueue")
    .WithTags("Queue")
    .Produces<QueueItemResponse>(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .WithDescription("Add a media item to the queue");
```

### 4. Export Spec to File (build step)

```xml
<!-- MediaPlatform.Api.csproj -->
<Target Name="GenerateOpenApiSpec" AfterTargets="Build" Condition="'$(Configuration)' == 'Debug'">
  <Exec Command="dotnet run --project $(MSBuildProjectFullPath) -- --export-openapi openapi.json" />
</Target>
```

Or use the `Microsoft.Extensions.ApiDescription.Server` package for automatic swagger.json on build.

### 5. Scalar UI

Available at `/scalar/v1` in dev mode — interactive API explorer.

---

## Output

```
projects/media-platform/openapi.json   ← checked into git
```

This file is consumed by the frontend's `orval.config.ts`:

```typescript
export default defineConfig({
  mediaPlatform: {
    input: "../../openapi.json", // relative path to spec
    output: {
      target: "./src/generated/api.ts",
      client: "vue-query",
      mode: "tags-split",
    },
  },
});
```

---

## Tasks

- [ ] Add `Microsoft.AspNetCore.OpenApi` and `Scalar.AspNetCore` packages
- [ ] Register OpenAPI in `Program.cs`
- [ ] Add `.WithName()` / `.WithTags()` / `.Produces<T>()` to all endpoints
- [ ] Add request/response XML docs for OpenAPI descriptions
- [ ] Export `openapi.json` to project root on build
- [ ] Verify spec with Scalar UI
- [ ] Validate Orval can generate client from spec

---

## Acceptance Criteria

- [ ] `/openapi/v1.json` returns valid OpenAPI 3.1 document in dev mode
- [ ] All endpoints have names, tags, and response types documented
- [ ] `openapi.json` file is generated and checked into git
- [ ] Scalar UI is accessible at `/scalar/v1`
- [ ] Orval generates client successfully from the spec

---

## Notes

- OpenAPI spec is **not exposed in production** (dev mode only via Scalar)
- The exported `openapi.json` file is the contract between API and frontend

---

## SSE Event Contract

### Event Types

| Event Name         | Payload                                                  | Trigger                  | Consumers       |
| ------------------ | -------------------------------------------------------- | ------------------------ | --------------- |
| `state-changed`    | `{ state: PlayerState }`                                 | Play, pause, stop, skip  | SPA, TV         |
| `track-changed`    | `{ item: QueueItem, position: number }`                  | New track starts playing | SPA, TV         |
| `position-updated` | `{ position: number, duration: number }`                 | Periodic (every 5s)      | SPA, TV         |
| `queue-updated`    | `{ action: 'add'\|'remove'\|'reorder', count: number }`  | Queue mutation           | SPA, TV         |
| `item-added`       | `{ id, title, url, addedByUserId, addedByName }`         | New item added to queue  | SPA (toast), TV |
| `kill-switch`      | `{ active: boolean }`                                    | Kill switch toggled      | SPA, TV         |
| `playback-error`   | `{ error: string, videoId: string, retryCount: number }` | Player error             | SPA, TV         |
| `heartbeat`        | `{}`                                                     | Every 30s (keepalive)    | SPA, TV         |
| `policy-changed`   | `{ action: 'add'\|'remove'\|'toggle' }`                  | Policy mutation          | SPA (admin)     |

### Payload Schema (TypeScript)

```typescript
type SSEEvent =
  | {
      type: "state-changed";
      data: { state: "Playing" | "Paused" | "Stopped" | "Idle" };
    }
  | { type: "track-changed"; data: { item: QueueItem; position: number } }
  | { type: "position-updated"; data: { position: number; duration: number } }
  | {
      type: "queue-updated";
      data: { action: "add" | "remove" | "reorder"; count: number };
    }
  | {
      type: "item-added";
      data: {
        id: string;
        title: string;
        url: string;
        addedByUserId: string;
        addedByName: string;
      };
    }
  | { type: "kill-switch"; data: { active: boolean } }
  | {
      type: "playback-error";
      data: { error: string; videoId: string; retryCount: number };
    }
  | { type: "heartbeat"; data: Record<string, never> }
  | { type: "policy-changed"; data: { action: "add" | "remove" | "toggle" } };
```

### Delivery Semantics

- **Transport:** Server-Sent Events (SSE) via `GET /events`
- **Content-Type:** `text/event-stream`
- **Retry:** Default 3000ms (server-sent `retry:` field)
- **Heartbeat:** Server sends `heartbeat` every 30s to keep connection alive
- **Ordering:** Events delivered in server-side order (Redis pub/sub)
- **Deduplication:** Clients should ignore duplicate events within a 1s window (same type + same data hash)

### Backend Implementation

The API must emit `item-added` events when items are added to the queue. This **enriches** the existing `queue-updated` event with user identity:

```csharp
// In QueueEndpoints — after adding item
await sseService.BroadcastAsync("item-added", new {
    id = item.Id,
    title = item.Title,
    url = item.Url,
    addedByUserId = userId,
    addedByName = userName,
});
await sseService.BroadcastAsync("queue-updated", new { action = "add", count = queue.Count });
```

### Tasks (SSE Contract)

- [ ] Define all SSE event types as C# records
- [ ] Document SSE events in OpenAPI spec as webhook definitions
- [ ] Implement `item-added` event emission in queue add endpoint
- [ ] Add server-side `heartbeat` event every 30s
- [ ] Export SSE event TypeScript types to `src/shared/types/sse.ts`
