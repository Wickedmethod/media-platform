# MEDIA-706: Frontend Docker & Deployment

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-700 (project setup)

---

## Summary

Create a production Dockerfile and integrate the frontend into the existing `docker-compose.production.yml`. The SPA is built with Vite and served by nginx with proper caching headers.

---

## Dockerfile

```dockerfile
# Stage 1: Build
FROM node:22-alpine AS build
WORKDIR /app

# pnpm setup
RUN corepack enable && corepack prepare pnpm@latest --activate

# Install deps (cached layer)
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

# Build
COPY . .
RUN pnpm build

# Stage 2: Serve
FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

---

## nginx.conf

```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    # SPA fallback
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache static assets aggressively
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff2)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Don't cache index.html (contains hashed asset references)
    location = /index.html {
        add_header Cache-Control "no-cache";
    }

    # Proxy API requests to backend
    location /api/ {
        proxy_pass http://media-platform-api:8080/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }

    # SSE endpoint needs long timeout
    location /api/events {
        proxy_pass http://media-platform-api:8080/events;
        proxy_http_version 1.1;
        proxy_set_header Connection "";
        proxy_buffering off;
        proxy_cache off;
        proxy_read_timeout 86400s;
    }
}
```

---

## Docker Compose Integration

Add to `docker-compose.production.yml`:

```yaml
services:
  media-platform-web:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    container_name: media-platform-web
    restart: unless-stopped
    ports:
      - "3000:80"
    depends_on:
      - media-platform-api
    networks:
      - media-platform
    labels:
      - "arcane.managed=true"
      - "arcane.stack=media-platform"
```

---

## Environment Handling

Vite env vars are baked at build time. For runtime configuration:

```typescript
// src/config.ts
export const config = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || "/api",
  keycloakUrl: import.meta.env.VITE_KEYCLOAK_URL || "http://keycloak:8080",
  keycloakRealm: import.meta.env.VITE_KEYCLOAK_REALM || "media-platform",
  keycloakClientId:
    import.meta.env.VITE_KEYCLOAK_CLIENT_ID || "media-platform-web",
};
```

For docker deployment, nginx proxies `/api/` to the backend, so `apiBaseUrl` defaults to `/api` (relative).

---

## Build Arguments

```yaml
media-platform-web:
  build:
    context: ./frontend
    args:
      VITE_KEYCLOAK_URL: http://keycloak:8080
      VITE_KEYCLOAK_REALM: media-platform
      VITE_KEYCLOAK_CLIENT_ID: media-platform-web
```

---

## Tasks

- [ ] Create `frontend/Dockerfile` (multi-stage: node → nginx)
- [ ] Create `frontend/nginx.conf` with SPA fallback + API proxy
- [ ] Add `media-platform-web` service to `docker-compose.production.yml`
- [ ] Create `src/config.ts` for runtime environment config
- [ ] Add `.dockerignore` for frontend (node_modules, dist, .git)
- [ ] Test local docker build
- [ ] Verify SPA routing works (nginx try_files)
- [ ] Verify API proxy works through nginx
- [ ] Verify SSE works through nginx proxy (no buffering)

---

## Acceptance Criteria

- [ ] `docker compose build media-platform-web` succeeds
- [ ] Frontend loads at `http://localhost:3000`
- [ ] SPA client-side routing works (refresh on any route)
- [ ] API calls proxied correctly to backend
- [ ] SSE events flow through nginx without buffering
- [ ] Static assets have cache headers (1y, immutable)
- [ ] index.html has no-cache header
- [ ] Image size < 30MB
