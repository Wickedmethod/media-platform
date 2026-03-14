# EPIC: MEDIA-001 Homelab Media Platform

## Goal

Create a self-hosted media control system capable of:

- Playing YouTube videos on a TV
- Queueing videos from phone/browser
- Controlling playback via TV remote
- Liking videos
- Adding videos to playlists
- Skipping or modifying queue

## Playback Device

- Raspberry Pi 4 Model B connected to TV

## Playback Engine

- mpv media player
- yt-dlp

## Infrastructure Integration

- Redis
- Caddy
- Keycloak
- HashiCorp Vault

## Architecture

Phone / Browser / TV Remote
-> Caddy (infra)
-> youtube-service
-> Redis queue + YouTube API
-> Raspberry Pi Player
-> TV

## Stories

Story files are tracked under [stories](stories).
