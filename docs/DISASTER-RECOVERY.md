# Disaster Recovery — Redis Backup & Restore

> Part of MEDIA-758. For crash recovery (AOF + RDB on restart), see MEDIA-606.

This document covers **external backups** for disaster recovery: disk failure, container deletion, or data corruption.

## Backup Architecture

```
Redis Container (media-redis)
    │  RDB snapshot (every 5 min via redis.conf)
    ▼
/data/dump.rdb (inside Docker volume)
    │
    ├── Hourly: scripts/backup-redis.sh → /backups/redis/hourly/
    │   └── dump-{YYYYMMDD-HHMMSS}.rdb   (keep last 24)
    │
    └── Daily (midnight): compressed copy → /backups/redis/daily/
        └── dump-{YYYY-MM-DD}.rdb.gz     (keep last 30)
```

## Backup Schedule

| Frequency | Retention | Method       | Storage             |
|-----------|-----------|--------------|---------------------|
| Hourly    | 24 files  | Copy RDB     | Local backup volume |
| Daily     | 30 files  | Copy + gzip  | Local backup volume |

## Setup

### 1. Add backup volume to Docker Compose

```yaml
services:
  redis:
    image: redis:7-alpine
    volumes:
      - redis-data:/data
      - redis-backups:/backups/redis

volumes:
  redis-data:
  redis-backups:
```

### 2. Schedule the backup script

```cron
# /etc/cron.d/media-platform-backup
0 * * * * root /opt/media-platform/scripts/backup-redis.sh >> /var/log/media-backup.log 2>&1
```

Or use a container-native cron (supercronic):

```yaml
  redis-backup:
    image: alpine:3
    volumes:
      - redis-data:/data:ro
      - redis-backups:/backups/redis
      - ./scripts/backup-redis.sh:/backup.sh:ro
    entrypoint: ["crond", "-f"]
    depends_on:
      - redis
```

### 3. Verify backups are running

```bash
ls -la /backups/redis/hourly/ | head -5
```

## Restore Procedure

### From Hourly RDB Backup

```bash
# 1. Stop the API to prevent writes
docker compose stop media-platform-api

# 2. Stop Redis
docker compose stop redis

# 3. Copy backup into Redis data volume
docker cp /backups/redis/hourly/dump-20260315-140000.rdb media-redis:/data/dump.rdb

# 4. Start Redis (loads from dump.rdb on boot)
docker compose start redis

# 5. Verify data was restored
docker exec media-redis redis-cli DBSIZE
docker exec media-redis redis-cli KEYS "queue:*"

# 6. Start API
docker compose start media-platform-api
```

### From Daily Compressed Backup

```bash
# 1. Stop API + Redis (same as above)
docker compose stop media-platform-api redis

# 2. Decompress the backup
gunzip -c /backups/redis/daily/dump-2026-03-15.rdb.gz > /tmp/dump.rdb

# 3. Copy into Redis container volume
docker cp /tmp/dump.rdb media-redis:/data/dump.rdb
rm /tmp/dump.rdb

# 4. Start Redis + API
docker compose start redis
docker compose start media-platform-api

# 5. Verify
docker exec media-redis redis-cli DBSIZE
```

## Backup Health Monitoring

| Check       | Alert Threshold            | How to Verify                          |
|-------------|----------------------------|----------------------------------------|
| Backup age  | No backup in 2 hours       | `ls -lt /backups/redis/hourly/ | head` |
| Backup size | Size < 1 KB (empty/corrupt)| `stat /backups/redis/hourly/latest`    |
| Disk space  | Backup volume > 90% full   | `df -h /backups/redis`                 |

## What Gets Restored

A full Redis restore recovers:

- **Queue state** — all queue items, order, metadata
- **Playback state** — current track, position, play/pause status
- **Player registrations** — worker IDs, capabilities, heartbeat data
- **Player logs** — diagnostic log ring buffers (up to 1000 entries/player)
- **Network metrics** — connectivity history (up to 24h)
- **Policies** — playback policy rules

## What Is NOT Restored

- **In-memory state** — analytics, audit log, anomaly detection windows (these reset on restart)
- **SSE connections** — clients reconnect automatically
- **Kill switch** — resets to inactive on restart
