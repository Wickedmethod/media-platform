# MEDIA-765: Platform Upgrade & Migration Strategy

## Story

**Epic:** MEDIA-DEPLOY — Deployment & Configuration  
**Priority:** Medium  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-744 (Docker Compose Stack), MEDIA-606 (Redis Persistence)

---

## Summary

Define a repeatable strategy for upgrading the media platform: Redis schema migrations, zero-downtime container updates, data migration scripts, and rollback procedures. Every breaking change gets a migration script, a rollback script, and a tested upgrade path. No manual steps — upgrades are scripted and idempotent.

---

## Architecture

```
Upgrade Flow:
    │
    ├─ 1. Pre-flight checks (health, disk, backup)
    ├─ 2. Create Redis snapshot (BGSAVE)
    ├─ 3. Run migration scripts (versioned, idempotent)
    ├─ 4. Rolling container update (docker compose up --no-deps)
    ├─ 5. Post-flight health checks
    └─ 6. Rollback if health checks fail

Rollback Flow:
    │
    ├─ 1. Stop new containers
    ├─ 2. Restore Redis from snapshot
    ├─ 3. Start previous container version
    └─ 4. Verify health
```

---

## Redis Schema Versioning

### Version Tracking

```
platform:schema:version → "3"  (current schema version, plain string)
platform:schema:history → sorted set [
  { score: timestamp, member: "v1→v2: renamed queue:items to queue:active" }
]
```

### Migration Script Structure

```
migrations/
├── 001_initial_schema.lua
├── 002_add_player_status_field.lua
├── 003_rename_queue_keys.lua
└── README.md
```

Each migration is a Lua script executed atomically in Redis:

```lua
-- migrations/002_add_player_status_field.lua
-- Migration: Add status field to player registrations
-- Version: 1 → 2

local version = redis.call('GET', 'platform:schema:version')
if version and tonumber(version) >= 2 then
    return 'SKIP: already at version 2+'
end

-- Migrate all player registration hashes
local players = redis.call('KEYS', 'player:*:info')
for _, key in ipairs(players) do
    local hasStatus = redis.call('HEXISTS', key, 'status')
    if hasStatus == 0 then
        redis.call('HSET', key, 'status', 'online')
    end
end

-- Update version
redis.call('SET', 'platform:schema:version', '2')
redis.call('ZADD', 'platform:schema:history', os.time(), 'v1→v2: add player status field')

return 'OK: migrated to version 2'
```

### Migration Runner (C#)

```csharp
public class MigrationRunner
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<MigrationRunner> _logger;

    public async Task RunMigrations(string migrationsPath)
    {
        var currentVersion = await GetCurrentVersion();
        var scripts = Directory.GetFiles(migrationsPath, "*.lua")
            .OrderBy(f => f)
            .ToList();

        foreach (var script in scripts)
        {
            var scriptVersion = ExtractVersion(script);
            if (scriptVersion <= currentVersion) continue;

            _logger.LogInformation("Running migration {Script}...", Path.GetFileName(script));
            var lua = await File.ReadAllTextAsync(script);
            var result = await _redis.GetDatabase().ScriptEvaluateAsync(lua);
            _logger.LogInformation("Migration result: {Result}", result);
        }
    }

    private async Task<int> GetCurrentVersion()
    {
        var version = await _redis.GetDatabase().StringGetAsync("platform:schema:version");
        return version.HasValue ? int.Parse(version!) : 0;
    }
}
```

---

## Container Update Strategy

### Rolling Update (Zero Downtime)

```bash
#!/bin/bash
# scripts/upgrade.sh

set -euo pipefail

VERSION=${1:?"Usage: upgrade.sh <version>"}
COMPOSE_FILE="docker-compose.yml"

echo "=== Pre-flight checks ==="
docker compose -f $COMPOSE_FILE exec api curl -sf http://localhost:5000/health/ready || {
    echo "ABORT: API not healthy before upgrade"
    exit 1
}

echo "=== Creating Redis backup ==="
docker compose -f $COMPOSE_FILE exec redis redis-cli BGSAVE
sleep 2  # Wait for BGSAVE to complete

echo "=== Running migrations ==="
docker compose -f $COMPOSE_FILE exec api dotnet MediaPlatform.Api.dll --migrate

echo "=== Pulling new images ==="
docker compose -f $COMPOSE_FILE pull api frontend

echo "=== Rolling update: API ==="
docker compose -f $COMPOSE_FILE up -d --no-deps api

echo "=== Waiting for API health ==="
for i in $(seq 1 30); do
    if docker compose -f $COMPOSE_FILE exec api curl -sf http://localhost:5000/health/ready; then
        echo "API healthy after upgrade"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "ABORT: API not healthy after upgrade, rolling back..."
        docker compose -f $COMPOSE_FILE up -d --no-deps api  # Will use cached previous image
        exit 1
    fi
    sleep 2
done

echo "=== Rolling update: Frontend ==="
docker compose -f $COMPOSE_FILE up -d --no-deps frontend

echo "=== Post-flight checks ==="
docker compose -f $COMPOSE_FILE exec api curl -sf http://localhost:5000/health/ready
echo "Upgrade to $VERSION complete"
```

### Rollback Script

```bash
#!/bin/bash
# scripts/rollback.sh

set -euo pipefail

COMPOSE_FILE="docker-compose.yml"
BACKUP_DIR="/data/backups/redis"

echo "=== Stopping services ==="
docker compose -f $COMPOSE_FILE stop api frontend

echo "=== Restoring Redis from backup ==="
LATEST_BACKUP=$(ls -t $BACKUP_DIR/dump_*.rdb | head -1)
docker compose -f $COMPOSE_FILE stop redis
cp "$LATEST_BACKUP" /data/redis/dump.rdb
docker compose -f $COMPOSE_FILE start redis

echo "=== Starting previous version ==="
docker compose -f $COMPOSE_FILE up -d api frontend

echo "=== Health check ==="
sleep 5
docker compose -f $COMPOSE_FILE exec api curl -sf http://localhost:5000/health/ready
echo "Rollback complete"
```

---

## Version Compatibility Matrix

Document in `docs/COMPATIBILITY.md`:

| API Version | Redis Schema | Frontend | Player | Notes                                           |
| ----------- | ------------ | -------- | ------ | ----------------------------------------------- |
| 1.0.0       | v1           | 1.0.x    | 1.0.x  | Initial release                                 |
| 1.1.0       | v2           | 1.0.x    | 1.0.x  | New player status field (backward compatible)   |
| 2.0.0       | v3           | 2.0.x    | 1.1.x+ | Queue key rename (breaking, migration required) |

---

## Pre-Flight Checklist (Automated)

Before any upgrade, the script validates:

```
✓ API health check passes
✓ Redis reachable and responding
✓ Disk space > 500 MB free
✓ Redis persistence (last BGSAVE < 1 hour ago)
✓ No active migrations in progress
✓ Current schema version matches expected
```

---

## Backup Strategy

| What              | How                      | Retention           | When                 |
| ----------------- | ------------------------ | ------------------- | -------------------- |
| Redis RDB         | `BGSAVE` → copy dump.rdb | 7 days, rolling     | Before every upgrade |
| Redis AOF         | Continuous               | Current             | Always on            |
| Docker images     | Tag with version         | 3 previous versions | On build             |
| Migration scripts | Git versioned            | Forever             | Committed with code  |

---

## Tasks

- [ ] Create `migrations/` directory with numbered Lua scripts
- [ ] Implement `MigrationRunner` service in C# (reads & executes Lua scripts)
- [ ] Add `--migrate` CLI flag to API for running migrations on startup
- [ ] Create Redis schema version tracking keys
- [ ] Write `scripts/upgrade.sh` with pre-flight, migration, rolling update flow
- [ ] Write `scripts/rollback.sh` with Redis restore + image rollback
- [ ] Create pre-flight check script (health, disk, backup status)
- [ ] Document version compatibility matrix in `docs/COMPATIBILITY.md`
- [ ] Write first migration script (001_initial_schema.lua)
- [ ] Test upgrade path: v1 → v2 with schema migration
- [ ] Test rollback path: restore Redis snapshot + previous container
- [ ] Add migration status to `/health/ready` endpoint

---

## Acceptance Criteria

- [ ] Redis schema version tracked in `platform:schema:version` key
- [ ] Migration scripts are numbered, idempotent Lua scripts
- [ ] `MigrationRunner` skips already-applied migrations
- [ ] Upgrade script creates Redis backup before any changes
- [ ] Upgrade script validates health before and after update
- [ ] Automatic rollback if post-upgrade health check fails
- [ ] Rollback script restores Redis from latest backup
- [ ] Each migration has a corresponding rollback instruction documented
- [ ] Version compatibility matrix documents all breaking changes
- [ ] Pre-flight checks prevent upgrade if system is unhealthy
