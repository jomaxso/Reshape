using System.Text.Json.Serialization;
// ============================================================================
// JSON Serializer Context for Source Generation
// ============================================================================

[JsonSerializable(typeof(FileInfo))]
[JsonSerializable(typeof(GpsCoordinates))]
[JsonSerializable(typeof(ScanRequest))]
[JsonSerializable(typeof(ScanResponse))]
[JsonSerializable(typeof(RenamePattern))]
[JsonSerializable(typeof(RenamePattern[]))]
[JsonSerializable(typeof(VacationModeOptions))]
[JsonSerializable(typeof(RenamePreviewRequest))]
[JsonSerializable(typeof(RenamePreviewItem))]
[JsonSerializable(typeof(RenamePreviewItem[]))]
[JsonSerializable(typeof(RenamePreviewResponse))]
[JsonSerializable(typeof(RenameExecuteRequest))]
[JsonSerializable(typeof(RenameResult))]
[JsonSerializable(typeof(RenameExecuteResponse))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
