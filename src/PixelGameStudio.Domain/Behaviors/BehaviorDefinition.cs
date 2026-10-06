namespace PixelGameStudio.Domain.Behaviors;

/// <summary>Well-known behavior groups (docs/PRODUCT_OVERVIEW.md). Groups are open strings — projects may add more.</summary>
public static class BehaviorGroups
{
    public const string Movement = "Di chuyển";
    public const string Combat = "Chiến đấu";
    public const string Survival = "Sinh tồn";
    public const string Management = "Quản lý/Tycoon";
    public const string Adventure = "Phiêu lưu/Trải nghiệm";
}

/// <summary>Behavior-level marker: effect/sound/gameplay event bound to an animation frame index.</summary>
public sealed record BehaviorMarker(string Type, string Name, int FrameIndex);

/// <summary>Behavior-level marker type keys.</summary>
public static class BehaviorMarkerTypes
{
    public const string Effect = "Effect";
    public const string Sound = "Sound";
    public const string Gameplay = "Gameplay";
}

/// <summary>
/// A behavior is what gameplay selects: an animation plus the bindings that
/// make it meaningful — held item slot/asset, interaction anchor, and
/// effect/sound/gameplay markers. Presets are data; custom behaviors share the
/// same shape (rule 04_animation_behavior).
/// </summary>
public sealed class BehaviorDefinition
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Group { get; set; } = BehaviorGroups.Movement;

    /// <summary>AnimationDefinition.Id in the same project; null = animation not bound yet.</summary>
    public string? AnimationId { get; set; }

    /// <summary>Equipment slot providing the held item (e.g. main_hand for an axe).</summary>
    public string? HeldItemSlotId { get; set; }

    /// <summary>Optional asset to force-equip in the held item slot while the behavior runs.</summary>
    public string? HeldItemAssetId { get; set; }

    /// <summary>Rig anchor where interactions happen (e.g. anchor.interact for chopping a tree).</summary>
    public string? InteractionAnchorId { get; set; }

    public List<BehaviorMarker> Markers { get; set; } = [];

    public string Notes { get; set; } = string.Empty;

    /// <summary>Deep copy — presets return clones so editor mutations never touch shared template data.</summary>
    public BehaviorDefinition Clone() => new()
    {
        Id = Id,
        DisplayName = DisplayName,
        Group = Group,
        AnimationId = AnimationId,
        HeldItemSlotId = HeldItemSlotId,
        HeldItemAssetId = HeldItemAssetId,
        InteractionAnchorId = InteractionAnchorId,
        Markers = Markers.Select(m => new BehaviorMarker(m.Type, m.Name, m.FrameIndex)).ToList(),
        Notes = Notes,
    };
}
