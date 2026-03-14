# MEDIA-760: Graceful Player Shutdown Handling

## Story

**Epic:** MEDIA-PI-OPS — Player Operations & Diagnostics  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-724 (Player Heartbeat), MEDIA-729 (Player Registration)

---

## Summary

When a Raspberry Pi player receives a shutdown signal (SIGTERM from systemd, manual stop, or OS reboot), it should perform a clean shutdown: notify the API that it's going offline, flush any pending logs, and save playback position. This prevents the API from treating a planned shutdown as an unexpected crash and triggering false alerts.

---

## Architecture

```
systemd stop / reboot / SIGTERM
    │
    ▼
Player Process (Node.js / Chromium)
    │ catches SIGTERM / SIGINT
    ▼
┌─────────────────────────────┐
│ Graceful Shutdown Sequence  │
│ 1. Save playback position   │
│ 2. Flush pending log buffer │
│ 3. POST /worker/disconnect  │
│ 4. Close SSE connection     │
│ 5. Exit process              │
└─────────────────────────────┘
    │
    ▼
API receives disconnect
    │ marks player: status = "offline" (planned)
    ▼
No crash alert triggered
```

---

## Shutdown vs. Crash — How the API Differentiates

| Scenario              | API sees                                            | Player status        | Alert? |
| --------------------- | --------------------------------------------------- | -------------------- | ------ |
| **Graceful shutdown** | `POST /worker/disconnect` with `reason: "shutdown"` | `offline`            | No     |
| **Unexpected crash**  | Heartbeat timeout (no disconnect call)              | `unreachable`        | Yes    |
| **Network loss**      | Heartbeat timeout (no disconnect call)              | `unreachable`        | Yes    |
| **Manual restart**    | Disconnect → re-register within timeout             | `offline` → `online` | No     |

---

## Player-Side Implementation

### Signal Handler (Node.js)

```typescript
// player/src/shutdown.ts
import { playerApi } from "./api-client";
import { logBuffer } from "./log-buffer";
import { playbackState } from "./playback";

let isShuttingDown = false;

export function registerShutdownHandlers() {
  const signals: NodeJS.Signals[] = ["SIGTERM", "SIGINT", "SIGHUP"];

  for (const signal of signals) {
    process.on(signal, () => gracefulShutdown(signal));
  }
}

async function gracefulShutdown(signal: string) {
  if (isShuttingDown) return;
  isShuttingDown = true;

  console.log(`[shutdown] Received ${signal}, starting graceful shutdown...`);

  const timeout = setTimeout(() => {
    console.error("[shutdown] Timeout reached, forcing exit");
    process.exit(1);
  }, 10_000); // 10s max for cleanup

  try {
    // 1. Save current playback position
    if (playbackState.isPlaying) {
      await playerApi.reportPosition(playbackState.position);
    }

    // 2. Flush pending logs
    await logBuffer.flush();

    // 3. Notify API of planned disconnect
    await playerApi.disconnect({ reason: "shutdown", signal });

    // 4. Close SSE connection
    playerApi.closeEventStream();

    console.log("[shutdown] Clean shutdown complete");
  } catch (err) {
    console.error("[shutdown] Error during cleanup:", err);
  } finally {
    clearTimeout(timeout);
    process.exit(0);
  }
}
```

### Systemd Integration

```ini
# /etc/systemd/system/media-player.service
[Service]
ExecStart=/usr/bin/node /opt/media-player/index.js
ExecStop=/bin/kill -SIGTERM $MAINPID
TimeoutStopSec=15
KillMode=mixed
KillSignal=SIGTERM
```

`TimeoutStopSec=15` gives the player 15s to clean up before systemd sends SIGKILL.

---

## API-Side Implementation

### Disconnect Endpoint

```csharp
// POST /worker/disconnect
public record DisconnectRequest(string Reason, string? Signal);

[HttpPost("disconnect")]
public async Task<IActionResult> Disconnect(
    [FromHeader(Name = "X-Worker-Key")] string workerKey,
    DisconnectRequest request)
{
    var player = await _playerService.GetByWorkerKey(workerKey);
    if (player is null) return NotFound();

    player.Status = PlayerStatus.Offline;
    player.DisconnectedAt = DateTimeOffset.UtcNow;
    player.DisconnectReason = request.Reason;

    await _playerService.Update(player);
    await _eventBus.Publish(new PlayerDisconnectedEvent(player.Id, request.Reason));

    return Ok();
}
```

### Heartbeat Timeout Differentiation

In the heartbeat monitor (MEDIA-724), when a player misses heartbeats:

```csharp
// Only alert if the player didn't disconnect gracefully
if (player.Status == PlayerStatus.Online && heartbeatAge > threshold)
{
    player.Status = PlayerStatus.Unreachable;
    await _alertService.PlayerUnreachable(player);
}
// If status is already Offline (graceful disconnect), skip alert
```

---

## Shutdown Timeout Strategy

```
SIGTERM received
    │
    ├─ t+0s:  Start cleanup sequence
    ├─ t+2s:  Position saved, logs flushed
    ├─ t+3s:  Disconnect API call complete
    ├─ t+3s:  Exit(0)
    │
    ├─ t+10s: Player's own timeout → Exit(1)
    └─ t+15s: systemd SIGKILL → forced kill
```

Three layers of protection: player timeout (10s), systemd timeout (15s), and the heartbeat monitor as a final catch-all.

---

## Tasks

- [ ] Implement `registerShutdownHandlers()` in player process
- [ ] Save playback position on shutdown
- [ ] Flush log buffer on shutdown
- [ ] Call `POST /worker/disconnect` with reason on shutdown
- [ ] Close SSE connection on shutdown
- [ ] Add 10s timeout fallback with forced exit
- [ ] Create `POST /worker/disconnect` endpoint on API
- [ ] Update heartbeat monitor to skip alerts for gracefully disconnected players
- [ ] Configure systemd with `TimeoutStopSec=15` and `KillMode=mixed`
- [ ] Write tests for shutdown sequence (signal → disconnect → exit)
- [ ] Write tests for crash vs. graceful differentiation in heartbeat monitor

---

## Acceptance Criteria

- [ ] Player catches SIGTERM/SIGINT and runs cleanup sequence
- [ ] Playback position saved before disconnect
- [ ] Pending logs flushed before disconnect
- [ ] API notified via `POST /worker/disconnect` with reason
- [ ] SSE connection closed cleanly
- [ ] API marks player as `offline` (not `unreachable`) on graceful disconnect
- [ ] No false crash alerts triggered for planned shutdowns
- [ ] Forced exit after 10s if cleanup hangs
- [ ] systemd SIGKILL after 15s as last resort
