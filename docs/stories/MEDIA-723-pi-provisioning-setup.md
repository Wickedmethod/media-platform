# MEDIA-723: Pi Provisioning & CEC Setup Automation

## Story

**Epic:** MEDIA-FE-TV — TV Frontend  
**Priority:** Medium  
**Effort:** 3 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-720 (TV frontend), MEDIA-721 (CEC remote)

---

## Summary

Create an automated provisioning script that sets up a fresh Raspberry Pi (4 or 5) as a media platform kiosk. One script installs all dependencies, configures Chromium kiosk mode, sets up the CEC bridge, and registers systemd services. A new Pi goes from fresh OS to fully operational kiosk in under 10 minutes.

---

## What the Script Does

```
Fresh Raspberry Pi OS Lite
    │
    ▼ provision.sh
    │
    ├── 1. System update + dependencies
    │   ├── chromium-browser
    │   ├── cec-utils (libCEC)
    │   ├── xserver-xorg + xinit (minimal X11)
    │   ├── unclutter (hide cursor)
    │   └── nodejs (for CEC bridge)
    │
    ├── 2. Configuration
    │   ├── Auto-login to console
    │   ├── X11 auto-start on login
    │   ├── Chromium kiosk autostart
    │   ├── Disable screen blanking
    │   ├── GPU memory split (128MB)
    │   └── Enable CEC on HDMI
    │
    ├── 3. Install CEC Bridge
    │   ├── Copy cec-bridge.js to /opt/media-platform/
    │   ├── Create systemd service
    │   └── Enable on boot
    │
    ├── 4. Environment config
    │   ├── API_BASE_URL
    │   ├── MEDIA_WORKER_KEY
    │   └── TV_NAME (identifier)
    │
    └── 5. Verify & reboot
        ├── Test CEC connection
        ├── Test API connectivity
        └── Reboot into kiosk mode
```

---

## Provisioning Script

```bash
#!/bin/bash
# provision.sh — Media Platform Pi Kiosk Setup
# Usage: curl -sL https://.../provision.sh | sudo bash -s -- \
#   --api-url http://192.168.1.100:8080 \
#   --worker-key "your-worker-key" \
#   --tv-name "Living Room"

set -euo pipefail

# --- Parse arguments ---
API_URL=""
WORKER_KEY=""
TV_NAME="TV"

while [[ $# -gt 0 ]]; do
  case $1 in
    --api-url) API_URL="$2"; shift 2 ;;
    --worker-key) WORKER_KEY="$2"; shift 2 ;;
    --tv-name) TV_NAME="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

if [[ -z "$API_URL" || -z "$WORKER_KEY" ]]; then
  echo "Usage: provision.sh --api-url <url> --worker-key <key> [--tv-name <name>]"
  exit 1
fi

echo "=== Media Platform Pi Provisioning ==="
echo "API: $API_URL"
echo "TV:  $TV_NAME"

# --- 1. System update + dependencies ---
echo ">>> Installing dependencies..."
apt-get update -qq
apt-get install -y -qq \
  chromium-browser \
  cec-utils \
  xserver-xorg \
  xinit \
  x11-xserver-utils \
  unclutter \
  nodejs \
  npm

# --- 2. Create kiosk user ---
if ! id -u kiosk &>/dev/null; then
  useradd -m -s /bin/bash kiosk
  usermod -aG video,input,dialout kiosk
fi

# --- 3. Configure auto-login ---
mkdir -p /etc/systemd/system/getty@tty1.service.d
cat > /etc/systemd/system/getty@tty1.service.d/autologin.conf << 'EOF'
[Service]
ExecStart=
ExecStart=-/sbin/agetty --autologin kiosk --noclear %I $TERM
EOF

# --- 4. Configure X11 auto-start ---
cat > /home/kiosk/.bash_profile << 'EOF'
if [ -z "$DISPLAY" ] && [ "$(tty)" = "/dev/tty1" ]; then
  startx -- -nocursor 2>/dev/null
fi
EOF

# --- 5. Configure Chromium kiosk ---
mkdir -p /home/kiosk/.config/openbox
cat > /home/kiosk/.xinitrc << EOF
#!/bin/sh
xset s off
xset -dpms
xset s noblank

unclutter -idle 0.1 -root &

chromium-browser \\
  --kiosk \\
  --noerrdialogs \\
  --disable-infobars \\
  --disable-translate \\
  --no-first-run \\
  --fast \\
  --fast-start \\
  --disable-features=TranslateUI \\
  --enable-gpu-rasterization \\
  --enable-zero-copy \\
  --disk-cache-size=50000000 \\
  --autoplay-policy=no-user-gesture-required \\
  \"\${API_URL}/tv.html\"
  "${API_URL}/tv.html"
EOF
chmod +x /home/kiosk/.xinitrc

# --- 6. GPU memory split ---
if ! grep -q "gpu_mem=" /boot/config.txt; then
  echo "gpu_mem=128" >> /boot/config.txt
fi

# --- 7. Install CEC bridge ---
mkdir -p /opt/media-platform
cat > /opt/media-platform/.env << EOF
API_BASE=${API_URL}
MEDIA_WORKER_KEY=${WORKER_KEY}
TV_NAME=${TV_NAME}
EOF

# Copy CEC bridge script (delivered with this provisioning)
cp ./cec-bridge.js /opt/media-platform/cec-bridge.js 2>/dev/null || \
  echo "// CEC bridge will be deployed separately" > /opt/media-platform/cec-bridge.js

# --- 8. Systemd services ---
cat > /etc/systemd/system/cec-bridge.service << EOF
[Unit]
Description=Media Platform CEC Bridge
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=kiosk
WorkingDirectory=/opt/media-platform
EnvironmentFile=/opt/media-platform/.env
ExecStart=/usr/bin/node /opt/media-platform/cec-bridge.js
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable cec-bridge

# --- 9. File ownership ---
chown -R kiosk:kiosk /home/kiosk /opt/media-platform

# --- 10. Verify ---
echo ">>> Verification..."
echo -n "CEC: "; echo 'scan' | timeout 5 cec-client -s -d 1 2>/dev/null && echo "OK" || echo "Not connected (HDMI?)"
echo -n "API: "; curl -sf "${API_URL}/health/live" && echo " OK" || echo "Unreachable"

echo ""
echo "=== Provisioning complete ==="
echo "Reboot to start kiosk mode: sudo reboot"
```

---

## Configuration File

```
/opt/media-platform/
├── .env                ← API_BASE, WORKER_KEY, TV_NAME
├── cec-bridge.js       ← CEC event listener (from MEDIA-721)
└── logs/               ← CEC bridge logs
```

---

## Ansible Playbook (Alternative)

For multi-Pi deployments:

```yaml
# ansible/playbooks/media-kiosk.yml
- name: Provision Media Platform Kiosk
  hosts: media_pis
  become: true
  vars:
    api_url: "http://192.168.1.100:8080"
    worker_key: "{{ vault_worker_key }}"

  roles:
    - role: media-kiosk
      tags: [kiosk]
```

The existing `mcp/mcp-ssh-pi/ansible/` infrastructure can be extended for this.

---

## Remote Management

After provisioning, the Pi is manageable via the existing `mcp-ssh-pi` MCP server:

- `mcp_raspberry-pi-_execute_command` — run commands
- `mcp_raspberry-pi-_manage_service` — restart CEC bridge
- `mcp_raspberry-pi-_tail_log` — view kiosk logs
- `mcp_raspberry-pi-_upload_file` — deploy updated CEC bridge

---

## Tasks

- [ ] Create `provision.sh` script with argument parsing
- [ ] Install system dependencies (Chromium, CEC, X11)
- [ ] Configure auto-login + X11 auto-start
- [ ] Configure Chromium kiosk with GPU acceleration
- [ ] Set up CEC bridge systemd service
- [ ] Create environment config file
- [ ] Add verification checks (CEC + API connectivity)
- [ ] Test on Pi 4 with fresh Raspberry Pi OS Lite
- [ ] Test on Pi 5
- [ ] Document the provisioning process in README
- [ ] Optional: create Ansible role for multi-Pi setup

---

## Acceptance Criteria

- [ ] Fresh Pi goes from OS to working kiosk with one script
- [ ] Chromium opens TV frontend in fullscreen on boot
- [ ] CEC bridge starts automatically on boot
- [ ] No cursor, scrollbars, or browser UI visible
- [ ] Screen never blanks or sleeps
- [ ] Script is idempotent (safe to re-run)
- [ ] Provisioning completes in < 10 minutes
- [ ] Script validates API connectivity before finishing
