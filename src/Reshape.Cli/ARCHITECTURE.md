# Reshape CLI - Code Architecture

## Overview

The Reshape CLI is structured to support both command-line and web-based interfaces, with a focus on maintainability, testability, and Native AOT compatibility.

## Project Structure

```
Reshape.Cli/
├── Commands/                          # Command organization
│   ├── Files/                         # File operation commands
│   │   ├── FileCommand.cs            # Base file command
│   │   ├── ListCommand.cs            # 'list' - Display files
│   │   ├── PreviewCommand.cs         # 'preview' - Preview renames
│   │   └── RenameCommand.cs          # 'rename' - Execute renames
│   │
│   ├── Patterns/                      # Pattern-related commands
│   │   └── PatternCommand.cs         # 'patterns' - Show available patterns
│   │
│   ├── ReshapeCliCommand.cs          # Root command definition
│   ├── RunCommand.cs                 # 'serve' - Web UI server
│   └── UpdateCommand.cs              # 'update' - Self-update functionality
│
├── Utilities/                         # Helper classes
│   └── FormatHelper.cs               # Formatting utility functions
│
├── Options/                           # Command-line option definitions
│
├── Program.cs                         # Application entry point
├── FileService.cs                     # Core file operations and metadata
├── ConfigurationService.cs            # Configuration management
├── Models.cs                          # Data models (Records)
└── AppJsonSerializerContext.cs       # JSON serialization for AOT
```

## Key Design Principles

### 1. **Separation of Concerns**
- Commands are organized by functionality (Files, Patterns)
- Each command has a clear, single responsibility
- Business logic is centralized in `FileService.cs`

### 2. **Native AOT Compatibility**
- All JSON types registered in `AppJsonSerializerContext.cs`
- No reflection-based APIs
- Source-generated JSON serialization

### 3. **Testability**
- Command logic separated from CLI infrastructure
- Core services can be tested independently
- Clear interfaces between components

### 4. **Embedded Web Server**
- ASP.NET Core minimal API for the web UI
- Vue.js SPA compiled into `wwwroot/`
- Single executable with embedded assets

## Command Structure

Commands follow a hierarchical structure using System.CommandLine:

```
reshape
├── serve           # Start web UI (RunCommand.cs)
├── list            # List files (ListCommand.cs)
├── preview         # Preview renames (PreviewCommand.cs)
├── rename          # Execute renames (RenameCommand.cs)
├── patterns        # Show patterns (PatternCommand.cs)
└── update          # Self-update (UpdateCommand.cs)
```

### Command Pattern

Commands in the Files/ directory extend from a base command pattern and follow this structure:

```csharp
namespace Reshape.Cli.Commands.Files;

internal static class SomeCommand
{
    public static Command Create()
    {
        var command = new Command("command-name", "Description");
        
        // Add arguments and options
        var pathArg = new Argument<string>("path", "Path description");
        command.AddArgument(pathArg);
        
        // Set handler
        command.SetHandler((string path) =>
        {
            try
            {
                // Command logic
                return 0; // Success
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
                return 1; // Error
            }
        }, pathArg);
        
        return command;
    }
}
```

## Core Components

### FileService.cs
Central service for all file operations:
- Metadata extraction (EXIF, file system)
- Pattern-based renaming
- File scanning and filtering
- Vacation mode logic
- GPS and timezone handling

### Models.cs
All data transfer objects as C# records:
- Immutable by default
- Clean serialization
- Type-safe data structures

### AppJsonSerializerContext.cs
Source-generated JSON serialization:
- Required for AOT compilation
- All API types must be registered
- Automatic camelCase conversion

## Web API Integration

The `RunCommand.cs` configures ASP.NET Core endpoints:

```csharp
POST /api/scan          # Scan folder
GET  /api/patterns      # Get patterns
POST /api/preview       # Preview rename
POST /api/rename        # Execute rename
GET  /api/metadata/{path} # Get file metadata
```

All endpoints use the same `FileService` as CLI commands, ensuring consistency.

## Benefits of This Structure

### ✅ **Maintainability**
- Clear separation of commands by category
- Easy to locate and modify specific functionality
- Minimal coupling between components

### ✅ **Extensibility**
- New commands can be added easily
- Consistent patterns to follow
- Modular structure

### ✅ **Performance**
- Native AOT for fast startup
- Efficient metadata extraction
- Single-file deployment option

### ✅ **Developer Experience**
- Organized codebase
- Clear naming conventions
- Type safety with records and TypeScript

## Adding New Features

### Adding a New Command

1. Create the command file in appropriate directory (Files/ or Patterns/)
2. Implement the Create() method following the pattern
3. Register in `Program.cs` or parent command
4. Update documentation

### Adding a New API Endpoint

1. Add endpoint in `RunCommand.cs`
2. Add models to `Models.cs`
3. Register models in `AppJsonSerializerContext.cs`
4. Add TypeScript types in `src/Reshape.Ui/src/types.ts`
5. Add API client method in `src/Reshape.Ui/src/api.ts`

### Adding New Metadata Fields

1. Extract in `FileService.ExtractMetadataWithGps()`
2. Document in pattern templates
3. Update API documentation

## Testing Strategy

- Unit tests for `FileService` operations
- Model validation tests
- Utility function tests
- Integration tests for command execution
- Following Arrange // Act // Assert pattern with Shouldly assertions

## Future Improvements

Potential architectural enhancements:
- Dependency injection for services
- Structured logging with ILogger
- Configuration file support
- Plugin system for custom patterns
- Command validation framework
