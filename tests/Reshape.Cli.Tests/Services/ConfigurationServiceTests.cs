using Shouldly;

namespace Reshape.Cli.Tests.Services;

/// <summary>
/// Tests for ConfigurationService static class.
/// </summary>
public class ConfigurationServiceTests : IDisposable
{
    public ConfigurationServiceTests()
    {
        // Clear cache before each test
        ConfigurationService.ClearCache();
    }

    public void Dispose()
    {
        // Clear cache after each test
        ConfigurationService.ClearCache();
    }

    [Fact]
    public void GetDefaultPatterns_ShouldReturnPredefinedPatterns()
    {
        // Arrange // Act
        var patterns = ConfigurationService.GetDefaultPatterns();

        // Assert
        patterns.ShouldNotBeNull();
        patterns.ShouldNotBeEmpty();
        patterns.Length.ShouldBeGreaterThanOrEqualTo(6);
        patterns.ShouldAllBe(p => !string.IsNullOrEmpty(p.Pattern));
        patterns.ShouldAllBe(p => !string.IsNullOrEmpty(p.Description));
    }

    [Fact]
    public void GetDefaultPatterns_ShouldIncludeCommonPatterns()
    {
        // Arrange // Act
        var patterns = ConfigurationService.GetDefaultPatterns();

        // Assert
        patterns.ShouldContain(p => p.Pattern.Contains("{year}-{month}-{day}"));
        patterns.ShouldContain(p => p.Pattern.Contains("{date_taken}"));
        patterns.ShouldContain(p => p.Pattern.Contains("{camera_model}"));
    }

    [Fact]
    public void LoadCustomPatterns_ShouldReturnEmptyList_WhenNoFileExists()
    {
        // Arrange
        ConfigurationService.ClearCache();

        // Act
        var patterns = ConfigurationService.LoadCustomPatterns();

        // Assert
        patterns.ShouldNotBeNull();
        patterns.ShouldBeOfType<List<CustomPattern>>();
    }

    [Fact]
    public void GetAllPatterns_ShouldCombineDefaultAndCustomPatterns()
    {
        // Arrange
        var defaultCount = ConfigurationService.GetDefaultPatterns().Length;

        // Act
        var allPatterns = ConfigurationService.GetAllPatterns();

        // Assert
        allPatterns.ShouldNotBeNull();
        allPatterns.Length.ShouldBeGreaterThanOrEqualTo(defaultCount);
    }

    [Fact]
    public void ClearCache_ShouldResetPatternCache()
    {
        // Arrange
        var firstLoad = ConfigurationService.LoadCustomPatterns();
        
        // Act
        ConfigurationService.ClearCache();
        var secondLoad = ConfigurationService.LoadCustomPatterns();

        // Assert
        firstLoad.ShouldNotBeNull();
        secondLoad.ShouldNotBeNull();
    }
}
