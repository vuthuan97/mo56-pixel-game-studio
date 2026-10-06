using PixelGameStudio.Domain.Role;

namespace PixelGameStudio.Domain.Templates;

/// <summary>
/// The five cultivation roles from the legacy prototype (CLASS_TEMPLATES),
/// ported as DEMO data only — the system never requires them and nothing in
/// the core branches on them (rule: no cultivation hard-coding). Equipment
/// families point at the starter content asset ids.
/// </summary>
public static class CultivationRoleTemplates
{
    public const string GroupName = "Tu tiên (demo)";

    public static IReadOnlyList<RoleDefinition> All { get; } =
    [
        Role("role.kiem-tu", "Kiếm tu", "eq.main_hand.kiem", "attack",
            [("outer", "eq.outer.kiem-tu"), ("belt", "eq.belt.dai-ngoc")],
            ["beh.attack_light", "beh.guard", "beh.stand", "beh.walk"],
            "Kiếm sĩ cổ điển — kim hệ, slash effect."),
        Role("role.dan-tu", "Đan tu", "eq.off_hand.ho-lo", "cast",
            [("outer", "eq.outer.dan-tu"), ("belt", "eq.belt.dai-vai"), ("cape", "eq.cape.ao-choang-ngan")],
            ["beh.skill", "beh.craft", "beh.stand"],
            "Luyện đan — mộc hệ, heal effect.", ["mộc"]),
        Role("role.phu-tu", "Phù tu", "eq.off_hand.phu", "cast",
            [("outer", "eq.outer.phu-tu"), ("belt", "eq.belt.dai-vai")],
            ["beh.skill", "beh.throw", "beh.stand"],
            "Vẽ phù — hỏa hệ, burst effect.", ["hỏa"]),
        Role("role.the-tu", "Thể tu", "eq.main_hand.dao", "attack",
            [("outer", "eq.outer.the-tu"), ("off_hand", "eq.off_hand.khien"), ("belt", "eq.belt.dai-giap")],
            ["beh.attack_heavy", "beh.block", "beh.stand", "beh.walk"],
            "Luyện thể — thổ hệ, glow effect.", ["thổ"]),
        Role("role.du-hiep", "Du hiệp", "eq.main_hand.quat", "attack",
            [("outer", "eq.outer.du-hiep"), ("belt", "eq.belt.dai-ngoc"), ("cape", "eq.cape.ao-choang-ngan")],
            ["beh.attack_light", "beh.dodge", "beh.stand", "beh.walk"],
            "Du hiệp — thủy hệ, spark effect.", ["thủy"]),
    ];

    private static RoleDefinition Role(
        string id, string name, string signatureFamily, string signatureAnimation,
        (string Slot, string Family)[] equipment, string[] behaviors, string description, string[]? elements = null)
    {
        var role = new RoleDefinition
        {
            Id = id,
            DisplayName = name,
            Group = GroupName,
            Description = description,
            DefaultAnimationId = signatureAnimation,
            StartingEquipment = new[]
                {
                    new RoleEquipmentBinding("main_hand", signatureFamily),
                }
                .Concat(equipment.Select(e => new RoleEquipmentBinding(e.Slot, e.Family)))
                .ToList(),
            BehaviorIds = [.. behaviors],
            Tags = ["demo", .. elements ?? []],
        };
        return role;
    }
}
