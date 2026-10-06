using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Profiles;
using PixelGameStudio.Domain.Templates;
using Xunit;

namespace PixelGameStudio.Domain.Tests;

public class ProfileRulesTests
{
    [Fact]
    public void ClassicTemplate_ValidatesClean()
    {
        Project project = ProjectTemplates.ClassicTopDown46();

        Assert.Empty(project.Validate());
    }

    [Fact]
    public void AllTemplates_ValidatesClean()
    {
        foreach ((string key, Func<Project> factory) in ProjectTemplates.All)
        {
            Assert.Empty(factory().Validate());
        }
    }

    [Fact]
    public void EmptyName_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Name = "  ";

        Assert.Contains(project.Validate(), issue => issue.Contains("Tên project"));
    }

    [Fact]
    public void OutOfRangeCanvas_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Pixels.CanvasWidth = 0;
        project.Pixels.CanvasHeight = 4096;

        var issues = project.Validate();
        Assert.Contains(issues, issue => issue.Contains("CanvasWidth"));
        Assert.Contains(issues, issue => issue.Contains("CanvasHeight"));
    }

    [Fact]
    public void DuplicateDirections_AreRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.View.Directions = ["Down", "Down", "Left"];

        Assert.Contains(project.Validate(), issue => issue.Contains("Direction bị trùng: Down"));
    }

    [Fact]
    public void DefaultDirectionOutsideList_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.View.DefaultDirection = "DownRight";

        Assert.Contains(project.Validate(), issue => issue.Contains("DefaultDirection"));
    }

    [Fact]
    public void PaletteOverBudget_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Palette.MaxColors = 2;
        project.Palette.Colors = Enumerable.Range(0, 3)
            .Select(i => new PaletteEntry { Name = $"M{i}", Hex = $"#10{i:X2}00FF" })
            .ToList();

        Assert.Contains(project.Validate(), issue => issue.Contains("vượt giới hạn"));
    }

    [Fact]
    public void InvalidHex_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Palette.Colors = [new PaletteEntry { Name = "Hỏng", Hex = "#12G45Z" }];

        Assert.Contains(project.Validate(), issue => issue.Contains("hex không hợp lệ"));
    }

    [Fact]
    public void DuplicateHex_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Palette.Colors =
        [
            new PaletteEntry { Name = "A", Hex = "#112233FF" },
            new PaletteEntry { Name = "B", Hex = "#112233FF" },
        ];

        Assert.Contains(project.Validate(), issue => issue.Contains("trùng màu"));
    }

    [Fact]
    public void DisablingNearestNeighbor_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Style.NearestNeighborOnly = false;

        Assert.Contains(project.Validate(), issue => issue.Contains("NearestNeighborOnly"));
    }

    [Fact]
    public void CharacterProportionsOverCanvas_IsRejected()
    {
        Project project = ProjectTemplates.ClassicTopDown46();
        project.Style.Character = new CharacterProportions { HeadHeightPx = 30, TorsoHeightPx = 30, LegHeightPx = 30 };

        Assert.Contains(project.Validate(), issue => issue.Contains("vượt chiều cao canvas"));
    }
}
