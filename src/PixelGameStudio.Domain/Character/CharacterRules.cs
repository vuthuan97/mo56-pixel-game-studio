using PixelGameStudio.Domain.Assets;

namespace PixelGameStudio.Domain.Character;

/// <summary>Character/rig-level validation across the project (ids exist, appearance matches rig, equipment sane).</summary>
public static class CharacterRules
{
    public static IReadOnlyList<string> Validate(Project project)
    {
        var issues = new List<string>();
        foreach (RigDefinition rig in project.Rigs)
        {
            issues.AddRange(rig.Validate());
        }

        foreach (CharacterEntity character in project.Characters)
        {
            ValidateCharacter(project, character, issues);
        }

        return issues;
    }

    private static void ValidateCharacter(Project project, CharacterEntity character, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(character.Id))
        {
            issues.Add("Character thiếu Id.");
        }

        if (string.IsNullOrWhiteSpace(character.Name))
        {
            issues.Add($"Character '{character.Id}' thiếu tên.");
        }

        RigDefinition? rig = project.Rigs.FirstOrDefault(r => r.Id.Equals(character.RigId, StringComparison.Ordinal));
        if (rig is null)
        {
            issues.Add($"Character '{character.Id}' trỏ tới rig không tồn tại '{character.RigId}'.");
            return;
        }

        var appearanceParts = new HashSet<string>(StringComparer.Ordinal);
        foreach (PartAppearance appearance in character.Appearance)
        {
            if (rig.FindPart(appearance.PartId) is null)
            {
                issues.Add($"Character '{character.Id}': appearance gắn part không tồn tại '{appearance.PartId}'.");
                continue;
            }

            if (!appearanceParts.Add(appearance.PartId))
            {
                issues.Add($"Character '{character.Id}': appearance bị trùng cho part '{appearance.PartId}'.");
            }

            ValidateAssetRef(project, character, appearance.PartId, appearance.AssetRef, issues);
            foreach (KeyValuePair<string, AssetReference> kv in appearance.ViewAssets)
            {
                if (!project.View.Directions.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                {
                    issues.Add($"Character '{character.Id}': view '{kv.Key}' không có trong ViewProfile.");
                }

                ValidateAssetRef(project, character, appearance.PartId, kv.Value, issues);
            }

            foreach (string view in appearance.ViewMirrors)
            {
                if (!project.View.Directions.Contains(view, StringComparer.OrdinalIgnoreCase))
                {
                    issues.Add($"Character '{character.Id}': ViewMirrors chứa view lạ '{view}'.");
                }
            }
        }

        var slots = new HashSet<string>(StringComparer.Ordinal);
        foreach (EquippedItem item in character.Equipment)
        {
            EquipmentSlotDef? slot = rig.FindSlot(item.SlotId);
            if (slot is null)
            {
                issues.Add($"Character '{character.Id}': slot không tồn tại '{item.SlotId}'.");
                continue;
            }

            if (!slots.Add(item.SlotId))
            {
                issues.Add($"Character '{character.Id}': slot '{item.SlotId}' bị trang bị nhiều hơn 1 lần.");
            }

            AssetDefinition? asset = project.Assets.FirstOrDefault(a => a.Id.Equals(item.AssetId, StringComparison.Ordinal));
            if (asset is null)
            {
                issues.Add($"Character '{character.Id}': slot '{item.SlotId}' trỏ tới asset không tồn tại '{item.AssetId}'.");
            }

            foreach (KeyValuePair<string, string> kv in item.ViewAssets)
            {
                if (!project.View.Directions.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                {
                    issues.Add($"Character '{character.Id}': slot '{item.SlotId}' khai báo view '{kv.Key}' không có trong ViewProfile.");
                }

                if (project.Assets.FirstOrDefault(a => a.Id.Equals(kv.Value, StringComparison.Ordinal)) is null)
                {
                    issues.Add($"Character '{character.Id}': slot '{item.SlotId}' view '{kv.Key}' trỏ tới asset không tồn tại '{kv.Value}'.");
                }
            }

            if (asset is null)
            {
                continue;
            }

            foreach (string tag in slot.AllowedTags)
            {
                if (!asset.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    issues.Add($"Asset '{asset.Id}' thiếu tag '{tag}' theo yêu cầu của slot '{slot.Id}'.");
                }
            }
        }
    }

    private static void ValidateAssetRef(Project project, CharacterEntity character, string partId, AssetReference? assetRef, List<string> issues)
    {
        if (assetRef is null)
        {
            return;
        }

        if (project.Assets.FirstOrDefault(a => a.Id.Equals(assetRef.AssetId, StringComparison.Ordinal)) is null)
        {
            issues.Add($"Character '{character.Id}': part '{partId}' trỏ tới asset không tồn tại '{assetRef.AssetId}'.");
        }
    }
}
