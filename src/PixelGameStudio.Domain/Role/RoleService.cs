using PixelGameStudio.Domain.Assets;
using PixelGameStudio.Domain.Character;

namespace PixelGameStudio.Domain.Role;

/// <summary>Structural validation of role data across the project.</summary>
public static class RoleRules
{
    public static IReadOnlyList<string> Validate(Project project)
    {
        var issues = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (RoleDefinition role in project.Roles)
        {
            if (!AssetRules.IsValidId(role.Id) || !ids.Add(role.Id))
            {
                issues.Add($"Role id không hợp lệ hoặc bị trùng: '{role.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(role.DisplayName))
            {
                issues.Add($"Role '{role.Id}' thiếu DisplayName.");
            }

            foreach (RoleEquipmentBinding binding in role.StartingEquipment)
            {
                if (!project.Rigs.Any(r => r.FindSlot(binding.SlotId) is not null))
                {
                    issues.Add($"Role '{role.Id}' gắn equipment slot không tồn tại '{binding.SlotId}'.");
                }

                // Fresh projects may only have base-body art — roles are stored
                // templates waiting for equipment content, so a missing equipment
                // library is not a broken reference. Once any equipment asset
                // exists, references are validated strictly.
                bool hasEquipmentLibrary = project.Assets.Any(a => a.Type == AssetTypes.Equipment);
                if (hasEquipmentLibrary &&
                    !project.Assets.Any(a => a.Id.StartsWith(binding.AssetFamily + ".", StringComparison.Ordinal)))
                {
                    issues.Add($"Role '{role.Id}' trỏ tới asset family không tồn tại '{binding.AssetFamily}'.");
                }
            }

            if (project.Behaviors.Count > 0)
            {
                foreach (string behaviorId in role.BehaviorIds)
                {
                    if (project.Behaviors.FirstOrDefault(b => b.Id == behaviorId) is null)
                    {
                        issues.Add($"Role '{role.Id}' trỏ tới behavior không tồn tại '{behaviorId}'.");
                    }
                }
            }

            if (project.Animations.Count > 0 &&
                role.DefaultAnimationId is not null &&
                project.Animations.FirstOrDefault(a => a.Id == role.DefaultAnimationId) is null)
            {
                issues.Add($"Role '{role.Id}' trỏ tới animation không tồn tại '{role.DefaultAnimationId}'.");
            }
        }

        return issues;
    }
}

/// <summary>Outcome of applying a role to a character.</summary>
public sealed record RoleApplyResult(IReadOnlyList<string> Warnings, int EquippedCount);

/// <summary>
/// Applies role templates to characters: resolves per-view equipment from
/// asset families, swaps the character's equipment, sets RoleId. Creating a
/// custom role is a clone-with-new-id operation.
/// </summary>
public sealed class RoleService
{
    /// <summary>Installs role templates that are not present yet (by id). Returns how many were added.</summary>
    public int InstallTemplates(Project project, IReadOnlyList<RoleDefinition> templates)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(templates);
        int added = 0;
        foreach (RoleDefinition template in templates)
        {
            if (project.Roles.All(r => r.Id != template.Id))
            {
                project.Roles.Add(template.Clone());
                added++;
            }
        }

        return added;
    }

    /// <summary>Duplicates an existing role into a new custom one.</summary>
    public RoleDefinition CreateCustom(Project project, string sourceRoleId, string newId, string newDisplayName)
    {
        ArgumentNullException.ThrowIfNull(project);
        RoleDefinition source = project.Roles.FirstOrDefault(r => r.Id == sourceRoleId)
            ?? throw new InvalidOperationException($"Không tìm thấy role nguồn '{sourceRoleId}'.");
        if (project.Roles.Any(r => r.Id == newId))
        {
            throw new InvalidOperationException($"Role id '{newId}' đã tồn tại.");
        }

        var copy = source.Clone();
        copy.Id = newId;
        copy.DisplayName = newDisplayName;
        copy.Group = "Custom";
        project.Roles.Add(copy);
        return copy;
    }

    public void Delete(Project project, string roleId)
    {
        ArgumentNullException.ThrowIfNull(project);
        project.Roles.RemoveAll(r => r.Id == roleId);
    }

    /// <summary>
    /// Applies a role: sets RoleId and swaps the character's equipment to the
    /// role's starting set. Equipment whose assets are missing is skipped with
    /// a warning (per-view assets are resolved from the family when available).
    /// </summary>
    public RoleApplyResult ApplyToCharacter(Project project, CharacterEntity character, string roleId)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(character);
        RoleDefinition role = project.Roles.FirstOrDefault(r => r.Id == roleId)
            ?? throw new InvalidOperationException($"Không tìm thấy role '{roleId}'.");

        var warnings = new List<string>();
        string fallbackView = string.IsNullOrWhiteSpace(project.View.DefaultDirection)
            ? project.View.Directions.FirstOrDefault() ?? "Down"
            : project.View.DefaultDirection;

        character.RoleId = role.Id;
        character.Equipment.Clear(); // "starting equipment" semantics: the role defines the loadout
        int equipped = 0;
        foreach (RoleEquipmentBinding binding in role.StartingEquipment)
        {
            if (!project.Rigs.Any(r => r.FindSlot(binding.SlotId) is not null))
            {
                warnings.Add($"Slot '{binding.SlotId}' không tồn tại trong rig — bỏ qua.");
                continue;
            }

            var viewAssets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? fallbackAssetId = null;
            foreach (string view in project.View.Directions)
            {
                string candidate = AssetFamilyId(binding.AssetFamily, view);
                if (project.Assets.Any(a => a.Id == candidate))
                {
                    viewAssets[view] = candidate;
                    fallbackAssetId ??= candidate;
                }
            }

            fallbackAssetId ??= project.Assets
                .Where(a => a.Id.StartsWith(binding.AssetFamily + ".", StringComparison.Ordinal))
                .OrderBy(a => a.Id, StringComparer.Ordinal)
                .Select(a => a.Id)
                .FirstOrDefault();

            if (fallbackAssetId is null)
            {
                warnings.Add($"Asset family '{binding.AssetFamily}' không có asset nào — bỏ qua slot '{binding.SlotId}'.");
                continue;
            }

            character.Equipment.RemoveAll(e => e.SlotId == binding.SlotId);
            character.Equipment.Add(new EquippedItem(binding.SlotId, fallbackAssetId)
            {
                ViewAssets = viewAssets,
            });
            equipped++;
        }

        return new RoleApplyResult(warnings, equipped);
    }

    /// <summary>"{family}.{view-slug}" — mirrors the starter content asset naming.</summary>
    public static string AssetFamilyId(string family, string view) =>
        $"{family}.{view.ToLowerInvariant()}";
}
