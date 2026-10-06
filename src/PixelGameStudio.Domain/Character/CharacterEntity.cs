using PixelGameStudio.Domain.Assets;

namespace PixelGameStudio.Domain.Character;

/// <summary>Palette remap applied to a part's pixels when composing (skin/hair variants — Phase 4).</summary>
public sealed record PaletteMapping(string FromHex, string ToHex);

/// <summary>
/// How a character dresses one rig part: default asset, per-view assets,
/// mirror flags, palette overrides and state values. Everything is metadata —
/// the composer resolves it against the project's ViewProfile.
/// </summary>
public sealed class PartAppearance
{
    public string PartId { get; set; } = string.Empty;

    /// <summary>Asset used when no per-view asset matches the rendered view.</summary>
    public AssetReference? AssetRef { get; set; }

    /// <summary>View (direction key from ViewProfile) → asset override.</summary>
    public Dictionary<string, AssetReference> ViewAssets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Views in which the part's pixels are mirrored horizontally (e.g. Left mirrors Right art).</summary>
    public List<string> ViewMirrors { get; set; } = [];

    /// <summary>Palette remaps applied before compositing (skin tone, hair color variants).</summary>
    public List<PaletteMapping> PaletteOverride { get; set; } = [];

    /// <summary>State values of the part's rig states for this character's default look.</summary>
    public Dictionary<string, string> States { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>State variant assets: stateKey → value → variant (e.g. hand → up → arm-up art per view).</summary>
    public Dictionary<string, Dictionary<string, StateAssetVariant>> StateAssets { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>One state variant: fallback asset id plus optional per-view overrides.</summary>
public sealed record StateAssetVariant(string AssetId)
{
    public Dictionary<string, string> ViewAssets { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// An equipped item: one asset in one rig equipment slot. AssetId is the
/// default (view-independent or fallback); ViewAssets optionally overrides per
/// project direction (e.g. a sword drawn per view).
/// </summary>
public sealed record EquippedItem(string SlotId, string AssetId)
{
    public Dictionary<string, string> ViewAssets { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>State variant assets: stateValue → variant (e.g. weapon: raise → front-raise art per view).</summary>
    public Dictionary<string, StateAssetVariant> StateAssets { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// A sprite entity of the character kind: rig + appearance + equipment.
/// Pose/animation/behavior sets attach in Phase 6-8 via ids stored here.
/// </summary>
public sealed class CharacterEntity
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>RigDefinition.Id in the same project.</summary>
    public string RigId { get; set; } = string.Empty;

    public List<PartAppearance> Appearance { get; set; } = [];

    /// <summary>Equipped items keyed by slot id; a slot holds at most one item.</summary>
    public List<EquippedItem> Equipment { get; set; } = [];

    public string? RoleId { get; set; }

    public string Notes { get; set; } = string.Empty;

    public PartAppearance? AppearanceOf(string partId) =>
        Appearance.FirstOrDefault(a => a.PartId.Equals(partId, StringComparison.Ordinal));
}
