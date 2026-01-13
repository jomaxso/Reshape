using System.Text.Json.Serialization;
using Reshape.Cli.Commands;

// ============================================================================
// JSON Serializer Context for Source Generation
// ============================================================================

[JsonSerializable(typeof(FileInfo))]
[JsonSerializable(typeof(GpsCoordinates))]
[JsonSerializable(typeof(ScanRequest))]
[JsonSerializable(typeof(ScanResponse))]
[JsonSerializable(typeof(RenamePattern))]
[JsonSerializable(typeof(RenamePattern[]))]
[JsonSerializable(typeof(CustomPattern))]
[JsonSerializable(typeof(List<CustomPattern>))]
[JsonSerializable(typeof(AddPatternRequest))]
[JsonSerializable(typeof(RemovePatternRequest))]
[JsonSerializable(typeof(PatternResponse))]
[JsonSerializable(typeof(VacationModeOptions))]
[JsonSerializable(typeof(RenamePreviewRequest))]
[JsonSerializable(typeof(RenamePreviewItem))]
[JsonSerializable(typeof(RenamePreviewItem[]))]
[JsonSerializable(typeof(RenamePreviewResponse))]
[JsonSerializable(typeof(RenameExecuteRequest))]
[JsonSerializable(typeof(RenameResult))]
[JsonSerializable(typeof(RenameExecuteResponse))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubRelease[]))]
[JsonSerializable(typeof(GitHubAsset))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
