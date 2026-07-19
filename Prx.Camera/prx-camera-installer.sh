#!/usr/bin/env bash
set -euo pipefail

REPO="pr0uxx/Prx.Camera"
BINARY_NAME="Prx.Camera"
STAGING="/tmp/${BINARY_NAME}"
INSTALL_PATH="/usr/local/bin/${BINARY_NAME}"
SERVICE_NAME="prx-camera.service"

# MODE can be: "LOCAL FILE", "LOCAL HTTP", "PRODUCTION"
MODE="${MODE:-PRODUCTION}"

LOCAL_FILE="${LOCAL_FILE:-}"

validate_mode() {
  case "$MODE" in
    "PRODUCTION"|"LOCAL FILE"|"LOCAL HTTP")
        echo "[MODE] Running in: $MODE"
        ;;
    *)
        echo "[ERROR] Invalid MODE: '$MODE'"
        echo "Valid options: PRODUCTION, LOCAL FILE, LOCAL HTTP"
        exit 1
        ;;
  esac
}

check_sudo() {
  if [[ "$EUID" -ne 0 ]]; then
      if ! command -v sudo >/dev/null 2>&1; then
          echo "[ERROR] This installer requires root privileges."
          echo "Please run as root or install sudo."
          exit 1
      fi
  
      if ! sudo -v >/dev/null 2>&1; then
          echo "[ERROR] This installer requires sudo privileges."
          echo "Please run: sudo $0"
          exit 1
      fi
  else
      echo "[INFO] Running as root — sudo not required."
  fi
}

run_as_root() {
    if [[ "$EUID" -eq 0 ]]; then
        "$@"
    else
        sudo "$@"
    fi
}

# Maps the running host to one of the release's asset-name suffixes:
# rpi-3, rpi-4, or linux-x64. Needed now that a release ships one binary
# per platform instead of a single asset.
detect_platform() {
    local machine
    machine="$(uname -m)"

    if [[ "$machine" == "x86_64" ]]; then
        echo "linux-x64"
        return
    fi

    if [[ "$machine" == "aarch64" || "$machine" == "arm64" ]]; then
        local model=""
        if [[ -f /proc/device-tree/model ]]; then
            model="$(tr -d '\0' < /proc/device-tree/model)"
        fi

        if [[ "$model" == *"Raspberry Pi 4"* ]]; then
            echo "rpi-4"
        elif [[ "$model" == *"Raspberry Pi 3"* ]]; then
            echo "rpi-3"
        else
            # rpi-3 and rpi-4 builds are identical today (same .NET RID),
            # so any unrecognised aarch64 board falls back to rpi-3.
            echo "rpi-3"
        fi
        return
    fi

    echo "[ERROR] Unsupported architecture: $machine" >&2
    exit 1
}

ensure_jq() {
    if command -v jq >/dev/null 2>&1; then
        return
    fi
    echo "[INFO] jq not found, installing..."
    run_as_root apt-get update -y
    run_as_root apt-get install -y jq
}


prepare_local_file_test() {
    echo "[TEST] Preparing LOCAL FILE test scenario..."

    # Create dummy ARM64 ELF if missing
    if [[ ! -f "$LOCAL_FILE" ]]; then
        echo "[TEST] Creating dummy ARM64 ELF at $LOCAL_FILE"
        echo 'int main(){return 0;}' > /tmp/test.c
        gcc /tmp/test.c -o "$LOCAL_FILE"
    fi
}

prepare_local_http_test() {
    echo "[TEST] Preparing LOCAL HTTP test scenario..."

    mkdir -p ~/mock-release
    cd ~/mock-release

    # Create dummy ELF
    echo "[TEST] Creating dummy ARM64 ELF for HTTP mock..."
    echo 'int main(){return 0;}' > test.c
    gcc test.c -o Prx.Camera

    # Create fake GitHub API JSON
    echo "[TEST] Creating latest.json..."
    echo '{"browser_download_url": "http://localhost:8080/Prx.Camera"}' > latest.json

    # Start HTTP server if not already running
    if ! pgrep -f "http.server 8080" >/dev/null; then
        echo "[TEST] Starting mock HTTP server on port 8080..."
        nohup python3 -m http.server 8080 >/dev/null 2>&1 &
        sleep 1
    fi

    cd -
}

check_existing_version() {
    if [[ -x "$INSTALL_PATH" ]]; then
        echo "[INFO] Checking existing installation..."
        
        # Execute the binary and capture the output
        local current_version
        current_version=$("$INSTALL_PATH" --version 2>/dev/null || true)

        if [[ "$current_version" == "$LATEST_VERSION" ]]; then
            echo "[INFO] Prx.Camera is already up to date ($current_version). Exiting."
            exit 0
        fi
        
        echo "[INFO] Updating from $current_version to $LATEST_VERSION..."
    else
        echo "[INFO] No existing installation found at $INSTALL_PATH."
    fi
}

download_release() {
    if [[ "$MODE" == "LOCAL FILE" ]]; then
        prepare_local_file_test
        echo "Using local file: $LOCAL_FILE"
        cp "$LOCAL_FILE" "$STAGING"
        return
    fi
    
    if [[ "$MODE" == "LOCAL HTTP" ]]; then
        prepare_local_http_test
        echo "Fetching latest release asset from LOCAL HTTP..."
        ASSET_URL=$(curl -s http://localhost:8080/latest.json | grep browser_download_url | cut -d '"' -f 4)
        echo "Downloading: $ASSET_URL"
        curl -L "$ASSET_URL" -o "$STAGING"
        return
    fi

    ensure_jq

    echo "Fetching latest release metadata from GitHub..."
    RELEASE_JSON=$(curl -s "https://api.github.com/repos/${REPO}/releases/latest")
    
    # Check if the API returned a "Not Found" message
        API_MESSAGE=$(echo "$RELEASE_JSON" | jq -r '.message // empty')
        if [[ "$API_MESSAGE" == "Not Found" ]]; then
            echo "[ERROR] No releases exist yet in ${REPO}. Run the pipeline first!"
            exit 1
        fi
    
    LATEST_VERSION=$(echo "$RELEASE_JSON" | jq -r '.tag_name')
        
        # Catch any other unexpected API responses
        if [[ -z "$LATEST_VERSION" || "$LATEST_VERSION" == "null" ]]; then
            echo "[ERROR] Failed to parse the latest version tag from GitHub API."
            exit 1
        fi
        
    check_existing_version

    # A release now ships one binary PER PLATFORM plus a .sha256 for each,
    # so grabbing "the" browser_download_url no longer works - filter down
    # to the asset that matches this host's platform, excluding checksums.
    ASSET_URL=$(echo "$RELEASE_JSON" | jq -r --arg platform "$PLATFORM" \
        '.assets[] | select((.name | startswith("prx-camera-\($platform)-")) and (.name | endswith(".sha256") | not)) | .browser_download_url' \
        | head -n1)
    CHECKSUM_URL=$(echo "$RELEASE_JSON" | jq -r --arg platform "$PLATFORM" \
        '.assets[] | select((.name | startswith("prx-camera-\($platform)-")) and (.name | endswith(".sha256"))) | .browser_download_url' \
        | head -n1)

    if [[ -z "$ASSET_URL" || "$ASSET_URL" == "null" ]]; then
        echo "[ERROR] No release asset found for platform '${PLATFORM}'."
        exit 1
    fi

    echo "Downloading: $ASSET_URL"
    curl -L "$ASSET_URL" -o "$STAGING"

    if [[ -n "$CHECKSUM_URL" && "$CHECKSUM_URL" != "null" ]]; then
        echo "Downloading checksum: $CHECKSUM_URL"
        curl -L "$CHECKSUM_URL" -o "${STAGING}.sha256"
    else
        echo "[WARN] No checksum asset found for platform '${PLATFORM}' - skipping integrity check."
    fi
}

verify_checksum() {
    if [[ ! -f "${STAGING}.sha256" ]]; then
        echo "[WARN] No checksum file available - skipping integrity check."
        return
    fi

    echo "Verifying checksum..."
    # The .sha256 file records the original release filename, not the local
    # staging filename, so compare hashes directly rather than relying on
    # `sha256sum -c`'s filename matching.
    local expected actual
    expected=$(awk '{print $1}' "${STAGING}.sha256")
    actual=$(sha256sum "$STAGING" | awk '{print $1}')

    if [[ "$expected" != "$actual" ]]; then
        echo "[ERROR] Checksum mismatch! Expected ${expected}, got ${actual}."
        exit 1
    fi
    echo "[INFO] Checksum verified."
}

validate_binary() {
    echo "Validating ELF binary..."
    case "$PLATFORM" in
        rpi-3|rpi-4)
            file "$STAGING" | grep -q "ARM aarch64" \
                || { echo "ERROR: Not a valid ARM64 ELF"; exit 1; }
            ;;
        linux-x64)
            file "$STAGING" | grep -q "x86-64" \
                || { echo "ERROR: Not a valid x86-64 ELF"; exit 1; }
            ;;
        *)
            echo "[WARN] Unknown platform '$PLATFORM' - skipping architecture check."
            ;;
    esac
}

install_binary() {
    echo "Installing binary to ${INSTALL_PATH}..."
    run_as_root cp "$STAGING" "$INSTALL_PATH"
    run_as_root chmod +x "$INSTALL_PATH"
}

install_dependencies() {
    if [[ "$MODE" != "PRODUCTION" ]]; then
        echo "[INFO] Skipping dependency installation (MODE=$MODE)"
        return
    fi

    echo "[INFO] Installing runtime dependencies for .NET 10 Native AOT..."

    run_as_root apt-get update -y

    run_as_root apt-get install -y \
        libicu-dev \
        zlib1g \
        libssl-dev \
        libpcap-dev

    echo "[INFO] Dependencies installed."
}

install_service() {
    echo "Installing systemd service..."

    run_as_root bash -c "cat >/etc/systemd/system/${SERVICE_NAME}" <<EOF
[Unit]
Description=PRX Camera Daemon
After=network.target

[Service]
ExecStart=${INSTALL_PATH}
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

    run_as_root systemctl daemon-reload
    run_as_root systemctl enable "${SERVICE_NAME}"
    run_as_root systemctl restart "${SERVICE_NAME}"
}

install_uninstaller() {
    echo "Creating companion uninstaller at /usr/local/bin/prx-camera-uninstall..."

    run_as_root bash -c "cat >/usr/local/bin/prx-camera-uninstall" <<EOF
#!/usr/bin/env bash
set -euo pipefail

if [[ "\$EUID" -ne 0 ]]; then
    echo "[ERROR] The uninstaller requires root privileges. Please run: sudo prx-camera-uninstall"
    exit 1
fi

echo "Stopping and removing ${SERVICE_NAME}..."
systemctl stop "${SERVICE_NAME}" 2>/dev/null || true
systemctl disable "${SERVICE_NAME}" 2>/dev/null || true
rm -f "/etc/systemd/system/${SERVICE_NAME}"
systemctl daemon-reload

echo "Removing binary at ${INSTALL_PATH}..."
rm -f "${INSTALL_PATH}"

echo "Removing uninstaller script..."
rm -f "/usr/local/bin/prx-camera-uninstall"

echo "Prx.Camera has been completely removed."
EOF

    run_as_root chmod +x /usr/local/bin/prx-camera-uninstall
}

validate_mode
check_sudo
PLATFORM="$(detect_platform)"
echo "[INFO] Detected platform: $PLATFORM"
download_release
verify_checksum
validate_binary
install_dependencies
install_binary
install_service
install_uninstaller

echo "Installation complete."
