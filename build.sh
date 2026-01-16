#!/usr/bin/env bash
# Build script for Reshape CLI and UI

set -e

# Colors
CYAN='\033[0;36m'
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

function step() { echo -e "${CYAN}▶ $1${NC}"; }
function success() { echo -e "${GREEN}✓ $1${NC}"; }
function error() { echo -e "${RED}✗ $1${NC}"; }

# Default values
CONFIGURATION="Debug"
RUNTIME=""
SKIP_UI=false
SKIP_RESTORE=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -r|--runtime)
            RUNTIME="$2"
            shift 2
            ;;
        --skip-ui)
            SKIP_UI=true
            shift
            ;;
        --skip-restore)
            SKIP_RESTORE=true
            shift
            ;;
        -h|--help)
            echo "Usage: ./build.sh [options]"
            echo ""
            echo "Options:"
            echo "  -c, --configuration <Debug|Release>  Build configuration (default: Debug)"
            echo "  -r, --runtime <RID>                   Target runtime (e.g., linux-x64, osx-arm64)"
            echo "  --skip-ui                             Skip building the UI"
            echo "  --skip-restore                        Skip dotnet restore"
            echo "  -h, --help                            Show this help"
            echo ""
            echo "Examples:"
            echo "  ./build.sh                            Build for development"
            echo "  ./build.sh -c Release                 Build for release"
            echo "  ./build.sh -c Release -r linux-x64    Publish Linux x64 executable"
            exit 0
            ;;
        *)
            error "Unknown option: $1"
            exit 1
            ;;
    esac
done

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UI_DIR="$ROOT_DIR/src/Reshape.Ui"
CLI_DIR="$ROOT_DIR/src/Reshape.Cli"
WWWROOT_DIR="$CLI_DIR/wwwroot"

# Step 1: Build UI
if [ "$SKIP_UI" = false ]; then
    step "Building Vue UI..."
    cd "$UI_DIR"
    
    if [ ! -d "node_modules" ]; then
        step "Installing npm dependencies..."
        npm install
    fi
    
    npm run build
    success "UI build complete → $WWWROOT_DIR"
else
    echo -e "${YELLOW}Skipping UI build${NC}"
fi

# Verify wwwroot exists
if [ ! -d "$WWWROOT_DIR" ]; then
    error "wwwroot folder not found! UI must be built first."
    exit 1
fi

# Step 2: Restore .NET dependencies
cd "$CLI_DIR"
if [ "$SKIP_RESTORE" = false ]; then
    step "Restoring .NET dependencies..."
    dotnet restore
fi

# Step 3: Build or Publish .NET CLI
if [ -n "$RUNTIME" ]; then
    step "Publishing for $RUNTIME ($CONFIGURATION)..."
    PUBLISH_DIR="$CLI_DIR/publish/$RUNTIME"
    dotnet publish -c "$CONFIGURATION" -r "$RUNTIME" --self-contained -o "$PUBLISH_DIR"
    
    success "Published to $PUBLISH_DIR"
    echo ""
    echo -n "Executable: "
    if [[ "$RUNTIME" == win-* ]]; then
        echo "$PUBLISH_DIR/reshape.exe"
    else
        echo "$PUBLISH_DIR/reshape"
    fi
else
    step "Building .NET CLI ($CONFIGURATION)..."
    dotnet build -c "$CONFIGURATION"
    
    success "Build complete"
    echo ""
    echo -n "Run with: "
    echo -e "${NC}dotnet run -- run${NC}"
fi

echo ""
success "All done!"
