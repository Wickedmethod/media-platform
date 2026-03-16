#!/bin/bash
# provision.sh — Media Platform Pi Kiosk Setup
# Sets up a fresh Raspberry Pi (4/5) as a media platform kiosk.
# Usage: sudo bash provision.sh --api-url http://192.168.1.100 --worker-key "key" [--tv-name "Living Room"]

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

if [[ $EUID -ne 0 ]]; then
  echo "Error: This script must be run as root (sudo)"
  exit 1
fi

echo "=== Media Platform Pi Provisioning ==="
echo "API: $API_URL"
echo "TV:  $TV_NAME"
echo ""

# --- 1. System update + dependencies ---
echo ">>> [1/9] Installing system dependencies..."
apt-get update -qq
apt-get install -y -qq \
  chromium-browser \
  cec-utils \
  xserver-xorg \
  xinit \
  x11-xserver-utils \
  unclutter \
  curl

# --- 2. Create kiosk user (idempotent) ---
echo ">>> [2/9] Creating kiosk user..."
if ! id -u kiosk &>/dev/null; then
  useradd -m -s /bin/bash kiosk
  usermod -aG video,input,dialout kiosk
  echo "    Created user 'kiosk'"
else
  echo "    User 'kiosk' already exists"
fi

# --- 3. Configure auto-login on tty1 ---
echo ">>> [3/9] Configuring auto-login..."
mkdir -p /etc/systemd/system/getty@tty1.service.d
cat > /etc/systemd/system/getty@tty1.service.d/autologin.conf << 'AUTOLOGIN_EOF'
[Service]
ExecStart=
ExecStart=-/sbin/agetty --autologin kiosk --noclear %I $TERM
AUTOLOGIN_EOF

# --- 4. Configure X11 auto-start on login ---
echo ">>> [4/9] Configuring X11 auto-start..."
cat > /home/kiosk/.bash_profile << 'BASH_PROFILE_EOF'
if [ -z "$DISPLAY" ] && [ "$(tty)" = "/dev/tty1" ]; then
  startx -- -nocursor 2>/dev/null
fi
BASH_PROFILE_EOF

# --- 5. Configure Chromium kiosk ---
echo ">>> [5/9] Configuring Chromium kiosk..."
cat > /home/kiosk/.xinitrc << XINITRC_EOF
#!/bin/sh
# Disable screen blanking / power management
xset s off
xset -dpms
xset s noblank

# Hide mouse cursor
unclutter -idle 0.1 -root &

# Launch Chromium in kiosk mode
exec chromium-browser \\
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
  --check-for-update-interval=31536000 \\
  "${API_URL}/#/tv"
XINITRC_EOF
chmod +x /home/kiosk/.xinitrc

# --- 6. GPU memory split ---
echo ">>> [6/9] Configuring GPU memory..."
CONFIG_TXT="/boot/config.txt"
# Newer Pi OS uses /boot/firmware/config.txt
if [[ -f /boot/firmware/config.txt ]]; then
  CONFIG_TXT="/boot/firmware/config.txt"
fi

if ! grep -q "^gpu_mem=" "$CONFIG_TXT" 2>/dev/null; then
  echo "gpu_mem=128" >> "$CONFIG_TXT"
  echo "    Set gpu_mem=128"
else
  echo "    gpu_mem already configured"
fi

# Disable screen blanking at kernel level
if ! grep -q "consoleblank=0" "$CONFIG_TXT" 2>/dev/null; then
  # Append to cmdline.txt if it exists (Pi OS)
  CMDLINE="/boot/cmdline.txt"
  if [[ -f /boot/firmware/cmdline.txt ]]; then
    CMDLINE="/boot/firmware/cmdline.txt"
  fi
  if [[ -f "$CMDLINE" ]] && ! grep -q "consoleblank=0" "$CMDLINE"; then
    sed -i 's/$/ consoleblank=0/' "$CMDLINE"
  fi
fi

# --- 7. Install media platform files ---
echo ">>> [7/9] Installing media platform files..."
mkdir -p /opt/media-platform/logs

cat > /opt/media-platform/.env << ENV_EOF
MEDIA_API_BASE=${API_URL}/api/v1
MEDIA_WORKER_KEY=${WORKER_KEY}
TV_NAME=${TV_NAME}
ENV_EOF

# Copy CEC bridge script if available alongside this script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [[ -f "${SCRIPT_DIR}/cec-bridge.sh" ]]; then
  cp "${SCRIPT_DIR}/cec-bridge.sh" /opt/media-platform/cec-bridge.sh
  chmod +x /opt/media-platform/cec-bridge.sh
  echo "    Installed cec-bridge.sh"
else
  echo "    Warning: cec-bridge.sh not found next to provision.sh — deploy it separately"
fi

# --- 8. Systemd services ---
echo ">>> [8/9] Installing systemd services..."

if [[ -f "${SCRIPT_DIR}/cec-bridge.service" ]]; then
  cp "${SCRIPT_DIR}/cec-bridge.service" /etc/systemd/system/cec-bridge.service
else
  cat > /etc/systemd/system/cec-bridge.service << SERVICE_EOF
[Unit]
Description=Media Platform CEC Bridge
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=kiosk
WorkingDirectory=/opt/media-platform
EnvironmentFile=/opt/media-platform/.env
ExecStart=/bin/bash /opt/media-platform/cec-bridge.sh
Restart=always
RestartSec=5
StandardOutput=journal
StandardError=journal
SyslogIdentifier=cec-bridge

NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/media-platform/logs /tmp

[Install]
WantedBy=multi-user.target
SERVICE_EOF
fi

systemctl daemon-reload
systemctl enable cec-bridge

# --- 9. File ownership ---
chown -R kiosk:kiosk /home/kiosk /opt/media-platform

# --- 10. Verification ---
echo ">>> [9/9] Verification..."
echo -n "  CEC: "
if echo 'scan' | timeout 5 cec-client -s -d 1 2>/dev/null; then
  echo "OK"
else
  echo "Not connected (no HDMI? — will work after reboot with TV on)"
fi

echo -n "  API: "
if curl -sf "${API_URL}/api/v1/health/live" --max-time 5 >/dev/null 2>&1; then
  echo "OK"
else
  echo "Unreachable (check network / API URL)"
fi

echo ""
echo "=== Provisioning complete ==="
echo ""
echo "Configuration:"
echo "  Kiosk URL: ${API_URL}/#/tv"
echo "  Worker key: (set in /opt/media-platform/.env)"
echo "  TV Name: ${TV_NAME}"
echo ""
echo "Next steps:"
echo "  1. Reboot to start kiosk mode: sudo reboot"
echo "  2. Verify the TV opens the media platform in fullscreen"
echo "  3. CEC bridge starts automatically on boot"
