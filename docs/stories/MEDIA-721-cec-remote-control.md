# MEDIA-721: CEC Remote Control Integration

## Story

**Epic:** MEDIA-FE-TV — TV Frontend  
**Priority:** High  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-720 (TV frontend)

---

## Summary

Map the TV remote's CEC (Consumer Electronics Control) signals over HDMI to media playback commands. Users control playback with their standard TV remote — no app needed for basic controls. A lightweight CEC listener on the Pi translates button presses to API calls.

---

## CEC Button Mapping

| TV Remote Button  | CEC Code         | Action                 | API Call                                    |
| ----------------- | ---------------- | ---------------------- | ------------------------------------------- |
| **Play/Pause**    | `play` / `pause` | Toggle play/pause      | `POST /player/play` or `POST /player/pause` |
| **Right →**       | `right`          | Skip to next track     | `POST /player/skip`                         |
| **Left ←**        | `left`           | Restart current track  | `POST /player/play` (with position 0)       |
| **Back / Return** | `back`           | Stop playback          | `POST /player/stop`                         |
| **OK / Select**   | `select`         | Toggle overlay bar     | Local (no API call)                         |
| **Up ↑**          | `up`             | Navigate up (search)   | Local navigation                            |
| **Down ↓**        | `down`           | Navigate down (search) | Local navigation                            |

---

## Architecture

```
TV Remote
    │ (infrared)
    ▼
TV (Samsung/LG/etc)
    │ (HDMI-CEC)
    ▼
Pi HDMI port
    │
    ▼
cec-client (libCEC)
    │ (stdout pipe)
    ▼
cec-bridge.sh (bash script)
    │ (HTTP calls)
    ▼
Media Platform API
```

---

## CEC Bridge Script

```bash
#!/bin/bash
# /opt/media-platform/cec-bridge.sh
# Listens for CEC key presses and sends API commands

API_BASE="http://localhost:8080"
WORKER_KEY="${MEDIA_WORKER_KEY}"
OVERLAY_VISIBLE=false

send_command() {
  curl -s -X POST "$API_BASE$1" \
    -H "X-Worker-Key: $WORKER_KEY" \
    -H "Content-Type: application/json" \
    ${2:+-d "$2"} &
}

# Listen to CEC events
cec-client -d 1 | while read -r line; do
  case "$line" in
    *"key pressed: play"*)
      send_command "/player/play"
      ;;
    *"key pressed: pause"*)
      send_command "/player/pause"
      ;;
    *"key pressed: right"*)
      send_command "/player/skip"
      ;;
    *"key pressed: left"*)
      send_command "/player/play" '{"startAtSeconds": 0}'
      ;;
    *"key pressed: return"*|*"key pressed: back"*)
      send_command "/player/stop"
      ;;
    *"key pressed: select"*)
      # Toggle overlay — sent to TV frontend via custom event
      if [ "$OVERLAY_VISIBLE" = true ]; then
        OVERLAY_VISIBLE=false
      else
        OVERLAY_VISIBLE=true
      fi
      # Signal the browser via a local WebSocket or file
      echo "{\"action\":\"toggleOverlay\"}" > /tmp/cec-event
      ;;
    *"key pressed: up"*)
      echo "{\"action\":\"navigateUp\"}" > /tmp/cec-event
      ;;
    *"key pressed: down"*)
      echo "{\"action\":\"navigateDown\"}" > /tmp/cec-event
      ;;
  esac
done
```

---

## Alternative: Node.js CEC Bridge

For better integration with the TV frontend (WebSocket communication):

```javascript
// /opt/media-platform/cec-bridge.js
import { spawn } from "child_process";
import { WebSocketServer } from "ws";

const API_BASE = process.env.API_BASE || "http://localhost:8080";
const WORKER_KEY = process.env.MEDIA_WORKER_KEY;
const wss = new WebSocketServer({ port: 8089 });

// Broadcast to all connected clients (the TV browser)
function broadcast(message) {
  wss.clients.forEach((client) => {
    if (client.readyState === 1) {
      client.send(JSON.stringify(message));
    }
  });
}

const cec = spawn("cec-client", ["-d", "1"]);

cec.stdout.on("data", (data) => {
  const line = data.toString();

  if (line.includes("key pressed: play")) {
    fetch(`${API_BASE}/player/play`, {
      method: "POST",
      headers: { "X-Worker-Key": WORKER_KEY },
    });
  } else if (line.includes("key pressed: select")) {
    broadcast({ action: "toggleOverlay" });
  } else if (line.includes("key pressed: up")) {
    broadcast({ action: "navigateUp" });
  }
  // ... etc
});
```

The TV frontend connects to the local WebSocket for local-only commands:

```javascript
// In tv.js
const cecWs = new WebSocket("ws://localhost:8089");
cecWs.onmessage = (event) => {
  const { action } = JSON.parse(event.data);
  switch (action) {
    case "toggleOverlay":
      toggleOverlay();
      break;
    case "navigateUp":
      searchUI.navigateUp();
      break;
    case "navigateDown":
      searchUI.navigateDown();
      break;
  }
};
```

---

## Pi Setup

### Install libCEC

```bash
sudo apt-get install -y cec-utils
```

### Verify CEC works

```bash
echo 'scan' | cec-client -s -d 1
# Should show connected TV device
```

### Systemd Service

```ini
# /etc/systemd/system/cec-bridge.service
[Unit]
Description=CEC Remote Bridge for Media Platform
After=network.target

[Service]
ExecStart=/opt/media-platform/cec-bridge.sh
Restart=always
RestartSec=5
Environment=MEDIA_WORKER_KEY=your-worker-key

[Install]
WantedBy=multi-user.target
```

---

## Debounce

CEC sends duplicate key events rapidly. Debounce with a 300ms window:

```bash
LAST_KEY=""
LAST_TIME=0

handle_key() {
  local key="$1"
  local now=$(date +%s%N | cut -b1-13)  # milliseconds
  local diff=$((now - LAST_TIME))

  if [ "$key" = "$LAST_KEY" ] && [ "$diff" -lt 300 ]; then
    return  # Skip duplicate
  fi

  LAST_KEY="$key"
  LAST_TIME="$now"
  # Process key...
}
```

---

## Tasks

- [ ] Install `cec-utils` on Pi
- [ ] Verify CEC connectivity with TV (`cec-client scan`)
- [ ] Create CEC bridge script (bash or Node.js)
- [ ] Map all 7 button actions
- [ ] Implement 300ms debounce for duplicate events
- [ ] Create WebSocket bridge for local commands (OK, Up, Down)
- [ ] Add WebSocket client code to `tv.js`
- [ ] Create systemd service for CEC bridge
- [ ] Test with actual TV remote (Samsung, LG, or similar)
- [ ] Handle CEC disconnection/reconnection
- [ ] Document TV remote button mapping for users

---

## Acceptance Criteria

- [ ] Play/Pause toggles playback via API
- [ ] Right arrow skips to next track
- [ ] Left arrow restarts current track
- [ ] Back/Return stops playback
- [ ] OK toggles overlay bar on TV
- [ ] Up/Down navigate in search UI
- [ ] No duplicate commands from rapid button presses (debounce)
- [ ] CEC bridge auto-starts on Pi boot
- [ ] Reconnects if CEC connection drops
- [ ] Works with standard TV remotes (not brand-specific)

---

## Notes

- CEC support varies by TV brand — some brands call it different names:
  - Samsung: Anynet+
  - LG: SimpLink
  - Sony: Bravia Sync
  - Philips: EasyLink
- Not all remotes have all buttons — Play/Pause and arrow keys are the most universal
- Volume is controlled by the TV directly (not through the Pi)
