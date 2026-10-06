using PixelGameStudio.Core.Primitives;
using PixelGameStudio.Domain.Templates;
using Xunit;

namespace PixelGameStudio.Domain.Tests;

public class TemplateTests
{
    [Fact]
    public void ClassicTopDown46_CarriesCanvasAsData()
    {
        // The legacy 32x46 canvas is just template data, not a system constant.
        Project project = ProjectTemplates.ClassicTopDown46();

        Assert.Equal(32, project.Pixels.CanvasWidth);
        Assert.Equal(46, project.Pixels.CanvasHeight);
        Assert.Equal(4, project.View.Directions.Count);
        Assert.Equal(15 + 15 + 12, project.Style.Character.HeadHeightPx + project.Style.Character.TorsoHeightPx + project.Style.Character.LegHeightPx);
    }

    [Fact]
    public void SideScrollerTemplate_UsesTwoDirections()
    {
        Project project = ProjectTemplates.SideScroller48x32();

        Assert.Equal(48, project.Pixels.CanvasWidth);
        Assert.Equal(32, project.Pixels.CanvasHeight);
        Assert.Equal(["Left", "Right"], project.View.Directions);
    }

    [Fact]
    public void SandboxTemplate_UsesEightDirectionsAndSquareCanvas()
    {
        Project project = ProjectTemplates.Sandbox8Direction64();

        Assert.Equal(64, project.Pixels.CanvasWidth);
        Assert.Equal(64, project.Pixels.CanvasHeight);
        Assert.Equal(8, project.View.Directions.Count);
    }

    [Fact]
    public void HexColor_RoundTrips()
    {
        Assert.True(Rgba32.TryParseHex("#16101EFF", out Rgba32 outline));
        Assert.Equal(new Rgba32(0x16, 0x10, 0x1E, 0xFF), outline);
        Assert.Equal("#16101EFF", outline.ToHex());

        Assert.True(Rgba32.TryParseHex("F6C854", out Rgba32 noAlpha));
        Assert.Equal(new Rgba32(0xF6, 0xC8, 0x54, 0xFF), noAlpha);

        Assert.True(Rgba32.TryParseHex("#F64", out Rgba32 shorthand));
        Assert.Equal(new Rgba32(0xFF, 0x66, 0x44, 0xFF), shorthand);

        Assert.False(Rgba32.TryParseHex("#12345", out _));
        Assert.False(Rgba32.TryParseHex("zzzzzz", out _));
        Assert.False(Rgba32.TryParseHex("", out _));
    }
}
