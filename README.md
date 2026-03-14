# Media Platform

Self-hosted YouTube media control platform for homelab. Queue, play, and control YouTube video playback on a TV connected to a Raspberry Pi — from phone, browser, or TV remote.

## Architecture

- **C# .NET 10 API** — Queue management, player control, state reporting, realtime updates
- **Redis 7** — Durable queue state and event channels
- **Raspberry Pi Worker** (separate) — mpv + yt-dlp playback execution

## Quick Start

```bash
# Build and run
dotnet build MediaPlatform.slnx
dotnet run --project src/MediaPlatform.Api

# Or with Docker
docker compose up -d
```

## Development

```bash
# Restore
dotnet restore MediaPlatform.slnx

# Build
dotnet build MediaPlatform.slnx

# Test
dotnet test MediaPlatform.slnx
```

## Project Structure

```
src/
├── MediaPlatform.Domain/          # Core domain (entities, enums, value objects)
├── MediaPlatform.Application/     # Use cases (commands, queries, abstractions)
├── MediaPlatform.Infrastructure/  # Redis, realtime, external integrations
└── MediaPlatform.Api/             # HTTP endpoints + host
tests/
├── MediaPlatform.UnitTests/
└── MediaPlatform.IntegrationTests/
```

## Epics & Stories

See [docs/epics/](docs/epics/) for planning documents.
