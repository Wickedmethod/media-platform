#!/bin/bash
# backup-redis.sh — Scheduled Redis backup for Media Platform
# Called by cron: 0 * * * * /opt/media-platform/backup-redis.sh
#
# Prerequisites:
#   - Docker container "media-redis" running
#   - BACKUP_DIR volume mounted and writable
#
# Usage:
#   ./backup-redis.sh                  # Normal backup
#   BACKUP_DIR=/custom/path ./backup-redis.sh  # Custom backup path

set -euo pipefail

REDIS_CONTAINER="${REDIS_CONTAINER:-media-redis}"
BACKUP_DIR="${BACKUP_DIR:-/backups/redis}"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
DATE=$(date +%Y-%m-%d)
LOG_PREFIX="[$(date '+%Y-%m-%d %H:%M:%S')]"

echo "${LOG_PREFIX} Starting Redis backup..."

# 1. Trigger RDB save
docker exec "${REDIS_CONTAINER}" redis-cli BGSAVE > /dev/null
sleep 2  # Wait for background save to complete

# 2. Copy RDB from container
mkdir -p "${BACKUP_DIR}/hourly"
docker cp "${REDIS_CONTAINER}:/data/dump.rdb" "${BACKUP_DIR}/hourly/dump-${TIMESTAMP}.rdb"

# 3. Verify backup file
BACKUP_FILE="${BACKUP_DIR}/hourly/dump-${TIMESTAMP}.rdb"
if [ ! -s "${BACKUP_FILE}" ]; then
    echo "${LOG_PREFIX} ERROR: Backup file is empty or missing: ${BACKUP_FILE}" >&2
    exit 1
fi

BACKUP_SIZE=$(stat -c%s "${BACKUP_FILE}" 2>/dev/null || stat -f%z "${BACKUP_FILE}" 2>/dev/null)
echo "${LOG_PREFIX} Hourly backup created: dump-${TIMESTAMP}.rdb (${BACKUP_SIZE} bytes)"

# 4. Hourly retention: keep last 24
HOURLY_COUNT=$(find "${BACKUP_DIR}/hourly" -name "dump-*.rdb" -type f | wc -l)
if [ "${HOURLY_COUNT}" -gt 24 ]; then
    find "${BACKUP_DIR}/hourly" -name "dump-*.rdb" -type f -printf '%T+ %p\n' \
        | sort | head -n -24 | awk '{print $2}' | xargs -r rm -f
    echo "${LOG_PREFIX} Cleaned hourly backups, kept last 24"
fi

# 5. Daily backup at midnight (compressed)
CURRENT_HOUR=$(date +%H)
if [ "${CURRENT_HOUR}" = "00" ]; then
    mkdir -p "${BACKUP_DIR}/daily"
    gzip -c "${BACKUP_FILE}" > "${BACKUP_DIR}/daily/dump-${DATE}.rdb.gz"
    echo "${LOG_PREFIX} Daily compressed backup created: dump-${DATE}.rdb.gz"

    # Daily retention: keep last 30
    DAILY_COUNT=$(find "${BACKUP_DIR}/daily" -name "dump-*.rdb.gz" -type f | wc -l)
    if [ "${DAILY_COUNT}" -gt 30 ]; then
        find "${BACKUP_DIR}/daily" -name "dump-*.rdb.gz" -type f -printf '%T+ %p\n' \
            | sort | head -n -30 | awk '{print $2}' | xargs -r rm -f
        echo "${LOG_PREFIX} Cleaned daily backups, kept last 30"
    fi
fi

echo "${LOG_PREFIX} Redis backup completed successfully"
