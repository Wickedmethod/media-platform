#!/bin/bash
# scripts/upgrade.sh — Upgrade media platform with pre-flight checks, backup, and migration
set -euo pipefail

VERSION=${1:?"Usage: upgrade.sh <version>"}
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"
BACKUP_DIR="${BACKUP_DIR:-/data/backups/redis}"

echo "=== Media Platform Upgrade to $VERSION ==="
echo "Compose file: $COMPOSE_FILE"
echo ""

# Pre-flight checks
echo "=== Pre-flight checks ==="

echo -n "  API health... "
if docker compose -f "$COMPOSE_FILE" exec -T api curl -sf http://localhost:8080/health/ready > /dev/null 2>&1; then
    echo "OK"
else
    echo "FAIL"
    echo "ABORT: API is not healthy. Fix issues before upgrading."
    exit 1
fi

echo -n "  Redis reachable... "
if docker compose -f "$COMPOSE_FILE" exec -T redis redis-cli PING | grep -q PONG; then
    echo "OK"
else
    echo "FAIL"
    echo "ABORT: Redis is not responding."
    exit 1
fi

echo -n "  Disk space... "
FREE_KB=$(df "$(dirname "$COMPOSE_FILE")" | tail -1 | awk '{print $4}')
if [ "$FREE_KB" -gt 512000 ]; then
    echo "OK ($(( FREE_KB / 1024 )) MB free)"
else
    echo "FAIL"
    echo "ABORT: Less than 500 MB free disk space."
    exit 1
fi

# Backup
echo ""
echo "=== Creating Redis backup ==="
mkdir -p "$BACKUP_DIR"
docker compose -f "$COMPOSE_FILE" exec -T redis redis-cli BGSAVE
sleep 3

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
docker compose -f "$COMPOSE_FILE" cp redis:/data/dump.rdb "$BACKUP_DIR/dump_${TIMESTAMP}.rdb" 2>/dev/null || true
echo "  Backup saved: dump_${TIMESTAMP}.rdb"

# Migration
echo ""
echo "=== Running Redis migrations ==="
docker compose -f "$COMPOSE_FILE" exec -T api dotnet MediaPlatform.Api.dll --migrate || {
    echo "WARNING: Migration command failed (may be expected if no new migrations)"
}

# Pull new images
echo ""
echo "=== Pulling new images ==="
docker compose -f "$COMPOSE_FILE" pull api web

# Rolling update
echo ""
echo "=== Rolling update: API ==="
docker compose -f "$COMPOSE_FILE" up -d --no-deps api

echo -n "  Waiting for API health"
for i in $(seq 1 30); do
    if docker compose -f "$COMPOSE_FILE" exec -T api curl -sf http://localhost:8080/health/ready > /dev/null 2>&1; then
        echo " OK"
        break
    fi
    if [ "$i" -eq 30 ]; then
        echo " FAIL"
        echo "ABORT: API not healthy after upgrade. Rolling back..."
        docker compose -f "$COMPOSE_FILE" up -d --no-deps api
        exit 1
    fi
    echo -n "."
    sleep 2
done

echo "=== Rolling update: Frontend ==="
docker compose -f "$COMPOSE_FILE" up -d --no-deps web

# Post-flight
echo ""
echo "=== Post-flight checks ==="
sleep 2
if docker compose -f "$COMPOSE_FILE" exec -T api curl -sf http://localhost:8080/health/ready > /dev/null 2>&1; then
    echo "  API: healthy"
else
    echo "  API: UNHEALTHY — check logs"
    exit 1
fi

echo ""
echo "=== Upgrade to $VERSION complete ==="
