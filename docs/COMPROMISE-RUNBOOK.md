# Account Compromise Recovery Runbook

> **Purpose**: Define containment, revoke, rotate, restore, and verification steps
> for a primary Google account or infrastructure credential compromise.

## Severity Classification

| Level    | Description                                    | Response Time |
|----------|------------------------------------------------|---------------|
| Critical | Google OAuth token leaked or used maliciously  | Immediate     |
| High     | Vault secret or API key exposed                | < 15 min      |
| Medium   | Suspicious access pattern detected             | < 1 hour      |
| Low      | Failed auth spike from unknown source          | < 4 hours     |

## 1. Containment (Immediate)

### Google Account Compromise

1. **Revoke all OAuth tokens immediately**
   ```bash
   # Via VaultFacade API (if accessible)
   curl -X POST http://vaultfacade:5100/api/secrets/rotate \
     -H "Content-Type: application/json" \
     -d '{"path": "secret/data/media-platform/google-oauth"}'
   ```

2. **Revoke tokens at Google**
   - Go to https://myaccount.google.com/permissions
   - Remove the Media Platform app
   - Change Google account password

3. **Kill switch — disable all YouTube write actions**
   ```bash
   # Stop the API to prevent any further actions
   docker stop media-api

   # Or use Redis to set a kill switch flag
   docker exec media-redis redis-cli SET media:kill-switch "true"
   ```

4. **Isolate the network segment**
   - Block outbound traffic from the media platform container to YouTube API
   - Keep internal Redis/health access for forensics

### Infrastructure Secret Compromise

1. **Rotate compromised secrets via VaultFacade**
   ```bash
   curl -X POST http://vaultfacade:5100/api/secrets/rotate \
     -H "Content-Type: application/json" \
     -d '{"path": "secret/data/<affected-path>"}'
   ```

2. **Restart affected services** to pick up rotated secrets
   ```bash
   docker restart media-api
   ```

## 2. Investigation

1. **Collect audit logs**
   ```bash
   # Check API request logs
   docker logs media-api --since "2h" | grep -E "error|unauthorized|403|401"

   # Check Redis command history
   docker exec media-redis redis-cli MONITOR > /tmp/redis-monitor.log &
   ```

2. **Review analytics for anomalies**
   ```bash
   curl http://localhost:5000/analytics?from=2026-03-13T00:00:00Z
   ```

3. **Check webhook delivery history** for unexpected external calls

4. **Document timeline**: When was the first suspicious activity? What actions were taken?

## 3. Rotate All Credentials

After containment, rotate ALL secrets even if only one was confirmed compromised:

| Secret                  | Rotation Method                           |
|-------------------------|-------------------------------------------|
| Google OAuth tokens     | Re-authenticate via consent flow          |
| Redis password          | Update `redis.conf` + restart             |
| API keys                | `mcp_vaultfacade_rotate_secret`           |
| JWT signing keys        | `mcp_vaultfacade_rotate_secret`           |
| Encryption keys         | Generate new + re-encrypt stored data     |

## 4. Restore and Verify

1. **Verify Redis data integrity**
   ```bash
   docker exec media-redis redis-cli DBSIZE
   docker exec media-redis redis-cli INFO persistence
   ```

2. **Health check all services**
   ```bash
   curl http://localhost:5000/health/ready
   curl http://localhost:5000/health/live
   ```

3. **Re-enable kill switch** (if used)
   ```bash
   docker exec media-redis redis-cli DEL media:kill-switch
   docker start media-api
   ```

4. **Verify normal operation**
   - Queue a test video and confirm playback
   - Check SSE events are flowing
   - Verify webhook deliveries resume

5. **Monitor for recurrence** over next 24–48 hours
   - Watch for unusual analytics spikes
   - Check audit logs hourly for first 4 hours

## 5. Post-Incident

### Incident Report Template

```markdown
## Incident Report: [Date]

**Severity**: Critical / High / Medium / Low
**Duration**: [Start] to [End]
**Impact**: [What was affected]

### Timeline
- HH:MM — Detection
- HH:MM — Containment started
- HH:MM — Investigation began
- HH:MM — Credentials rotated
- HH:MM — Services restored
- HH:MM — All-clear confirmed

### Root Cause
[Description]

### Actions Taken
1. ...
2. ...

### Lessons Learned
- [What worked]
- [What didn't]
- [What to improve]

### Follow-up Actions
- [ ] Action item 1
- [ ] Action item 2
```

### Recovery Drill Schedule

Run a simulated compromise drill **quarterly** to validate this runbook:

1. Simulate token revocation (dry run mode)
2. Time the containment steps
3. Verify rotation procedures work
4. Update this runbook with any findings

**Target drill time**: < 15 minutes from detection to full restore.

## Emergency Contacts

| Role              | Contact                    |
|-------------------|----------------------------|
| System admin      | [Configure in `.vault-env`]|
| Google account    | [Primary account holder]   |
| Infrastructure    | [Homelab operator]         |
