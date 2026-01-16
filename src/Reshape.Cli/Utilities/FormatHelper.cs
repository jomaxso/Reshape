namespace Reshape.Cli.Utilities;

/// <summary>
/// Provides helper methods for formatting and displaying data.
/// </summary>
internal static class FormatHelper
{
    /// <summary>
    /// Formats a file size in bytes to a human-readable string (B, KB, MB, GB).
    /// </summary>
    /// <param name="bytes">The file size in bytes.</param>
    /// <returns>A formatted string representing the file size.</returns>
    public static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
