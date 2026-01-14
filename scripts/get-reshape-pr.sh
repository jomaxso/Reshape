#!/usr/bin/env bash
#
# Install Reshape CLI from a specific Pull Request build.
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/scripts/get-reshape-pr.sh | bash -s -- 9
#
# Or download and run:
#   ./get-reshape-pr.sh 9

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
MAGENTA='\033[0;35m'
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
            BINARY_NAME="reshape"
            ;;
        Darwin*)
            PLATFORM="macos"
            if [[ "$arch" == "arm64" ]]; then
                ARTIFACT_NAME="reshape-osx-arm64"
            else
                ARTIFACT_NAME="reshape-osx-x64"
            fi
            BINARY_NAME="reshape"
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

echo -e "${GREEN}📦 Fetching PR #$PR_NUMBER artifacts...${NC}"
echo -e "${GRAY}   Platform: $PLATFORM ($ARTIFACT_NAME)${NC}"
echo ""

# Get workflow runs for this PR
WORKFLOWS_URL="https://api.github.com/repos/$REPO/actions/runs?event=pull_request&status=success&per_page=100"

# Fetch workflow runs
RESPONSE=$(curl -fsSL -H "Accept: application/vnd.github+json" \
    -H "User-Agent: Reshape-PR-Installer" \
    "$WORKFLOWS_URL")

# Find the most recent "Build and Release" run for this PR
# We need to parse JSON to filter by workflow name and PR number
RUN_ID=$(echo "$RESPONSE" | jq -r ".workflow_runs[] | select(.name == \"Build and Release\" and (.pull_requests[].number == $PR_NUMBER)) | .id" | head -1)
RUN_URL=$(echo "$RESPONSE" | jq -r ".workflow_runs[] | select(.name == \"Build and Release\" and (.pull_requests[].number == $PR_NUMBER)) | .html_url" | head -1)

if [[ -z "$RUN_ID" ]] || [[ "$RUN_ID" == "null" ]]; then
    echo -e "${RED}❌ No successful 'Build and Release' workflow run found for PR #$PR_NUMBER${NC}"
    echo -e "${YELLOW}   Check if the PR has completed builds: https://github.com/$REPO/pull/$PR_NUMBER/checks${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Found workflow run ID: $RUN_ID${NC}"
echo ""

# Get artifacts for this run
ARTIFACTS_URL="https://api.github.com/repos/$REPO/actions/runs/$RUN_ID/artifacts"
ARTIFACTS_RESPONSE=$(curl -fsSL -H "Accept: application/vnd.github+json" \
    -H "User-Agent: Reshape-PR-Installer" \
    "$ARTIFACTS_URL")

# GitHub requires authentication to download artifacts via API
echo -e "${YELLOW}⚠️  GitHub requires authentication to download artifacts.${NC}"
echo ""
echo -e "${CYAN}Option 1: Download manually${NC}"
echo -e "${NC}  1. Go to: ${BLUE}$RUN_URL${NC}"
echo -e "${NC}  2. Scroll to 'Artifacts'${NC}"
echo -e "${NC}  3. Download '${ARTIFACT_NAME}.tar.gz' or '${ARTIFACT_NAME}.zip'${NC}"
echo -e "${NC}  4. Extract to: ${OUTPUT_PATH}${NC}"
echo ""
echo -e "${CYAN}Option 2: Use GitHub CLI (gh)${NC}"
echo -e "${NC}  gh run download $RUN_ID --name $ARTIFACT_NAME --repo $REPO${NC}"
echo -e "${NC}  tar -xzf ${ARTIFACT_NAME}.tar.gz -C $OUTPUT_PATH${NC}"
echo ""
echo -e "${MAGENTA}Direct link to workflow run:${NC}"
echo -e "  ${BLUE}$RUN_URL${NC}"
echo ""
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${CYAN}💡 After extracting, add to PATH:${NC}"
echo -e "${NC}   export PATH=\"\$PATH:$OUTPUT_PATH\"${NC}"
echo ""
echo -e "${CYAN}   Or run directly:${NC}"
echo -e "${NC}   $OUTPUT_PATH/$BINARY_NAME run${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
