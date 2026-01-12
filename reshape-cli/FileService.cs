using System.Text.RegularExpressions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Spectre.Console;
// ============================================================================
// File Operations Service
// ============================================================================

internal static class FileService
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".heic"];
    private static readonly string[] VideoExtensions = [".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv"];
    private static readonly string[] AudioExtensions = [".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma"];

    public static FileInfo[] ScanFolder(string folderPath, string[]? extensions = null)
    {
        if (!System.IO.Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

        var basePath = System.IO.Path.GetFullPath(folderPath);

        var files = System.IO.Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Select(path => new System.IO.FileInfo(path))
            .Where(f => extensions == null || extensions.Length == 0 ||
                        extensions.Any(ext => f.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
            .Select(f =>
            {
                var relativePath = System.IO.Path.GetRelativePath(basePath, f.DirectoryName ?? basePath);
                return new FileInfo(
                    Name: f.Name,
                    FullPath: f.FullName,
                    RelativePath: relativePath == "." ? "" : relativePath,
                    Extension: f.Extension.ToLowerInvariant(),
                    Size: f.Length,
                    CreatedAt: f.CreationTime,
                    ModifiedAt: f.LastWriteTime,
                    IsSelected: true,  // Standardmäßig alle selektiert
                    Metadata: ExtractMetadata(f.FullName)
                );
            })
            .OrderBy(f => f.FullPath)
            .ToArray();

        return files;
    }

    public static Dictionary<string, string> ExtractMetadata(string filePath)
    {
        var metadata = new Dictionary<string, string>();
        var fileInfo = new System.IO.FileInfo(filePath);
        var ext = fileInfo.Extension.ToLowerInvariant();

        // Basic file metadata
        metadata["filename"] = System.IO.Path.GetFileNameWithoutExtension(filePath);
        metadata["ext"] = ext.TrimStart('.');
        metadata["size"] = fileInfo.Length.ToString();
        metadata["created"] = fileInfo.CreationTime.ToString("yyyy-MM-dd");
        metadata["created_time"] = fileInfo.CreationTime.ToString("HH-mm-ss");
        metadata["modified"] = fileInfo.LastWriteTime.ToString("yyyy-MM-dd");
        metadata["modified_time"] = fileInfo.LastWriteTime.ToString("HH-mm-ss");
        metadata["year"] = fileInfo.LastWriteTime.Year.ToString();
        metadata["month"] = fileInfo.LastWriteTime.Month.ToString("D2");
        metadata["day"] = fileInfo.LastWriteTime.Day.ToString("D2");

        // Try to extract EXIF data for images
        if (ImageExtensions.Contains(ext))
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);

                // EXIF DateTime
                var exifSubIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (exifSubIfd != null)
                {
                    if (exifSubIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTaken))
                    {
                        metadata["date_taken"] = dateTaken.ToString("yyyy-MM-dd");
                        metadata["time_taken"] = dateTaken.ToString("HH-mm-ss");
                        metadata["year"] = dateTaken.Year.ToString();
                        metadata["month"] = dateTaken.Month.ToString("D2");
                        metadata["day"] = dateTaken.Day.ToString("D2");
                    }
                }

                // Camera info
                var exifIfd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                if (exifIfd0 != null)
                {
                    var make = exifIfd0.GetDescription(ExifDirectoryBase.TagMake);
                    var model = exifIfd0.GetDescription(ExifDirectoryBase.TagModel);
                    if (!string.IsNullOrEmpty(make)) metadata["camera_make"] = SanitizeForFilename(make);
                    if (!string.IsNullOrEmpty(model)) metadata["camera_model"] = SanitizeForFilename(model);
                }

                // Image dimensions
                foreach (var dir in directories)
                {
                    var width = dir.GetDescription(ExifDirectoryBase.TagImageWidth);
                    var height = dir.GetDescription(ExifDirectoryBase.TagImageHeight);
                    if (!string.IsNullOrEmpty(width)) metadata["width"] = width;
                    if (!string.IsNullOrEmpty(height)) metadata["height"] = height;
                }
            }
            catch
            {
                // Silently ignore metadata extraction errors
            }
        }

        return metadata;
    }

    public static string ApplyPattern(string pattern, Dictionary<string, string> metadata)
    {
        var result = pattern;

        foreach (var kvp in metadata)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value, StringComparison.OrdinalIgnoreCase);
        }

        // Add counter placeholder support
        result = Regex.Replace(result, @"\{counter(?::(\d+))?\}", m =>
        {
            var padding = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : 3;
            return $"{{counter:{padding}}}"; // Keep placeholder for batch processing
        });

        return SanitizeForFilename(result);
    }

    public static string SanitizeForFilename(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return sanitized.Trim();
    }

    public static RenamePreviewItem[] GeneratePreview(string folderPath, string pattern, string[]? extensions = null)
    {
        var files = ScanFolder(folderPath, extensions);
        var results = new List<RenamePreviewItem>();
        var newNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var counter = 1;

        foreach (var file in files)
        {
            var newName = ApplyPattern(pattern, file.Metadata);

            // Handle counter placeholder
            if (newName.Contains("{counter:"))
            {
                var match = Regex.Match(newName, @"\{counter:(\d+)\}");
                if (match.Success)
                {
                    var padding = int.Parse(match.Groups[1].Value);
                    newName = newName.Replace(match.Value, counter.ToString($"D{padding}"));
                    counter++;
                }
            }

            // Add extension
            newName = newName + file.Extension;

            var hasConflict = newNames.Contains(newName) ||
                             (newName != file.Name && System.IO.File.Exists(System.IO.Path.Combine(folderPath, newName)));

            newNames.Add(newName);
            results.Add(new RenamePreviewItem(file.Name, newName, file.FullPath, file.RelativePath, hasConflict, file.IsSelected));
        }

        return results.ToArray();
    }

    public static RenameResult[] ExecuteRename(RenamePreviewItem[] items, bool dryRun = false)
    {
        var results = new List<RenameResult>();

        foreach (var item in items.Where(i => i.IsSelected && !i.HasConflict && i.OriginalName != i.NewName))
        {
            var directory = System.IO.Path.GetDirectoryName(item.FullPath)!;
            var newPath = System.IO.Path.Combine(directory, item.NewName);

            try
            {
                if (!dryRun)
                {
                    System.IO.File.Move(item.FullPath, newPath);
                }
                results.Add(new RenameResult(item.FullPath, newPath, true, null));
            }
            catch (Exception ex)
            {
                results.Add(new RenameResult(item.FullPath, newPath, false, ex.Message));
            }
        }

        return results.ToArray();
    }

    public static RenamePattern[] GetPatternTemplates() =>
    [
        new("{year}-{month}-{day}_{filename}", "Date prefix: 2024-01-15_photo"),
        new("{date_taken}_{time_taken}_{filename}", "EXIF date/time: 2024-01-15_14-30-00_photo"),
        new("{year}/{month}/{filename}", "Year/Month folders (use with caution)"),
        new("{camera_model}_{date_taken}_{counter:4}", "Camera + date + counter: iPhone_2024-01-15_0001"),
        new("{filename}_{counter:3}", "Original name + counter: photo_001"),
        new("IMG_{year}{month}{day}_{counter:4}", "Standard format: IMG_20240115_0001"),
    ];
}
