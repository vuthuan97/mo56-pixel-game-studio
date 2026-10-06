using PixelGameStudio.Domain.Animation;

namespace PixelGameStudio.Domain.Templates;

/// <summary>
/// Preset poses ported from the legacy prototype's 30-pose table (generic
/// animations only) as pure data: per-part integer offsets and state values.
/// The composer applies them against state variant assets.
/// </summary>
public static class PosePresets
{
    public static readonly string[] BodyParts =
        ["torso", "head", "face", "hair_front", "hair_back", "arm_left", "arm_right", "leg_left", "leg_right"];

    public static readonly string[] EquipmentSlotsExceptWeapon =
        ["inner", "outer", "chest_armor", "shoulder", "gloves", "pants", "boots", "belt", "cape", "off_hand", "accessory"];

    public static IReadOnlyList<PoseDefinition> All => BuildAll().Select(p => p.Clone()).ToList();

    private static IReadOnlyList<PoseDefinition> BuildAll() =>
    [
        Legacy("idle_0"),
        Legacy("idle_1", bodyDy: 1, headDy: 1),
        Legacy("walk_0", leftArm: "forward", rightArm: "back", leftLeg: "back", rightLeg: "forward"),
        Legacy("walk_1", bodyDy: 1),
        Legacy("walk_2", leftArm: "back", rightArm: "forward", leftLeg: "forward", rightLeg: "back"),
        Legacy("walk_3", bodyDy: 1),
        Legacy("attack_0", leftArm: "back", weaponState: "ready"),
        Legacy("attack_1", bodyDy: -1, leftArm: "up", rightArm: "back", leftLeg: "back", rightLeg: "forward", weaponState: "raise"),
        Legacy("attack_2", leftArm: "forward", rightArm: "back", leftLeg: "forward", rightLeg: "back", weaponState: "slash"),
        Legacy("attack_3", bodyDy: 1, weaponState: "recover"),
        Legacy("hurt_0", headDx: 1, leftArm: "back", rightArm: "back"),
        Legacy("hurt_1", bodyDy: 1, headDx: -1, leftLeg: "back", rightLeg: "forward"),
        Legacy("cast_0", leftArm: "up", rightArm: "up"),
        Legacy("cast_1", bodyDy: -1, leftArm: "up", rightArm: "up"),
        Legacy("death_0", bodyDy: 2, headDx: 1, leftArm: "back", rightArm: "back"),
        Legacy("death_1", bodyDy: 3, headDx: 2, leftLeg: "back", rightLeg: "forward"),
    ];

    public static PoseDefinition? Find(string id) => BuildAll().FirstOrDefault(p => p.Id == id)?.Clone();

    /// <summary>Builds one legacy-shaped pose (body offsets + head offsets + limb states + weapon state).</summary>
    public static PoseDefinition Legacy(
        string id,
        int bodyDx = 0,
        int bodyDy = 0,
        int headDx = 0,
        int headDy = 0,
        string leftArm = "down",
        string rightArm = "down",
        string leftLeg = "neutral",
        string rightLeg = "neutral",
        string? weaponState = null)
    {
        var pose = new PoseDefinition { Id = id, DisplayName = id };
        foreach (string partId in BodyParts)
        {
            int x = bodyDx;
            int y = bodyDy;
            if (partId is "head" or "face" or "hair_front")
            {
                x += headDx;
                y += headDy;
            }

            var partPose = new PartPose(x, y);
            if (partId == "arm_left")
            {
                partPose = partPose with { States = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["hand"] = leftArm } };
            }
            else if (partId == "arm_right")
            {
                partPose = partPose with { States = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["hand"] = rightArm } };
            }
            else if (partId == "leg_left")
            {
                partPose = partPose with { States = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["leg"] = leftLeg } };
            }
            else if (partId == "leg_right")
            {
                partPose = partPose with { States = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["leg"] = rightLeg } };
            }

            pose.Parts[partId] = partPose;
        }

        foreach (string slotId in EquipmentSlotsExceptWeapon)
        {
            pose.Slots[slotId] = new SlotPose(bodyDx, bodyDy);
        }

        // The legacy weapon follows body_dx but ignores body_dy.
        var weaponPose = new SlotPose(bodyDx, 0);
        if (weaponState is not null)
        {
            weaponPose = weaponPose with
            {
                States = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["weapon"] = weaponState },
                ZIndexOverride = weaponState is "raise" or "slash" ? 23 : null,
            };
        }

        pose.Slots["main_hand"] = weaponPose;
        return pose;
    }
}
