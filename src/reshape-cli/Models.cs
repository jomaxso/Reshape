
// ============================================================================
// Models
// ============================================================================

internal record FileInfo(
    string Name,
    string FullPath,
    string RelativePath,
    string Extension,
    long Size,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    bool IsSelected,
    Dictionary<string, string> Metadata,
    GpsCoordinates? GpsCoordinates = null,
    DateTime? DateTakenUtc = null
);

internal record GpsCoordinates(double Latitude, double Longitude);

internal record ScanRequest(string FolderPath, string[]? Extensions = null);
internal record ScanResponse(string FolderPath, FileInfo[] Files, int TotalCount);

internal record RenamePattern(string Pattern, string Description);

internal record CustomPattern(string Pattern, string Description);

internal record VacationModeOptions(
    bool Enabled,
    DateTime? StartDate,
    string DayFolderPattern,  // e.g., "Day {day_number}" or "Tag {day_number}"
    string? SubfolderPattern  // Optional: pattern for subfolders within day folders
);

internal record RenamePreviewRequest(
    string FolderPath,
    string Pattern,
    string[]? Extensions = null,
    VacationModeOptions? VacationMode = null
);

internal record RenamePreviewItem(
    string OriginalName,
    string NewName,
    string FullPath,
    string RelativePath,
    bool HasConflict,
    bool IsSelected,
    int? DayNumber = null  // For vacation mode: which day of vacation (1, 2, 3...)
);

internal record RenamePreviewResponse(RenamePreviewItem[] Items, int ConflictCount);

internal record RenameExecuteRequest(RenamePreviewItem[] Items, string? BaseFolderPath = null, bool DryRun = false);
internal record RenameResult(string OriginalPath, string NewPath, bool Success, string? Error);
internal record RenameExecuteResponse(RenameResult[] Results, int SuccessCount, int ErrorCount);

internal record AddPatternRequest(string Pattern, string Description);
internal record RemovePatternRequest(string Pattern);
internal record PatternResponse(bool Success, string? Message = null);
