using PixelGameStudio.Domain.Assets;

namespace PixelGameStudio.Domain.Character;

/// <summary>Integer-only local transform of a part relative to its parent. No fractional transforms, no arbitrary rotation (TECH_STACK.md).</summary>
public sealed class PartTransform
{
    public int OffsetX { get; set; }

    public int OffsetY { get; set; }

    /// <summary>Horizontal mirror (discrete flip — nearest-safe, used for side views).</summary>
    public bool MirrorX { get; set; }
}

/// <summary>
/// One node of the rig tree (docs/ARCHITECTURE.md). A part may be a pure
/// grouping node (no assetRef) or carry an asset reference; per-view assets and
/// appearance live in the character's PartAppearance, not here — the rig is the
/// shared skeleton, the character dresses it.
/// </summary>
public sealed class PartNode
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Parent part id; null for the root.</summary>
    public string? ParentId { get; set; }

    /// <summary>Anchor (on the parent) this part hangs from; optional.</summary>
    public string? AnchorId { get; set; }

    /// <summary>Pivot in the part's own canvas coordinates (metadata for editors/animation).</summary>
    public AssetAnchor? Pivot { get; set; }

    /// <summary>Explicit layer order (back → front, smaller = further back).</summary>
    public int ZIndex { get; set; }

    /// <summary>Default asset for this part; per-view assets live in PartAppearance.</summary>
    public AssetReference? AssetRef { get; set; }

    public PartTransform Transform { get; set; } = new();

    /// <summary>Named state keys this part supports (e.g. hand: down/up/forward/back). Values are free-form.</summary>
    public Dictionary<string, string> States { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> Tags { get; set; } = [];
}
