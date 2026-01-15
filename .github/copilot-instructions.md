# Reshape CLI - AI Agent Instructions

## Architecture Overview

**Reshape** is a dual-mode file renaming tool with both CLI and Web UI:
- **src/reshape-cli/**: .NET 10 AOT-compiled CLI with embedded ASP.NET minimal API web server
- **src/reshape-ui/**: Vue 3 + TypeScript + Vite SPA served by the CLI's web server

The CLI embeds the compiled Vue app in [wwwroot/](../src/reshape-cli/wwwroot/) and serves it via ASP.NET Core when running `dotnet run -- serve`.

## Critical Patterns

### Command Handler Pattern (CLI)
Each command lives in its own file under [Commands/](../src/reshape-cli/Commands/). Follow this structure:

```csharp
namespace Reshape.Cli.Commands;

internal static class CommandNameHandler
{
    public static int Execute(/* params */)
    {
        try {
            // Logic here
            return 0; // Success
        }
        catch (Exception ex) {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return 1; // Error
        }
    }
}
```

Register new commands in [Program.cs](../src/reshape-cli/Program.cs) following existing patterns. Return 0 for success, 1 for errors.

### Source-Generated JSON Serialization
**CRITICAL**: All API models must be registered in [AppJsonSerializerContext.cs](../src/reshape-cli/AppJsonSerializerContext.cs) for AOT compatibility:

```csharp
[JsonSerializable(typeof(YourNewModel))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

Missing types will cause runtime failures in AOT builds. Use camelCase naming (enforced by context options).

### Metadata Extraction System
[FileService.cs](../src/reshape-cli/FileService.cs) extracts metadata using MetadataExtractor library:
- EXIF data from images (date taken, camera info, dimensions)
- File system metadata (created, modified, size)
- Pattern placeholders: `{year}`, `{month}`, `{day}`, `{filename}`, `{camera_make}`, etc.

New metadata fields must be added to `ExtractMetadata()` and documented in `GetPatternTemplates()`.

### API-First Architecture
The web UI is a thin client. ALL business logic lives in [FileService.cs](../src/reshape-cli/FileService.cs) and command handlers. API endpoints in [RunCommand.cs](../src/reshape-cli/Commands/RunCommand.cs) are simple pass-throughs.

TypeScript types in [src/reshape-ui/src/types.ts](../src/reshape-ui/src/types.ts) must mirror C# records in [Models.cs](../src/reshape-cli/Models.cs).

## Development Workflows

### Building & Running
```powershell
# CLI mode (from src/reshape-cli/)
dotnet run -- list C:\Photos --ext .jpg .png
dotnet run -- serve  # Starts web UI on http://localhost:5000

# Build Vue UI (from src/reshape-ui/)
npm run build  # Output goes to src/reshape-cli/wwwroot/
```

The Vue app must be built BEFORE running the CLI's serve command. The build output is copied to src/reshape-cli's wwwroot automatically via Vite config.

### AOT Compilation Requirements
- Enable `<PublishAot>true</PublishAot>` in [Reshape.Cli.csproj](../src/reshape-cli/Reshape.Cli.csproj)
- All JSON types need explicit source generation (see AppJsonSerializerContext)
- Avoid reflection-based APIs
- Test with `dotnet publish -c Release`

## Project Conventions

### File Organization
- Command handlers: One class per file in `Commands/`, suffix with `CommandHandler`
- Models: All record types in single [Models.cs](../src/reshape-cli/Models.cs) file
- Utilities: Helper methods in `Utilities/` folder (e.g., [FormatHelper.cs](../src/reshape-cli/Utilities/FormatHelper.cs))
- Keep [Program.cs](../src/reshape-cli/Program.cs) minimal (~80 lines) - just command registration

### Error Handling
- CLI: Use `AnsiConsole.MarkupLine()` from Spectre.Console for colored output
- API: Return `Results.BadRequest(new { error = message })` for validation errors
- Never throw unhandled exceptions - catch and convert to user-friendly messages

### Naming Conventions
- C# records: PascalCase properties
- JSON serialization: Automatically converted to camelCase
- TypeScript: camelCase for API contract compatibility
- File extensions: Lowercase with dot (`.jpg`, not `JPG`)

## Key Integration Points

### CLI ↔ Web API
API endpoints are defined in `RunCommand.ConfigureApiEndpoints()`:
- POST `/api/scan` - Scan folder for files
- GET `/api/patterns` - Get rename pattern templates  
- POST `/api/preview` - Generate rename preview
- POST `/api/rename` - Execute rename operations
- GET `/api/metadata/{filePath}` - Get file metadata

### Vue UI ↔ API
All API calls in [src/reshape-ui/src/api.ts](../src/reshape-ui/src/api.ts) use typed request/response models. Components should never directly manipulate files or metadata - always go through the API.

## Common Pitfalls

1. **Forgetting to register JSON types** - leads to serialization errors in AOT mode
2. **Building Vue app without copying to wwwroot** - web UI won't load
3. **Using reflection** - breaks AOT compilation
4. **Hardcoding file paths** - always use user-provided paths via CLI args or API
5. **Not sanitizing filenames** - use `FileService.SanitizeForFilename()` for metadata in filenames

## Testing Locally

1. Build Vue app: `cd src/reshape-ui && npm run build`
2. Run CLI serve: `cd src/reshape-cli && dotnet run -- serve`
3. Open http://localhost:5000 in browser
4. Use real photo folders with EXIF data for full testing
