#!/bin/bash
# CEC Remote Bridge — translates HDMI-CEC button presses to Media Platform API calls
# Deployed to: /opt/media-platform/cec-bridge.sh
# Requires: cec-utils (sudo apt-get install -y cec-utils)

set -euo pipefail

API_BASE="${MEDIA_API_BASE:-http://localhost:8080/api/v1}"
WORKER_KEY="${MEDIA_WORKER_KEY:?MEDIA_WORKER_KEY must be set}"
CEC_WS_PORT="${CEC_WS_PORT:-8089}"

# Debounce: ignore duplicate keys within 300ms
LAST_KEY=""
LAST_TIME=0
DEBOUNCE_MS=300

log() {
  echo "[$(date -u +%Y-%m-%dT%H:%M:%SZ)] $*"
}

send_command() {
  local endpoint="$1"
  shift
  curl -sf -X POST "${API_BASE}${endpoint}" \
    -H "X-Worker-Key: ${WORKER_KEY}" \
    -H "Content-Type: application/json" \
    "$@" &>/dev/null &
}

# Send local event to TV browser via temp file (read by inotify or polling)
send_local_event() {
  local action="$1"
  echo "{\"action\":\"${action}\",\"ts\":$(date +%s%N)}" > /tmp/cec-event
}

debounce() {
  local key="$1"
  local now
  now=$(date +%s%N | cut -b1-13)
  local diff=$(( now - LAST_TIME ))

  if [ "$key" = "$LAST_KEY" ] && [ "$diff" -lt "$DEBOUNCE_MS" ]; then
    return 1
  fi

  LAST_KEY="$key"
  LAST_TIME="$now"
  return 0
}

handle_key() {
  local key="$1"

  if ! debounce "$key"; then
    return
  fi

  case "$key" in
    play)
      log "CEC: play"
      send_command "/player/play"
      ;;
    pause)
      log "CEC: pause"
      send_command "/player/pause"
      ;;
    right)
      log "CEC: skip →"
      send_command "/player/skip"
      ;;
    left)
      log "CEC: restart ←"
      send_command "/player/play" -d '{"startAtSeconds": 0}'
      ;;
    return|back)
      log "CEC: stop"
      send_command "/player/stop"
      ;;
    select)
      log "CEC: toggle overlay"
      send_local_event "toggleOverlay"
      ;;
    up)
      log "CEC: navigate up"
      send_local_event "navigateUp"
      ;;
    down)
      log "CEC: navigate down"
      send_local_event "navigateDown"
      ;;
    *)
      # Ignore unmapped keys
      ;;
  esac
}

log "Starting CEC bridge (API: ${API_BASE})"
log "Waiting for CEC events..."

# Listen to CEC events from TV
cec-client -d 1 2>/dev/null | while read -r line; do
  # cec-client outputs: "key pressed: <keyname> (<duration>)"
  if [[ "$line" =~ "key pressed:" ]]; then
    key=$(echo "$line" | sed -n 's/.*key pressed: \([a-zA-Z]*\).*/\1/p')
    if [ -n "$key" ]; then
      handle_key "$key"
    fi
  fi
done

log "CEC client exited — restarting in 5s"
