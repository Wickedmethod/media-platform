# Copilot Instructions for Media Platform Frontend

> **SHARED INFRASTRUCTURE — DO NOT DUPLICATE**
> Keycloak and Vault are shared services. Never add `keycloak` or `vault` services to any docker-compose.
> The frontend connects to Keycloak for auth. See `infrastructure/docs/SHARED-INFRASTRUCTURE.md`.

---

## Purpose and Scope

Media Platform Frontend is the user interface for the media-platform project. It provides queue management, playback control, admin functions, and a fullscreen TV kiosk view. Built as a Vue 3 SPA with a separate TV entry point sharing the same codebase.

---

## Tech Stack

| Tool               | Version | Purpose                         |
| ------------------ | ------- | ------------------------------- |
| Vue 3              | 3.5+    | UI framework                    |
| Vite               | 6+      | Build tool (multi-page)         |
| TypeScript         | 5.5+    | Type safety                     |
| TailwindCSS v4     | 4+      | Utility CSS (@tailwindcss/vite) |
| shadcn-vue         | latest  | Component library (new-york)    |
| Pinia              | 3+      | Client state                    |
| TanStack Vue Query | 5+      | Server state / caching          |
| Vue Router         | 4+      | Client-side routing (SPA only)  |
| Orval              | latest  | OpenAPI → typed API client      |
| keycloak-js        | 26+     | Auth (SPA only)                 |
| pnpm               | 10+     | Package manager                 |

---

## Architectural Foundation

### Feature-Based Organization

Code is organized by business domain under `src/features/`:

- `queue/` — Queue management views + components (SPA)
- `player/` — Playback controls (SPA)
- `admin/` — Admin-only views (SPA)
- `tv/` — TV kiosk views (TvPlayer, TvOverlay, TvIdle, TvSearch, TvError)
- `shared/` — Shared layout + navigation

### State Management

**Server State**: Flows through TanStack Query. Use Orval-generated hooks for API calls. Never use raw `fetch` for API endpoints.

**Client State**: Pinia stores for state that persists across components. Component `ref()` for local state.

**Real-time State**: SSE events update the player store. The `useSSE` composable manages the connection. TV and SPA both consume the same player store.

### Generated Types Are Source of Truth

Orval-generated types in `src/generated/` are authoritative. Do not create type aliases that rename DTO types. When the API changes, Orval regenerates types and TypeScript flags incompatibilities immediately.

---

## Multi-Page Architecture

The project builds two entry points from one codebase:

- **`index.html` → `src/main.ts`**: Full SPA with Keycloak auth, router, queue/admin/player features
- **`tv.html` → `src/tv.ts`**: Minimal TV kiosk — no auth, no router, just SSE + player + TV components

TV imports only what it needs. Tree-shaking removes SPA code from the TV bundle.

---

## Project Structure

```
src/
├── main.ts                  # SPA bootstrap
├── tv.ts                    # TV bootstrap (no auth)
├── App.vue                  # SPA root
├── TvApp.vue                # TV root (fullscreen)
├── env.d.ts                 # Vite env types
├── router/index.ts          # SPA routes
├── stores/
│   ├── auth.ts              # Keycloak state (SPA only)
│   └── player.ts            # Player state via SSE (shared)
├── composables/
│   ├── useSSE.ts            # SSE connection (shared)
│   ├── useApi.ts            # Auth-aware fetch (SPA)
│   └── useCEC.ts            # CEC bridge (TV only)
├── generated/               # Orval output
├── features/                # Domain features
└── shared/
    ├── components/ui/       # shadcn-vue
    ├── styles/main.css      # Tailwind v4 theme
    ├── lib/utils.ts         # Helpers
    └── types/               # Shared types
```

---

## Conventions

### Components

- Use `<script setup lang="ts">` for all components
- shadcn-vue (new-york style, slate base) for UI primitives
- Mobile-first responsive design
- Dark mode as default (`.dark` class on `<html>`)

### Styling

- TailwindCSS v4 with `@tailwindcss/vite` plugin (no `tailwind.config.ts`)
- CSS variables defined in `@theme` block in `main.css`
- Use `cn()` helper from `@shared/utils/cn` for conditional classes

### API

- All API calls through Orval-generated hooks or `apiFetch` from `useApi.ts`
- Proxy `/api` → `http://localhost:5000` in dev
- SSE endpoint at `/events` with worker-key query param for TV

### Auth

- SPA: keycloak-js initializes in `main.ts`, token stored in `useAuthStore`
- TV: No auth — uses worker-key query param for SSE access
- API calls include `Authorization: Bearer <token>` via `useApi.ts`

---

## Commands

```bash
pnpm dev          # Dev server on :5173
pnpm build        # Production build
pnpm preview      # Preview production build
pnpm generate:api # Regenerate API client from OpenAPI
```

---

## Mandatory MCP Tool Usage (CRITICAL)

### Context Mode — Always sandbox large output

- **ALWAYS** use `mcp_context-mode_ctx_batch_execute` instead of `run_in_terminal` when output may exceed 20 lines (builds, test runs, log tailing, grep results, file listings).
- Use `ctx_execute` for data analysis, file processing, and code analysis — only stdout enters context.
- Use `ctx_fetch_and_index` + `ctx_search` for web pages — raw HTML never enters context.

### Obsidian — Always tag notes

- **ALWAYS** use `mcp_obsidian_add-tags` after creating or editing any note to maintain consistent tagging and discoverability.
- Tag conventions: use project names (`nexus`, `caddyadmin`, `media-platform`), content type (`story`, `meeting`, `decision`, `bug`), and status (`in-progress`, `done`, `blocked`).
