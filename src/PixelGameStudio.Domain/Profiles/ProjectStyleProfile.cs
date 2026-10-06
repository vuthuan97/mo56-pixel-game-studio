namespace PixelGameStudio.Domain.Profiles;

/// <summary>Humanoid proportion preset in native pixels. Optional; 0 means "not specified".</summary>
public sealed class CharacterProportions
{
    public int HeadHeightPx { get; set; }

    public int TorsoHeightPx { get; set; }

    public int LegHeightPx { get; set; }
}

/// <summary>
/// Shared style rules of the project (docs/STYLE_PROFILE_SPEC.md). Every asset
/// produced in the project must conform; Art QA thresholds also come from here.
/// Canvas size, view and palette live in their own profiles.
/// </summary>
public sealed class ProjectStyleProfile
{
    /// <summary>
    /// Procedural starter-art source. Legacy is kept for existing projects;
    /// ReferenceGrid uses the compact hand-authored pixel style in the studio.
    /// </summary>
    public string CharacterRenderer { get; set; } = "Legacy";

    /// <summary>Starter-art schema version used to refresh generated assets after renderer fixes.</summary>
    public int CharacterRendererVersion { get; set; }

    public string OutlineStyle { get; set; } = "External1px";

    public int OutlineThickness { get; set; } = 1;

    public string OutlineColorHex { get; set; } = "#16101EFF";

    /// <summary>Free-form light direction key, e.g. "TopLeft", "TopRight".</summary>
    public string LightDirection { get; set; } = "TopLeft";

    public int ShadowLevels { get; set; } = 1;

    public int HighlightLevels { get; set; } = 1;

    /// <summary>"Low", "Medium" or "High" — guidance for detail density.</summary>
    public string DetailDensity { get; set; } = "Medium";

    public bool TransparentBackground { get; set; } = true;

    /// <summary>Alpha above this threshold counts as visible for QA checks.</summary>
    public int AlphaThreshold { get; set; } = 1;

    /// <summary>
    /// Pixel-art stability rule. Must stay enabled: the renderer and every
    /// preview scale with nearest-neighbor only (docs/TECH_STACK.md).
    /// </summary>
    public bool NearestNeighborOnly { get; set; } = true;

    public CharacterProportions Character { get; set; } = new();

    /// <summary>Environment tile grid in native pixels (0 = not specified).</summary>
    public int TileSizePx { get; set; } = 32;

    /// <summary>Minimum light/dark value range a sprite must reach to stay readable at native 1x.</summary>
    public int MinValueRange { get; set; } = 90;
}
