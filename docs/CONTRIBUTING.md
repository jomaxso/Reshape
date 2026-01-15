# Contributing to Reshape

Thank you for your interest in contributing to Reshape! This document provides guidelines and information for contributors.

## Code of Conduct

Please be respectful and considerate in all interactions. We welcome contributors of all experience levels.

## How to Contribute

### Reporting Bugs

1. Check if the bug has already been reported in [Issues](https://github.com/jomaxso/Reshape/issues)
2. If not, create a new issue with:
   - Clear, descriptive title
   - Steps to reproduce
   - Expected behavior
   - Actual behavior
   - Environment details (OS, .NET version, etc.)
   - Screenshots if applicable

### Suggesting Features

1. Check existing issues for similar suggestions
2. Create a new issue with:
   - Clear description of the feature
   - Use case / problem it solves
   - Proposed implementation (optional)

### Submitting Changes

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes
4. Test thoroughly
5. Commit with clear messages
6. Push to your fork
7. Open a Pull Request

## Development Setup

See [DEVELOPMENT.md](DEVELOPMENT.md) for detailed setup instructions.

### Quick Start

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/Reshape.git
cd Reshape

# Build CLI
cd src/reshape-cli
dotnet build

# Build UI
cd ../reshape-ui
npm install
npm run build

# Run
cd ../reshape-cli
dotnet run -- serve
```

## Pull Request Process

### Before Submitting

- [ ] Code follows project style guidelines
- [ ] Self-reviewed the changes
- [ ] Added/updated tests if applicable
- [ ] Updated documentation
- [ ] Tested both CLI and Web UI
- [ ] Verified AOT compatibility
- [ ] No new warnings or errors

### PR Guidelines

1. **Title**: Clear, descriptive summary
2. **Description**: Explain what and why
3. **Size**: Keep PRs focused and reasonably sized
4. **Tests**: Include relevant tests
5. **Documentation**: Update as needed

### Review Process

1. A maintainer will review your PR
2. Address any feedback
3. Once approved, it will be merged

## Coding Standards

### C# / .NET

```csharp
// Use descriptive names
internal static class FileOperationHandler { }

// Records for DTOs
internal record ScanRequest(string FolderPath, string[]? Extensions = null);

// Consistent error handling
try
{
    // Logic
    return 0;
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
    return 1;
}
```

### TypeScript / Vue

```typescript
// Use TypeScript properly
interface Props {
    items: FileInfo[];
}

// Composition API
const props = defineProps<Props>();
const selectedItems = ref<FileInfo[]>([]);
```

### Commit Messages

Use clear, descriptive commit messages:

```
feat: Add vacation mode subfolder support
fix: Correct timezone calculation for GPS coordinates
docs: Update API documentation
refactor: Extract metadata extraction to separate method
```

## Project Structure

```
src/
  reshape-cli/
  ├── Commands/          # Command structure
  │   ├── Files/         # File operation commands (list, preview, rename)
  │   ├── Patterns/      # Pattern-related commands
  │   ├── RunCommand.cs  # Web server command (serve)
  │   └── UpdateCommand.cs # Self-update command
  ├── Utilities/         # Helper classes
  ├── FileService.cs     # Core business logic
  ├── Models.cs          # All data models
  └── AppJsonSerializerContext.cs  # AOT JSON config

  reshape-ui/
  └── src/
      ├── components/    # Vue components
      ├── api.ts         # API client
      └── types.ts       # TypeScript types (mirror Models.cs)
```

## Important Notes

### AOT Compatibility

All new types used in API must be registered:

```csharp
// AppJsonSerializerContext.cs
[JsonSerializable(typeof(YourNewType))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

### Type Synchronization

Keep TypeScript types in sync with C# models:

```csharp
// Models.cs
internal record MyModel(string Name, int Value);
```

```typescript
// types.ts
export interface MyModel {
    name: string;
    value: number;
}
```

### Testing Checklist

- [ ] CLI commands work correctly
- [ ] Web UI loads and functions
- [ ] API endpoints respond correctly
- [ ] Preview mode shows accurate results
- [ ] Rename operations work as expected
- [ ] Error handling works properly
- [ ] Edge cases are handled

## Getting Help

- Check existing documentation
- Search closed issues
- Ask in a new issue with the "question" label

## Recognition

Contributors will be recognized in:
- GitHub contributors list
- Release notes (for significant contributions)

Thank you for contributing! 🎉
