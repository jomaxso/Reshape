
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
    Dictionary<string, string> Metadata
);

internal record ScanRequest(string FolderPath, string[]? Extensions = null);
internal record ScanResponse(string FolderPath, FileInfo[] Files, int TotalCount);

internal record RenamePattern(string Pattern, string Description);
internal record RenamePreviewRequest(string FolderPath, string Pattern, string[]? Extensions = null);
internal record RenamePreviewItem(string OriginalName, string NewName, string FullPath, string RelativePath, bool HasConflict, bool IsSelected);
internal record RenamePreviewResponse(RenamePreviewItem[] Items, int ConflictCount);

internal record RenameExecuteRequest(RenamePreviewItem[] Items, bool DryRun = false);
internal record RenameResult(string OriginalPath, string NewPath, bool Success, string? Error);
internal record RenameExecuteResponse(RenameResult[] Results, int SuccessCount, int ErrorCount);
