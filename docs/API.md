# Reshape REST API Reference

The Reshape CLI includes an embedded REST API server that powers the Web UI. This API can also be used for custom integrations.

## Base URL

```
http://localhost:5000/api
```

## Authentication

No authentication required for local usage. The API is designed for local development and personal use.

---

## Endpoints

### POST `/api/scan`

Scan a folder and retrieve all files with their metadata.

**Request Body:**
```json
{
    "folderPath": "C:\\Photos\\Vacation",
    "extensions": [".jpg", ".png", ".heic"]
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `folderPath` | string | Yes | Absolute path to the folder to scan |
| `extensions` | string[] | No | Filter by file extensions |

**Response:**
```json
{
    "folderPath": "C:\\Photos\\Vacation",
    "files": [
        {
            "name": "IMG_0001.jpg",
            "fullPath": "C:\\Photos\\Vacation\\IMG_0001.jpg",
            "relativePath": "",
            "extension": ".jpg",
            "size": 4194304,
            "createdAt": "2024-01-15T14:30:00",
            "modifiedAt": "2024-01-15T14:30:00",
            "isSelected": true,
            "metadata": {
                "filename": "IMG_0001",
                "ext": "jpg",
                "year": "2024",
                "month": "01",
                "day": "15",
                "date_taken": "2024-01-15",
                "time_taken": "14-30-00",
                "camera_make": "Apple",
                "camera_model": "iPhone_15_Pro"
            },
            "gpsCoordinates": {
                "latitude": 48.858844,
                "longitude": 2.294351
            },
            "dateTakenUtc": "2024-01-15T13:30:00Z"
        }
    ],
    "totalCount": 1
}
```

**Error Response:**
```json
{
    "error": "Folder not found: C:\\NonExistent"
}
```

---

### GET `/api/patterns`

Get available rename pattern templates.

**Response:**
```json
[
    {
        "pattern": "{year}-{month}-{day}_{filename}",
        "description": "Date prefix: 2024-01-15_photo"
    },
    {
        "pattern": "{date_taken}_{time_taken}_{filename}",
        "description": "EXIF date/time: 2024-01-15_14-30-00_photo"
    },
    {
        "pattern": "{year}/{month}/{filename}",
        "description": "Year/Month folders (use with caution)"
    },
    {
        "pattern": "{camera_model}_{date_taken}_{counter:4}",
        "description": "Camera + date + counter: iPhone_2024-01-15_0001"
    },
    {
        "pattern": "{filename}_{counter:3}",
        "description": "Original name + counter: photo_001"
    },
    {
        "pattern": "IMG_{year}{month}{day}_{counter:4}",
        "description": "Standard format: IMG_20240115_0001"
    }
]
```

---

### POST `/api/patterns/add`

Add a custom pattern template.

**Request Body:**
```json
{
    "pattern": "{camera_model}_{year}_{counter:3}",
    "description": "Custom camera pattern with year and counter"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `pattern` | string | Yes | The pattern template to add |
| `description` | string | Yes | Description of what the pattern does |

**Response:**
```json
{
    "success": true,
    "message": "Pattern added successfully"
}
```

**Error Response:**
```json
{
    "success": false,
    "message": "Pattern already exists"
}
```

---

### POST `/api/patterns/remove`

Remove a custom pattern template.

**Request Body:**
```json
{
    "pattern": "{camera_model}_{year}_{counter:3}"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `pattern` | string | Yes | The pattern template to remove |

**Response:**
```json
{
    "success": true,
    "message": "Pattern removed successfully"
}
```

**Error Response:**
```json
{
    "success": false,
    "message": "Pattern not found"
}
```

---

### POST `/api/preview`

Generate a preview of rename operations.

**Request Body:**
```json
{
    "folderPath": "C:\\Photos\\Vacation",
    "pattern": "{year}-{month}-{day}_{filename}",
    "extensions": [".jpg"],
    "vacationMode": {
        "enabled": false,
        "startDate": null,
        "dayFolderPattern": "Tag {day_number}",
        "subfolderPattern": null
    }
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `folderPath` | string | Yes | Folder path |
| `pattern` | string | Yes | Rename pattern |
| `extensions` | string[] | No | Filter by extensions |
| `vacationMode` | object | No | Vacation mode configuration |

**Vacation Mode Options:**
| Field | Type | Description |
|-------|------|-------------|
| `enabled` | boolean | Enable vacation mode |
| `startDate` | string | ISO date string (optional, auto-detected if null) |
| `dayFolderPattern` | string | Folder naming pattern (e.g., "Day {day_number}") |
| `subfolderPattern` | string | Optional subfolder pattern |

**Response:**
```json
{
    "items": [
        {
            "originalName": "IMG_0001.jpg",
            "newName": "2024-01-15_IMG_0001.jpg",
            "fullPath": "C:\\Photos\\Vacation\\IMG_0001.jpg",
            "relativePath": "",
            "hasConflict": false,
            "isSelected": true,
            "dayNumber": null
        },
        {
            "originalName": "IMG_0002.jpg",
            "newName": "2024-01-15_IMG_0002.jpg",
            "fullPath": "C:\\Photos\\Vacation\\IMG_0002.jpg",
            "relativePath": "",
            "hasConflict": false,
            "isSelected": true,
            "dayNumber": null
        }
    ],
    "conflictCount": 0
}
```

**Vacation Mode Response Example:**
```json
{
    "items": [
        {
            "originalName": "IMG_0001.jpg",
            "newName": "Tag 1\\2024-01-15_001.jpg",
            "fullPath": "C:\\Photos\\Vacation\\IMG_0001.jpg",
            "relativePath": "",
            "hasConflict": false,
            "isSelected": true,
            "dayNumber": 1
        },
        {
            "originalName": "IMG_0005.jpg",
            "newName": "Tag 2\\2024-01-16_001.jpg",
            "fullPath": "C:\\Photos\\Vacation\\IMG_0005.jpg",
            "relativePath": "",
            "hasConflict": false,
            "isSelected": true,
            "dayNumber": 2
        }
    ],
    "conflictCount": 0
}
```

---

### POST `/api/rename`

Execute rename operations.

**Request Body:**
```json
{
    "items": [
        {
            "originalName": "IMG_0001.jpg",
            "newName": "2024-01-15_IMG_0001.jpg",
            "fullPath": "C:\\Photos\\Vacation\\IMG_0001.jpg",
            "relativePath": "",
            "hasConflict": false,
            "isSelected": true,
            "dayNumber": null
        }
    ],
    "baseFolderPath": "C:\\Photos\\Vacation",
    "dryRun": false
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `items` | array | Yes | Array of preview items to rename |
| `baseFolderPath` | string | No | Base folder for vacation mode |
| `dryRun` | boolean | No | Preview without executing (default: false) |

**Response:**
```json
{
    "results": [
        {
            "originalPath": "C:\\Photos\\Vacation\\IMG_0001.jpg",
            "newPath": "C:\\Photos\\Vacation\\2024-01-15_IMG_0001.jpg",
            "success": true,
            "error": null
        }
    ],
    "successCount": 1,
    "errorCount": 0
}
```

**Error Response:**
```json
{
    "results": [
        {
            "originalPath": "C:\\Photos\\Vacation\\IMG_0001.jpg",
            "newPath": "C:\\Photos\\Vacation\\2024-01-15_IMG_0001.jpg",
            "success": false,
            "error": "Access denied"
        }
    ],
    "successCount": 0,
    "errorCount": 1
}
```

---

### GET `/api/metadata/{filePath}`

Get metadata for a specific file.

**URL Parameters:**
| Parameter | Description |
|-----------|-------------|
| `filePath` | URL-encoded absolute file path |

**Example:**
```
GET /api/metadata/C%3A%5CPhotos%5CIMG_0001.jpg
```

**Response:**
```json
{
    "filename": "IMG_0001",
    "ext": "jpg",
    "size": "4194304",
    "created": "2024-01-15",
    "created_time": "14-30-00",
    "modified": "2024-01-15",
    "modified_time": "14-30-00",
    "year": "2024",
    "month": "01",
    "day": "15",
    "date_taken": "2024-01-15",
    "time_taken": "14-30-00",
    "camera_make": "Apple",
    "camera_model": "iPhone_15_Pro",
    "width": "4000",
    "height": "3000",
    "gps_lat": "48.858844",
    "gps_lon": "2.294351"
}
```

---

## Data Types

### FileInfo

```typescript
interface FileInfo {
    name: string;
    fullPath: string;
    relativePath: string;
    extension: string;
    size: number;
    createdAt: string;      // ISO datetime
    modifiedAt: string;     // ISO datetime
    isSelected: boolean;
    metadata: Record<string, string>;
    gpsCoordinates?: GpsCoordinates;
    dateTakenUtc?: string;  // ISO datetime
}
```

### GpsCoordinates

```typescript
interface GpsCoordinates {
    latitude: number;
    longitude: number;
}
```

### RenamePreviewItem

```typescript
interface RenamePreviewItem {
    originalName: string;
    newName: string;
    fullPath: string;
    relativePath: string;
    hasConflict: boolean;
    isSelected: boolean;
    dayNumber?: number;
}
```

### RenameResult

```typescript
interface RenameResult {
    originalPath: string;
    newPath: string;
    success: boolean;
    error?: string;
}
```

### VacationModeOptions

```typescript
interface VacationModeOptions {
    enabled: boolean;
    startDate?: string;         // ISO date
    dayFolderPattern: string;   // e.g., "Tag {day_number}"
    subfolderPattern?: string;  // e.g., "{camera_model}"
}
```

### PatternResponse

```typescript
interface PatternResponse {
    success: boolean;
    message: string;
}
```

---

## Error Handling

All endpoints return appropriate HTTP status codes:

| Status | Description |
|--------|-------------|
| `200` | Success |
| `400` | Bad request (invalid parameters) |
| `404` | Not found (file/folder doesn't exist) |
| `500` | Internal server error |

Error responses follow this format:
```json
{
    "error": "Error message describing what went wrong"
}
```

---

## Usage Examples

### JavaScript/TypeScript

```typescript
// Scan folder
const scanResponse = await fetch('/api/scan', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        folderPath: 'C:\\Photos',
        extensions: ['.jpg', '.png']
    })
});
const { files } = await scanResponse.json();

// Preview rename
const previewResponse = await fetch('/api/preview', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        folderPath: 'C:\\Photos',
        pattern: '{year}-{month}-{day}_{filename}'
    })
});
const { items, conflictCount } = await previewResponse.json();

// Execute rename
const renameResponse = await fetch('/api/rename', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ items })
});
const { successCount, errorCount } = await renameResponse.json();
```

### cURL

```bash
# Scan folder
curl -X POST http://localhost:5000/api/scan \
  -H "Content-Type: application/json" \
  -d '{"folderPath": "C:\\Photos", "extensions": [".jpg"]}'

# Get patterns
curl http://localhost:5000/api/patterns

# Preview rename
curl -X POST http://localhost:5000/api/preview \
  -H "Content-Type: application/json" \
  -d '{"folderPath": "C:\\Photos", "pattern": "{year}_{filename}"}'
```

### PowerShell

```powershell
# Scan folder
$body = @{
    folderPath = "C:\Photos"
    extensions = @(".jpg", ".png")
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/scan" `
    -Method Post -ContentType "application/json" -Body $body

$response.files | Format-Table name, size, extension
```
