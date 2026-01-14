# 🔄 Reshape CLI

> **Batch rename files using metadata patterns** - A powerful dual-mode file renaming tool with both CLI and Web UI.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Vue 3](https://img.shields.io/badge/Vue-3.x-4FC08D?logo=vuedotjs)](https://vuejs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## ✨ Features

- 🖥️ **Dual Mode**: Use via CLI commands or modern Web UI
- 📷 **EXIF Metadata Extraction**: Extract date, camera info, GPS coordinates from images
- 🏝️ **Vacation Mode**: Organize photos into day-based folder structures
- 🌍 **GPS & Timezone Support**: Automatic timezone detection from GPS coordinates
- 🔄 **Pattern-based Renaming**: Flexible patterns with placeholders
- 👁️ **Preview Mode**: See changes before executing
- ⚡ **AOT Compiled**: Fast startup with Native AOT compilation
- 🎨 **Beautiful CLI Output**: Powered by Spectre.Console

## 📋 Table of Contents

- [Installation](#-installation)
- [Testing Pull Requests](#-testing-pull-requests)
- [Quick Start](#-quick-start)
- [CLI Commands](#-cli-commands)
- [Web UI](#-web-ui)
- [Patterns & Placeholders](#-patterns--placeholders)
- [Vacation Mode](#-vacation-mode)
- [Development](#-development)
- [Documentation](#-documentation)

## 📦 Installation

### Quick Install (Recommended)

#### Windows (PowerShell)
```powershell
iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.ps1) }"
```

#### Linux / macOS (Bash)
```bash
curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/scripts/install.sh | bash
```

### Install Specific Version

#### Windows
```powershell
iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.ps1) } -Version v0.1.0"
```

#### Linux / macOS
```bash
curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.sh | bash -s -- --version v0.1.0
```

### Update to Latest Version

If you already have Reshape installed, you can update it using:

```bash
reshape update
```

### Manual Installation from Releases

Download the latest release for your platform from the [Releases page](https://github.com/jomaxso/Reshape/releases):

- **Windows**: `reshape-win-x64.zip`
- **Linux**: `reshape-linux-x64.tar.gz`
- **macOS (ARM)**: `reshape-osx-arm64.tar.gz`

Extract the archive and add the executable to your PATH.

### Build from Source

#### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/) (for UI development)

#### Steps

```powershell
# Clone the repository
git clone https://github.com/jomaxso/Reshape.git
cd Reshape

# Build the CLI
dotnet build src/reshape-cli/

# (Optional) Build the Web UI
cd src/reshape-ui
npm install
npm run build
```

#### Publish as Native Executable

```powershell
cd src/reshape-cli
dotnet publish -c Release -r <RID> --self-contained
# Replace <RID> with: win-x64, linux-x64, or osx-arm64
```

## 🧪 Testing Pull Requests

Want to try out a feature before it's merged? Each pull request automatically gets a comment with installation instructions!

When you open a PR, GitHub Actions will automatically:
- ✅ Build the code for all platforms (Windows, Linux, macOS)
- 📦 Create downloadable artifacts
- 💬 Post a comment with detailed installation instructions

Just check the PR comments for the "🚀 Test This Pull Request" section with:
- Direct links to download pre-built binaries
- Instructions for building locally from the PR branch
- Quick start examples specific to that PR

This makes it easy for maintainers and contributors to test changes before merging!

## 🚀 Quick Start

### CLI Mode

```powershell
# List files in a folder
dotnet run --project src/reshape-cli/ -- list "C:\Photos" --ext .jpg .png

# Preview rename operations
dotnet run --project src/reshape-cli/ -- preview "C:\Photos" --pattern "{year}-{month}-{day}_{filename}" --ext .jpg

# Execute rename (interactive - will prompt for confirmation)
dotnet run --project src/reshape-cli/ -- rename "C:\Photos" --pattern "{year}-{month}-{day}_{filename}" --ext .jpg

# Execute rename without confirmation prompt
dotnet run --project src/reshape-cli/ -- rename "C:\Photos" --pattern "{year}-{month}-{day}_{filename}" --ext .jpg --no-interactive

# Show available patterns
dotnet run --project src/reshape-cli/ -- patterns
```

### Web UI Mode

```powershell
# Start the web server
dotnet run --project src/reshape-cli/ -- serve

# Open browser at http://localhost:5000
```

## 💻 CLI Commands

| Command | Description | Example |
|---------|-------------|---------|
| `serve` | Start the Web UI server | `reshape serve` |
| `list` | List files in a folder | `reshape list ./photos --ext .jpg .png` |
| `preview` | Preview rename operations | `reshape preview ./photos --pattern "{date_taken}_{filename}"` |
| `rename` | Execute rename operations | `reshape rename ./photos --pattern "{year}/{month}/{filename}"` |
| `patterns` | Show available pattern templates | `reshape patterns` |
| `update` | Update to the latest version | `reshape update` |

### Update Command Options

| Option | Description |
|--------|-------------|
| `--check` | Check for updates without installing |
| `--prerelease` | Include prerelease versions |
| `--stable` | Only update to stable releases (default) |

### Common Options

| Option | Description |
|--------|-------------|
| `--ext` | Filter by file extensions (e.g., `.jpg .png .heic`) |
| `--pattern` | Rename pattern with placeholders |
| `--dry-run` | Preview changes without executing |
| `--no-interactive` | Skip confirmation prompts and execute automatically |

## 🌐 Web UI

The Web UI provides an intuitive interface for:

- 📁 Browsing and selecting folders
- 🔍 Scanning files with metadata preview
- ✏️ Building rename patterns visually
- 👁️ Live preview of rename operations
- 🏝️ Vacation mode configuration
- ✅ Selective file processing

![Web UI Screenshot](docs/images/web-ui-preview.png)

## 🎯 Patterns & Placeholders

### Available Placeholders

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{filename}` | Original filename (without extension) | `IMG_0001` |
| `{ext}` | File extension (without dot) | `jpg` |
| `{year}` | Year (4 digits) | `2024` |
| `{month}` | Month (2 digits) | `01` |
| `{day}` | Day (2 digits) | `15` |
| `{date_taken}` | EXIF date taken | `2024-01-15` |
| `{time_taken}` | EXIF time taken | `14-30-00` |
| `{camera_make}` | Camera manufacturer | `Canon` |
| `{camera_model}` | Camera model | `EOS_R5` |
| `{width}` | Image width | `4000` |
| `{height}` | Image height | `3000` |
| `{counter:N}` | Counter with N-digit padding | `001`, `0001` |
| `{gps_lat}` | GPS latitude | `48.858844` |
| `{gps_lon}` | GPS longitude | `2.294351` |

### Pattern Examples

```
# Date prefix
{year}-{month}-{day}_{filename}
→ 2024-01-15_IMG_0001.jpg

# Camera + date + counter
{camera_model}_{date_taken}_{counter:4}
→ iPhone15_2024-01-15_0001.jpg

# Standard format
IMG_{year}{month}{day}_{counter:4}
→ IMG_20240115_0001.jpg

# Organized by date folders
{year}/{month}/{filename}
→ 2024/01/IMG_0001.jpg
```

## 🏝️ Vacation Mode

Vacation Mode is designed for organizing holiday photos into day-based folder structures.

### Features

- 📅 Automatic day grouping based on photo dates
- 🌍 GPS-based timezone detection
- 📁 Customizable folder structure
- 🔢 Day-specific counters

### Vacation-Specific Placeholders

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{day_number}` | Day of vacation (1, 2, 3...) | `1` |
| `{day_counter}` | Counter within a day | `001` |
| `{global_counter}` | Global counter across all days | `0001` |

### Example Output

```
Tag 1/
  ├─ 2024-07-15_001.jpg
  ├─ 2024-07-15_002.jpg
  └─ 2024-07-15_003.jpg
Tag 2/
  ├─ 2024-07-16_001.jpg
  └─ 2024-07-16_002.jpg
Tag 3/
  └─ 2024-07-17_001.jpg
```

📖 See [VACATION_MODE.md](VACATION_MODE.md) for detailed documentation.

## 🛠️ Development

### Project Structure

```
Reshape/
├── src/
│   ├── reshape-cli/           # .NET 10 CLI with embedded web server
│   │   ├── Commands/          # CLI command handlers
│   │   ├── Utilities/         # Helper classes
│   │   ├── wwwroot/           # Compiled Vue app (auto-generated)
│   │   ├── Program.cs         # Entry point
│   │   ├── FileService.cs     # Core file operations
│   │   └── Models.cs          # Data models
│   │
│   └── reshape-ui/            # Vue 3 + TypeScript frontend
│       ├── src/
│       │   ├── components/    # Vue components
│       │   ├── api.ts         # API client
│       │   └── types.ts       # TypeScript types
│       └── vite.config.ts     # Build configuration
│
└── docs/                  # Documentation
```

### Development Workflow

```powershell
# Terminal 1: Start CLI in watch mode
cd src/reshape-cli
dotnet watch run -- serve

# Terminal 2: Start Vue dev server (optional, for hot reload)
cd src/reshape-ui
npm run dev
```

### Building for Production

```powershell
# Build Vue UI first
cd src/reshape-ui
npm run build

# Build CLI (UI is embedded in wwwroot/)
cd ../reshape-cli
dotnet publish -c Release
```

### AOT Compilation

The CLI supports Native AOT compilation for fast startup:

```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

⚠️ **Important**: All JSON types must be registered in `AppJsonSerializerContext.cs` for AOT compatibility.

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [README.md](README.md) | This file - overview and quick start |
| [VACATION_MODE.md](VACATION_MODE.md) | Vacation mode detailed documentation |
| [docs/CICD.md](docs/CICD.md) | CI/CD pipeline and release workflow |
| [docs/CLI.md](docs/CLI.md) | CLI reference documentation |
| [docs/API.md](docs/API.md) | REST API documentation |
| [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) | Development guide |
| [src/reshape-cli/ARCHITECTURE.md](src/reshape-cli/ARCHITECTURE.md) | Code architecture overview |

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please read our [Contributing Guide](docs/CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## � Versioning

Reshape follows [Semantic Versioning](https://semver.org/) with centralized version management. See [VERSIONING.md](docs/VERSIONING.md) for details on:
- How versions are managed in `eng/Versions.props`
- Automated nightly version bumps
- Release workflows and processes

## �🙏 Acknowledgments

- [Spectre.Console](https://spectreconsole.net/) - Beautiful console UI
- [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet) - EXIF metadata extraction
- [Vue.js](https://vuejs.org/) - Progressive JavaScript framework
- [Vite](https://vitejs.dev/) - Next generation frontend tooling

---

Made with ❤️ by [jomaxso](https://github.com/jomaxso)
