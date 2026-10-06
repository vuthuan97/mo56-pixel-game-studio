using PixelGameStudio.Domain.Profiles;

namespace PixelGameStudio.Domain.Templates;

/// <summary>
/// Ready-made starting points. Templates are plain data factories — the system
/// never requires any specific template, canvas or direction set.
/// </summary>
public static class ProjectTemplates
{
    /// <summary>Classic top-down 4-direction RPG look inherited from the prototype era (32x46 canvas).</summary>
    public static Project ClassicTopDown46() => new()
    {
        Name = "New Top-Down Project",
        Game = new GameProfile { Genre = GameGenres.TopDownRpg },
        View = ViewProfile.TopDown4(),
        Pixels = new PixelProfile { CanvasWidth = 32, CanvasHeight = 46 },
        Palette = new PaletteProfile(),
        Style = new ProjectStyleProfile
        {
            OutlineStyle = "External1px",
            OutlineThickness = 1,
            OutlineColorHex = "#16101EFF",
            TileSizePx = 32,
            Character = new CharacterProportions { HeadHeightPx = 15, TorsoHeightPx = 15, LegHeightPx = 12 },
        },
    };

    /// <summary>Side-view platformer/runner look (2 directions, wider canvas).</summary>
    public static Project SideScroller48x32() => new()
    {
        Name = "New Side-Scroller Project",
        Game = new GameProfile { Genre = GameGenres.SideScroller },
        View = ViewProfile.SideView2(),
        Pixels = new PixelProfile { CanvasWidth = 48, CanvasHeight = 32 },
        Palette = new PaletteProfile { Name = "Side-Scroller", MaxColors = 24 },
        Style = new ProjectStyleProfile
        {
            OutlineStyle = "External1px",
            LightDirection = "TopRight",
            TileSizePx = 16,
            Character = new CharacterProportions { HeadHeightPx = 10, TorsoHeightPx = 12, LegHeightPx = 10 },
        },
    };

    /// <summary>Minimal custom sandbox with 8 directions and a square canvas.</summary>
    public static Project Sandbox8Direction64() => new()
    {
        Name = "New Sandbox Project",
        Game = new GameProfile { Genre = GameGenres.Custom },
        View = ViewProfile.TopDown8(),
        Pixels = new PixelProfile { CanvasWidth = 64, CanvasHeight = 64 },
        Palette = new PaletteProfile { MaxColors = 48 },
        Style = new ProjectStyleProfile
        {
            DetailDensity = "High",
            ShadowLevels = 2,
            HighlightLevels = 2,
            TileSizePx = 32,
        },
    };

    public static IReadOnlyList<(string Key, Func<Project> Factory)> All { get; } =
    [
        ("ClassicTopDown46", ClassicTopDown46),
        ("SideScroller48x32", SideScroller48x32),
        ("Sandbox8Direction64", Sandbox8Direction64),
    ];
}
