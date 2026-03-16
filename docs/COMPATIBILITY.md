# Version Compatibility Matrix

## Current Versions

| Component | Version | Redis Schema | Notes                     |
| --------- | ------- | ------------ | ------------------------- |
| API       | 1.0.0   | v2           | Initial versioned release |
| Frontend  | 1.0.0   | —            | SPA + TV kiosk            |
| Redis     | 7.x     | v2           | Schema version tracked    |

## Schema Version History

| Schema | Migration                          | Breaking? | API Compat |
| ------ | ---------------------------------- | --------- | ---------- |
| v1     | Initial schema version tracking    | No        | 1.0.0+     |
| v2     | Add player status field to workers | No        | 1.0.0+     |

## Upgrade Path

| From    | To    | Migration Required? | Steps                          |
| ------- | ----- | ------------------- | ------------------------------ |
| pre-1.0 | 1.0.0 | Yes                 | Run `scripts/upgrade.sh 1.0.0` |

## Backup Strategy

| What              | How                      | Retention           | When                 |
| ----------------- | ------------------------ | ------------------- | -------------------- |
| Redis RDB         | `BGSAVE` → copy dump.rdb | 7 days, rolling     | Before every upgrade |
| Redis AOF         | Continuous               | Current             | Always on            |
| Docker images     | Tag with version         | 3 previous versions | On build             |
| Migration scripts | Git versioned            | Forever             | Committed with code  |

## Redis Key Inventory

| Key Pattern                    | Type   | TTL  | Purpose                     |
| ------------------------------ | ------ | ---- | --------------------------- |
| `platform:schema:version`      | STRING | —    | Current schema version      |
| `platform:schema:history`      | ZSET   | —    | Migration history log       |
| `media:queue`                  | LIST   | —    | Queue items                 |
| `media:now-playing`            | HASH   | —    | Current playback state      |
| `media:queue-mode`             | STRING | —    | Queue mode (Normal/Shuffle) |
| `queue:version`                | STRING | —    | ETag version counter        |
| `player:{id}:heartbeat`        | HASH   | 120s | Player heartbeat            |
| `worker:{id}`                  | HASH   | —    | Worker registration         |
| `player:logs:{id}`             | LIST   | —    | Player log ring buffer      |
| `network:metrics:{id}`         | STRING | —    | Current network metrics     |
| `network:metrics:{id}:history` | ZSET   | 24h  | Network metrics history     |
