# MEDIA-700: Admin Frontend — Project Setup

## Story

**Epic:** MEDIA-FE-ADMIN — Admin & User Frontend  
**Priority:** Critical (foundation for all other frontend stories)  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** None

---

## Summary

Create a new Vue 3 SPA project for the media-platform admin/user frontend. This is the primary control interface used from phone/desktop to manage queue, playback, policies, and admin functions.

The project lives at `projects/media-platform/frontend/` and follows the same tech stack as `nexus-frontend` and `CaddyAdmin.web`.

---

## Tech Stack

| Tool | Version | Purpose |
|------|---------|---------|
| Vue 3 | 3.5+ | UI framework |
| Vite | 6+ | Build tool |
| TypeScript | 5.5+ | Type safety |
| TailwindCSS v4 | 4+ | Utility CSS |
| shadcn-vue (new-york) | latest | Component library |
| Pinia | 3+ | State management |
| TanStack Vue Query | 5+ | Server state / caching |
| Vue Router | 4+ | Client-side routing |
| Orval | latest | OpenAPI → generated API client |
| keycloak-js | 26+ | Auth |
| pnpm | 10+ | Package manager |

---

## Project Structure

```
projects/media-platform/frontend/
├── index.html
├── package.json
├── pnpm-lock.yaml
├── vite.config.ts
├── tsconfig.json
├── tsconfig.node.json
├── tailwind.config.ts
├── components.json              # shadcn-vue config
├── orval.config.ts
├── Dockerfile
├── .env.example
├── .github/
│   └── copilot-instructions.md
├── public/
│   └── icons/                   # PWA icons
└── src/
    ├── main.ts
    ├── App.vue
    ├── env.d.ts
    ├── router/
    │   └── index.ts
    ├── stores/
    │   ├── auth.ts              # Keycloak auth state
    │   └── player.ts            # Real-time player state via SSE
    ├── composables/
    │   ├── useSSE.ts            # SSE connection manager
    │   └── useApi.ts            # Base API fetch with auth
    ├── generated/               # Orval output
    ├── features/
    │   ├── queue/               # Queue views + components
    │   ├── player/              # Player controls
    │   ├── admin/               # Admin-only views
    │   └── shared/              # Layout, nav, etc.
    └── shared/
        ├── components/
        │   └── ui/              # shadcn-vue components
        ├── styles/
        │   └── main.css         # Tailwind base
        ├── lib/
        │   └── utils.ts         # cn() helper etc.
        └── types/
```

---

## Tasks

- [ ] `pnpm create vite frontend --template vue-ts`
- [ ] Install dependencies (tailwind, shadcn-vue, pinia, tanstack-query, vue-router, keycloak-js, orval)
- [ ] Configure Vite with proxy to `http://localhost:5000`
- [ ] Configure shadcn-vue (new-york style, slate base color)
- [ ] Setup TailwindCSS with CSS variables
- [ ] Create base layout with mobile-first nav
- [ ] Setup Vue Router with placeholder routes
- [ ] Setup Pinia with auth + player stores
- [ ] Configure Orval to generate from API's OpenAPI spec
- [ ] Add PWA manifest (VitePWA plugin)
- [ ] Create Dockerfile (multi-stage: node build → nginx serve)
- [ ] Create `.env.example` with `VITE_API_URL` and `VITE_KEYCLOAK_URL`
- [ ] Create `copilot-instructions.md`
- [ ] Verify `pnpm dev` runs and shows placeholder page

---

## Acceptance Criteria

- [ ] `pnpm dev` runs on `http://localhost:5173` and proxies API calls to `:5000`
- [ ] TailwindCSS + shadcn-vue components render correctly
- [ ] Vue Router navigates between placeholder routes
- [ ] Orval generates typed API client from OpenAPI spec
- [ ] PWA manifest configured (installable on mobile)
- [ ] Dockerfile builds and serves static files via nginx
- [ ] Mobile viewport works correctly (responsive meta tag)

---

## Notes

- API needs a minimal OpenAPI spec (or we generate one from minimal API endpoints)
- The API should serve the frontend's `dist/` from wwwroot in production, OR the frontend runs as a separate container behind Caddy
- In dev mode, Vite proxies to API on `:5000`
