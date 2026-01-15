#!/usr/bin/env bash
# Reshape CLI Installation Script for Linux/macOS
# Usage: curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.sh | bash
# Or with version: curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.sh | bash -s -- --version v0.1.0

set -e

# Default values
VERSION="latest"
INSTALL_DIR="$HOME/.local/bin"
PREVIEW=false

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

info() {
    echo -e "${CYAN}$1${NC}"
}

success() {
    echo -e "${GREEN}$1${NC}"
}

warning() {
    echo -e "${YELLOW}$1${NC}"
}

error() {
    echo -e "${RED}$1${NC}"
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --version)
            VERSION="$2"
            shift 2
            ;;
        --install-dir)
            INSTALL_DIR="$2"
            shift 2
            ;;
        --preview|--prerelease)
            PREVIEW=true
            shift
            ;;
        *)
            error "Unknown option: $1"
            exit 1
            ;;
    esac
done

info "🔄 Reshape CLI Installer"
info "========================"

# Detect OS and architecture
OS=$(uname -s | tr '[:upper:]' '[:lower:]')
ARCH=$(uname -m)

case "$OS" in
    linux)
        OS_NAME="linux"
        ;;
    darwin)
        OS_NAME="osx"
        ;;
    *)
        error "Unsupported operating system: $OS"
        exit 1
        ;;
esac

case "$ARCH" in
    x86_64|amd64)
        ARCH_NAME="x64"
        ;;
    arm64|aarch64)
        ARCH_NAME="arm64"
        ;;
    *)
        error "Unsupported architecture: $ARCH"
        exit 1
        ;;
esac

# Repository information
REPO="jomaxso/Reshape"
API_URL="https://api.github.com/repos/$REPO/releases"

# Fetch release information
if [ "$VERSION" = "latest" ]; then
    if [ "$PREVIEW" = true ]; then
        info "Fetching latest preview release information..."
        RELEASE_JSON=$(curl -fsSL "$API_URL" -H "User-Agent: Reshape-Installer")
        # Find first prerelease
        RELEASE_JSON=$(echo "$RELEASE_JSON" | python3 -c "import sys, json; releases = json.load(sys.stdin); prereleases = [r for r in releases if r.get('prerelease')]; print(json.dumps(prereleases[0]) if prereleases else '')" 2>/dev/null || echo "")
        if [ -z "$RELEASE_JSON" ] || [ "$RELEASE_JSON" = "" ]; then
            warning "No preview release found, falling back to latest stable release"
            RELEASE_JSON=$(curl -fsSL "$API_URL/latest" -H "User-Agent: Reshape-Installer")
        fi
        VERSION=$(echo "$RELEASE_JSON" | grep -o '"tag_name": *"[^"]*"' | head -1 | sed 's/"tag_name": *"\(.*\)"/\1/')
    else
        info "Fetching latest release information..."
        RELEASE_JSON=$(curl -fsSL "$API_URL/latest" -H "User-Agent: Reshape-Installer")
        VERSION=$(echo "$RELEASE_JSON" | grep -o '"tag_name": *"[^"]*"' | sed 's/"tag_name": *"\(.*\)"/\1/')
    fi
else
    info "Fetching release information for $VERSION..."
    RELEASE_JSON=$(curl -fsSL "$API_URL/tags/$VERSION" -H "User-Agent: Reshape-Installer")
fi

if [ -z "$VERSION" ]; then
    error "Failed to fetch release information"
    exit 1
fi

success "Installing Reshape CLI version: $VERSION"

# Determine asset name
ASSET_NAME="reshape-${OS_NAME}-${ARCH_NAME}.tar.gz"

# Extract download URL
DOWNLOAD_URL=$(echo "$RELEASE_JSON" | grep -o "\"browser_download_url\": *\"[^\"]*${ASSET_NAME}\"" | sed 's/"browser_download_url": *"\(.*\)"/\1/')

if [ -z "$DOWNLOAD_URL" ]; then
    error "Could not find asset $ASSET_NAME in release $VERSION"
    info "Available assets:"
    echo "$RELEASE_JSON" | grep -o '"name": *"[^"]*"' | sed 's/"name": *"\(.*\)"/  - \1/'
    exit 1
fi

# Create temporary directory
TEMP_DIR=$(mktemp -d)
trap 'rm -rf "$TEMP_DIR"' EXIT

# Download archive
info "Downloading from: $DOWNLOAD_URL"
ARCHIVE_PATH="$TEMP_DIR/$ASSET_NAME"
curl -fsSL "$DOWNLOAD_URL" -o "$ARCHIVE_PATH" -H "User-Agent: Reshape-Installer"
success "Downloaded successfully"

# Extract archive
info "Extracting archive..."
tar -xzf "$ARCHIVE_PATH" -C "$TEMP_DIR"

# Find executable
EXE_NAME="reshape"
EXE_PATH=$(find "$TEMP_DIR" -type f -name "$EXE_NAME" | head -n 1)

if [ -z "$EXE_PATH" ]; then
    error "Could not find $EXE_NAME in the downloaded archive"
    exit 1
fi

# Create installation directory
if [ ! -d "$INSTALL_DIR" ]; then
    info "Creating installation directory: $INSTALL_DIR"
    mkdir -p "$INSTALL_DIR"
fi

# Install executable
TARGET_PATH="$INSTALL_DIR/reshape"
info "Installing to: $TARGET_PATH"

if [ -f "$TARGET_PATH" ]; then
    warning "Replacing existing installation"
    rm -f "$TARGET_PATH"
fi

cp "$EXE_PATH" "$TARGET_PATH"
chmod +x "$TARGET_PATH"

success "✓ Reshape CLI installed successfully!"

# Check if directory is in PATH
if [[ ":$PATH:" != *":$INSTALL_DIR:"* ]]; then
    warning ""
    warning "ⓘ Installation directory is not in your PATH"
    info "To add it to your PATH, add this line to your shell profile:"
    info "  export PATH=\"\$PATH:$INSTALL_DIR\""
    info ""
    
    # Suggest shell-specific config file based on $SHELL
    case "$SHELL" in
        */bash)
            info "For Bash, add to: ~/.bashrc or ~/.bash_profile"
            ;;
        */zsh)
            info "For Zsh, add to: ~/.zshrc"
            ;;
        */fish)
            info "For Fish, add to: ~/.config/fish/config.fish"
            ;;
    esac
    
    info ""
    info "To use reshape right now, run:"
    info "  $TARGET_PATH --help"
else
    success ""
    success "✓ You can now use the 'reshape' command"
    info "Try: reshape --help"
fi

info ""
info "For more information, visit: https://github.com/$REPO"
