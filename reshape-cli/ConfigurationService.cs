using System.Text.Json;

// ============================================================================
// Configuration Service - Manages User Settings and Custom Patterns
// ============================================================================

internal static class ConfigurationService
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".reshape"
    );

    private static readonly string PatternsFilePath = Path.Combine(ConfigDirectory, "patterns.json");

    private static List<CustomPattern>? _cachedPatterns;

    /// <summary>
    /// Ensures the configuration directory exists
    /// </summary>
    public static void EnsureConfigDirectoryExists()
    {
        if (!Directory.Exists(ConfigDirectory))
        {
            Directory.CreateDirectory(ConfigDirectory);
        }
    }

    /// <summary>
    /// Loads custom patterns from the configuration file
    /// </summary>
    public static List<CustomPattern> LoadCustomPatterns()
    {
        if (_cachedPatterns != null)
            return _cachedPatterns;

        EnsureConfigDirectoryExists();

        if (!File.Exists(PatternsFilePath))
        {
            _cachedPatterns = new List<CustomPattern>();
            return _cachedPatterns;
        }

        try
        {
            var json = File.ReadAllText(PatternsFilePath);
            var patterns = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.ListCustomPattern);
            _cachedPatterns = patterns ?? new List<CustomPattern>();
            return _cachedPatterns;
        }
        catch
        {
            // If file is corrupted, return empty list
            _cachedPatterns = new List<CustomPattern>();
            return _cachedPatterns;
        }
    }

    /// <summary>
    /// Saves custom patterns to the configuration file
    /// </summary>
    public static void SaveCustomPatterns(List<CustomPattern> patterns)
    {
        EnsureConfigDirectoryExists();

        var json = JsonSerializer.Serialize(patterns, AppJsonSerializerContext.Default.ListCustomPattern);
        File.WriteAllText(PatternsFilePath, json);

        // Update cache
        _cachedPatterns = patterns;
    }

    /// <summary>
    /// Adds a new custom pattern
    /// </summary>
    public static void AddCustomPattern(string pattern, string description)
    {
        var patterns = LoadCustomPatterns();
        
        // Check if pattern already exists
        if (patterns.Any(p => p.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Pattern '{pattern}' already exists");
        }

        patterns.Add(new CustomPattern(pattern, description));
        SaveCustomPatterns(patterns);
    }

    /// <summary>
    /// Removes a custom pattern by pattern string
    /// </summary>
    public static bool RemoveCustomPattern(string pattern)
    {
        var patterns = LoadCustomPatterns();
        var removed = patterns.RemoveAll(p => p.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase));
        
        if (removed > 0)
        {
            SaveCustomPatterns(patterns);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all patterns (default + custom)
    /// </summary>
    public static RenamePattern[] GetAllPatterns()
    {
        var defaultPatterns = GetDefaultPatterns();
        var customPatterns = LoadCustomPatterns()
            .Select(cp => new RenamePattern(cp.Pattern, cp.Description))
            .ToArray();

        return defaultPatterns.Concat(customPatterns).ToArray();
    }

    /// <summary>
    /// Gets only default patterns
    /// </summary>
    public static RenamePattern[] GetDefaultPatterns() =>
    [
        new("{year}-{month}-{day}_{filename}", "Date prefix: 2024-01-15_photo"),
        new("{date_taken}_{time_taken}_{filename}", "EXIF date/time: 2024-01-15_14-30-00_photo"),
        new("{year}/{month}/{filename}", "Year/Month folders (use with caution)"),
        new("{camera_model}_{date_taken}_{counter:4}", "Camera + date + counter: iPhone_2024-01-15_0001"),
        new("{filename}_{counter:3}", "Original name + counter: photo_001"),
        new("IMG_{year}{month}{day}_{counter:4}", "Standard format: IMG_20240115_0001"),
    ];

    /// <summary>
    /// Clears the pattern cache (useful for testing)
    /// </summary>
    public static void ClearCache()
    {
        _cachedPatterns = null;
    }
}
