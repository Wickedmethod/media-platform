# MEDIA-713: Guest Access Model — TV Anonymous + SPA Authenticated

## Story

**Epic:** MEDIA-BE-MULTI — Multi-User Backend Support  
**Priority:** High  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-604 (JWT auth), MEDIA-622 (Worker Key), MEDIA-711 (added-by tracking)  \n**Absorbs:** MEDIA-727 (SSE Authorization & Guest Access Policy)

---

## Summary

Define and implement the dual access model: **TV is guest-friendly** (no login), **SPA requires Keycloak**. The API must support both authenticated JWT requests and anonymous TV requests using the X-Worker-Key, with proper CORS configuration for cross-origin access.

---

## Access Model

```
┌─────────────────────────────────┐
│           API Endpoints          │
├─────────────────────────────────┤
│                                 │
│  TV Frontend (Guest)            │
│  ├── Auth: X-Worker-Key header  │
│  ├── Identity: "TV" / "Guest"   │
│  ├── Can: search, add to queue  │
│  │   view queue, see now-playing│
│  └── Cannot: admin, policies,   │
│      delete others' items,      │
│      kill switch, audit log     │
│                                 │
│  SPA Frontend (Authenticated)   │
│  ├── Auth: Bearer JWT (Keycloak)│
│  ├── Identity: user ID + name   │
│  ├── media-user: add, delete    │
│  │   own, view queue, search    │
│  └── media-admin: everything    │
│                                 │
└─────────────────────────────────┘
```

---

## Endpoint Access Matrix

| Endpoint | TV (Worker Key) | User (JWT) | Admin (JWT) |
|----------|:-:|:-:|:-:|
| `GET /queue` | ✅ | ✅ | ✅ |
| `POST /queue/add` | ✅ (as "Guest") | ✅ (with user) | ✅ |
| `DELETE /queue/{id}` | ❌ | ✅ (own only) | ✅ (any) |
| `GET /now-playing` | ✅ | ✅ | ✅ |
| `GET /events` (SSE) | ✅ | ✅ | ✅ |
| `POST /player/play` | ❌ | ❌ | ✅ |
| `POST /player/pause` | ❌ | ❌ | ✅ |
| `POST /player/skip` | ❌ | ❌ | ✅ |
| `POST /player/stop` | ❌ | ❌ | ✅ |
| `POST /player/position` | ✅ (TV reports) | ❌ | ✅ |
| `POST /player/report-end` | ✅ (TV reports) | ❌ | ✅ |
| `GET /policies` | ❌ | ❌ | ✅ |
| `POST /policies` | ❌ | ❌ | ✅ |
| `GET /admin/*` | ❌ | ❌ | ✅ |
| `GET /analytics` | ❌ | ❌ | ✅ |

---

## TV Guest Adding — Identity

When the TV adds to the queue using X-Worker-Key:

```json
POST /queue/add
X-Worker-Key: <key>

{
  "url": "https://youtube.com/watch?v=...",
  "title": "Bohemian Rhapsody"
}
```

The API resolves identity:

```csharp
// If JWT present → extract user from claims
// Else if X-Worker-Key present → identity = "TV Guest"
var (userId, userName) = context.User.Identity?.IsAuthenticated == true
    ? (context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, 
       context.User.FindFirst("preferred_username")?.Value)
    : IsWorkerKeyValid(context)
        ? ("tv-guest", "TV")
        : throw new UnauthorizedAccessException();
```

Queue items from TV show as:
```json
{
  "addedByUserId": "tv-guest",
  "addedByName": "TV"
}
```

---

## Authorization Pipeline

```csharp
// Program.cs — Auth policy setup
builder.Services.AddAuthorization(options =>
{
    // Default: requires any valid auth (JWT or Worker Key)
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("Bearer", "WorkerKey")
        .RequireAuthenticatedUser()
        .Build();

    // Read-only: JWT users or Worker Key
    options.AddPolicy("ReadAccess", policy =>
        policy.AddAuthenticationSchemes("Bearer", "WorkerKey")
              .RequireAuthenticatedUser());

    // Queue add: JWT users or Worker Key
    options.AddPolicy("QueueAdd", policy =>
        policy.AddAuthenticationSchemes("Bearer", "WorkerKey")
              .RequireAuthenticatedUser());

    // Own items: JWT user who added the item
    options.AddPolicy("QueueOwner", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("Bearer"));

    // TV reporting: Worker Key only
    options.AddPolicy("WorkerOnly", policy =>
        policy.AddAuthenticationSchemes("WorkerKey")
              .RequireAuthenticatedUser());

    // Admin actions: JWT with media-admin role
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("Bearer")
              .RequireRole("media-admin"));
});
```

---

## CORS Configuration

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("MediaPlatform", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",           // Vite dev server
                "http://localhost:3000",            // Frontend production
                builder.Configuration["Cors:AllowedOrigins"] ?? ""
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();                   // Needed for SSE with cookies
    });

    // TV frontend served from same origin (wwwroot) — no CORS needed
    // But if TV is on a different host, add its origin too
});

// In pipeline
app.UseCors("MediaPlatform");
```

### When CORS is NOT Needed

| Scenario | CORS Required? |
|----------|:-:|
| TV served from API wwwroot (`/tv.html`) | ❌ Same origin |
| SPA served from nginx on same domain (via proxy) | ❌ Same origin (proxied) |
| SPA on `localhost:5173` during dev | ✅ Different port |
| SPA on separate container/domain | ✅ Different origin |

---

## SSE and CORS

Server-Sent Events (`EventSource`) don't support custom headers. For authenticated SSE:

```typescript
// Option A: Token as query param (less secure, but standard for SSE)
const eventSource = new EventSource(`/api/events?token=${authStore.token}`)

// Option B: Cookie-based auth (preferred)
// Keycloak sets httpOnly cookie, SSE sends it automatically with credentials
const eventSource = new EventSource('/api/events', { withCredentials: true })
```

For the TV frontend (Worker Key), SSE doesn't need auth — it's read-only state.

---

## SSE Authorization Policy (absorbed from MEDIA-727)

The SSE endpoint (`GET /events`) requires access control:

| Client | Auth Method | Event Access |
|--------|------------|--------------|
| TV (Worker Key) | `X-Worker-Key` query param | All shared events (state, track, queue, error) |
| SPA User (JWT) | Cookie or `?token=` param | All shared events |
| SPA Admin (JWT) | Cookie or `?token=` param | All events + policy-changed, anomaly alerts |
| Unauthenticated | None | ❌ Rejected (401) |

### SSE Auth Implementation

Since `EventSource` doesn't support custom headers, use query-param or cookie-based auth:

```csharp
// SSE endpoint with dual auth support
app.MapGet("/events", async (HttpContext ctx, ISseService sse) =>
{
    // Try cookie auth first, then query-param token, then worker key
    var (isAuthenticated, identity) = await ResolveSSEIdentity(ctx);
    if (!isAuthenticated) {
        ctx.Response.StatusCode = 401;
        return;
    }

    ctx.Response.ContentType = "text/event-stream";
    ctx.Response.Headers.CacheControl = "no-cache";

    // Filter events based on identity role
    var filter = identity.IsAdmin
        ? EventFilter.All
        : EventFilter.SharedOnly; // Excludes admin-only events

    await sse.StreamEventsAsync(ctx, filter, ctx.RequestAborted);
});
```

### Tasks (SSE Auth)

- [ ] Add `?token=` query param auth support for SSE
- [ ] Add `?worker-key=` query param auth support for SSE (TV)
- [ ] Implement event filtering (admin vs user vs TV)
- [ ] Reject unauthenticated SSE connections

---

## TV Rate Limiting

To prevent abuse, TV endpoints have stricter rate limits:

```csharp
// POST /queue/add with Worker Key: max 10 per minute (vs 30 for JWT users)
app.MapPost("/queue/add", handler)
    .RequireAuthorization("QueueAdd")
    .RequireRateLimiting("queue-add-tv"); // Lower limit for guest
```

---

## Tasks

### Backend
- [ ] Add "WorkerKey" authentication scheme (extend existing MEDIA-622)
- [ ] Define authorization policies: `ReadAccess`, `QueueAdd`, `QueueOwner`, `WorkerOnly`, `AdminOnly`
- [ ] Apply policies to all endpoints
- [ ] Resolve identity to "tv-guest" / "TV" for Worker Key requests
- [ ] Configure CORS for SPA origins (dev + prod)
- [ ] Handle SSE auth (query param token or cookie)
- [ ] Add TV-specific rate limits for `POST /queue/add`
- [ ] Write integration tests for guest vs authenticated access

### Frontend (SPA)
- [ ] Ensure Orval custom fetch sends Bearer token
- [ ] Handle 401 (redirect to Keycloak login)
- [ ] Handle 403 (show "Not authorized" toast)

### TV Frontend
- [ ] Ensure all API calls include `X-Worker-Key` header
- [ ] No Keycloak dependency in TV code

---

## Acceptance Criteria

- [ ] TV can view queue, add items, and receive SSE without Keycloak login
- [ ] TV items show "Added by: TV" in queue
- [ ] SPA requires Keycloak login to access any route
- [ ] SPA users can add items with their name tracked
- [ ] Admin endpoints return 403 for non-admin users and TV
- [ ] Player control endpoints (play/pause/skip/stop) return 403 for TV and regular users
- [ ] CORS allows SPA origin in dev and production
- [ ] SSE works for both TV (no auth) and SPA (with auth)
- [ ] TV rate limit is stricter than authenticated user limit
- [ ] TV cannot delete queue items or access admin features

---

## Notes

- The TV is a **trusted device** on the local network — Worker Key authorization is sufficient
- In v1, there's no IP restriction. If needed later, add middleware to restrict Worker Key to local network IPs
- The "tv-guest" user ID is a constant — all TV additions share the same identity
- If multiple TVs are added later (MEDIA-730 multi-room), each TV could have its own Worker Key and identity
