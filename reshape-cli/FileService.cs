using System.Text.RegularExpressions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Spectre.Console;
using TimeZoneConverter;
using MetadataExtractor.Formats.Exif.Makernotes;
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
                var (metadata, gps, dateTaken) = ExtractMetadataWithGps(f.FullName);

                return new FileInfo(
                    Name: f.Name,
                    FullPath: f.FullName,
                    RelativePath: relativePath == "." ? "" : relativePath,
                    Extension: f.Extension.ToLowerInvariant(),
                    Size: f.Length,
                    CreatedAt: f.CreationTime,
                    ModifiedAt: f.LastWriteTime,
                    IsSelected: true,
                    Metadata: metadata,
                    GpsCoordinates: gps,
                    DateTakenUtc: dateTaken
                );
            })
            .OrderBy(f => f.FullPath)
            .ToArray();

        return files;
    }

    public static (Dictionary<string, string> metadata, GpsCoordinates? gps, DateTime? dateTakenUtc) ExtractMetadataWithGps(string filePath)
    {
        var metadata = new Dictionary<string, string>();
        var fileInfo = new System.IO.FileInfo(filePath);
        var ext = fileInfo.Extension.ToLowerInvariant();
        GpsCoordinates? gps = null;
        DateTime? dateTakenUtc = null;

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

                        // Store original date as DateTime for vacation mode
                        dateTakenUtc = dateTaken;
                    }
                }

                // GPS Data
                var gpsDir = directories.OfType<GpsDirectory>().FirstOrDefault();
                if (gpsDir != null)
                {
                    var location = gpsDir.GetGeoLocation();
                    if (location != null && !location.IsZero)
                    {
                        gps = new GpsCoordinates(location.Latitude, location.Longitude);
                        metadata["gps_lat"] = location.Latitude.ToString("F6");
                        metadata["gps_lon"] = location.Longitude.ToString("F6");

                        // If we have GPS and dateTaken, try to convert to UTC
                        if (dateTakenUtc.HasValue && gps != null)
                        {
                            var timeZoneId = GetTimeZoneFromCoordinates(gps.Latitude, gps.Longitude);
                            if (timeZoneId != null)
                            {
                                try
                                {
                                    var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
                                    dateTakenUtc = TimeZoneInfo.ConvertTimeToUtc(dateTakenUtc.Value, tz);
                                }
                                catch
                                {
                                    // Fallback: assume local time
                                }
                            }
                        }
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

        return (metadata, gps, dateTakenUtc);
    }

    public static Dictionary<string, string> ExtractMetadata(string filePath)
    {
        var (metadata, _, _) = ExtractMetadataWithGps(filePath);
        return metadata;
    }

    private static string? GetTimeZoneFromCoordinates(double latitude, double longitude)
    {
        // Simplified timezone lookup based on longitude
        // For a production app, use a proper timezone API or library like GeoTimeZone
        var hourOffset = (int)Math.Round(longitude / 15.0);

        // Basic mapping - this is simplified and won't work for all edge cases
        return hourOffset switch
        {
            -12 => "Pacific/Auckland",  // Approximate
            -11 => "Pacific/Midway",
            -10 => "Pacific/Honolulu",
            -9 => "America/Anchorage",
            -8 => "America/Los_Angeles",
            -7 => "America/Denver",
            -6 => "America/Chicago",
            -5 => "America/New_York",
            -4 => "America/Halifax",
            -3 => "America/Sao_Paulo",
            -2 => "Atlantic/South_Georgia",
            -1 => "Atlantic/Azores",
            0 => "Europe/London",
            1 => "Europe/Paris",
            2 => "Europe/Athens",
            3 => "Europe/Moscow",
            4 => "Asia/Dubai",
            5 => "Asia/Karachi",
            6 => "Asia/Dhaka",
            7 => "Asia/Bangkok",
            8 => "Asia/Singapore",
            9 => "Asia/Tokyo",
            10 => "Australia/Sydney",
            11 => "Pacific/Noumea",
            12 => "Pacific/Auckland",
            _ => null
        };
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

    public static RenamePreviewItem[] GeneratePreview(
        string folderPath,
        string pattern,
        string[]? extensions = null,
        VacationModeOptions? vacationMode = null)
    {
        var files = ScanFolder(folderPath, extensions);

        if (vacationMode?.Enabled == true)
        {
            return GenerateVacationPreview(files, folderPath, pattern, vacationMode);
        }

        return GenerateStandardPreview(files, folderPath, pattern);
    }

    private static RenamePreviewItem[] GenerateStandardPreview(
        FileInfo[] files,
        string folderPath,
        string pattern)
    {
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
            results.Add(new RenamePreviewItem(
                file.Name,
                newName,
                file.FullPath,
                file.RelativePath,
                hasConflict,
                file.IsSelected,
                null
            ));
        }

        return results.ToArray();
    }

    private static RenamePreviewItem[] GenerateVacationPreview(
        FileInfo[] files,
        string folderPath,
        string pattern,
        VacationModeOptions vacationMode)
    {
        var results = new List<RenamePreviewItem>();

        // Find files with valid UTC dates
        var filesWithDates = files
            .Where(f => f.DateTakenUtc.HasValue)
            .OrderBy(f => f.DateTakenUtc!.Value)
            .ToList();

        if (!filesWithDates.Any())
        {
            // Fallback to standard preview if no dates found
            return GenerateStandardPreview(files, folderPath, pattern);
        }

        // Determine start date
        var startDate = vacationMode.StartDate ?? filesWithDates.First().DateTakenUtc!.Value.Date;

        // Group files by day number
        var filesByDay = filesWithDates
            .GroupBy(f =>
            {
                var daysDiff = (f.DateTakenUtc!.Value.Date - startDate).Days + 1;
                return daysDiff > 0 ? daysDiff : 1;
            })
            .OrderBy(g => g.Key)
            .ToList();

        var globalCounter = 1;
        var newPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dayGroup in filesByDay)
        {
            var dayNumber = dayGroup.Key;
            var dayCounter = 1;

            // Create day folder name
            var dayFolderName = vacationMode.DayFolderPattern
                .Replace("{day_number}", dayNumber.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{day}", dayNumber.ToString("D2"), StringComparison.OrdinalIgnoreCase);
            dayFolderName = SanitizeForFilename(dayFolderName);

            foreach (var file in dayGroup.OrderBy(f => f.DateTakenUtc))
            {
                // Apply pattern with additional placeholders
                var metadata = new Dictionary<string, string>(file.Metadata)
                {
                    ["day_number"] = dayNumber.ToString(),
                    ["day_counter"] = dayCounter.ToString("D3"),
                    ["global_counter"] = globalCounter.ToString("D4")
                };

                var newName = ApplyPattern(pattern, metadata);

                // Handle counter placeholder
                if (newName.Contains("{counter:"))
                {
                    var match = Regex.Match(newName, @"\{counter:(\d+)\}");
                    if (match.Success)
                    {
                        var padding = int.Parse(match.Groups[1].Value);
                        newName = newName.Replace(match.Value, dayCounter.ToString($"D{padding}"));
                    }
                }

                newName = newName + file.Extension;

                // Build full relative path with day folder
                string relativePath;
                if (!string.IsNullOrEmpty(vacationMode.SubfolderPattern))
                {
                    var subfolder = ApplyPattern(vacationMode.SubfolderPattern, metadata);
                    subfolder = SanitizeForFilename(subfolder);
                    relativePath = System.IO.Path.Combine(dayFolderName, subfolder);
                }
                else
                {
                    relativePath = dayFolderName;
                }

                var fullNewPath = System.IO.Path.Combine(relativePath, newName);
                var hasConflict = newPaths.Contains(fullNewPath);
                newPaths.Add(fullNewPath);

                results.Add(new RenamePreviewItem(
                    file.Name,
                    fullNewPath,  // Include folder structure in preview
                    file.FullPath,
                    file.RelativePath,
                    hasConflict,
                    file.IsSelected,
                    dayNumber
                ));

                dayCounter++;
                globalCounter++;
            }
        }

        // Add files without dates at the end (not in day folders)
        var filesWithoutDates = files.Where(f => !f.DateTakenUtc.HasValue).ToList();
        foreach (var file in filesWithoutDates)
        {
            results.Add(new RenamePreviewItem(
                file.Name,
                file.Name,  // Keep original name
                file.FullPath,
                file.RelativePath,
                false,
                false,  // Deselect files without dates
                null
            ));
        }

        return results.ToArray();
    }

    public static RenameResult[] ExecuteRename(RenamePreviewItem[] items, string? baseFolderPath = null, bool dryRun = false)
    {
        var results = new List<RenameResult>();

        foreach (var item in items.Where(i => i.IsSelected && !i.HasConflict && i.OriginalName != i.NewName))
        {
            var sourceDirectory = System.IO.Path.GetDirectoryName(item.FullPath)!;

            // Use provided base folder path, or fall back to source directory's parent
            var baseDirectory = !string.IsNullOrEmpty(baseFolderPath)
                ? System.IO.Path.GetFullPath(baseFolderPath)
                : sourceDirectory;

            // Handle paths with folder structure (for vacation mode)
            string newPath;
            if (item.NewName.Contains(Path.DirectorySeparatorChar) || item.NewName.Contains(Path.AltDirectorySeparatorChar))
            {
                // NewName contains folder structure - create folders relative to base directory
                newPath = System.IO.Path.Combine(baseDirectory, item.NewName);
                var newDirectory = System.IO.Path.GetDirectoryName(newPath)!;

                try
                {
                    if (!dryRun && !System.IO.Directory.Exists(newDirectory))
                    {
                        System.IO.Directory.CreateDirectory(newDirectory);
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new RenameResult(item.FullPath, newPath, false, $"Failed to create directory: {ex.Message}"));
                    continue;
                }
            }
            else
            {
                // Simple rename in same directory
                newPath = System.IO.Path.Combine(sourceDirectory, item.NewName);
            }

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
