namespace PixelGameStudio.Rendering.Procedural;

/// <summary>Pose state ported verbatim from the legacy prototype (string states kept).</summary>
public sealed record LegacyPose(
    string Name,
    int BodyDx = 0,
    int BodyDy = 0,
    int HeadDx = 0,
    int HeadDy = 0,
    string LeftArm = "down",
    string RightArm = "down",
    string LeftLeg = "neutral",
    string RightLeg = "neutral",
    string HairState = "still",
    string WeaponState = "idle",
    string TorsoTilt = "neutral");

/// <summary>Preset poses/animations from the legacy prototype, kept as reference data.</summary>
public static class LegacyPosePresets
{
    public static readonly Dictionary<string, LegacyPose> Poses = new()
    {
        ["idle_0"] = new LegacyPose("idle_0"),
        ["idle_1"] = new LegacyPose("idle_1", BodyDy: 1, HeadDy: 1, HairState: "right"),
        ["walk_0"] = new LegacyPose("walk_0", LeftArm: "forward", RightArm: "back", LeftLeg: "back", RightLeg: "forward"),
        ["walk_1"] = new LegacyPose("walk_1", BodyDy: 1, HairState: "right"),
        ["walk_2"] = new LegacyPose("walk_2", LeftArm: "back", RightArm: "forward", LeftLeg: "forward", RightLeg: "back"),
        ["walk_3"] = new LegacyPose("walk_3", BodyDy: 1, HairState: "left"),
        ["attack_0"] = new LegacyPose("attack_0", LeftArm: "back", WeaponState: "ready"),
        ["attack_1"] = new LegacyPose("attack_1", BodyDy: -1, LeftArm: "up", RightArm: "back", LeftLeg: "back", RightLeg: "forward", HairState: "right", WeaponState: "raise"),
        ["attack_2"] = new LegacyPose("attack_2", LeftArm: "forward", RightArm: "back", LeftLeg: "forward", RightLeg: "back", HairState: "left", WeaponState: "slash"),
        ["attack_3"] = new LegacyPose("attack_3", BodyDy: 1, WeaponState: "recover"),
        ["hurt_0"] = new LegacyPose("hurt_0", HeadDx: 1, LeftArm: "back", RightArm: "back", HairState: "right"),
        ["hurt_1"] = new LegacyPose("hurt_1", BodyDy: 1, HeadDx: -1, LeftLeg: "back", RightLeg: "forward", HairState: "left"),
        ["cast_0"] = new LegacyPose("cast_0", LeftArm: "up", RightArm: "up", HairState: "right"),
        ["cast_1"] = new LegacyPose("cast_1", BodyDy: -1, LeftArm: "up", RightArm: "up", HairState: "left"),
        ["death_0"] = new LegacyPose("death_0", BodyDy: 2, HeadDx: 1, LeftArm: "back", RightArm: "back"),
        ["death_1"] = new LegacyPose("death_1", BodyDy: 3, HeadDx: 2, LeftLeg: "back", RightLeg: "forward"),
        ["sword_dash_0"] = new LegacyPose("sword_dash_0", BodyDx: -1, LeftArm: "back", RightArm: "forward", LeftLeg: "back", RightLeg: "forward", TorsoTilt: "forward"),
        ["sword_dash_1"] = new LegacyPose("sword_dash_1", BodyDx: 1, BodyDy: -1, LeftArm: "up", RightArm: "back", LeftLeg: "forward", RightLeg: "back", WeaponState: "raise", HairState: "right", TorsoTilt: "forward"),
        ["sword_dash_2"] = new LegacyPose("sword_dash_2", BodyDx: 2, LeftArm: "forward", RightArm: "back", LeftLeg: "forward", RightLeg: "back", WeaponState: "slash", HairState: "left", TorsoTilt: "forward"),
        ["brew_0"] = new LegacyPose("brew_0", LeftArm: "down", RightArm: "down", BodyDy: 1),
        ["brew_1"] = new LegacyPose("brew_1", LeftArm: "up", RightArm: "forward", BodyDy: -1, HairState: "right"),
        ["brew_2"] = new LegacyPose("brew_2", LeftArm: "up", RightArm: "up", BodyDy: -1, HairState: "left"),
        ["seal_0"] = new LegacyPose("seal_0", LeftArm: "up", RightArm: "up"),
        ["seal_1"] = new LegacyPose("seal_1", BodyDy: -1, LeftArm: "forward", RightArm: "up", HairState: "right"),
        ["seal_2"] = new LegacyPose("seal_2", LeftArm: "forward", RightArm: "forward", HairState: "left"),
        ["smash_0"] = new LegacyPose("smash_0", BodyDy: 1, LeftArm: "back", RightArm: "back", LeftLeg: "back", RightLeg: "forward", TorsoTilt: "forward"),
        ["smash_1"] = new LegacyPose("smash_1", BodyDy: -1, LeftArm: "up", RightArm: "up", HairState: "right", TorsoTilt: "forward"),
        ["smash_2"] = new LegacyPose("smash_2", BodyDy: 2, LeftArm: "forward", RightArm: "forward", LeftLeg: "forward", RightLeg: "back", HairState: "left", TorsoTilt: "forward"),
        ["fan_0"] = new LegacyPose("fan_0", LeftArm: "down", RightArm: "forward", HairState: "right"),
        ["fan_1"] = new LegacyPose("fan_1", BodyDy: -1, LeftArm: "up", RightArm: "up", HairState: "left"),
        ["fan_2"] = new LegacyPose("fan_2", LeftArm: "forward", RightArm: "forward", BodyDy: 1),
    };

    public static readonly Dictionary<string, string[]> Animations = new()
    {
        ["idle"] = ["idle_0", "idle_1"],
        ["walk"] = ["walk_0", "walk_1", "walk_2", "walk_3"],
        ["attack"] = ["attack_0", "attack_1", "attack_2", "attack_3"],
        ["hurt"] = ["hurt_0", "hurt_1"],
        ["cast"] = ["cast_0", "cast_1"],
        ["death"] = ["death_0", "death_1"],
        ["class_sword_dash"] = ["sword_dash_0", "sword_dash_1", "sword_dash_2"],
        ["class_brew"] = ["brew_0", "brew_1", "brew_2"],
        ["class_seal"] = ["seal_0", "seal_1", "seal_2"],
        ["class_smash"] = ["smash_0", "smash_1", "smash_2"],
        ["class_fan_dance"] = ["fan_0", "fan_1", "fan_2"],
    };

    public static readonly Dictionary<string, string> ClassAnimationMap = new()
    {
        ["Kiếm tu"] = "class_sword_dash",
        ["Đan tu"] = "class_brew",
        ["Phù tu"] = "class_seal",
        ["Thể tu"] = "class_smash",
        ["Du hiệp"] = "class_fan_dance",
    };
}
