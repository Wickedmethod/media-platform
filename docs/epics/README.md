# Media Platform Epics Index

This folder contains one file per epic and one file per story.

## Decisions

- [Architecture Decisions](ARCHITECTURE-DECISIONS.md)

## Epics

- [MEDIA-001 Homelab Media Platform](MEDIA-001.md)
- [MEDIA-002 Raspberry Pi Player Node](MEDIA-002.md)
- [MEDIA-003 Queue and Player API](MEDIA-003.md)
- [MEDIA-004 YouTube Integration](MEDIA-004.md)
- [MEDIA-005 Remote Control Layer](MEDIA-005.md)
- [MEDIA-006 Web UI](MEDIA-006.md)
- [MEDIA-007 Queue Logic](MEDIA-007.md)
- [MEDIA-008 Multi-device Playback](MEDIA-008.md)
- [MEDIA-009 Spotify Integration](MEDIA-009.md)
- [MEDIA-010 Chromecast Compatibility](MEDIA-010.md)

## Stories

- [MEDIA-201 Install Raspberry Pi OS](stories/MEDIA-201.md)
- [MEDIA-202 Install Playback Dependencies](stories/MEDIA-202.md)
- [MEDIA-203 Implement Player Worker](stories/MEDIA-203.md)
- [MEDIA-204 Create Systemd Service](stories/MEDIA-204.md)
- [MEDIA-205 HDMI + CEC Remote Support](stories/MEDIA-205.md)
- [MEDIA-401 Implement OAuth Login](stories/MEDIA-401.md)
- [MEDIA-402 Store Refresh Tokens in Vault](stories/MEDIA-402.md)
- [MEDIA-403 Like Video Endpoint](stories/MEDIA-403.md)
- [MEDIA-404 Add to Playlist Endpoint](stories/MEDIA-404.md)
- [MEDIA-501 CEC Event Listener](stories/MEDIA-501.md)
- [MEDIA-502 Command Mapping](stories/MEDIA-502.md)
- [MEDIA-503 Forward Commands to Player API](stories/MEDIA-503.md)

## Proposed Stories - Must Have

- [MEDIA-601 Queue State Machine](stories/MEDIA-601.md)
- [MEDIA-602 Playback Retry and Failure Handling](stories/MEDIA-602.md)
- [MEDIA-603 Health Checks and Watchdog](stories/MEDIA-603.md)
- [MEDIA-604 AuthZ Roles, Rate Limits, and Audit Logs](stories/MEDIA-604.md)
- [MEDIA-605 Vault-Only Secret and Token Handling](stories/MEDIA-605.md)
- [MEDIA-606 Redis Persistence and Restore Runbook](stories/MEDIA-606.md)
- [MEDIA-607 Idempotent Player Commands](stories/MEDIA-607.md)

## Proposed Stories - Nice To Have

- [MEDIA-608 Resume Playback and Start Timestamp](stories/MEDIA-608.md)
- [MEDIA-609 Smart Queue Modes](stories/MEDIA-609.md)
- [MEDIA-610 Multi-User Queue and Voting](stories/MEDIA-610.md)
- [MEDIA-611 TV On-Screen Overlay](stories/MEDIA-611.md)
- [MEDIA-612 Local Video Caching](stories/MEDIA-612.md)
- [MEDIA-613 Event Notifications](stories/MEDIA-613.md)
- [MEDIA-614 Usage and Reliability Analytics](stories/MEDIA-614.md)

## Proposed Stories - Advanced

- [MEDIA-615 Multi-Room Synchronized Playback](stories/MEDIA-615.md)
- [MEDIA-616 Offline Fallback Content](stories/MEDIA-616.md)
- [MEDIA-617 Playback Policy Engine](stories/MEDIA-617.md)

## Proposed Stories - Architecture and Platform

- [MEDIA-618 Realtime Transport Decision](stories/MEDIA-618.md)
- [MEDIA-619 Redis Schema and Key Design](stories/MEDIA-619.md)
- [MEDIA-620 API Contract and Versioning Strategy](stories/MEDIA-620.md)
- [MEDIA-621 Internal Network Security Baseline](stories/MEDIA-621.md)
- [MEDIA-622 Worker API Trust Boundary](stories/MEDIA-622.md)
- [MEDIA-623 Clean Architecture Enforcement in CI](stories/MEDIA-623.md)
- [MEDIA-624 API Worker Contract Tests](stories/MEDIA-624.md)
- [MEDIA-625 Primary Account OAuth Hardening](stories/MEDIA-625.md)
- [MEDIA-626 Keycloak Gate for YouTube Actions](stories/MEDIA-626.md)

## Proposed Stories - Account Security Hardening

- [MEDIA-627 OAuth Scope Minimization and Review](stories/MEDIA-627.md)
- [MEDIA-628 Emergency Token Revoke and Kill Switch](stories/MEDIA-628.md)
- [MEDIA-629 Vault Policy Hardening and Secret Rotation](stories/MEDIA-629.md)
- [MEDIA-630 YouTube Action Approval Controls](stories/MEDIA-630.md)
- [MEDIA-631 Security Alerts and Anomaly Detection](stories/MEDIA-631.md)
- [MEDIA-632 Device and Session Management](stories/MEDIA-632.md)
- [MEDIA-633 Account Compromise Runbook and Recovery Drill](stories/MEDIA-633.md)

## Security Checklist

- [Primary Account Safety Checklist](PRIMARY-ACCOUNT-SAFETY-CHECKLIST.md)

## Target Outcome

A self-hosted YouTube TV system where users can control playback from phone, browser, and TV remote, with queueing, likes, and playlist actions integrated into the homelab stack.
