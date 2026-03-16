# Redis Schema Migrations

Numbered Lua scripts that run atomically in Redis.

## Naming Convention

```
NNN_description.lua
```

- `NNN` — 3-digit zero-padded version number
- Scripts run in filename order
- Each script checks `platform:schema:version` and skips if already applied

## Writing a Migration

```lua
local version = redis.call('GET', 'platform:schema:version')
if version and tonumber(version) >= N then
    return 'SKIP: already at version N+'
end

-- Your migration logic here

redis.call('SET', 'platform:schema:version', 'N')
redis.call('ZADD', 'platform:schema:history', redis.call('TIME')[1], 'vX→vN: description')

return 'OK: migrated to version N'
```

## Guidelines

- **Idempotent** — Safe to run multiple times (guards at top)
- **Atomic** — Lua scripts execute atomically in Redis
- **Forward-only** — Rollbacks use Redis snapshot restore, not reverse scripts
- **No KEYS in production** — Use known key patterns or SCAN for large datasets
