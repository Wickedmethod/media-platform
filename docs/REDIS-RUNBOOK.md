# Redis Backup and Restore Runbook

## Overview
Media Platform uses Redis with AOF persistence (primary) and RDB snapshots (safety net).
Data directory: `/data` inside the container, mapped to the `redis-data` Docker volume.

## Backup

### Quick backup (RDB snapshot)
```bash
docker exec media-platform-redis redis-cli BGSAVE
docker cp media-platform-redis:/data/dump.rdb ./backup-$(date +%Y%m%d).rdb
```

### Full backup (AOF + RDB)
```bash
docker exec media-platform-redis redis-cli BGREWRITEAOF
sleep 2
docker cp media-platform-redis:/data/ ./redis-backup-$(date +%Y%m%d)/
```

## Restore

### From RDB snapshot
```bash
docker compose down
# Copy backup into volume
docker run --rm -v media-platform_redis-data:/data -v $(pwd):/backup alpine \
  cp /backup/dump.rdb /data/dump.rdb
docker compose up -d
```

### From AOF
```bash
docker compose down
docker run --rm -v media-platform_redis-data:/data -v $(pwd)/redis-backup:/backup alpine \
  sh -c "cp /backup/appendonlydir/* /data/appendonlydir/"
docker compose up -d
```

## Verify Restore
```bash
docker exec media-platform-redis redis-cli DBSIZE
docker exec media-platform-redis redis-cli LLEN media:queue
docker exec media-platform-redis redis-cli HGETALL media:now-playing
```

## State Survives Restart — Verification
```bash
# Add some test data, then:
docker compose restart redis
docker exec media-platform-redis redis-cli LLEN media:queue  # Should match pre-restart
docker exec media-platform-redis redis-cli HGETALL media:now-playing  # Should match
```

## Emergency: Full Reset
```bash
docker exec media-platform-redis redis-cli FLUSHALL
```
