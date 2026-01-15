using Shouldly;
using ReshapeFileInfo = FileInfo;

namespace Reshape.Cli.Tests.Services;

/// <summary>
/// Tests for FileService static class.
/// </summary>
public class FileServiceTests : IDisposable
{
    private readonly string _testDataPath;

    public FileServiceTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), "ReshapeTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataPath);
    }

    public void Dispose()
    {
        // Clean up test directory and files
        if (Directory.Exists(_testDataPath))
        {
            try
            {
                Directory.Delete(_testDataPath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
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

    [Fact]
    public void GeneratePreview_ShouldCreatePreviewItems()
    {
        // Arrange
        CreateTestFile("photo1.jpg");
        CreateTestFile("photo2.jpg");
        var pattern = "{filename}_renamed";

        // Act
        var preview = FileService.GeneratePreview(_testDataPath, pattern, [".jpg"]);

        // Assert
        preview.ShouldNotBeNull();
        preview.Length.ShouldBe(2);
        preview.ShouldAllBe(p => p.NewName.Contains("_renamed"));
    }

    [Fact]
    public void GeneratePreview_ShouldDetectConflicts()
    {
        // Arrange
        CreateTestFile("photo1.jpg");
        CreateTestFile("photo2.jpg");
        // Pattern that will create same name for all files
        var pattern = "samename";

        // Act
        var preview = FileService.GeneratePreview(_testDataPath, pattern, [".jpg"]);

        // Assert
        preview.ShouldNotBeNull();
        preview.ShouldContain(p => p.HasConflict);
    }

    [Fact]
    public void GeneratePreview_ShouldHandleCounterPlaceholder()
    {
        // Arrange
        CreateTestFile("photo1.jpg");
        CreateTestFile("photo2.jpg");
        CreateTestFile("photo3.jpg");
        var pattern = "IMG_{counter:4}";

        // Act
        var preview = FileService.GeneratePreview(_testDataPath, pattern, [".jpg"]);

        // Assert
        preview.ShouldNotBeNull();
        preview.Length.ShouldBe(3);
        preview[0].NewName.ShouldContain("0001");
        preview[1].NewName.ShouldContain("0002");
        preview[2].NewName.ShouldContain("0003");
    }

    [Fact]
    public void ExecuteRename_ShouldReturnResults_WhenDryRun()
    {
        // Arrange
        var filePath = CreateTestFile("original.txt");
        var items = new[]
        {
            new RenamePreviewItem(
                OriginalName: "original.txt",
                NewName: "renamed.txt",
                FullPath: filePath,
                RelativePath: "",
                HasConflict: false,
                IsSelected: true
            )
        };

        // Act
        var results = FileService.ExecuteRename(items, _testDataPath, dryRun: true);

        // Assert
        results.ShouldNotBeNull();
        results.Length.ShouldBe(1);
        results[0].Success.ShouldBeTrue();
        File.Exists(filePath).ShouldBeTrue(); // Original file should still exist in dry run
    }

    [Fact]
    public void ExecuteRename_ShouldRenameFiles_WhenNotDryRun()
    {
        // Arrange
        var originalPath = CreateTestFile("original.txt");
        var newPath = Path.Combine(_testDataPath, "renamed.txt");
        var items = new[]
        {
            new RenamePreviewItem(
                OriginalName: "original.txt",
                NewName: "renamed.txt",
                FullPath: originalPath,
                RelativePath: "",
                HasConflict: false,
                IsSelected: true
            )
        };

        // Act
        var results = FileService.ExecuteRename(items, _testDataPath, dryRun: false);

        // Assert
        results.ShouldNotBeNull();
        results.Length.ShouldBe(1);
        results[0].Success.ShouldBeTrue();
        File.Exists(originalPath).ShouldBeFalse(); // Original should be gone
        File.Exists(newPath).ShouldBeTrue(); // New file should exist
    }

    [Fact]
    public void SanitizeForPath_ShouldPreservePathStructure()
    {
        // Arrange
        var path = "2024/01/photos";

        // Act
        var result = FileService.SanitizeForPath(path);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain(Path.DirectorySeparatorChar);
    }

    [Fact]
    public void ApplyPattern_ShouldHandleMissingMetadata()
    {
        // Arrange
        var pattern = "{year}-{missing_key}_{filename}";
        var metadata = new Dictionary<string, string>
        {
            ["year"] = "2024",
            ["filename"] = "photo"
        };

        // Act
        var result = FileService.ApplyPattern(pattern, metadata);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("2024");
        result.ShouldContain("photo");
    }

    private string CreateTestFile(string fileName, string content = "test content")
    {
        var filePath = Path.Combine(_testDataPath, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }
}
