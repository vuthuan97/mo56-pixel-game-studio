namespace PixelGameStudio.Domain.Assets;

/// <summary>Well-known asset type keys. The value is an open string — projects may introduce their own types.</summary>
public static class AssetTypes
{
    public const string Sprite = "Sprite";
    public const string Equipment = "Equipment";
    public const string Placeholder = "Placeholder";
    public const string Custom = "Custom";
}

/// <summary>Anchor/pivot point in the asset's native canvas coordinates (metadata only — rule 03_character_rig).</summary>
public sealed record AssetAnchor(int X, int Y);

/// <summary>
/// Pointer to a library asset, used by parts/equipment/behaviors (Phase 3+)
/// so nothing hard-codes asset identities. <see cref="View"/> optionally binds
/// the reference to one project direction.
/// </summary>
public sealed record AssetReference
{
    public string AssetId { get; init; } = string.Empty;

    public string? View { get; init; }
}

/// <summary>
/// One library asset. The PNG binary lives in the project's assets/ folder —
/// this definition is the JSON-side metadata (id, type, tags, view coverage,
/// anchor, z-order) and is the only thing project.pgsproj stores.
/// </summary>
public sealed class AssetDefinition
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Type { get; set; } = AssetTypes.Custom;

    /// <summary>Relative path inside the project assets/ folder, '/'-separated, e.g. "sword.png".</summary>
    public string File { get; set; } = string.Empty;

    /// <summary>Native canvas of the stored PNG (after any import-time normalization).</summary>
    public int CanvasWidth { get; set; }

    public int CanvasHeight { get; set; }

    public List<string> Tags { get; set; } = [];

    /// <summary>Project direction keys this asset covers; empty = view-independent.</summary>
    public List<string> Views { get; set; } = [];

    public AssetAnchor? Anchor { get; set; }

    /// <summary>Explicit layer ordering metadata (replaces the legacy "overlay always on top" behavior).</summary>
    public int ZIndex { get; set; }

    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;

    public string Notes { get; set; } = string.Empty;
}
