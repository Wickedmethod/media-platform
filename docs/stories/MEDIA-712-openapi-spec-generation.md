# MEDIA-712: API — OpenAPI Spec Generation

## Story

**Epic:** MEDIA-BE-MULTI — Multi-User Backend Support  
**Priority:** High  
**Effort:** 1 point  
**Status:** ⏳ Planned  
**Depends on:** None (existing API)

---

## Summary

Generate an OpenAPI 3.1 specification from the existing minimal API endpoints using `Microsoft.AspNetCore.OpenApi`. This spec is consumed by **Orval** in the frontend project to auto-generate a fully typed TypeScript API client.

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
    input: '../../openapi.json',  // relative path to spec
    output: {
      target: './src/generated/api.ts',
      client: 'vue-query',
      mode: 'tags-split',
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
