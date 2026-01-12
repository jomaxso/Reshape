# Reshape CLI Reference

Complete command-line interface documentation for Reshape.

## Global Usage

```bash
reshape [command] [arguments] [options]
```

## Commands

### `serve` - Start Web UI

Starts the Reshape web user interface server.

```bash
reshape serve
```

**Output:**
- Server starts on `http://localhost:5000`
- Opens the web UI in your default browser

**Example:**
```bash
$ reshape serve
🚀 Server Started
✓ Reshape Web UI is running!
→ http://localhost:5000
Press Ctrl+C to stop the server
```

---

### `list` - List Files

Display files in a folder with metadata information.

```bash
reshape list <path> [options]
```

**Arguments:**
| Argument | Description | Required |
|----------|-------------|----------|
| `path` | Folder path to scan | Yes |

**Options:**
| Option | Description | Example |
|--------|-------------|---------|
| `--ext` | Filter by file extensions | `--ext .jpg .png` |

**Example:**
```bash
$ reshape list "C:\Photos\Vacation" --ext .jpg .png .heic

┌──────────────────┬────────────┬──────────────┬───────────────────────┐
│ Name             │ Size       │ Extension    │ Date Modified         │
├──────────────────┼────────────┼──────────────┼───────────────────────┤
│ IMG_0001.jpg     │ 4.2 MB     │ .jpg         │ 2024-01-15 14:30:00   │
│ IMG_0002.jpg     │ 3.8 MB     │ .jpg         │ 2024-01-15 15:45:00   │
│ DSC_0003.png     │ 8.1 MB     │ .png         │ 2024-01-16 09:00:00   │
└──────────────────┴────────────┴──────────────┴───────────────────────┘

Total: 3 files (16.1 MB)
```

---

### `preview` - Preview Renames

Show a preview of rename operations without executing them.

```bash
reshape preview <path> [options]
```

**Arguments:**
| Argument | Description | Required |
|----------|-------------|----------|
| `path` | Folder path | Yes |

**Options:**
| Option | Description | Default |
|--------|-------------|---------|
| `--pattern` | Rename pattern with placeholders | None |
| `--ext` | Filter by file extensions | All files |

**Example:**
```bash
$ reshape preview "C:\Photos" --pattern "{year}-{month}-{day}_{filename}" --ext .jpg

Preview: Rename Operations
══════════════════════════

┌──────────────────┬──────────────────────────────┬──────────┐
│ Original         │ New Name                     │ Status   │
├──────────────────┼──────────────────────────────┼──────────┤
│ IMG_0001.jpg     │ 2024-01-15_IMG_0001.jpg      │ ✓ OK     │
│ IMG_0002.jpg     │ 2024-01-15_IMG_0002.jpg      │ ✓ OK     │
│ photo.jpg        │ 2024-01-16_photo.jpg         │ ✓ OK     │
└──────────────────┴──────────────────────────────┴──────────┘

3 files will be renamed, 0 conflicts
```

---

### `rename` - Execute Renames

Execute rename operations on files.

```bash
reshape rename <path> [options]
```

**Arguments:**
| Argument | Description | Required |
|----------|-------------|----------|
| `path` | Folder path | Yes |

**Options:**
| Option | Description | Default |
|--------|-------------|---------|
| `--pattern` | Rename pattern with placeholders | None |
| `--ext` | Filter by file extensions | All files |
| `--dry-run` | Preview changes without executing | `false` |
| `--no-interactive` | Skip confirmation prompts and execute automatically | `false` |

**Example:**
```bash
# Interactive mode (default) - will prompt for confirmation
$ reshape rename "C:\Photos" --pattern "{date_taken}_{counter:3}" --ext .jpg

Rename 3 file(s)? [y/n] (y): y

Renaming files...
✓ IMG_0001.jpg → 2024-01-15_001.jpg
✓ IMG_0002.jpg → 2024-01-15_002.jpg
✓ photo.jpg → 2024-01-16_001.jpg

Completed: 3 successful, 0 failed
```

**Non-Interactive Example:**
```bash
# Skip confirmation prompt - useful for automation/scripts
$ reshape rename "C:\Photos" --pattern "{date_taken}_{counter:3}" --ext .jpg --no-interactive

Renaming files...
✓ IMG_0001.jpg → 2024-01-15_001.jpg
✓ IMG_0002.jpg → 2024-01-15_002.jpg
✓ photo.jpg → 2024-01-16_001.jpg

Completed: 3 successful, 0 failed
```

**Dry Run Example:**
```bash
$ reshape rename "C:\Photos" --pattern "{year}/{filename}" --dry-run

DRY RUN - No files will be modified
───────────────────────────────────
Would rename: IMG_0001.jpg → 2024/IMG_0001.jpg
Would rename: IMG_0002.jpg → 2024/IMG_0002.jpg

Dry run complete: 2 files would be renamed
```

---

### `patterns` - Show Pattern Templates

Display available pattern templates with descriptions.

```bash
reshape patterns
```

**Output:**
```bash
$ reshape patterns

Available Pattern Templates
═══════════════════════════

┌────────────────────────────────────────────────┬─────────────────────────────────────────┐
│ Pattern                                        │ Description                             │
├────────────────────────────────────────────────┼─────────────────────────────────────────┤
│ {year}-{month}-{day}_{filename}                │ Date prefix: 2024-01-15_photo           │
│ {date_taken}_{time_taken}_{filename}           │ EXIF date/time: 2024-01-15_14-30-00_... │
│ {year}/{month}/{filename}                      │ Year/Month folders (use with caution)   │
│ {camera_model}_{date_taken}_{counter:4}        │ Camera + date + counter: iPhone_2024... │
│ {filename}_{counter:3}                         │ Original name + counter: photo_001      │
│ IMG_{year}{month}{day}_{counter:4}             │ Standard format: IMG_20240115_0001      │
└────────────────────────────────────────────────┴─────────────────────────────────────────┘
```

---

## Placeholder Reference

### File Information

| Placeholder | Description | Example Value |
|-------------|-------------|---------------|
| `{filename}` | Original filename without extension | `IMG_0001` |
| `{ext}` | File extension without dot | `jpg` |
| `{size}` | File size in bytes | `4194304` |

### Date & Time

| Placeholder | Description | Example Value |
|-------------|-------------|---------------|
| `{year}` | Year (from EXIF or file date) | `2024` |
| `{month}` | Month (2 digits) | `01` |
| `{day}` | Day (2 digits) | `15` |
| `{date_taken}` | EXIF DateTimeOriginal | `2024-01-15` |
| `{time_taken}` | EXIF time | `14-30-00` |
| `{created}` | File creation date | `2024-01-15` |
| `{created_time}` | File creation time | `14-30-00` |
| `{modified}` | File modified date | `2024-01-15` |
| `{modified_time}` | File modified time | `14-30-00` |

### Camera Metadata

| Placeholder | Description | Example Value |
|-------------|-------------|---------------|
| `{camera_make}` | Camera manufacturer | `Canon` |
| `{camera_model}` | Camera model | `EOS_R5` |
| `{width}` | Image width in pixels | `4000` |
| `{height}` | Image height in pixels | `3000` |

### GPS Data

| Placeholder | Description | Example Value |
|-------------|-------------|---------------|
| `{gps_lat}` | GPS latitude | `48.858844` |
| `{gps_lon}` | GPS longitude | `2.294351` |

### Counters

| Placeholder | Description | Example Value |
|-------------|-------------|---------------|
| `{counter:N}` | Counter with N-digit padding | `001`, `0001` |

### Vacation Mode Specific

| Placeholder | Description | Example Value |
|-------------|-------------|---------------|
| `{day_number}` | Day of vacation | `1`, `2`, `3` |
| `{day_counter}` | Counter within day | `001` |
| `{global_counter}` | Global counter | `0001` |

---

## Exit Codes

| Code | Description |
|------|-------------|
| `0` | Success |
| `1` | Error (invalid arguments, file not found, etc.) |

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `RESHAPE_PORT` | Web UI server port | `5000` |

---

## Examples

### Batch Rename Photos by Date

```bash
# Preview first
reshape preview "C:\Photos" --pattern "{year}-{month}-{day}_{filename}" --ext .jpg .png

# Execute if preview looks good (will prompt for confirmation)
reshape rename "C:\Photos" --pattern "{year}-{month}-{day}_{filename}" --ext .jpg .png

# Execute without confirmation prompt (useful for scripts)
reshape rename "C:\Photos" --pattern "{year}-{month}-{day}_{filename}" --ext .jpg .png --no-interactive
```

### Organize by Camera Model

```bash
reshape rename "C:\Photos" --pattern "{camera_model}/{date_taken}_{counter:4}" --ext .jpg
```

### Create Dated Folders

```bash
reshape rename "C:\Photos" --pattern "{year}/{month}/{day}/{filename}" --ext .jpg .heic
```

### Standard Photo Naming

```bash
reshape rename "C:\Photos" --pattern "IMG_{year}{month}{day}_{counter:4}" --ext .jpg
```

---

## Tips & Best Practices

1. **Always preview first**: Use `preview` or `--dry-run` before executing renames
2. **Backup your files**: Always keep backups before batch operations
3. **Use specific extensions**: Filter with `--ext` to avoid renaming unwanted files
4. **Test with small folders**: Try your pattern on a small folder first
5. **Check for conflicts**: The preview shows conflicts before execution
6. **Use --no-interactive for scripts**: When automating renames in scripts or CI/CD pipelines, use `--no-interactive` to skip confirmation prompts
