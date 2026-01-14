#!/usr/bin/env bash
#
# Install Reshape CLI from a specific Pull Request build.
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/scripts/get-reshape-pr.sh | bash -s -- 9
#
# Or download and run:
#   ./get-reshape-pr.sh 9
#
# Requires:
#   - GitHub CLI (gh) must be installed and authenticated

set -e

PR_NUMBER="${1:-}"
REPO="jomaxso/Reshape"
OUTPUT_PATH="${HOME}/.reshape"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Detect OS and architecture
detect_platform() {
    local os="$(uname -s)"
    local arch="$(uname -m)"
    
    case "$os" in
        Linux*)
            PLATFORM="linux"
            ARTIFACT_NAME="reshape-linux-x64"
            ;;
        Darwin*)
            PLATFORM="macos"
            if [[ "$arch" == "arm64" ]]; then
                ARTIFACT_NAME="reshape-osx-arm64"
            else
                ARTIFACT_NAME="reshape-osx-x64"
            fi
            ;;
        *)
            echo -e "${RED}❌ Unsupported OS: $os${NC}"
            exit 1
            ;;
    esac
}

print_header() {
    echo -e "${CYAN}🔄 Reshape PR Installer${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
}

print_usage() {
    echo "Usage: $0 <pr-number>"
    echo ""
    echo "Example:"
    echo "  curl -fsSL https://raw.githubusercontent.com/$REPO/main/scripts/get-reshape-pr.sh | bash -s -- 9"
    echo ""
}

if [[ -z "$PR_NUMBER" ]]; then
    print_header
    echo -e "${RED}❌ Error: PR number is required${NC}"
    echo ""
    print_usage
    exit 1
fi

print_header

# Check for gh CLI
if ! command -v gh >/dev/null 2>&1; then
    echo -e "${RED}❌ GitHub CLI (gh) is required but not installed.${NC}"
    echo -e "${YELLOW}   Install from: https://cli.github.com/${NC}"
    exit 1
fi

# Warning
echo -e "${YELLOW}⚠️  WARNING: Only install from PRs you trust!${NC}"
echo -e "${YELLOW}   Review the code at: https://github.com/$REPO/pull/$PR_NUMBER${NC}"
echo ""

# Ask for confirmation
read -p "Continue with installation? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${RED}❌ Installation cancelled.${NC}"
    exit 1
fi

echo ""
detect_platform

echo -e "${GREEN}📦 Fetching PR #$PR_NUMBER information...${NC}"
echo -e "${GRAY}   Platform: $PLATFORM ($ARTIFACT_NAME)${NC}"

# Get PR head SHA
PR_INFO=$(gh api "repos/$REPO/pulls/$PR_NUMBER" --jq '{head_sha: .head.sha, state: .state}')
HEAD_SHA=$(echo "$PR_INFO" | jq -r '.head_sha')
PR_STATE=$(echo "$PR_INFO" | jq -r '.state')

if [[ "$PR_STATE" != "open" ]]; then
    echo -e "${YELLOW}⚠️  Warning: PR #$PR_NUMBER is $PR_STATE${NC}"
fi

echo -e "${GRAY}   PR HEAD SHA: $HEAD_SHA${NC}"

# Find workflow run
echo -e "${GREEN}🔍 Finding Build and Release workflow run...${NC}"

WORKFLOW_RUN=$(gh api "repos/$REPO/actions/workflows/build-and-release.yml/runs?event=pull_request&head_sha=$HEAD_SHA" --jq '.workflow_runs | sort_by(.created_at) | reverse | .[0] | {id: .id, status: .status, conclusion: .conclusion, html_url: .html_url}')

RUN_ID=$(echo "$WORKFLOW_RUN" | jq -r '.id')
RUN_STATUS=$(echo "$WORKFLOW_RUN" | jq -r '.status')
RUN_CONCLUSION=$(echo "$WORKFLOW_RUN" | jq -r '.conclusion')
RUN_URL=$(echo "$WORKFLOW_RUN" | jq -r '.html_url')

if [[ -z "$RUN_ID" ]] || [[ "$RUN_ID" == "null" ]]; then
    echo -e "${RED}❌ No Build and Release workflow run found for this PR${NC}"
    echo -e "${YELLOW}   The workflow may not have started yet.${NC}"
    echo -e "${YELLOW}   Check: https://github.com/$REPO/pull/$PR_NUMBER/checks${NC}"
    exit 1
fi

echo -e "${GRAY}   Run ID: $RUN_ID${NC}"
echo -e "${GRAY}   Status: $RUN_STATUS | Conclusion: $RUN_CONCLUSION${NC}"

if [[ "$RUN_STATUS" != "completed" ]]; then
    echo -e "${YELLOW}⏳ Workflow is still $RUN_STATUS. Please wait for it to complete.${NC}"
    echo -e "${YELLOW}   Check: $RUN_URL${NC}"
    exit 1
fi

if [[ "$RUN_CONCLUSION" != "success" ]]; then
    echo -e "${RED}❌ Workflow run did not succeed (conclusion: $RUN_CONCLUSION)${NC}"
    echo -e "${YELLOW}   Check: $RUN_URL${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Found successful workflow run!${NC}"
echo ""

# Download artifact
echo -e "${GREEN}📥 Downloading artifact: $ARTIFACT_NAME${NC}"

TEMP_DIR=$(mktemp -d -t reshape-pr-${PR_NUMBER}-XXXXXXXX)
DOWNLOAD_DIR="$TEMP_DIR/download"

cleanup() {
    if [[ -d "$TEMP_DIR" ]]; then
        rm -rf "$TEMP_DIR"
    fi
}

trap cleanup EXIT

if ! gh run download "$RUN_ID" --name "$ARTIFACT_NAME" --dir "$DOWNLOAD_DIR" --repo "$REPO"; then
    echo -e "${RED}❌ Failed to download artifact${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Download complete!${NC}"
echo ""
echo -e "${GREEN}📦 Installing to: $OUTPUT_PATH${NC}"

# Create output directory
mkdir -p "$OUTPUT_PATH"

# Find and extract archive
ARCHIVE=$(find "$DOWNLOAD_DIR" -name "*.tar.gz" -o -name "*.zip" | head -1)

if [[ -z "$ARCHIVE" ]]; then
    echo -e "${RED}❌ No archive found in downloaded artifact${NC}"
    exit 1
fi

# Extract
if [[ "$ARCHIVE" == *.tar.gz ]]; then
    tar -xzf "$ARCHIVE" -C "$OUTPUT_PATH"
elif [[ "$ARCHIVE" == *.zip ]]; then
    unzip -o "$ARCHIVE" -d "$OUTPUT_PATH" >/dev/null
fi

echo -e "${GREEN}✅ Installation complete!${NC}"
echo ""
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}🎉 Reshape CLI from PR #$PR_NUMBER is ready!${NC}"
echo ""
echo -e "${CYAN}Run the CLI:${NC}"
echo -e "${NC}   $OUTPUT_PATH/reshape run${NC}"
echo ""
echo -e "${CYAN}Or add to PATH:${NC}"
echo -e "${NC}   export PATH=\"\$PATH:$OUTPUT_PATH\"${NC}"
echo -e "${NC}   reshape run${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
