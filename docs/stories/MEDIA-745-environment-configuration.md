# MEDIA-745: Environment Configuration Management

## Story

**Epic:** Deployment  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-744 (Docker Compose Stack)  
**Absorbs:** MEDIA-764 (Secrets Management for Docker Compose)

---

## Summary

Create a centralized environment configuration schema with `.env.example` files, strongly-typed C# configuration classes, and documentation. Every configurable value has a default, a description, and clear separation between development and production settings.

---

## Configuration Categories

| Category     | Example Variables                                                 | Where Used        |
| ------------ | ----------------------------------------------------------------- | ----------------- |
| **API**      | `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT`                       | API container     |
| **Redis**    | `Redis__ConnectionString`, `Redis__InstanceName`                  | API → Redis       |
| **Keycloak** | `Keycloak__Authority`, `Keycloak__ClientId`, `Keycloak__Audience` | API auth          |
| **CORS**     | `Cors__AllowedOrigins`                                            | API CORS policy   |
| **Worker**   | `WorkerAuth__Key`                                                 | API ↔ Pi auth     |
| **Frontend** | `VITE_API_URL`, `VITE_KEYCLOAK_URL`, `VITE_KEYCLOAK_REALM`        | Vue build         |
| **TV**       | `VITE_API_URL` (shared)                                           | TV Vue entry      |
| **Alerting** | `Alerting__Discord__WebhookUrl`                                   | Alert dispatcher  |
| **Metrics**  | `Metrics__Enabled`                                                | Prometheus export |

---

## .env.example (root)

```bash
# Media Platform — Environment Configuration
# Copy to .env and fill in values

# ─── API ───────────────────────────────────
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000

# ─── Redis ─────────────────────────────────
Redis__ConnectionString=redis:6379
Redis__InstanceName=media-platform

# ─── Keycloak ──────────────────────────────
Keycloak__Authority=http://keycloak:8080/realms/homelab
Keycloak__ClientId=media-platform
Keycloak__Audience=media-platform

# ─── CORS ──────────────────────────────────
Cors__AllowedOrigins=https://media.homelab.local

# ─── Worker Auth (Pi players) ──────────────
# Generate with: openssl rand -hex 32
WorkerAuth__Key=CHANGE_ME

# ─── Alerting ──────────────────────────────
Alerting__Enabled=true
Alerting__Discord__WebhookUrl=
Alerting__CooldownMinutes=5

# ─── Metrics ───────────────────────────────
Metrics__Enabled=true
```

---

## .env.example (frontend/)

```bash
# Frontend — Build-time variables
VITE_API_URL=http://localhost:5000
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=homelab
VITE_KEYCLOAK_CLIENT_ID=media-platform-web
```

---

## .vault-env (secrets via Arcane Vault Bridge)

```ini
# Secrets injected by Arcane Vault Bridge
WorkerAuth__Key=vault:secret/data/media-platform/worker-key#value
Alerting__Discord__WebhookUrl=vault:secret/data/media-platform/discord-webhook#url
```

---

## Docker Compose Secrets Integration (absorbed from MEDIA-764)

All sensitive values are managed through VaultFacade + Arcane Vault Bridge. Never hardcode secrets in compose files or `.env`.

### Secret Creation Workflow

1. Generate secrets via `mcp_vaultfacade_generate_batch`:
   - `secret/data/media-platform/worker-key` — Worker auth key (type: `ApiKey`)
   - `secret/data/media-platform/discord-webhook` — Discord webhook URL (type: `Generic`)
   - `secret/data/media-platform/redis-password` — Redis password if auth enabled (type: `Password`)

2. Map secrets in `.vault-env` (see above)

3. Arcane Vault Bridge resolves `.vault-env` → `.env` automatically on deploy

### Security Rules

- `.env` is in `.gitignore` — never committed
- `.vault-env` is safe to commit (contains only Vault paths, not values)
- Rotate secrets via `mcp_vaultfacade_rotate_secret` — bridge picks up new values on next deploy
- Validate in CI that no secret values appear in compose files or tracked env files

---

## C# Configuration Classes

```csharp
public class RedisOptions
{
    public string ConnectionString { get; init; } = "localhost:6379";
    public string InstanceName { get; init; } = "media-platform";
}

public class KeycloakOptions
{
    public string Authority { get; init; } = "";
    public string ClientId { get; init; } = "media-platform";
    public string Audience { get; init; } = "media-platform";
}

public class WorkerAuthOptions
{
    public string Key { get; init; } = "";
}

// Program.cs
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"));
builder.Services.Configure<WorkerAuthOptions>(builder.Configuration.GetSection("WorkerAuth"));
```

---

## Tasks

- [ ] Create `.env.example` in project root with all variables
- [ ] Create `frontend/.env.example` with VITE\_ variables
- [ ] Create `.vault-env` for secret mapping
- [ ] Generate initial secrets in VaultFacade (`worker-key`, `discord-webhook`)
- [ ] Ensure `.env` is in `.gitignore` (never committed)
- [ ] Document secret rotation procedure in `docs/CONFIG.md`
- [ ] Create C# options classes (`RedisOptions`, `KeycloakOptions`, `WorkerAuthOptions`)
- [ ] Register options classes in `Program.cs`
- [ ] Replace hardcoded config values with injected options
- [ ] Create `docs/CONFIG.md` documenting all variables
- [ ] Validate required config on startup (fail fast if missing)

---

## Acceptance Criteria

- [ ] All configurable values documented in `.env.example`
- [ ] Strongly-typed C# options classes for all config sections
- [ ] Missing required config fails at startup with clear error
- [ ] `.vault-env` ready for Arcane Vault Bridge secret injection
- [ ] All secrets created in VaultFacade with proper paths and types
- [ ] No secret values in compose files, `.env.example`, or version control
- [ ] Secret rotation documented and works via `mcp_vaultfacade_rotate_secret`
- [ ] `docs/CONFIG.md` explains every variable
