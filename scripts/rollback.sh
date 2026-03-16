#!/bin/bash
# scripts/rollback.sh — Rollback media platform to previous state
set -euo pipefail

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"
BACKUP_DIR="${BACKUP_DIR:-/data/backups/redis}"

echo "=== Media Platform Rollback ==="

# Find latest backup
if [ ! -d "$BACKUP_DIR" ] || [ -z "$(ls -A "$BACKUP_DIR" 2>/dev/null)" ]; then
    echo "ERROR: No backups found in $BACKUP_DIR"
    echo "Cannot rollback without a Redis backup."
    exit 1
fi

LATEST_BACKUP=$(ls -t "$BACKUP_DIR"/dump_*.rdb 2>/dev/null | head -1)
echo "  Using backup: $(basename "$LATEST_BACKUP")"

# Stop services
echo ""
echo "=== Stopping services ==="
docker compose -f "$COMPOSE_FILE" stop api web

# Restore Redis
echo ""
echo "=== Restoring Redis from backup ==="
docker compose -f "$COMPOSE_FILE" stop redis
docker compose -f "$COMPOSE_FILE" cp "$LATEST_BACKUP" redis:/data/dump.rdb 2>/dev/null || \
    docker cp "$LATEST_BACKUP" "$(docker compose -f "$COMPOSE_FILE" ps -q redis)":/data/dump.rdb
docker compose -f "$COMPOSE_FILE" start redis
sleep 3

# Start previous version
echo ""
echo "=== Starting previous version ==="
docker compose -f "$COMPOSE_FILE" up -d api web

# Health check
echo ""
echo "=== Health check ==="
sleep 5
if docker compose -f "$COMPOSE_FILE" exec -T api curl -sf http://localhost:8080/health/ready > /dev/null 2>&1; then
    echo "  API: healthy"
else
    echo "  API: UNHEALTHY — manual intervention required"
    exit 1
fi

echo ""
echo "=== Rollback complete ==="
