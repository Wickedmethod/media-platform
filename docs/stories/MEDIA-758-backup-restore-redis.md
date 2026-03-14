# MEDIA-758: Backup & Restore Strategy for Redis Persistence

## Story

**Epic:** Infrastructure & Security  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-606 (Redis Persistence — already done)

---

## Summary

Define and implement a backup strategy for the Redis data store. While MEDIA-606 ensures data survives restarts (AOF + RDB), this story adds scheduled external backups, offsite copies, and documented restore procedures for disaster recovery.

**Related:** MEDIA-606 (Redis Persistence) is ✅ Done — configures AOF + RDB for crash recovery. This story handles **disaster recovery**: what happens when the disk fails, the container is deleted, or data is corrupted.

---

## Backup Architecture

```
Redis Container
    │ RDB snapshot every 5 min
    ▼
/data/dump.rdb (inside volume)
    │
    ├── Hourly: copy to backup volume
    │   └── /backups/redis/hourly/dump-{timestamp}.rdb
    │
    ├── Daily: copy + compress
    │   └── /backups/redis/daily/dump-{date}.rdb.gz
    │
    └── Weekly: offsite (NAS / remote)
        └── rsync to backup server
```

---

## Backup Schedule

| Frequency | Retention    | Method        | Storage             |
| --------- | ------------ | ------------- | ------------------- |
| Hourly    | Keep last 24 | Copy RDB file | Local backup volume |
| Daily     | Keep last 30 | Copy + gzip   | Local backup volume |
| Weekly    | Keep last 12 | rsync to NAS  | Offsite             |

---

## Backup Script

```bash
#!/bin/bash
# backup-redis.sh — Called by cron

set -euo pipefail

REDIS_DATA="/var/lib/docker/volumes/media-redis-data/_data"
BACKUP_DIR="/backups/redis"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
DATE=$(date +%Y-%m-%d)

# Trigger RDB save
docker exec media-redis redis-cli BGSAVE
sleep 2  # Wait for background save

# Hourly backup
mkdir -p "${BACKUP_DIR}/hourly"
cp "${REDIS_DATA}/dump.rdb" "${BACKUP_DIR}/hourly/dump-${TIMESTAMP}.rdb"

# Cleanup: keep last 24 hourly backups
ls -t "${BACKUP_DIR}/hourly/"dump-*.rdb | tail -n +25 | xargs -r rm

# Daily backup (run at midnight)
if [ "$(date +%H)" = "00" ]; then
    mkdir -p "${BACKUP_DIR}/daily"
    gzip -c "${REDIS_DATA}/dump.rdb" > "${BACKUP_DIR}/daily/dump-${DATE}.rdb.gz"

    # Cleanup: keep last 30 daily backups
    ls -t "${BACKUP_DIR}/daily/"dump-*.rdb.gz | tail -n +31 | xargs -r rm
fi

echo "[$(date)] Redis backup completed: dump-${TIMESTAMP}.rdb"
```

### Cron Schedule

```cron
# /etc/cron.d/media-platform-backup
0 * * * * root /opt/media-platform/backup-redis.sh >> /var/log/media-backup.log 2>&1
```

---

## Docker Compose Integration

```yaml
# In docker-compose.stack.yml
services:
  redis:
    volumes:
      - redis-data:/data
      - redis-backups:/backups/redis

  redis-backup:
    image: alpine:3
    volumes:
      - redis-data:/redis-data:ro
      - redis-backups:/backups/redis
      - ./scripts/backup-redis.sh:/backup.sh:ro
    entrypoint: crond -f
    # or use ofelia/supercronic for container-native cron
    depends_on:
      - redis

volumes:
  redis-backups:
```

---

## Restore Procedure

### From RDB Backup

```bash
# 1. Stop the API (prevent writes during restore)
docker compose stop media-platform-api

# 2. Stop Redis
docker compose stop redis

# 3. Copy backup into Redis data volume
cp /backups/redis/daily/dump-2026-03-15.rdb.gz /tmp/
gunzip /tmp/dump-2026-03-15.rdb.gz
docker cp /tmp/dump-2026-03-15.rdb media-redis:/data/dump.rdb

# 4. Start Redis (loads from dump.rdb on boot)
docker compose start redis

# 5. Verify data
docker exec media-redis redis-cli DBSIZE

# 6. Start API
docker compose start media-platform-api
```

### Verify Backup Integrity

```bash
# Check RDB file is valid
redis-check-rdb /backups/redis/daily/dump-2026-03-15.rdb.gz
```

---

## Monitoring

| Check       | Alert Threshold            |
| ----------- | -------------------------- |
| Backup age  | No backup in 2 hours       |
| Backup size | Size < 1KB (empty/corrupt) |
| Disk space  | Backup volume > 90% full   |

---

## Tasks

- [ ] Create `backup-redis.sh` script
- [ ] Set up hourly cron schedule
- [ ] Add backup volume to Docker Compose
- [ ] Implement retention cleanup (24 hourly, 30 daily)
- [ ] Document restore procedure in `docs/DISASTER-RECOVERY.md`
- [ ] Add backup health check (verify age and size)
- [ ] Test full backup → restore cycle
- [ ] Add `redis-check-rdb` validation to backup script

---

## Acceptance Criteria

- [ ] Hourly RDB backups created automatically
- [ ] Old backups cleaned up (24 hourly, 30 daily retained)
- [ ] Documented restore procedure works end-to-end
- [ ] Backup integrity verified (redis-check-rdb)
- [ ] Restore recovers complete queue and playback state
- [ ] Backup script logs to file for monitoring
