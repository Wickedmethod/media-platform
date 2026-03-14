# MEDIA Platform Architecture Decisions

Date: 2026-03-12
Status: Accepted

## Chosen Scope and Stack

- MVP scope: Single-room first (one Raspberry Pi and one TV)
- Backend service language: C# (.NET)
- Raspberry Pi player worker language: TypeScript (Node.js)
- Realtime model: WebSocket or SSE from API
- Queue source of truth: Redis with persistence enabled
- Auth boundary for v1: Auth moved to later roadmap
- Deployment model: Docker container on apps VM
- Observability baseline: Structured logs and health endpoints

## High-Level Architecture

Client devices (phone/browser/TV remote) connect through Caddy to API service.

API service responsibilities:

- Queue management and now-playing state API
- Player command API (play, pause, skip)
- WebSocket or SSE status updates
- Redis persistence integration

Raspberry Pi worker responsibilities:

- Consume queue and execute playback via mpv and yt-dlp
- Report player state and errors to API
- Run as resilient service with restart behavior

Redis responsibilities:

- Durable queue state
- Now-playing state snapshot
- Optional event channels

## Service Boundaries

- C# API is the command and query boundary for clients
- Node.js worker is the playback execution boundary
- Redis is state store, not business rules owner

## Clean Architecture Requirements

This platform follows Clean Architecture for both API and worker components.

### Layering

- Domain layer:
  - Entities, value objects, domain enums, domain rules, domain errors
  - No framework or infrastructure dependencies
- Application layer:
  - Use cases, commands, queries, handlers, orchestration
  - Depends on domain abstractions only
- Infrastructure layer:
  - Redis access, HTTP clients, mpv and yt-dlp integrations, external adapters
  - Implements application or domain interfaces
- Interface layer:
  - REST endpoints, realtime gateway (WebSocket or SSE), input/output DTOs
  - No business logic

### Dependency Rule

- Dependencies point inward only.
- Domain depends on nothing.
- Application can depend on domain.
- Infrastructure can depend on application and domain.
- Interface layer can depend on application contracts, not concrete infrastructure classes.

### Architectural Guardrails

- Thin controllers/endpoints: map request to command or query and return result only.
- Business rules stay in use cases and domain rules, never in controllers or Redis scripts.
- Queue state machine and transitions are centralized in one application service.
- All category-like values use enums (state, command type, event type, error type).
- Public contracts are versioned and backward-compatible for additive changes.
- Every critical path has tests:
  - Unit tests for domain rules and handlers
  - Integration tests for Redis and worker/API boundaries

### Worker Alignment

- Node.js worker mirrors the same boundaries:
  - Playback domain logic separate from mpv process integration
  - Adapters wrap external commands and I/O
  - Command handling remains deterministic and idempotent

## Architecture Compliance Checklist

Use this checklist for each PR touching media platform services.

- [ ] Layer boundaries are respected (domain, application, infrastructure, interface)
- [ ] No business logic in controllers/endpoints/realtime gateway
- [ ] No infrastructure dependencies imported into domain layer
- [ ] Queue state transitions go through the centralized state machine
- [ ] Categorical values use enums (no magic strings)
- [ ] Contracts remain backward-compatible or explicitly versioned
- [ ] Secrets are not hardcoded and are loaded from secure configuration
- [ ] Error handling is explicit with domain or application-specific error types
- [ ] Unit tests exist for new or changed domain/application logic
- [ ] Integration tests cover changed infrastructure boundaries (Redis, worker/API, realtime)
- [ ] Structured logs are emitted for command handling and failure paths
- [ ] Health endpoints remain valid after the change

## Security and Risk Notes

- Since auth is deferred in v1, network boundaries must be restricted:
  - Place API behind trusted network/VPN or internal ingress rules
  - Disable public write endpoints unless explicitly required
- Secrets must still use Vault when OAuth features are enabled later
- Keycloak should protect app endpoints and user roles, but YouTube delegated access still requires Google OAuth consent.
- Google access and refresh tokens must be managed outside Keycloak and stored in Vault.

## Immediate Implementation Sequence

1. Implement MEDIA-002 and MEDIA-003 core with Redis persistence
2. Add MEDIA-601 and MEDIA-607 for deterministic state and idempotency
3. Add MEDIA-603 for health endpoints and watchdog behavior
4. Add realtime updates via WebSocket or SSE
5. Revisit MEDIA-604 auth controls in next milestone

## Open Decisions for Next Checkpoint

- WebSocket versus SSE final choice
- Exact Redis schema for queue, now-playing, and events
- API contract format and versioning strategy
