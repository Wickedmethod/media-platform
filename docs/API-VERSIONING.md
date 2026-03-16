# API Versioning Strategy

## Scheme: URL Path Versioning

All API routes use a version prefix in the URL:

```
/api/v1/queue
/api/v1/player/play
/api/v1/admin/players
```

## Unversioned Endpoints

These endpoints have a stable contract and are not versioned:

| Endpoint      | Reason                                       |
| ------------- | -------------------------------------------- |
| `/api/events` | SSE stream — event schema includes `version` |
| `/health/*`   | Standard health checks, no API contract      |
| `/metrics`    | Prometheus scrape target, stable format      |
| `/openapi/*`  | OpenAPI spec generation                      |

## Current Versions

| Version | Status | Route Prefix |
| ------- | ------ | ------------ |
| v1      | Active | `/api/v1/`   |

## Deprecation Policy

| Stage          | Duration     | Action                                                   |
| -------------- | ------------ | -------------------------------------------------------- |
| **Active**     | Current      | Default version, fully supported                         |
| **Deprecated** | 3 months     | `Sunset` header, warning in docs, logs deprecation usage |
| **Removed**    | After sunset | Returns 410 Gone with migration link                     |

## Response Headers

Every versioned response includes:

```
api-supported-versions: 1.0
```

Deprecated versions additionally include:

```
api-deprecated-versions: 1.0
Sunset: Sat, 01 Mar 2027 00:00:00 GMT
```

## Frontend Integration

- Orval generates client code from the versioned OpenAPI spec (`/openapi/v1.json`)
- Generated URLs include the full path (e.g., `/api/v1/queue`)
- The `apiClient` mutator uses URLs as-is (no base URL prepending)
- Manual fetch calls use `config.apiBaseUrl` which defaults to `/api/v1`
- SSE uses `config.apiEventsUrl` which defaults to `/api/events` (unversioned)

## Adding a New Version

1. Add a new API version to the version set in `Program.cs`
2. Create a v2 endpoint group mapping to the new version
3. Add the v2 routes with breaking changes
4. Mark v1 routes as deprecated with `[Deprecated]` or version-set configuration
5. Update the OpenAPI spec generation for v2
6. Regenerate Orval client from v2 spec
7. Document breaking changes in a migration guide below

## Migration Guide Template

```markdown
## Migrating from v1 to v2

### Breaking Changes

- `POST /api/v1/queue/add` → `POST /api/v2/queue` (simplified path)
- `QueueItemResponse.videoId` → `QueueItemResponse.mediaId` (renamed field)

### New Features in v2

- Batch add support: `POST /api/v2/queue/batch`

### Timeline

- v1 deprecated: YYYY-MM-DD
- v1 removed: YYYY-MM-DD (3 months after deprecation)
```
