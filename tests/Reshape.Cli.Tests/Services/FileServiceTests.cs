using Shouldly;
using ReshapeFileInfo = FileInfo;

namespace Reshape.Cli.Tests.Services;

/// <summary>
/// Tests for FileService static class.
/// </summary>
public class FileServiceTests
{
    private readonly string _testDataPath;

    public FileServiceTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), "ReshapeTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataPath);
    }

    [Fact]
    public void ScanFolder_ShouldThrowException_WhenFolderDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => FileService.ScanFolder(nonExistentPath));
    }

    [Fact]
    public void ScanFolder_ShouldReturnEmptyArray_WhenFolderIsEmpty()
    {
        // Arrange // Act
        var result = FileService.ScanFolder(_testDataPath);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ScanFolder_ShouldReturnAllFiles_WhenNoExtensionFilter()
    {
        // Arrange
        CreateTestFile("test1.txt");
        CreateTestFile("test2.jpg");
        CreateTestFile("test3.png");

        // Act
        var result = FileService.ScanFolder(_testDataPath);

        // Assert
        result.Length.ShouldBe(3);
    }

    [Fact]
    public void ScanFolder_ShouldFilterByExtension()
    {
        // Arrange
        CreateTestFile("test1.txt");
        CreateTestFile("test2.jpg");
        CreateTestFile("test3.png");

        // Act
        var result = FileService.ScanFolder(_testDataPath, [".jpg", ".png"]);

        // Assert
        result.Length.ShouldBe(2);
        result.ShouldAllBe(f => f.Extension == ".jpg" || f.Extension == ".png");
    }

    [Fact]
    public void ScanFolder_ShouldSetExtensionToLowerCase()
    {
        // Arrange
        CreateTestFile("TEST.JPG");

        // Act
        var result = FileService.ScanFolder(_testDataPath);

        // Assert
        result[0].Extension.ShouldBe(".jpg");
    }

    [Fact]
    public void ScanFolder_ShouldIncludeMetadata()
    {
        // Arrange
        CreateTestFile("photo.jpg");

        // Act
        var result = FileService.ScanFolder(_testDataPath);

        // Assert
        result[0].Metadata.ShouldNotBeNull();
        result[0].Metadata.ShouldContainKey("filename");
        result[0].Metadata["filename"].ShouldBe("photo");
    }

    [Fact]
    public void ApplyPattern_ShouldReplaceMetadataPlaceholders()
    {
        // Arrange
        var pattern = "{year}-{month}-{day}_{filename}";
        var metadata = new Dictionary<string, string>
        {
            ["year"] = "2024",
            ["month"] = "01",
            ["day"] = "15",
            ["filename"] = "IMG_0001"
        };

        // Act
        var result = FileService.ApplyPattern(pattern, metadata);

        // Assert
        result.ShouldBe("2024-01-15_IMG_0001");
    }

    [Fact]
    public void ApplyPattern_ShouldBeCaseInsensitive()
    {
        // Arrange
        var pattern = "{YEAR}-{Month}-{DAY}";
        var metadata = new Dictionary<string, string>
        {
            ["year"] = "2024",
            ["month"] = "01",
            ["day"] = "15"
        };

        // Act
        var result = FileService.ApplyPattern(pattern, metadata);

        // Assert
        result.ShouldBe("2024-01-15");
    }

    [Fact]
    public void ApplyPattern_ShouldHandleCounterPlaceholder()
    {
        // Arrange
        var pattern = "IMG_{counter:4}";
        var metadata = new Dictionary<string, string>();

        // Act
        var result = FileService.ApplyPattern(pattern, metadata);

        // Assert
        result.ShouldContain("{counter:4}");
    }

    [Fact]
    public void SanitizeForFilename_ShouldRemoveInvalidCharacters()
    {
        // Arrange
        var invalidName = "test/filename";

        // Act
        var result = FileService.SanitizeForFilename(invalidName);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe("testfilename");
    }

    [Fact]
    public void SanitizeForFilename_ShouldTrimWhitespace()
    {
        // Arrange
        var name = "  testfile  ";

        // Act
        var result = FileService.SanitizeForFilename(name);

        // Assert
        result.ShouldBe("testfile");
    }

    [Fact]
    public void SanitizeForPath_ShouldAllowDirectorySeparators()
    {
        // Arrange
        var path = "2024/01/photos";

        // Act
        var result = FileService.SanitizeForPath(path);

        // Assert
        result.ShouldContain(Path.DirectorySeparatorChar.ToString());
    }

    [Fact]
    public void GetPatternTemplates_ShouldReturnTemplates()
    {
        // Arrange // Act
        var templates = FileService.GetPatternTemplates();

        // Assert
        templates.ShouldNotBeNull();
        templates.ShouldNotBeEmpty();
        templates.ShouldAllBe(t => !string.IsNullOrEmpty(t.Pattern));
        templates.ShouldAllBe(t => !string.IsNullOrEmpty(t.Description));
    }

    [Fact]
    public void ExtractMetadata_ShouldIncludeBasicFileInformation()
    {
        // Arrange
        var filePath = CreateTestFile("test.txt");

        // Act
        var metadata = FileService.ExtractMetadata(filePath);

        // Assert
        metadata.ShouldContainKey("filename");
        metadata.ShouldContainKey("ext");
        metadata.ShouldContainKey("size");
        metadata.ShouldContainKey("year");
        metadata.ShouldContainKey("month");
        metadata.ShouldContainKey("day");
    }

    private string CreateTestFile(string fileName, string content = "test content")
    {
        var filePath = Path.Combine(_testDataPath, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }
}
