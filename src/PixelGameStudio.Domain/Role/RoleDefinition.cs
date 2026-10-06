using PixelGameStudio.Domain.Character;

namespace PixelGameStudio.Domain.Role;

/// <summary>One starting-equipment binding of a role: slot + asset FAMILY id.</summary>
/// <remarks>
/// AssetFamily is the id prefix without the view suffix (e.g. "eq.main_hand.kiem");
/// per-view assets are resolved as "{family}.{view-slug}" against project.Assets,
/// falling back to any existing family member. Nothing is hard-coded — roles are data.
/// </remarks>
public sealed record RoleEquipmentBinding(string SlotId, string AssetFamily);

/// <summary>
/// A role = a named template binding a character to starting equipment,
/// behaviors and a signature animation. Roles are project data (no fixed
/// list in code); the five cultivation roles ship as DEMO templates only.
/// </summary>
public sealed class RoleDefinition
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Free-form grouping (e.g. "Tu tiên (demo)", "Hiệp sĩ") — open string.</summary>
    public string Group { get; set; } = "Custom";

    public string Description { get; set; } = string.Empty;

    /// <summary>Equipment equipped when the role is applied to a character.</summary>
    public List<RoleEquipmentBinding> StartingEquipment { get; set; } = [];

    /// <summary>Behaviors (project.Behaviors ids) this role exposes, in gameplay order.</summary>
    public List<string> BehaviorIds { get; set; } = [];

    /// <summary>Signature animation (project.Animations id), e.g. the class-specific attack.</summary>
    public string? DefaultAnimationId { get; set; }

    public List<string> Tags { get; set; } = [];

    public RoleDefinition Clone() => new()
    {
        Id = Id,
        DisplayName = DisplayName,
        Group = Group,
        Description = Description,
        StartingEquipment = StartingEquipment.Select(b => b with { }).ToList(),
        BehaviorIds = [.. BehaviorIds],
        DefaultAnimationId = DefaultAnimationId,
        Tags = [.. Tags],
    };
}
