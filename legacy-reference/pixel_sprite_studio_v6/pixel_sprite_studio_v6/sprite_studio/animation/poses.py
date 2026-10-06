
from sprite_studio.core.models import Pose

POSES = {
    "idle_0": Pose("idle_0"),
    "idle_1": Pose("idle_1", body_dy=1, head_dy=1, hair_state="right"),
    "walk_0": Pose("walk_0", left_arm="forward", right_arm="back", left_leg="back", right_leg="forward"),
    "walk_1": Pose("walk_1", body_dy=1, hair_state="right"),
    "walk_2": Pose("walk_2", left_arm="back", right_arm="forward", left_leg="forward", right_leg="back"),
    "walk_3": Pose("walk_3", body_dy=1, hair_state="left"),
    "attack_0": Pose("attack_0", left_arm="back", weapon_state="ready"),
    "attack_1": Pose("attack_1", body_dy=-1, left_arm="up", right_arm="back", left_leg="back", right_leg="forward", hair_state="right", weapon_state="raise"),
    "attack_2": Pose("attack_2", left_arm="forward", right_arm="back", left_leg="forward", right_leg="back", hair_state="left", weapon_state="slash"),
    "attack_3": Pose("attack_3", body_dy=1, weapon_state="recover"),
    "hurt_0": Pose("hurt_0", head_dx=1, left_arm="back", right_arm="back", hair_state="right"),
    "hurt_1": Pose("hurt_1", body_dy=1, head_dx=-1, left_leg="back", right_leg="forward", hair_state="left"),
    "cast_0": Pose("cast_0", left_arm="up", right_arm="up", hair_state="right"),
    "cast_1": Pose("cast_1", body_dy=-1, left_arm="up", right_arm="up", hair_state="left"),
    "death_0": Pose("death_0", body_dy=2, head_dx=1, left_arm="back", right_arm="back"),
    "death_1": Pose("death_1", body_dy=3, head_dx=2, left_leg="back", right_leg="forward"),

    # class specific
    "sword_dash_0": Pose("sword_dash_0", body_dx=-1, left_arm="back", right_arm="forward", left_leg="back", right_leg="forward", torso_tilt="forward"),
    "sword_dash_1": Pose("sword_dash_1", body_dx=1, body_dy=-1, left_arm="up", right_arm="back", left_leg="forward", right_leg="back", weapon_state="raise", hair_state="right", torso_tilt="forward"),
    "sword_dash_2": Pose("sword_dash_2", body_dx=2, left_arm="forward", right_arm="back", left_leg="forward", right_leg="back", weapon_state="slash", hair_state="left", torso_tilt="forward"),
    "brew_0": Pose("brew_0", left_arm="down", right_arm="down", body_dy=1),
    "brew_1": Pose("brew_1", left_arm="up", right_arm="forward", body_dy=-1, hair_state="right"),
    "brew_2": Pose("brew_2", left_arm="up", right_arm="up", body_dy=-1, hair_state="left"),
    "seal_0": Pose("seal_0", left_arm="up", right_arm="up"),
    "seal_1": Pose("seal_1", body_dy=-1, left_arm="forward", right_arm="up", hair_state="right"),
    "seal_2": Pose("seal_2", left_arm="forward", right_arm="forward", hair_state="left"),
    "smash_0": Pose("smash_0", body_dy=1, left_arm="back", right_arm="back", left_leg="back", right_leg="forward", torso_tilt="forward"),
    "smash_1": Pose("smash_1", body_dy=-1, left_arm="up", right_arm="up", hair_state="right", torso_tilt="forward"),
    "smash_2": Pose("smash_2", body_dy=2, left_arm="forward", right_arm="forward", left_leg="forward", right_leg="back", hair_state="left", torso_tilt="forward"),
    "fan_0": Pose("fan_0", left_arm="down", right_arm="forward", hair_state="right"),
    "fan_1": Pose("fan_1", body_dy=-1, left_arm="up", right_arm="up", hair_state="left"),
    "fan_2": Pose("fan_2", left_arm="forward", right_arm="forward", body_dy=1),
}

ANIMATIONS = {
    "idle": ["idle_0","idle_1"],
    "walk": ["walk_0","walk_1","walk_2","walk_3"],
    "attack": ["attack_0","attack_1","attack_2","attack_3"],
    "hurt": ["hurt_0","hurt_1"],
    "cast": ["cast_0","cast_1"],
    "death": ["death_0","death_1"],

    "class_sword_dash": ["sword_dash_0","sword_dash_1","sword_dash_2"],
    "class_brew": ["brew_0","brew_1","brew_2"],
    "class_seal": ["seal_0","seal_1","seal_2"],
    "class_smash": ["smash_0","smash_1","smash_2"],
    "class_fan_dance": ["fan_0","fan_1","fan_2"],
}

CLASS_ANIMATION_MAP = {
    "Kiếm tu": "class_sword_dash",
    "Đan tu": "class_brew",
    "Phù tu": "class_seal",
    "Thể tu": "class_smash",
    "Du hiệp": "class_fan_dance",
}
