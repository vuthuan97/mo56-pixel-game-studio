namespace PixelGameStudio.Domain.Profiles;

/// <summary>Well-known perspective presets. Projects may also use any custom perspective string.</summary>
public static class KnownPerspectives
{
    public const string TopDown4 = "TopDown4";
    public const string TopDown8 = "TopDown8";
    public const string Diagonal4 = "Diagonal4";
    public const string SideView2 = "SideView2";
    public const string Frontal = "Frontal";
    public const string Custom = "Custom";

    public static IReadOnlyList<string> All { get; } =
    [
        TopDown4, TopDown8, Diagonal4, SideView2, Frontal, Custom,
    ];
}

/// <summary>
/// Camera/perspective of the target game and the ordered list of view/direction
/// keys assets must exist for. Direction keys are stable identifiers (Down, Up,
/// DownRight, ...) — nothing in the core assumes a fixed set.
/// </summary>
public sealed class ViewProfile
{
    public string Perspective { get; set; } = KnownPerspectives.TopDown4;

    public List<string> Directions { get; set; } = ["Down", "Up", "Left", "Right"];

    /// <summary>Direction used when rendering a default preview.</summary>
    public string DefaultDirection { get; set; } = "Down";

    public static ViewProfile TopDown4() => new()
    {
        Perspective = KnownPerspectives.TopDown4,
        Directions = ["Down", "Up", "Left", "Right"],
    };

    public static ViewProfile TopDown8() => new()
    {
        Perspective = KnownPerspectives.TopDown8,
        Directions = ["Down", "DownRight", "Right", "UpRight", "Up", "UpLeft", "Left", "DownLeft"],
    };

    public static ViewProfile Diagonal4() => new()
    {
        Perspective = KnownPerspectives.Diagonal4,
        Directions = ["DownRight", "DownLeft", "UpRight", "UpLeft"],
        DefaultDirection = "DownRight",
    };

    public static ViewProfile SideView2() => new()
    {
        Perspective = KnownPerspectives.SideView2,
        Directions = ["Left", "Right"],
        DefaultDirection = "Right",
    };

    public static ViewProfile Frontal() => new()
    {
        Perspective = KnownPerspectives.Frontal,
        Directions = ["Front"],
        DefaultDirection = "Front",
    };

    public static ViewProfile Custom(IReadOnlyList<string> directions, string defaultDirection)
    {
        if (directions.Count == 0)
        {
            throw new ArgumentException("Custom view needs at least one direction.", nameof(directions));
        }

        if (!directions.Contains(defaultDirection))
        {
            throw new ArgumentException("Default direction must be one of the directions.", nameof(defaultDirection));
        }

        return new ViewProfile
        {
            Perspective = KnownPerspectives.Custom,
            Directions = [.. directions],
            DefaultDirection = defaultDirection,
        };
    }
}
