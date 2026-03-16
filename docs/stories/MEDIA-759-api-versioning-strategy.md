# MEDIA-759: API Versioning Strategy

## Story

**Epic:** MEDIA-BE-MULTI — Multi-User Backend Support  
**Priority:** Medium  
**Effort:** 3 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-712 (OpenAPI Spec Generation)

---

## Summary

Define and implement an API versioning strategy so clients can adopt new endpoints without breaking existing integrations. Use URL path versioning (`/api/v1/...`) as the primary scheme — simple, visible in logs, and cache-friendly. Include a deprecation policy and migration guide for version bumps.

---

## Architecture

```
Client Request
    │
    ▼
/api/v1/queue/add  ──→  V1 Controller  ──→  Shared Service Layer
/api/v2/queue/add  ──→  V2 Controller  ──→  Shared Service Layer
    │
    ▼
Asp.Versioning middleware (resolves version from URL segment)
    │
    ▼
OpenAPI generates per-version spec: /swagger/v1/swagger.json
```

---

## Versioning Scheme

### URL Path Versioning (primary)

All API routes prefixed with version:

```
/api/v1/queue
/api/v1/player/play
/api/v1/events
```

### Why URL Path?

| Approach                       | Pros                                  | Cons                          |
| ------------------------------ | ------------------------------------- | ----------------------------- |
| **URL path** (`/api/v1/`)      | Visible, cacheable, easy to log/route | URL changes on version bump   |
| Header (`Api-Version: 1`)      | URL stays clean                       | Hidden, hard to debug         |
| Query param (`?api-version=1`) | Simple                                | Breaks caching, messy URLs    |
| Content negotiation            | RESTful purists like it               | Complex, poor tooling support |

URL path is the right choice for a homelab platform — simple, debuggable, and works with reverse proxies.

---

## Implementation

### 1. Install Asp.Versioning

```xml
<!-- MediaPlatform.Api.csproj -->
<PackageReference Include="Asp.Versioning.Http" Version="8.*" />
<PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" Version="8.*" />
```

### 2. Configure Versioning

```csharp
// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true; // Adds api-supported-versions header
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

### 3. Version Controllers

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/queue")]
public class QueueController : ControllerBase
{
    [HttpPost("add")]
    public async Task<ActionResult<QueueItemResponse>> Add(AddQueueItemRequest request) { ... }
}
```

### 4. Deprecation Header

When a version is deprecated, the response includes:

```
api-supported-versions: 1.0, 2.0
api-deprecated-versions: 1.0
Sunset: Sat, 01 Mar 2027 00:00:00 GMT
```

```csharp
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/queue")]
public class QueueController : ControllerBase { ... }
```

### 5. Per-Version OpenAPI Docs

```csharp
// Generate separate specs per version
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Media Platform API", Version = "v1" });
    options.SwaggerDoc("v2", new() { Title = "Media Platform API", Version = "v2" });
});
```

Orval generates clients from the versioned spec URL: `/swagger/v1/swagger.json`.

---

## Deprecation Policy

| Stage          | Duration     | Action                                                   |
| -------------- | ------------ | -------------------------------------------------------- |
| **Active**     | Current      | Default version, fully supported                         |
| **Deprecated** | 3 months     | `Sunset` header, warning in docs, logs deprecation usage |
| **Removed**    | After sunset | Returns 410 Gone with migration link                     |

### Migration Guide Template

```markdown
## Migrating from v1 to v2

### Breaking Changes

- `POST /api/v1/queue/add` → `POST /api/v2/queue` (simplified path)
- `QueueItemResponse.videoId` → `QueueItemResponse.mediaId` (renamed field)

### New Features in v2

- Batch add support: `POST /api/v2/queue/batch`
- Rich metadata in responses

### Timeline

- v1 deprecated: 2027-01-01
- v1 removed: 2027-04-01
```

---

## SSE & Non-Versioned Endpoints

Some endpoints stay unversioned:

| Endpoint    | Reason                                                              |
| ----------- | ------------------------------------------------------------------- |
| `/health/*` | Standard health checks, no API contract                             |
| `/metrics`  | Prometheus scrape target, stable format                             |
| `/events`   | SSE stream — version negotiated via `Accept` header or event schema |

SSE events include a `version` field in the payload for forward compatibility:

```json
{ "type": "queue-changed", "version": 1, "data": { ... } }
```

---

## Tasks

- [ ] Add `Asp.Versioning.Http` and `.ApiExplorer` NuGet packages
- [ ] Configure API versioning in `Program.cs` with URL path reader
- [ ] Add `[ApiVersion]` and versioned `[Route]` to all controllers
- [ ] Configure Swagger to generate per-version specs
- [ ] Update Orval config to point to versioned spec URL
- [ ] Document deprecation policy in `docs/API-VERSIONING.md`
- [ ] Add `Sunset` header middleware for deprecated versions
- [ ] Return 410 Gone for fully removed versions
- [ ] Write tests for version routing (v1 resolves, unknown version → 400)

---

## Acceptance Criteria

- [ ] All API routes use `/api/v1/` prefix
- [ ] `api-supported-versions` header present on every response
- [ ] OpenAPI spec generated per version at `/swagger/v1/swagger.json`
- [ ] Deprecated versions include `Sunset` header in responses
- [ ] Removed versions return 410 Gone with migration link
- [ ] SSE events include `version` field in payload
- [ ] Deprecation policy documented with timeline template
- [ ] Orval client generation works with versioned spec URL
