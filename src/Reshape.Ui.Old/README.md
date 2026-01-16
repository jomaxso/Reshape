# Reshape Web UI

Modern web interface for the Reshape CLI, built with Vue 3, TypeScript, and Vite.

## Overview

This is the frontend application for Reshape, providing an intuitive graphical interface for batch file renaming operations. The UI communicates with the embedded ASP.NET Core API server in the CLI application.

## Tech Stack

- **Vue 3** - Progressive JavaScript framework with Composition API
- **TypeScript** - Type-safe development
- **Vite** - Fast build tool and dev server
- **Composition API** - Modern Vue development with `<script setup>`

## Project Structure

```
src/
├── components/       # Vue components
│   ├── FileList.vue  # File listing and selection
│   ├── PatternBuilder.vue # Rename pattern builder
│   └── ...           # Other UI components
├── api.ts           # API client for backend communication
├── types.ts         # TypeScript type definitions (mirrors C# models)
├── App.vue          # Root application component
└── main.ts          # Application entry point
```

## Development

### Prerequisites

- Node.js 20+ 
- npm

### Setup

```bash
# Install dependencies
npm install

# Start development server
npm run dev
```

The dev server runs at `http://localhost:5173` with hot module replacement.

**Note:** You must also run the backend API server for the UI to function:

```bash
cd ../Reshape.Cli
dotnet run -- run
```

### Building for Production

```bash
# Build for production
npm run build
```

Output goes to `../Reshape.Cli/wwwroot/` where it's embedded in the CLI application.

## Type Safety

TypeScript types in `types.ts` mirror the C# models in `src/Reshape.Cli/Models.cs`. When adding new API endpoints or models:

1. Update C# models in `Models.cs`
2. Register in `AppJsonSerializerContext.cs` for AOT
3. Update TypeScript types in `types.ts`
4. Update API client in `api.ts`

Example:

```typescript
// types.ts
export interface FileInfo {
    name: string;
    fullPath: string;
    size: number;
    // ... matches C# record
}

// api.ts
async scanFolder(path: string): Promise<ScanResponse> {
    return request<ScanResponse>('/scan', {
        method: 'POST',
        body: JSON.stringify({ folderPath: path })
    });
}
```

## API Integration

All API calls go through the centralized `api.ts` client:

- `POST /api/scan` - Scan folder for files
- `GET /api/patterns` - Get rename patterns
- `POST /api/preview` - Preview rename operations
- `POST /api/rename` - Execute renames
- `GET /api/metadata/{path}` - Get file metadata

See [API.md](../../docs/API.md) for complete API documentation.

## Component Guidelines

- Use Composition API with `<script setup lang="ts">`
- Maintain strict TypeScript mode
- Follow Vue 3 best practices
- Keep components focused and reusable
- Use proper type annotations

Example component structure:

```vue
<script setup lang="ts">
import { ref, computed } from 'vue';
import type { FileInfo } from '../types';

interface Props {
    files: FileInfo[];
}

const props = defineProps<Props>();
const selectedFiles = ref<FileInfo[]>([]);

const totalSize = computed(() => 
    selectedFiles.value.reduce((sum, f) => sum + f.size, 0)
);
</script>

<template>
    <div>
        <!-- Template here -->
    </div>
</template>
```

## Resources

- [Vue 3 Documentation](https://vuejs.org/)
- [TypeScript Guide](https://vuejs.org/guide/typescript/overview.html)
- [Vite Documentation](https://vitejs.dev/)
- [Composition API Guide](https://vuejs.org/guide/extras/composition-api-faq.html)
