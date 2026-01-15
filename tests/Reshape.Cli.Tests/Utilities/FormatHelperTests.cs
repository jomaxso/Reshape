using Reshape.Cli.Utilities;
using Shouldly;

namespace Reshape.Cli.Tests.Utilities;

/// <summary>
/// Tests for FormatHelper utility class.
/// </summary>
public class FormatHelperTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(2560, "2.5 KB")]
    [InlineData(5242880, "5 MB")]
    public void FormatSize_ShouldFormatBytesCorrectly(long bytes, string expected)
    {
        // Arrange // Act // Assert
        var result = FormatHelper.FormatSize(bytes);
        result.ShouldBe(expected);
    }

    [Fact]
    public void FormatSize_ShouldHandleZeroBytes()
    {
        // Arrange
        long bytes = 0;

        // Act
        var result = FormatHelper.FormatSize(bytes);

        // Assert
        result.ShouldBe("0 B");
    }

    [Fact]
    public void FormatSize_ShouldHandleLargeValues()
    {
        // Arrange
        long bytes = 1099511627776; // 1 TB in bytes

        // Act
        var result = FormatHelper.FormatSize(bytes);

        // Assert
        result.ShouldBe("1024 GB"); // Should max out at GB
    }

    [Fact]
    public void FormatSize_ShouldRoundToTwoDecimalPlaces()
    {
        // Arrange
        long bytes = 1536000; // 1.465 MB

        // Act
        var result = FormatHelper.FormatSize(bytes);

        // Assert
        result.ShouldBe("1.46 MB");
    }
}
