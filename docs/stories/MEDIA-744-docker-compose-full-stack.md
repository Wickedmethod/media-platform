# MEDIA-744: Docker Compose Stack for Full Platform

## Story

**Epic:** Deployment  
**Priority:** High  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-706 (Frontend Docker), MEDIA-700 (Frontend setup)

---

## Summary

Create a production-ready `docker-compose.yml` that runs the complete media platform: API, Redis, frontend (SPA + TV), and reverse proxy. Extends the existing compose files to include the Vue frontend container and proper networking.

---

## Stack Overview

```
┌──────────────────────────────────────────────┐
│  Docker Compose: media-platform              │
│                                              │
│  ┌──────────┐  ┌──────────┐  ┌───────────┐ │
│  │  Caddy    │  │  API     │  │  Redis    │ │
│  │  (proxy)  │──│  (.NET)  │──│  (7-alp)  │ │
│  │  :80/:443 │  │  :5000   │  │  :6379    │ │
│  └──────────┘  └──────────┘  └───────────┘ │
│       │                                      │
│       │        ┌───────────┐                │
│       └────────│  Frontend │                │
│                │  (nginx)  │                │
│                │  :3000    │                │
│                └───────────┘                │
│                                              │
│  Network: keycloak-public (external)         │
└──────────────────────────────────────────────┘
```

---

## Compose File

```yaml
# docker-compose.stack.yml — Full platform stack
services:
  media-platform-api:
    build:
      context: .
      dockerfile: src/MediaPlatform.Api/Dockerfile
    environment:
      - ASPNETCORE_URLS=http://+:5000
      - Redis__ConnectionString=redis:6379
      - Keycloak__Authority=http://keycloak:8080/realms/homelab
      - Cors__AllowedOrigins=http://localhost:3000
    ports:
      - "5000:5000"
    depends_on:
      redis:
        condition: service_healthy
    networks:
      - media-internal
      - keycloak-public

  media-platform-web:
    build:
      context: frontend/
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    environment:
      - VITE_API_URL=http://media-platform-api:5000
    networks:
      - media-internal

  redis:
    image: redis:7-alpine
    volumes:
      - redis-data:/data
      - ./redis.conf:/usr/local/etc/redis/redis.conf
    command: redis-server /usr/local/etc/redis/redis.conf
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 3
    networks:
      - media-internal

  caddy:
    image: caddy:2-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile
      - caddy-data:/data
    depends_on:
      - media-platform-api
      - media-platform-web
    networks:
      - media-internal

volumes:
  redis-data:
  caddy-data:

networks:
  media-internal:
    driver: bridge
  keycloak-public:
    external: true
```

---

## Production Overlay

```yaml
# docker-compose.production.yml
services:
  media-platform-api:
    deploy:
      resources:
        limits:
          memory: 256M
          cpus: "0.5"
    restart: unless-stopped

  media-platform-web:
    deploy:
      resources:
        limits:
          memory: 64M
          cpus: "0.25"
    restart: unless-stopped

  redis:
    deploy:
      resources:
        limits:
          memory: 128M
    restart: unless-stopped

  caddy:
    deploy:
      resources:
        limits:
          memory: 64M
    restart: unless-stopped
```

---

## Tasks

- [ ] Create `docker-compose.stack.yml` with all services
- [ ] Create `docker-compose.production.yml` overlay
- [ ] Create Caddyfile for reverse proxy (MEDIA-746)
- [ ] Configure `keycloak-public` external network
- [ ] Add `.vault-env` mapping for secrets
- [ ] Test full stack startup: `docker compose -f docker-compose.stack.yml up`
- [ ] Verify API ↔ Redis ↔ Frontend connectivity
- [ ] Document startup procedure in README

---

## Acceptance Criteria

- [ ] Single `docker compose up` starts entire platform
- [ ] API, Redis, Frontend, and Caddy all healthy
- [ ] SPA accessible at `http://localhost/` via Caddy
- [ ] TV accessible at `http://localhost/tv.html` via Caddy
- [ ] API proxied at `http://localhost/api/`
- [ ] Keycloak network connectivity verified
