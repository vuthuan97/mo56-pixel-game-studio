using PixelGameStudio.Domain.Assets;

namespace PixelGameStudio.Domain.Character;

/// <summary>Equipment slot definition. Slots are rig data — projects define any slot set they need.</summary>
public sealed class EquipmentSlotDef
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Anchor (in the rig) the equipped asset is attached to.</summary>
    public string AnchorId { get; set; } = string.Empty;

    /// <summary>Layer order of the equipped asset (back → front).</summary>
    public int ZIndex { get; set; }

    /// <summary>Optional tag filter: an asset must carry all these tags to be equipped in this slot.</summary>
    public List<string> AllowedTags { get; set; } = [];
}

/// <summary>
/// Data-driven rig skeleton: a tree of parts plus named anchors and equipment
/// slots. Not locked to humanoid — humanoid is just a template
/// (docs/ARCHITECTURE.md, rule 03_character_rig).
/// </summary>
public sealed class RigDefinition
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public List<PartNode> Parts { get; set; } = [];

    public List<AnchorPoint> Anchors { get; set; } = [];

    public List<EquipmentSlotDef> EquipmentSlots { get; set; } = [];

    public string RootPartId => Parts.FirstOrDefault(p => p.ParentId is null)?.Id
        ?? throw new InvalidOperationException($"Rig '{Id}' has no root part.");

    public PartNode? FindPart(string partId) => Parts.FirstOrDefault(p => p.Id.Equals(partId, StringComparison.Ordinal));

    public AnchorPoint? FindAnchor(string anchorId) => Anchors.FirstOrDefault(a => a.Id.Equals(anchorId, StringComparison.Ordinal));

    public EquipmentSlotDef? FindSlot(string slotId) => EquipmentSlots.FirstOrDefault(s => s.Id.Equals(slotId, StringComparison.Ordinal));

    public IEnumerable<PartNode> ChildrenOf(string partId) => Parts.Where(p => p.ParentId?.Equals(partId, StringComparison.Ordinal) == true);

    /// <summary>Walks from a part to the root and returns the path (child first).</summary>
    public IReadOnlyList<PartNode> PathToRoot(string partId)
    {
        var path = new List<PartNode>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        PartNode? current = FindPart(partId);
        while (current is not null && seen.Add(current.Id))
        {
            path.Add(current);
            current = current.ParentId is null ? null : FindPart(current.ParentId);
        }

        return path;
    }

    /// <summary>Structural validation: unique ids, parents exist, single root, no cycles, slots/anchors sane.</summary>
    public IReadOnlyList<string> Validate()
    {
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(Id))
        {
            issues.Add("Rig thiếu Id.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (PartNode part in Parts)
        {
            if (!AssetRules.IsValidId(part.Id))
            {
                issues.Add($"Rig '{Id}': part id không hợp lệ '{part.Id}'.");
            }

            if (!ids.Add(part.Id))
            {
                issues.Add($"Rig '{Id}': part id bị trùng '{part.Id}'.");
            }

            if (part.ParentId is not null && !Parts.Any(p => p.Id == part.ParentId))
            {
                issues.Add($"Rig '{Id}': part '{part.Id}' trỏ tới parent không tồn tại '{part.ParentId}'.");
            }
        }

        int roots = Parts.Count(p => p.ParentId is null);
        if (roots != 1)
        {
            issues.Add($"Rig '{Id}' phải có đúng 1 root (hiện {roots}).");
        }

        // cycle check: every part must reach a root
        foreach (PartNode part in Parts)
        {
            var path = PathToRoot(part.Id);
            if (path.Count == 0 || path[^1].ParentId is not null)
            {
                issues.Add($"Rig '{Id}': part '{part.Id}' nằm trong chu kỳ hoặc không tới được root.");
            }
        }

        var anchorIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (AnchorPoint anchor in Anchors)
        {
            if (!anchorIds.Add(anchor.Id))
            {
                issues.Add($"Rig '{Id}': anchor id bị trùng '{anchor.Id}'.");
            }

            if (FindPart(anchor.PartId) is null)
            {
                issues.Add($"Rig '{Id}': anchor '{anchor.Id}' gắn vào part không tồn tại '{anchor.PartId}'.");
            }
        }

        var slotIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (EquipmentSlotDef slot in EquipmentSlots)
        {
            if (!slotIds.Add(slot.Id))
            {
                issues.Add($"Rig '{Id}': equipment slot id bị trùng '{slot.Id}'.");
            }

            if (FindAnchor(slot.AnchorId) is null)
            {
                issues.Add($"Rig '{Id}': slot '{slot.Id}' gắn anchor không tồn tại '{slot.AnchorId}'.");
            }
        }

        return issues;
    }
}
