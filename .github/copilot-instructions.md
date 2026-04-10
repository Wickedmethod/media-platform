# Copilot Instructions for Media Platform

**Version: 2026-03-14**
**Target: .NET 10 Web API — Self-Hosted YouTube Media Control Platform**

This document defines architectural expectations, coding constraints, and behavioral rules for AI assistants and developers working on the **Media Platform** project.

---

## How to Use These Instructions (READ FIRST)

These instructions are meant to be followed automatically by Copilot/agents when working inside this subproject.

### When You're Working on the Media Platform

- Work in this folder: `projects/media-platform/`
- If you are chatting from the workspace root, explicitly say: "I'm working on Media Platform" so the agent applies the correct rules.

### Active MCP Servers (MANDATORY)

**You MUST use these MCP servers when working on this project.** Do not skip them.

| Server           | Purpose                                    | When to Use (REQUIRED)                                                                                                                                   |
| ---------------- | ------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **context7**     | Up-to-date library docs and code examples  | **ALWAYS** before implementing anything that depends on external library APIs (.NET, Redis, etc.)                                                        |
| **serena**       | Semantic code exploration + safe refactors | **ALWAYS** before reading large files or refactoring. Use for symbol search, references, targeted reads/edits                                            |
| **context-mode** | Context window optimization                | **ALWAYS** use `mcp_context-mode_ctx_batch_execute` instead of `run_in_terminal` when output may exceed 20 lines (builds, test runs, logs, grep results) |
| **obsidian**     | Obsidian vault note management             | **ALWAYS** use `mcp_obsidian_add-tags` after creating or editing any note to maintain consistent tagging                                                 |

**MANDATORY Rules:**

1. **Before writing code that uses an external library** → Call `context7` to get current documentation
2. **Before reading a file > 100 lines** → Use `serena` symbol tools instead
3. **Before refactoring** → Use `serena` to find all references first

❌ **Do NOT** guess library APIs — use `context7`
❌ **Do NOT** read entire files when `serena` symbols suffice

---

## Purpose & Scope

Media Platform is a **self-hosted YouTube media control system** for a Proxmox homelab. It enables users to queue, play, and control YouTube video playback on a TV connected to a Raspberry Pi, from any phone, browser, or TV remote.

The platform:

- Manages a playback queue with deterministic state transitions
- Controls playback on a Raspberry Pi player node (mpv + yt-dlp)
- Provides REST API and realtime updates (WebSocket/SSE)
- Integrates with Redis for durable queue and state management
- Will support YouTube API actions (likes, playlists) via OAuth in later milestones

### What This Is NOT

- NOT the Raspberry Pi player worker (that's a separate TypeScript/Node.js component)
- NOT a video streaming server (mpv + yt-dlp handle playback locally)
- NOT a general media library (YouTube content only for v1)
- NOT a smart home controller (Nexus handles that)
- This is the **API and queue orchestration layer** of the media platform

---

## Tech Stack

- **.NET 10** Web API (minimal APIs)
- **StackExchange.Redis** (Redis client for queue and state)
- **Redis 7** (durable queue, now-playing state, event channels)
- **xUnit v3** (testing framework)
- **NSubstitute** (mocking)
- **FluentAssertions** (test assertions)

### Future Additions (Not Yet)

- WebSocket or SSE for realtime updates
- Keycloak authentication (deferred to later milestone)
- YouTube Data API v3 (OAuth, likes, playlists)
- VaultSharp (secret management for OAuth tokens)

---

## Critical Constraints

### Queue State Machine (CRITICAL)

All queue state transitions MUST go through a centralized state machine. Never mutate queue state directly from controllers or endpoints.

Valid states: `Empty`, `Playing`, `Paused`, `Buffering`, `Error`, `Stopped`

State transitions must be:

- **Deterministic** — same input always produces same output
- **Centralized** — one service owns all transitions
- **Logged** — every transition emits a structured log event

### Idempotent Player Commands

All player commands (play, pause, skip) MUST be idempotent. Sending the same command twice must produce the same result without side effects.

### Security (Even Without Auth in v1)

Since auth is deferred:

- **Network boundaries must be restricted** — API behind trusted network/VPN only
- **No public write endpoints** unless explicitly required
- **Never hardcode secrets** — use environment variables or Vault
- **Validate all input** — especially queue item URLs and identifiers
- **Only allow YouTube URLs** — validate URL format before accepting into queue

### Input Validation

```csharp
// MANDATORY: Validate YouTube URLs before accepting
private static bool IsValidYouTubeUrl(string url) =>
    Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
    (uri.Host is "www.youtube.com" or "youtube.com" or "youtu.be" or "m.youtube.com");
```

---

## Architecture

```
media-platform/
├── src/
│   ├── MediaPlatform.Domain/          # Core domain
│   │   ├── Entities/                  # QueueItem, NowPlaying
│   │   ├── Enums/                     # PlayerState, QueueItemStatus, CommandType
│   │   ├── ValueObjects/              # VideoUrl, QueuePosition
│   │   └── Errors/                    # DomainError types
│   │
│   ├── MediaPlatform.Application/     # Use cases
│   │   ├── Commands/                  # AddToQueue, RemoveFromQueue, PlayerCommand
│   │   ├── Queries/                   # GetQueue, GetNowPlaying
│   │   └── Abstractions/             # IQueueRepository, IPlayerClient, IEventPublisher
│   │
│   ├── MediaPlatform.Infrastructure/  # External integrations
│   │   ├── Redis/                     # Queue repository, state store
│   │   └── Realtime/                  # WebSocket/SSE publisher (future)
│   │
│   └── MediaPlatform.Api/            # HTTP interface
│       ├── Endpoints/                 # Minimal API endpoint groups
│       ├── Program.cs                 # DI setup + host builder
│       └── appsettings.json
│
├── tests/
│   ├── MediaPlatform.UnitTests/       # Domain + Application tests
│   └── MediaPlatform.IntegrationTests/ # Redis + API integration tests
│
├── docs/
│   └── epics/                         # Planning epics and stories
│       └── stories/
│
├── MediaPlatform.slnx                 # Solution file
├── Directory.Build.props              # Shared build properties
├── Dockerfile                         # Multi-stage Alpine build
├── docker-compose.yml                 # API + Redis
├── global.json                        # SDK version (10.0.104)
└── NuGet.config                       # Package sources
```

### Dependency Flow

```
Program.cs (Host)
    │
    └── Minimal API Endpoints
            │
            ├── Queue Endpoints     → Application Commands/Queries
            ├── Player Endpoints    → Application Commands
            └── Status Endpoints    → Application Queries
                    │
                    ├── IQueueRepository (Redis)      → Queue state
                    ├── IPlayerClient (HTTP/WS)       → Pi worker commands
                    └── IEventPublisher (WebSocket)    → Realtime updates
```

### Layer Rules

- `Domain` has NO dependencies on other layers or frameworks
- `Application` depends only on `Domain` abstractions
- `Infrastructure` implements `Application` interfaces, depends on Domain + Application
- `Api` wires everything via DI, maps requests to commands/queries — NO business logic in endpoints

---

## Redis Schema

Follow these key conventions:

| Key Pattern         | Type           | Purpose                |
| ------------------- | -------------- | ---------------------- |
| `media:queue`       | List           | Ordered playback queue |
| `media:now-playing` | Hash           | Current playback state |
| `media:history`     | List           | Recently played items  |
| `media:events`      | Stream/Channel | State change events    |

All Redis operations must go through `IQueueRepository` — never access Redis directly from endpoints.

---

## API Endpoints

| Method   | Path            | Purpose                    |
| -------- | --------------- | -------------------------- |
| `POST`   | `/queue/add`    | Add video to queue         |
| `DELETE` | `/queue/{id}`   | Remove from queue          |
| `GET`    | `/queue`        | Get current queue          |
| `POST`   | `/player/play`  | Start/resume playback      |
| `POST`   | `/player/pause` | Pause playback             |
| `POST`   | `/player/skip`  | Skip to next in queue      |
| `GET`    | `/now-playing`  | Get current playback state |
| `GET`    | `/health`       | Health check               |

### Endpoint Rules

- Thin endpoints: map request → command/query → return result
- No business logic in endpoint methods
- Return appropriate HTTP status codes (201 for created, 404 for not found, 409 for conflicts)
- All responses use consistent DTO shapes

---

## Enum Usage (MANDATORY)

All categorical values MUST use C# enums. Never use magic strings.

```csharp
public enum PlayerState { Idle, Playing, Paused, Buffering, Error, Stopped }
public enum CommandType { Play, Pause, Skip, Stop }
public enum QueueItemStatus { Pending, Playing, Played, Failed, Removed }
```

---

## File Size Limits

- Max **150 lines** for services/classes
- Max **100 lines** for utilities and models
- **One class per file**
- Split files when they exceed limits

---

## No Analyzer Disable Comments

Never disable Roslyn analyzers. If code violates a rule, fix the code.

---

## Error Handling

| Scenario                 | Behavior                                   |
| ------------------------ | ------------------------------------------ |
| Invalid YouTube URL      | Return 400 with validation error           |
| Queue item not found     | Return 404                                 |
| Player unreachable       | Return 503, log error, retry with backoff  |
| Redis connection lost    | Return 503, health check reports unhealthy |
| Duplicate queue add      | Idempotent — return existing item          |
| State transition invalid | Return 409 Conflict with current state     |

---

## Testing

- **xUnit v3** (package: `xunit.v3`, requires `using Xunit;`)
- **NSubstitute** for mocking interfaces
- **FluentAssertions** for readable assertions
- xUnit v3 enforces `CancellationToken` via analyzer rule `xUnit1051` — use `TestContext.Current.CancellationToken`
- Test files live in dedicated `tests/` projects, never alongside source
- Test naming: `MethodName_Scenario_ExpectedResult`

### What to Test

- Queue state machine transitions (all valid + invalid paths)
- Command idempotency
- URL validation
- Endpoint request/response mapping
- Redis repository (integration tests with real Redis)

---

## Quality Expectations

### Before Any Commit

- All tests pass: `dotnet test MediaPlatform.slnx`
- Build succeeds with zero warnings: `dotnet build MediaPlatform.slnx`
- No hardcoded secrets
- No analyzer disable comments
- Each file defines at most one class and stays under 150 lines
- Queue state transitions go through centralized state machine
- All categorical values use enums

---

## Raspberry Pi Worker (Separate Component)

The player worker runs on the Raspberry Pi as a TypeScript/Node.js service. It is NOT part of this .NET solution.

Communication between API and worker:

- API sends commands to worker via HTTP or WebSocket
- Worker reports state back to API
- Worker executes playback via mpv + yt-dlp

The worker will be developed separately. This API must define clear contracts for the worker to implement against.

---

## Infrastructure Integration

| Service  | Role                      | How                                          |
| -------- | ------------------------- | -------------------------------------------- |
| Redis    | Queue state + now-playing | StackExchange.Redis via `docker-compose.yml` |
| Caddy    | Reverse proxy             | External (infrastructure stack)              |
| Keycloak | Auth (future)             | External network `keycloak-public`           |
| Vault    | Secret storage (future)   | VaultSharp + `.vault-env`                    |

---

_Last updated: 2026-03-14_
