using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Domain.Templates;
using Xunit;

namespace PixelGameStudio.Domain.Tests;

public class AssetRulesTests
{
    [Fact]
    public void ValidAsset_Passes()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        var asset = new AssetDefinition
        {
            Id = "sword_iron",
            DisplayName = "Kiếm sắt",
            Type = AssetTypes.Equipment,
            File = "sword_iron.png",
            CanvasWidth = 32,
            CanvasHeight = 46,
            Tags = ["weapon", "metal"],
            Views = ["Down", "Left"],
            Anchor = new AssetAnchor(4, 29),
            ZIndex = 3,
        };

        Assert.Empty(AssetRules.Validate(project, asset));
        Assert.Empty(project.Validate()); // no duplicates, everything clean
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("_bad")]
    [InlineData("-bad")]
    [InlineData("has space")]
    [InlineData("ky_tu_dac_biet@#")]
    [InlineData(null, true)]
    public void InvalidId_IsRejected(string? id, bool tooLong = false)
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        var asset = new AssetDefinition
        {
            Id = tooLong ? new string('a', 65) : id!,
            File = "a.png",
            CanvasWidth = 32,
            CanvasHeight = 46,
        };

        Assert.Contains(AssetRules.Validate(project, asset), issue => issue.Contains("id không hợp lệ"));
    }

    [Fact]
    public void PathTraversal_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        var asset = new AssetDefinition { Id = "a", File = "../escape.png", CanvasWidth = 32, CanvasHeight = 46 };

        Assert.Contains(AssetRules.Validate(project, asset), issue => issue.Contains("không an toàn"));
    }

    [Fact]
    public void AbsoluteOrBackslashPath_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        var asset = new AssetDefinition { Id = "a", File = @"C:\abs\x.png", CanvasWidth = 32, CanvasHeight = 46 };

        Assert.Contains(AssetRules.Validate(project, asset), issue => issue.Contains("không an toàn"));
    }

    [Fact]
    public void NonPngFile_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        var asset = new AssetDefinition { Id = "a", File = "sprite.jpg", CanvasWidth = 32, CanvasHeight = 46 };

        Assert.Contains(AssetRules.Validate(project, asset), issue => issue.Contains(".png"));
    }

    [Fact]
    public void AnchorOutsideCanvas_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        var asset = new AssetDefinition
        {
            Id = "a",
            File = "a.png",
            CanvasWidth = 32,
            CanvasHeight = 46,
            Anchor = new AssetAnchor(32, 10),
        };

        Assert.Contains(AssetRules.Validate(project, asset), issue => issue.Contains("anchor"));
    }

    [Fact]
    public void AssetLargerThanProjectCanvas_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        var asset = new AssetDefinition { Id = "a", File = "a.png", CanvasWidth = 64, CanvasHeight = 64 };

        Assert.Contains(AssetRules.Validate(project, asset), issue => issue.Contains("lớn hơn canvas"));
    }

    [Fact]
    public void UnknownView_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        var asset = new AssetDefinition
        {
            Id = "a",
            File = "a.png",
            CanvasWidth = 32,
            CanvasHeight = 46,
            Views = ["DownRight"], // project is TopDown4
        };

        Assert.Contains(AssetRules.Validate(project, asset), issue => issue.Contains("không có trong ViewProfile"));
    }

    [Fact]
    public void DuplicateTags_AreRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        var asset = new AssetDefinition
        {
            Id = "a",
            File = "a.png",
            CanvasWidth = 32,
            CanvasHeight = 46,
            Tags = ["sword", "SWORD"],
        };

        Assert.Contains(AssetRules.Validate(project, asset), issue => issue.Contains("trùng tag"));
    }

    [Fact]
    public void DuplicateAssetIds_AreRejectedByProjectValidation()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Assets.Add(new AssetDefinition { Id = "same", File = "a.png", CanvasWidth = 32, CanvasHeight = 46 });
        project.Assets.Add(new AssetDefinition { Id = "SAME", File = "b.png", CanvasWidth = 32, CanvasHeight = 46 });

        Assert.Contains(project.Validate(), issue => issue.Contains("Asset id bị trùng"));
    }
}
