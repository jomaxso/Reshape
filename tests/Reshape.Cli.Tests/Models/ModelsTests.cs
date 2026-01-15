using Shouldly;
using ReshapeFileInfo = FileInfo;

namespace Reshape.Cli.Tests.Models;

/// <summary>
/// Tests for data models and records.
/// </summary>
public class ModelsTests
{
    [Fact]
    public void FileInfo_ShouldInitializeWithValidData()
    {
        // Arrange
        var metadata = new Dictionary<string, string> { ["filename"] = "test" };
        var createdAt = DateTime.Now;
        var modifiedAt = DateTime.Now;

        // Act
        var fileInfo = new ReshapeFileInfo(
            Name: "test.jpg",
            FullPath: "/path/to/test.jpg",
            RelativePath: "subfolder",
            Extension: ".jpg",
            Size: 1024,
            CreatedAt: createdAt,
            ModifiedAt: modifiedAt,
            IsSelected: true,
            Metadata: metadata
        );

        // Assert
        fileInfo.Name.ShouldBe("test.jpg");
        fileInfo.FullPath.ShouldBe("/path/to/test.jpg");
        fileInfo.RelativePath.ShouldBe("subfolder");
        fileInfo.Extension.ShouldBe(".jpg");
        fileInfo.Size.ShouldBe(1024);
        fileInfo.IsSelected.ShouldBeTrue();
        fileInfo.Metadata.ShouldBe(metadata);
    }

    [Fact]
    public void GpsCoordinates_ShouldStoreLatitudeAndLongitude()
    {
        // Arrange // Act
        var gps = new GpsCoordinates(48.858844, 2.294351);

        // Assert
        gps.Latitude.ShouldBe(48.858844);
        gps.Longitude.ShouldBe(2.294351);
    }

    [Fact]
    public void RenamePreviewItem_ShouldIncludeOriginalAndNewName()
    {
        // Arrange // Act
        var item = new RenamePreviewItem(
            OriginalName: "IMG_0001.jpg",
            NewName: "2024-01-15_001.jpg",
            FullPath: "/path/to/IMG_0001.jpg",
            RelativePath: "",
            HasConflict: false,
            IsSelected: true
        );

        // Assert
        item.OriginalName.ShouldBe("IMG_0001.jpg");
        item.NewName.ShouldBe("2024-01-15_001.jpg");
        item.HasConflict.ShouldBeFalse();
        item.IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void RenamePreviewItem_ShouldHandleConflicts()
    {
        // Arrange // Act
        var item = new RenamePreviewItem(
            OriginalName: "IMG_0001.jpg",
            NewName: "existing.jpg",
            FullPath: "/path/to/IMG_0001.jpg",
            RelativePath: "",
            HasConflict: true,
            IsSelected: true
        );

        // Assert
        item.HasConflict.ShouldBeTrue();
    }

    [Fact]
    public void RenamePattern_ShouldHavePatternAndDescription()
    {
        // Arrange // Act
        var pattern = new RenamePattern(
            Pattern: "{year}-{month}-{day}_{filename}",
            Description: "Date prefix format"
        );

        // Assert
        pattern.Pattern.ShouldBe("{year}-{month}-{day}_{filename}");
        pattern.Description.ShouldBe("Date prefix format");
    }

    [Fact]
    public void VacationModeOptions_ShouldConfigureVacationMode()
    {
        // Arrange
        var startDate = new DateTime(2024, 7, 1);

        // Act
        var options = new VacationModeOptions(
            Enabled: true,
            StartDate: startDate,
            DayFolderPattern: "Day {day_number}",
            SubfolderPattern: null
        );

        // Assert
        options.Enabled.ShouldBeTrue();
        options.StartDate.ShouldBe(startDate);
        options.DayFolderPattern.ShouldBe("Day {day_number}");
        options.SubfolderPattern.ShouldBeNull();
    }

    [Fact]
    public void RenameResult_ShouldIndicateSuccessOrFailure()
    {
        // Arrange // Act
        var successResult = new RenameResult(
            OriginalPath: "/path/to/old.jpg",
            NewPath: "/path/to/new.jpg",
            Success: true,
            Error: null
        );

        var failureResult = new RenameResult(
            OriginalPath: "/path/to/old.jpg",
            NewPath: "/path/to/new.jpg",
            Success: false,
            Error: "File already exists"
        );

        // Assert
        successResult.Success.ShouldBeTrue();
        successResult.Error.ShouldBeNull();
        
        failureResult.Success.ShouldBeFalse();
        failureResult.Error.ShouldBe("File already exists");
    }

    [Fact]
    public void ScanRequest_ShouldAcceptFolderPathAndExtensions()
    {
        // Arrange // Act
        var request = new ScanRequest(
            FolderPath: "/path/to/folder",
            Extensions: [".jpg", ".png"]
        );

        // Assert
        request.FolderPath.ShouldBe("/path/to/folder");
        request.Extensions.ShouldNotBeNull();
        request.Extensions.Length.ShouldBe(2);
    }

    [Fact]
    public void RenamePreviewRequest_ShouldIncludePatternAndOptions()
    {
        // Arrange
        var vacationMode = new VacationModeOptions(
            Enabled: true,
            StartDate: DateTime.Now,
            DayFolderPattern: "Day {day_number}",
            SubfolderPattern: null
        );

        // Act
        var request = new RenamePreviewRequest(
            FolderPath: "/path/to/folder",
            Pattern: "{year}-{month}-{day}_{filename}",
            Extensions: [".jpg"],
            VacationMode: vacationMode
        );

        // Assert
        request.FolderPath.ShouldBe("/path/to/folder");
        request.Pattern.ShouldBe("{year}-{month}-{day}_{filename}");
        request.VacationMode.ShouldNotBeNull();
        request.VacationMode!.Enabled.ShouldBeTrue();
    }
}
