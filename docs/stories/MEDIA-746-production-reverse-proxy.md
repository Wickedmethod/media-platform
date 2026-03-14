# MEDIA-746: Production Reverse Proxy Configuration

## Story

**Epic:** Deployment  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-744 (Docker Compose Stack), MEDIA-706 (Frontend Docker)

---

## Summary

Create a Caddy reverse proxy configuration that routes traffic to the media platform API and frontend, handles TLS, compression, rate limiting, and SSE proxying. Integrates with the existing Caddy infrastructure in the homelab.

---

## Routing

```
media.homelab.local
    │
    ├── /api/*          → media-platform-api:5000 (strip /api prefix)
    ├── /events         → media-platform-api:5000 (SSE, no buffering)
    ├── /metrics        → media-platform-api:5000 (internal only)
    ├── /health/*       → media-platform-api:5000
    ├── /tv.html        → media-platform-web:80/tv.html
    ├── /tv/*           → media-platform-web:80 (TV assets)
    └── /*              → media-platform-web:80 (SPA fallback)
```

---

## Caddyfile

```caddyfile
media.homelab.local {
    # TLS via ACME or internal CA
    tls internal

    # Compression
    encode gzip zstd

    # API routes (reverse proxy to .NET API)
    handle /api/* {
        uri strip_prefix /api
        reverse_proxy media-platform-api:5000
    }

    # SSE endpoint — disable buffering for real-time streaming
    handle /events {
        reverse_proxy media-platform-api:5000 {
            flush_interval -1
            transport http {
                read_timeout 0
            }
        }
    }

    # Health endpoints (direct passthrough)
    handle /health/* {
        reverse_proxy media-platform-api:5000
    }

    # Metrics — restrict to internal network
    handle /metrics {
        @internal remote_ip 192.168.0.0/16 172.16.0.0/12 10.0.0.0/8
        reverse_proxy @internal media-platform-api:5000
        respond 403
    }

    # Frontend (SPA + TV)
    handle {
        reverse_proxy media-platform-web:80
    }
}
```

---

## SSE Proxy Considerations

Server-Sent Events require special proxy configuration:

| Setting            | Value    | Reason                           |
| ------------------ | -------- | -------------------------------- |
| `flush_interval`   | `-1`     | Flush immediately, don't buffer  |
| `read_timeout`     | `0`      | No timeout, keep connection open |
| Response buffering | Disabled | SSE is a stream, not a response  |

Without these, SSE events are buffered by the proxy and delivered in batches.

---

## Security Headers

```caddyfile
(security_headers) {
    header {
        X-Content-Type-Options nosniff
        X-Frame-Options DENY
        X-XSS-Protection "1; mode=block"
        Referrer-Policy strict-origin-when-cross-origin
        -Server
    }
}

media.homelab.local {
    import security_headers
    # ... rest of config
}
```

---

## Rate Limiting

```caddyfile
# Rate limit API writes (10 req/min per IP)
handle /api/* {
    rate_limit {
        zone api_write {
            key {remote_host}
            events 10
            window 1m
        }
        match {
            method POST PUT DELETE PATCH
        }
    }
    # ... reverse proxy
}
```

---

## Tasks

- [ ] Create `Caddyfile` with all routing rules
- [ ] Configure SSE proxy (no buffering, no timeout)
- [ ] Add security headers
- [ ] Restrict `/metrics` to internal network
- [ ] Configure TLS (internal CA for homelab)
- [ ] Add rate limiting for API write operations
- [ ] Test SPA refresh (fallback to `index.html`)
- [ ] Test TV access at `/tv.html`
- [ ] Test SSE streaming through proxy

---

## Acceptance Criteria

- [ ] SPA accessible at `https://media.homelab.local/`
- [ ] TV accessible at `https://media.homelab.local/tv.html`
- [ ] API routed at `/api/*` with prefix stripped
- [ ] SSE events stream in real-time through proxy (no buffering)
- [ ] `/metrics` blocked from external IPs
- [ ] Security headers present on all responses
- [ ] SPA client-side routing works (refresh on any route)
