
from __future__ import annotations
from dataclasses import dataclass, asdict, field
from typing import Dict, Any
import json

@dataclass
class CharacterSpec:
    name: str = "Lăng Hàn"

    # Base body / identity
    gender: str = "Nam"
    class_type: str = "Kiếm tu"
    body_type: str = "Tiêu chuẩn"
    skin_tone: str = "Sáng"

    # Face / hair
    head_shape: str = "Tròn"
    eye_style: str = "Bình tĩnh"
    nose_style: str = "Chấm"
    mouth_style: str = "Trung tính"
    hair_style: str = "Búi cao"
    hair_color: str = "Đen tím"

    # Direction / theme
    direction: str = "Down"
    skin_variant: str = "Mặc định"

    equipment: Dict[str, str] = field(default_factory=lambda: {
        "head": "Không",
        "inner": "Không",
        "outer": "Kiếm tu",
        "chest_armor": "Không",
        "shoulder": "Không",
        "gloves": "Không",
        "pants": "Quần tối",
        "boots": "Giày da",
        "belt": "Đai ngọc",
        "cape": "Không",
        "main_hand": "Kiếm",
        "off_hand": "Không",
        "accessory": "Ngọc bội",
    })

    aura: str = "Không"
    effect: str = "Không"
    element: str = "Kim"

    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "CharacterSpec":
        fields = cls.__dataclass_fields__
        clean = {k: v for k, v in data.items() if k in fields and k != "equipment"}
        obj = cls(**clean)
        if isinstance(data.get("equipment"), dict):
            obj.equipment.update(data["equipment"])
        return obj

    def save_json(self, path: str):
        with open(path, "w", encoding="utf-8") as f:
            json.dump(self.to_dict(), f, ensure_ascii=False, indent=2)

    @classmethod
    def load_json(cls, path: str):
        with open(path, "r", encoding="utf-8") as f:
            return cls.from_dict(json.load(f))


@dataclass
class Pose:
    name: str
    body_dx: int = 0
    body_dy: int = 0
    head_dx: int = 0
    head_dy: int = 0
    left_arm: str = "down"
    right_arm: str = "down"
    left_leg: str = "neutral"
    right_leg: str = "neutral"
    hair_state: str = "still"
    weapon_state: str = "idle"
    torso_tilt: str = "neutral"


@dataclass
class SkillSpec:
    skill_id: str = "skill_01"
    name: str = "Kiếm Trảm"
    animation: str = "attack"
    effect: str = "Slash"
    element: str = "Kim"
    hit_frame: int = 2

    def to_dict(self):
        return asdict(self)


@dataclass
class AIConfig:
    provider: str = "OpenRouter"
    model: str = ""
    base_url: str = ""
    api_key_env: str = "OPENROUTER_API_KEY"
    prompt_template: str = (
        "Design one modular pixel-art asset for this game. "
        "Preserve 32x46 canvas, direction, anchors, slot, palette, and top-down readability."
    )

    def to_dict(self):
        return asdict(self)
