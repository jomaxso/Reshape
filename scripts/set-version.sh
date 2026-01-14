#!/usr/bin/env bash
# Updates the version in eng/Versions.props

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSIONS_PROPS="$SCRIPT_DIR/../eng/Versions.props"

show_help() {
    cat << EOF
Usage: $0 [OPTIONS]

Updates the version in eng/Versions.props

OPTIONS:
    --major                 Increment the major version
    --minor                 Increment the minor version
    --patch                 Increment the patch version
    --set-version VERSION   Set a specific version (format: MAJOR.MINOR.PATCH)
    --prerelease LABEL      Set the prerelease label (e.g., 'preview.1', 'rc.1')
    --show                  Display the current version without making changes
    -h, --help              Display this help message

EXAMPLES:
    $0 --patch
        Increments the patch version (e.g., 0.1.0 -> 0.1.1)
    
    $0 --minor --prerelease "preview.1"
        Increments the minor version and sets prerelease label
    
    $0 --set-version "1.0.0"
        Sets the version to 1.0.0
    
    $0 --show
        Displays the current version

EOF
}

get_xml_value() {
    local element=$1
    grep -oP "<${element}>\K[^<]+" "$VERSIONS_PROPS" || echo ""
}

set_xml_value() {
    local element=$1
    local value=$2
    sed -i "s|<${element}>[^<]*</${element}>|<${element}>${value}</${element}>|g" "$VERSIONS_PROPS"
}

# Check if Versions.props exists
if [ ! -f "$VERSIONS_PROPS" ]; then
    echo "Error: eng/Versions.props not found at: $VERSIONS_PROPS"
    exit 1
fi

# Parse arguments
OPERATION=""
SET_VERSION=""
PRERELEASE=""
SHOW=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --major)
            OPERATION="major"
            shift
            ;;
        --minor)
            OPERATION="minor"
            shift
            ;;
        --patch)
            OPERATION="patch"
            shift
            ;;
        --set-version)
            SET_VERSION="$2"
            shift 2
            ;;
        --prerelease)
            PRERELEASE="$2"
            shift 2
            ;;
        --show)
            SHOW=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Get current version
CURRENT_MAJOR=$(get_xml_value "MajorVersion")
CURRENT_MINOR=$(get_xml_value "MinorVersion")
CURRENT_PATCH=$(get_xml_value "PatchVersion")
CURRENT_PRERELEASE=$(get_xml_value "PreReleaseVersionLabel")

CURRENT_VERSION="${CURRENT_MAJOR}.${CURRENT_MINOR}.${CURRENT_PATCH}"
if [ -n "$CURRENT_PRERELEASE" ]; then
    CURRENT_VERSION="${CURRENT_VERSION}-${CURRENT_PRERELEASE}"
fi

# Show current version if requested
if [ "$SHOW" = true ]; then
    echo ""
    echo -e "\033[0;36mCurrent Version Information:\033[0m"
    echo -e "  Version:    \033[0;32m${CURRENT_VERSION}\033[0m"
    echo -e "  Major:      \033[0;90m${CURRENT_MAJOR}\033[0m"
    echo -e "  Minor:      \033[0;90m${CURRENT_MINOR}\033[0m"
    echo -e "  Patch:      \033[0;90m${CURRENT_PATCH}\033[0m"
    if [ -n "$CURRENT_PRERELEASE" ]; then
        echo -e "  Prerelease: \033[0;90m${CURRENT_PRERELEASE}\033[0m"
    fi
    echo ""
    exit 0
fi

# Calculate new version
NEW_MAJOR=$CURRENT_MAJOR
NEW_MINOR=$CURRENT_MINOR
NEW_PATCH=$CURRENT_PATCH

if [ -n "$SET_VERSION" ]; then
    # Parse the provided version
    if [[ $SET_VERSION =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
        NEW_MAJOR="${BASH_REMATCH[1]}"
        NEW_MINOR="${BASH_REMATCH[2]}"
        NEW_PATCH="${BASH_REMATCH[3]}"
    else
        echo "Error: Invalid version format. Use MAJOR.MINOR.PATCH (e.g., 1.0.0)"
        exit 1
    fi
elif [ "$OPERATION" = "major" ]; then
    NEW_MAJOR=$((CURRENT_MAJOR + 1))
    NEW_MINOR=0
    NEW_PATCH=0
elif [ "$OPERATION" = "minor" ]; then
    NEW_MINOR=$((CURRENT_MINOR + 1))
    NEW_PATCH=0
elif [ "$OPERATION" = "patch" ]; then
    NEW_PATCH=$((CURRENT_PATCH + 1))
else
    echo "Error: No operation specified. Use --major, --minor, --patch, --set-version, or --show"
    echo ""
    show_help
    exit 1
fi

# Update XML
set_xml_value "MajorVersion" "$NEW_MAJOR"
set_xml_value "MinorVersion" "$NEW_MINOR"
set_xml_value "PatchVersion" "$NEW_PATCH"

if [ -n "${PRERELEASE+x}" ]; then
    set_xml_value "PreReleaseVersionLabel" "$PRERELEASE"
    NEW_PRERELEASE="$PRERELEASE"
else
    NEW_PRERELEASE="$CURRENT_PRERELEASE"
fi

NEW_VERSION="${NEW_MAJOR}.${NEW_MINOR}.${NEW_PATCH}"
if [ -n "$NEW_PRERELEASE" ]; then
    NEW_VERSION="${NEW_VERSION}-${NEW_PRERELEASE}"
fi

echo ""
echo -e "\033[0;32m✅ Version Updated Successfully\033[0m"
echo ""
echo -e "  Previous: \033[0;31m${CURRENT_VERSION}\033[0m"
echo -e "  New:      \033[0;32m${NEW_VERSION}\033[0m"
echo ""
echo -e "\033[0;90mChanges saved to: ${VERSIONS_PROPS}\033[0m"
echo ""
