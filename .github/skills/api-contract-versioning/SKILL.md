---
name: api-contract-versioning
description: 'Manage API contract changes between backend (Nexus/CaddyAdmin) and frontend (NexusFrontend/CaddyAdmin.Web). Handle OpenAPI spec updates, Orval regeneration, breaking vs non-breaking changes, and deprecation. Use when modifying API endpoints, DTOs, or response shapes.'
argument-hint: Endpoint, DTO change, or API compatibility concern
---

# API Contract Versioning

## When to Use

- Adding, modifying, or removing API endpoints.
- Changing request/response DTO shapes.
- Renaming fields or changing types in API responses.
- Deprecating endpoints or fields.
- Regenerating frontend API clients after backend changes.
- Reviewing if a change is breaking or non-breaking.

## Breaking vs Non-Breaking Changes

### Non-Breaking (Safe)

These changes are backward-compatible and can be deployed independently:

| Change | Why Safe |
|--------|----------|
| Add new optional field to response | Existing clients ignore unknown fields |
| Add new endpoint | No existing code calls it |
| Add new optional query parameter | Existing calls still work |
| Add new enum value (additive) | Existing code handles known values |
| Widen accepted input type | Stricter inputs still accepted |

### Breaking (Requires Coordination)

These changes require synchronized frontend+backend deployment:

| Change | Why Breaking |
|--------|-------------|
| Remove field from response | Frontend reads it → runtime error |
| Rename field in response | Frontend has wrong key |
| Change field type (string → number) | Frontend type mismatch |
| Remove endpoint | Frontend calls fail |
| Remove enum value | Frontend may send removed value |
| Make optional field required | Existing requests missing it fail |
| Change URL path | Frontend route mismatch |

## Workflow: Non-Breaking Change

1. **Backend**: Modify handler/DTO, add field/endpoint.
2. **Backend**: Update/regenerate OpenAPI spec.
3. **Backend**: Deploy or merge PR.
4. **Frontend**: Run `pnpm orval` to regenerate API client.
5. **Frontend**: Adopt new field/endpoint where needed.
6. **Frontend**: Deploy.

Order doesn't matter — both sides work independently.

## Workflow: Breaking Change

1. **Plan**: Document what's changing and impact on frontend.
2. **Backend**: Implement change with temporary backward compatibility if possible.
3. **Backend**: Regenerate and export OpenAPI spec.
4. **Frontend**: Run `pnpm orval` to regenerate types.
5. **Frontend**: Fix all type errors from regenerated client.
6. **Frontend**: Test affected views and flows manually.
7. **Deploy**: Coordinate simultaneous deployment.

## OpenAPI Spec Regeneration

### Nexus Backend (tsoa)

```bash
# Regenerate OpenAPI spec from controllers
pnpm run generate:openapi

# Output: openapi.json at project root or generated/ folder
```

### CaddyAdmin.Api (.NET)

```bash
# Spec generated automatically from controllers at startup
# Export: GET /swagger/v1/swagger.json
```

### Frontend Client Regeneration

```bash
# Both CaddyAdmin.Web and NexusFrontend use Orval
pnpm orval

# Regenerates:
# - API hooks (TanStack Query wrappers)
# - Request/response types (from schemas)
# - API client functions
```

## Deprecation Strategy

When removing a field or endpoint, use a two-phase approach:

### Phase 1: Deprecate (Mark as deprecated, keep working)

```typescript
// Backend: Add @deprecated to OpenAPI
/**
 * @deprecated Use `brightness` instead. Will be removed in v2.
 */
export interface DeviceDto {
  level?: number;     // @deprecated
  brightness: number; // new field
}
```

### Phase 2: Remove (After all consumers updated)

1. Verify no frontend code references the deprecated field.
2. Remove from backend DTO.
3. Regenerate OpenAPI + Orval client.
4. Deploy.

## Orval-Generated Types Rules

- **Generated types are source of truth** — never create aliases.
- **Import directly** from `src/shared/api/schemas/`.
- After `pnpm orval`, check for new TypeScript errors immediately.
- Fix all type errors before committing.

```typescript
// ✅ Correct: Use generated type directly
import type { DeviceDto } from '@/shared/api/schemas';

// ❌ Prohibited: Alias that hides the source
type Device = DeviceDto;
```

## Pre-Change Checklist

- [ ] Classified change as breaking or non-breaking.
- [ ] If breaking: documented migration path for frontend.
- [ ] Backend changes implemented and tested.
- [ ] OpenAPI spec regenerated.
- [ ] Frontend `pnpm orval` run successfully.
- [ ] All TypeScript errors resolved.
- [ ] Frontend views tested manually (if breaking).
- [ ] Coordinated deployment if breaking.

## Guardrails

- ✅ Always: Classify every API change as breaking or non-breaking.
- ✅ Always: Regenerate OpenAPI spec after backend DTO/endpoint changes.
- ✅ Always: Run `pnpm orval` on frontend after backend spec changes.
- ⚠️ Ask First: Removing fields, changing types, renaming endpoints.
- 🚫 Never: Deploy breaking backend changes without updating frontend.
- 🚫 Never: Manually edit generated Orval files.
- 🚫 Never: Create type aliases for generated DTO types.
