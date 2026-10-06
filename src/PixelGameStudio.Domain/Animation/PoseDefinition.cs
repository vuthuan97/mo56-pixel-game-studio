namespace PixelGameStudio.Domain.Animation;

/// <summary>
/// Per-part pose state: integer offsets, visibility and state values
/// (rule 04_animation_behavior: Pose = trạng thái rig).
/// </summary>
public sealed record PartPose(int OffsetX = 0, int OffsetY = 0, bool Hidden = false)
{
    /// <summary>State values for the part's rig states during this pose (e.g. hand: "up").</summary>
    public Dictionary<string, string> States { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Per-equipment-slot pose state: offsets, visibility, state value and optional z-order override (weapon front/back).</summary>
public sealed record SlotPose(int OffsetX = 0, int OffsetY = 0, bool Hidden = false, int? ZIndexOverride = null)
{
    public Dictionary<string, string> States { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// A pose = the rig state at one moment: per-part integer offsets, hidden
/// flags and state values, plus per-equipment-slot states. Poses are pure
/// data; the composer applies them.
/// </summary>
public sealed class PoseDefinition
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>part id → pose state. Parts not listed keep their default.</summary>
    public Dictionary<string, PartPose> Parts { get; set; } = new(StringComparer.Ordinal);

    /// <summary>slot id → pose state for equipped items (offsets, weapon raise/slash, z override).</summary>
    public Dictionary<string, SlotPose> Slots { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Generic state values for this pose (e.g. hair: "left") available to behaviors/markers.</summary>
    public Dictionary<string, string> StateValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static PoseDefinition Identity(string id) => new() { Id = id, DisplayName = id };

    /// <summary>Deep copy — presets return clones so editor mutations never touch shared template data.</summary>
    public PoseDefinition Clone()
    {
        var copy = new PoseDefinition { Id = Id, DisplayName = DisplayName };
        foreach (KeyValuePair<string, PartPose> kv in Parts)
        {
            copy.Parts[kv.Key] = kv.Value with { States = new Dictionary<string, string>(kv.Value.States, StringComparer.OrdinalIgnoreCase) };
        }

        foreach (KeyValuePair<string, SlotPose> kv in Slots)
        {
            copy.Slots[kv.Key] = kv.Value with { States = new Dictionary<string, string>(kv.Value.States, StringComparer.OrdinalIgnoreCase) };
        }

        foreach (KeyValuePair<string, string> kv in StateValues)
        {
            copy.StateValues[kv.Key] = kv.Value;
        }

        return copy;
    }
}
