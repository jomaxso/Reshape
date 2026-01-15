# Reshape Development Guide

This guide covers everything you need to know to contribute to Reshape.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Visual Studio Code](https://code.visualstudio.com/) (recommended)
- Git

### Recommended VS Code Extensions

- C# Dev Kit
- Vue - Official
- TypeScript Vue Plugin
- ESLint
- Prettier

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/jomaxso/Reshape.git
cd Reshape
```

### 2. Build the CLI

```bash
cd src/reshape-cli
dotnet restore
dotnet build
```

### 3. Build the Vue UI

```bash
cd src/reshape-ui
npm install
npm run build
```

### 4. Run the Application

```bash
cd src/reshape-cli
dotnet run -- serve
```

Open `http://localhost:5000` in your browser.

---

## Project Architecture

### Overview

```
Reshape/
├── src/
│   ├── reshape-cli/           # .NET Backend + CLI
│   │   ├── Commands/          # Command organization
│   │   │   ├── Files/         # File operation commands
│   │   │   ├── Patterns/      # Pattern commands
│   │   │   ├── RunCommand.cs  # Web server
│   │   │   └── UpdateCommand.cs # Self-update
│   │   ├── Utilities/         # Helper classes
│   │   ├── Options/           # CLI options
│   │   ├── wwwroot/           # Compiled Vue app
│   │   ├── Program.cs         # CLI entry point
│   │   ├── FileService.cs     # Core business logic
│   │   ├── ConfigurationService.cs # Configuration
│   │   ├── Models.cs          # Data models
│   │   └── AppJsonSerializerContext.cs  # AOT JSON config
│   │
│   └── reshape-ui/            # Vue 3 Frontend
│       └── src/
│           ├── components/    # Vue components
│           ├── api.ts         # API client
│           └── types.ts       # TypeScript types
```

### Key Design Decisions

1. **Embedded Web Server**: The CLI embeds ASP.NET Core to serve both the API and Vue app
2. **AOT Compilation**: Native AOT for fast startup times
3. **Source-Generated JSON**: Required for AOT compatibility
4. **Single-File Output**: Can be published as a single executable

---

## Development Workflow

### CLI Development

```bash
cd reshape-cli

# Run in development mode
dotnet run -- list "C:\TestFolder" --ext .jpg

# Run web server
dotnet run -- serve

# Watch mode (auto-restart on changes)
dotnet watch run -- serve
```

### UI Development

For UI-only development with hot reload:

```bash
cd reshape-ui

# Start Vite dev server
npm run dev
```

This runs the Vue app at `http://localhost:5173` with hot module replacement.

**Note:** You still need the CLI running (`dotnet run -- serve`) for API calls.

### Full-Stack Development

Terminal 1 - Backend:
```bash
cd reshape-cli
dotnet watch run -- serve
```

Terminal 2 - Frontend (optional, for hot reload):
```bash
cd reshape-ui
npm run dev
```

---

## Adding New Features

### Adding a New CLI Command

1. **Create the command** in `src/reshape-cli/Commands/Files/` or `Commands/Patterns/`:

```csharp
namespace Reshape.Cli.Commands.Files;

internal static class MyCommand
{
    public static Command Create()
    {
        var command = new Command("mycommand", "Description of my command");
        
        var param1Arg = new Argument<string>("param1", "Parameter description");
        var flag1Option = new Option<bool>("--flag1", "Flag description");
        
        command.AddArgument(param1Arg);
        command.AddOption(flag1Option);
        
        command.SetHandler((string param1, bool flag1) =>
        {
            try
            {
                // Your logic here
                AnsiConsole.MarkupLine("[green]Success![/]");
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
                return 1;
            }
        }, param1Arg, flag1Option);
        
        return command;
    }
}
```

2. **Register the command** in `Program.cs` or the appropriate parent command:

```csharp
var myCommand = MyCommand.Create();
rootCommand.AddCommand(myCommand);
```

### Adding a New API Endpoint

1. **Add to `RunCommand.cs`** in the `ConfigureApiEndpoints` method:

```csharp
api.MapPost("/myendpoint", (MyRequest request) =>
{
    try
    {
        var result = FileService.MyMethod(request.Param);
        return Results.Ok(new MyResponse(result));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});
```

2. **Add models to `Models.cs`**:

```csharp
internal record MyRequest(string Param);
internal record MyResponse(string Result);
```

3. **Register in `AppJsonSerializerContext.cs`**:

```csharp
[JsonSerializable(typeof(MyRequest))]
[JsonSerializable(typeof(MyResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

4. **Add TypeScript types** in `src/reshape-ui/src/types.ts`:

```typescript
export interface MyRequest {
    param: string;
}

export interface MyResponse {
    result: string;
}
```

5. **Add API method** in `src/reshape-ui/src/api.ts`:

```typescript
async myEndpoint(param: string): Promise<MyResponse> {
    const body: MyRequest = { param };
    return request<MyResponse>('/myendpoint', {
        method: 'POST',
        body: JSON.stringify(body),
    });
}
```

### Adding New Metadata Fields

1. **Extract in `FileService.ExtractMetadataWithGps()`**:

```csharp
// Add to the metadata dictionary
if (someCondition)
{
    metadata["my_field"] = extractedValue;
}
```

2. **Document in `FileService.GetPatternTemplates()`**:

```csharp
new("{my_field}_{filename}", "Description of pattern"),
```

3. **Update documentation** in `docs/CLI.md`

---

## Testing

### Manual Testing

```bash
# Test CLI commands
dotnet run -- list "C:\TestPhotos" --ext .jpg
dotnet run -- preview "C:\TestPhotos" --pattern "{year}_{filename}"
dotnet run -- rename "C:\TestPhotos" --pattern "{year}_{filename}" --dry-run

# Test Web UI
dotnet run -- serve
# Then open http://localhost:5000 and test manually
```

### Test Data

Create a test folder with sample images that have EXIF data for comprehensive testing.

---

## Building for Production

### Standard Build

```bash
cd reshape-ui
npm run build

cd ../reshape-cli
dotnet publish -c Release
```

### Native AOT Build

```bash
cd reshape-cli
dotnet publish -c Release -r win-x64 --self-contained
```

Output will be in `bin/Release/net10.0/win-x64/publish/`

### Cross-Platform Builds

```bash
# Windows
dotnet publish -c Release -r win-x64

# Linux
dotnet publish -c Release -r linux-x64

# macOS (Intel)
dotnet publish -c Release -r osx-x64

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64
```

---

## Code Style Guidelines

### C# (.NET)

- Use `internal static` for command handlers
- Records for DTOs/Models
- Return `0` for success, `1` for errors
- Use Spectre.Console for terminal output
- XML documentation for public APIs
- Nullable reference types enabled

```csharp
/// <summary>
/// Brief description of the method.
/// </summary>
/// <param name="path">Parameter description.</param>
/// <returns>Return value description.</returns>
internal static int Execute(string path)
{
    // Implementation
}
```

### TypeScript (Vue)

- Use Composition API with `<script setup>`
- TypeScript strict mode
- PascalCase for components
- camelCase for variables/functions
- Proper type annotations

```vue
<script setup lang="ts">
import { ref, computed } from 'vue';

interface Props {
    items: string[];
}

const props = defineProps<Props>();
const selectedIndex = ref<number>(0);

const selectedItem = computed(() => props.items[selectedIndex.value]);
</script>
```

---

## Common Issues

### AOT Compilation Fails

**Problem:** Runtime error about serialization

**Solution:** Register all types in `AppJsonSerializerContext.cs`:
```csharp
[JsonSerializable(typeof(YourType))]
```

### Vue App Not Loading

**Problem:** 404 errors for static files

**Solution:** Rebuild the Vue app:
```bash
cd reshape-ui
npm run build
```

### API Not Responding

**Problem:** CORS or connection errors in dev mode

**Solution:** Ensure the CLI server is running on port 5000

### Metadata Not Extracted

**Problem:** EXIF data not showing

**Solution:** Ensure test files have actual EXIF data (camera photos, not screenshots)

---

## Debugging

### CLI Debugging in VS Code

```json
// .vscode/launch.json
{
    "configurations": [
        {
            "name": "Debug CLI",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/reshape-cli/bin/Debug/net10.0/Reshape.Cli.dll",
            "args": ["serve"],
            "cwd": "${workspaceFolder}/src/reshape-cli"
        }
    ]
}
```

### Vue Debugging

Use Vue DevTools browser extension for component inspection.

---

## Pull Request Guidelines

1. Create feature branches from `main`
2. Follow the PR template
3. Include tests for new features
4. Update documentation
5. Ensure AOT compatibility
6. Test both CLI and Web UI

---

## Resources

- [.NET CLI Documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [System.CommandLine](https://github.com/dotnet/command-line-api)
- [Spectre.Console](https://spectreconsole.net/)
- [Vue 3 Documentation](https://vuejs.org/)
- [Vite Documentation](https://vitejs.dev/)
- [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet)
