"""Generate golden-master fixtures from the legacy Pillow prototype.

Read-only import of legacy-reference source; writes PNG fixtures + manifests
into tests/PixelGameStudio.Rendering.Tests/Fixtures/.

Run from repo root:  python tools/fixturegen/generate_fixtures.py
"""
import json
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "legacy-reference" / "pixel_sprite_studio_v6" / "pixel_sprite_studio_v6"))

from PIL import Image, ImageDraw, ImageOps  # noqa: E402

from sprite_studio.core.models import CharacterSpec  # noqa: E402
from sprite_studio.animation.poses import POSES  # noqa: E402
from sprite_studio.rendering.renderer import CharacterRenderer, OUTLINE  # noqa: E402
from sprite_studio.editor.art_qa import analyze_sprite  # noqa: E402
from sprite_studio.editor.anchors import AnchorStore  # noqa: E402

FIXTURES = REPO / "tests" / "PixelGameStudio.Rendering.Tests" / "Fixtures"
PRIM = FIXTURES / "primitives"
SPRITES = FIXTURES / "sprites"

SPEC_FIELDS = [
    "gender", "class_type", "body_type", "skin_tone", "head_shape", "eye_style",
    "nose_style", "mouth_style", "hair_style", "hair_color", "skin_variant",
    "aura", "effect", "element",
]
EQ_FIELDS = [
    "head", "inner", "outer", "chest_armor", "shoulder", "gloves", "pants",
    "boots", "belt", "cape", "main_hand", "off_hand", "accessory",
]

CLASS_TEMPLATES = {
    "Kiếm tu": {"outer": "Kiếm tu", "main_hand": "Kiếm", "off_hand": "Không", "belt": "Đai ngọc", "cape": "Không"},
    "Đan tu": {"outer": "Đan tu", "main_hand": "Không", "off_hand": "Hồ lô", "belt": "Đai vải", "cape": "Áo choàng ngắn"},
    "Phù tu": {"outer": "Phù tu", "main_hand": "Không", "off_hand": "Phù", "belt": "Đai vải", "cape": "Không"},
    "Thể tu": {"outer": "Thể tu", "main_hand": "Đao", "off_hand": "Khiên", "belt": "Đai giáp", "cape": "Không"},
    "Du hiệp": {"outer": "Du hiệp", "main_hand": "Quạt", "off_hand": "Không", "belt": "Đai ngọc", "cape": "Áo choàng ngắn"},
}


def new_spec(**overrides):
    eq = {k: overrides.pop(f"eq_{k}", None) for k in EQ_FIELDS}
    spec = CharacterSpec()
    for k, v in eq.items():
        if v is not None:
            spec.equipment[k] = v
    for k, v in overrides.items():
        setattr(spec, k, v)
    return spec


def save(img: Image.Image, path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path)


def gen_primitives():
    prims = {"ellipse": [], "line": [], "rect": [], "polygon": [], "outline": [], "grayscale": [], "nearest": []}

    # --- ellipse fills: full grid of bbox sizes (w,h) 1..18 on 20x20 ---
    for w in range(1, 19):
        for h in range(1, 19):
            im = Image.new("RGBA", (20, 20), (0, 0, 0, 0))
            d = ImageDraw.Draw(im)
            d.ellipse((1, 1, w, h), fill=(255, 90, 40, 255))
            name = f"ellipse/e_{w}x{h}.png"
            save(im, PRIM / name)
            prims["ellipse"].append({"bbox": [1, 1, w, h], "file": name})

    # --- lines (width 1, includes endpoint) ---
    line_cases = [
        (2, 2, 7, 5), (3, 25, 9, 27), (3, 25, 8, 20), (0, 0, 0, 9), (0, 0, 9, 0),
        (0, 0, 9, 9), (9, 9, 0, 0), (5, 1, 1, 8), (8, 2, 2, 6), (1, 8, 8, 2),
        (10, 20, 12, 2), (2, 15, 13, 15), (7, 3, 7, 14), (0, 5, 11, 5),
    ]
    for i, (x0, y0, x1, y1) in enumerate(line_cases):
        im = Image.new("RGBA", (16, 30), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.line([(x0, y0), (x1, y1)], fill=(60, 200, 120, 255), width=1)
        name = f"line/l_{i:02d}_{x0}_{y0}_{x1}_{y1}.png"
        save(im, PRIM / name)
        prims["line"].append({"from": [x0, y0], "to": [x1, y1], "file": name})

    # --- rect fills (inclusive bbox) ---
    rect_cases = [(1, 1, 3, 4), (0, 0, 19, 19), (5, 7, 5, 7), (2, 3, 10, 3),
                  (4, 2, 4, 9), (0, 0, 19, 8), (3, 9, 16, 18)]
    for i, (x0, y0, x1, y1) in enumerate(rect_cases):
        im = Image.new("RGBA", (20, 20), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.rectangle((x0, y0, x1, y1), fill=(90, 90, 220, 255))
        name = f"rect/r_{i:02d}_{x0}_{y0}_{x1}_{y1}.png"
        save(im, PRIM / name)
        prims["rect"].append({"bbox": [x0, y0, x1, y1], "file": name})

    # --- polygon fills (even-odd scanline, RGBA path) ---
    poly_cases = [
        [(3, 10), (8, 5), (9, 12)],                      # fan shape (dx=0)
        [(2, 25), (7, 20), (8, 27)],                     # fan dx=-1
        [(4, 25), (9, 20), (10, 27)],                    # fan dx=+1
        [(2, 2), (12, 2), (12, 9), (2, 9)],              # quad
        [(2, 2), (12, 5), (2, 9), (12, 12)],             # concave (even-odd)
        [(6, 1), (10, 8), (6, 14), (2, 8)],              # diamond
    ]
    for i, pts in enumerate(poly_cases):
        im = Image.new("RGBA", (18, 30), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.polygon(pts, fill=(230, 210, 90, 255))
        name = f"polygon/p_{i:02d}.png"
        save(im, PRIM / name)
        prims["polygon"].append({"points": pts, "file": name})

    # --- outline pass on synthetic masks (legacy outline() is hard-coded to 32x46) ---
    def mask_pixels(mask_case):
        im = Image.new("RGBA", (32, 46), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        if mask_case == "plus":
            d.rectangle((14, 6, 17, 26), fill=(255, 255, 255, 255))
            d.rectangle((6, 14, 26, 17), fill=(255, 255, 255, 255))
        elif mask_case == "hollow_box":
            d.rectangle((6, 8, 25, 30), outline=(255, 255, 255, 255), width=2)
        elif mask_case == "diag":
            d.line([(3, 3), (28, 40)], fill=(255, 255, 255, 255))
        elif mask_case == "two_blobs":
            d.rectangle((4, 6, 9, 12), fill=(255, 255, 255, 255))
            d.rectangle((14, 6, 19, 12), fill=(255, 255, 255, 255))
        elif mask_case == "edge_block":
            d.rectangle((30, 0, 31, 45), fill=(255, 255, 255, 255))
        elif mask_case == "single":
            d.point((16, 22), fill=(255, 255, 255, 255))
        elif mask_case == "with_hole":
            d.rectangle((6, 8, 25, 30), fill=(255, 255, 255, 255))
            d.rectangle((12, 14, 19, 22), fill=(0, 0, 0, 0))
        return im

    from sprite_studio.rendering import renderer as _r
    for case in ["plus", "hollow_box", "diag", "two_blobs", "edge_block", "single", "with_hole"]:
        im = mask_pixels(case)
        out = _r.outline(im)
        name = f"outline/o_{case}.png"
        save(out, PRIM / name)
        px = sorted([(x, y) for y in range(46) for x in range(32) if im.getpixel((x, y))[3] > 0])
        prims["outline"].append({"mask": px, "outlineColor": list(OUTLINE), "file": name})

    # --- grayscale (L24, alpha preserved) ---
    cols = [(0, 0, 0, 255), (1, 2, 3, 255), (34, 58, 130, 255), (128, 82, 64, 255),
            (196, 140, 48, 255), (250, 250, 255, 255), (255, 255, 255, 255),
            (22, 16, 30, 255), (255, 240, 225, 150), (44, 46, 78, 120)]
    strip = Image.new("RGBA", (len(cols), 1), (0, 0, 0, 0))
    d = ImageDraw.Draw(strip)
    for i, c in enumerate(cols):
        d.point((i, 0), fill=c)
    g = ImageOps.grayscale(strip.convert("RGB")).convert("RGBA")
    g.putalpha(strip.getchannel("A"))
    name = "grayscale/strip.png"
    save(g, PRIM / name)
    prims["grayscale"].append({"colors": [list(c) for c in cols], "file": name})

    # --- nearest-neighbor resize ---
    marker = new_spec()
    r = CharacterRenderer()
    src = r.render(marker, POSES["idle_0"])
    for scale, tag in [(2, "2x"), (4, "4x"), (10, "10x")]:
        up = src.resize((src.width * scale, src.height * scale), Image.Resampling.NEAREST)
        name = f"nearest/sprite_{tag}.png"
        save(up, PRIM / name)
        prims["nearest"].append({"target": [up.width, up.height], "file": name})
    save(src, PRIM / "nearest/src_32x46.png")
    small = Image.new("RGBA", (5, 3), (0, 0, 0, 0))
    d = ImageDraw.Draw(small)
    for y in range(3):
        for x in range(5):
            d.point((x, y), fill=(x * 40, y * 80, 0, 255))
    for tw, th in [(13, 8), (3, 2), (7, 4), (50, 30)]:
        up = small.resize((tw, th), Image.Resampling.NEAREST)
        name = f"nearest/marker_{tw}x{th}.png"
        save(up, PRIM / name)
        prims["nearest"].append({"target": [tw, th], "file": name})
    save(small, PRIM / "nearest/src_5x3.png")

    (PRIM / "manifest.json").write_text(json.dumps(prims, indent=1), encoding="utf-8")
    print(f"primitives: {sum(len(v) for v in prims.values())} cases")


def gen_sprites():
    renderer = CharacterRenderer()
    anchors = AnchorStore()
    renderer.set_anchor_store(anchors)
    entries = []

    def add(tag, spec, pose_name, mode="Normal"):
        pose = POSES[pose_name]
        img = renderer.render(spec, pose, mode)
        fname = f"{len(entries):03d}_{tag}.png"
        save(img, SPRITES / fname)
        qa = analyze_sprite(img)
        entry = {
            "file": fname, "pose": pose_name, "mode": mode,
            "direction": spec.direction,
            "spec": {k: getattr(spec, k) for k in SPEC_FIELDS},
            "equipment": {k: spec.equipment[k] for k in EQ_FIELDS},
            "qa": {"palette_count": qa["palette_count"], "visible_size": qa["visible_size"],
                   "value_range": qa["value_range"], "warnings": qa["warnings"]},
        }
        entries.append(entry)

    default = new_spec()

    # direction sweep for key poses
    for pose in ["idle_0", "attack_1", "sword_dash_1"]:
        for direction in ["Down", "Up", "Left", "Right"]:
            add(f"{pose}_{direction}", new_spec(direction=direction), pose)

    # pose sweep (Down, Normal)
    for pose in ["idle_1", "walk_0", "walk_2", "attack_0", "attack_2", "attack_3",
                 "hurt_0", "cast_1", "death_1", "sword_dash_0", "sword_dash_2",
                 "brew_1", "seal_2", "smash_1", "fan_0"]:
        add(f"{pose}_Down", default, pose)

    # preview modes for default sprite
    for mode in ["Grayscale", "Silhouette", "Part Debug", "Anchor Debug"]:
        add(f"idle_0_Down_{mode.replace(' ', '')}", default, "idle_0", mode)

    # body/face/hair variants
    variants = [
        {"body_type": "Mảnh"}, {"body_type": "Đậm"},
        {"skin_tone": "Trung bình"}, {"skin_tone": "Ngăm"},
        {"head_shape": "Gọn"},
        {"eye_style": "Vui"}, {"eye_style": "Lạnh lùng"},
        {"eye_style": "Nghiêm túc"}, {"eye_style": "Buồn ngủ"},
        {"nose_style": "Không"},
        {"mouth_style": "Cười"}, {"mouth_style": "Nghiêm"},
        {"hair_style": "Tóc dài"}, {"hair_style": "Tóc ngắn"}, {"hair_style": "Đuôi ngựa"},
        {"hair_style": "Tóc xõa"}, {"hair_style": "Tóc dựng"}, {"hair_style": "Tóc lệch"},
        {"hair_style": "Hai búi"}, {"hair_style": "Tóc mái dài"}, {"hair_style": "Tóc võ sĩ"},
        {"hair_color": "Trắng bạc"}, {"hair_color": "Đỏ"}, {"hair_color": "Xanh lá"},
        {"hair_color": "Nâu"}, {"hair_color": "Vàng"},
        {"skin_variant": "Lạnh"}, {"skin_variant": "Hỏa"}, {"skin_variant": "U tối"}, {"skin_variant": "Mộc"},
    ]
    for v in variants:
        tag = "_".join(f"{k}={val}" for k, val in v.items())
        add(tag, new_spec(**v), "idle_0")

    # class templates (equipment combos)
    for cls, tpl in CLASS_TEMPLATES.items():
        if cls == "Kiếm tu":
            continue  # default spec already matches
        add(f"class_{cls}", new_spec(**{f"eq_{k}": v for k, v in tpl.items()}), "idle_0")

    # weapon sweep
    for w in ["Kiếm", "Đao", "Thương", "Quạt", "Trượng"]:
        add(f"weapon_{w}", new_spec(eq_outer="Không", eq_main_hand=w, eq_belt="Không"), "idle_0")

    # weapon states
    from sprite_studio.core.models import Pose
    states = ["slash", "raise", "recover", "ready", "back"]
    for st in states:
        spec = new_spec(eq_outer="Không", eq_main_hand="Kiếm", eq_belt="Không")
        pose = Pose(f"wpn_{st}", weapon_state=st)
        img = renderer.render(spec, pose)
        fname = f"{len(entries):03d}_wpnstate_{st}.png"
        save(img, SPRITES / fname)
        qa = analyze_sprite(img)
        entries.append({"file": fname, "pose": None, "mode": "Normal", "direction": spec.direction,
                        "poseOverride": {"weapon_state": st},
                        "spec": {k: getattr(spec, k) for k in SPEC_FIELDS},
                        "equipment": {k: spec.equipment[k] for k in EQ_FIELDS},
                        "qa": {"palette_count": qa["palette_count"], "visible_size": qa["visible_size"],
                               "value_range": qa["value_range"], "warnings": qa["warnings"]}})
    # fan + trượng with slash/raise (polygon/line paths)
    for w, st in [("Quạt", "slash"), ("Trượng", "raise"), ("Đao", "slash")]:
        pose = Pose(f"wpn2_{w}_{st}", weapon_state=st)
        spec = new_spec(eq_outer="Không", eq_main_hand=w, eq_belt="Không")
        img = renderer.render(spec, pose)
        fname = f"{len(entries):03d}_wpnstate_{w}_{st}.png"
        save(img, SPRITES / fname)
        qa = analyze_sprite(img)
        entries.append({"file": fname, "pose": None, "mode": "Normal", "direction": spec.direction,
                        "poseOverride": {"weapon_state": st},
                        "spec": {k: getattr(spec, k) for k in SPEC_FIELDS},
                        "equipment": {k: spec.equipment[k] for k in EQ_FIELDS},
                        "qa": {"palette_count": qa["palette_count"], "visible_size": qa["visible_size"],
                               "value_range": qa["value_range"], "warnings": qa["warnings"]}})

    # effects / aura
    for eff in ["Glow", "Spark", "Burst", "Slash", "Heal", "Shield"]:
        add(f"effect_{eff}", new_spec(effect=eff), "idle_0")
    add("aura_Kim", new_spec(aura="Kim"), "idle_0")

    # equipment slot sweep
    eq_cases = [
        {"cape": "Áo choàng ngắn"}, {"cape": "Áo choàng dài"},
        {"pants": "Quần sáng"}, {"boots": "Ủng giáp"}, {"boots": "Giày vải"},
        {"inner": "Áo trong sáng"}, {"inner": "Áo trong tối"},
        {"chest_armor": "Giáp ngực nhẹ"}, {"chest_armor": "Giáp ngực nặng"},
        {"shoulder": "Giáp vai nhẹ"}, {"shoulder": "Giáp vai nặng"},
        {"gloves": "Bao tay vải"}, {"gloves": "Bao tay giáp"},
        {"belt": "Đai vải"}, {"belt": "Đai giáp"},
        {"head": "Băng trán"}, {"head": "Mũ vải"}, {"head": "Mũ giáp"},
        {"off_hand": "Khiên"}, {"off_hand": "Phù"}, {"off_hand": "Hồ lô"},
        {"accessory": "Khuyên tai"}, {"accessory": "Hồ lô"},
    ]
    for i, v in enumerate(eq_cases):
        add(f"eq{i:02d}", new_spec(**{f"eq_{k}": val for k, val in v.items()}), "idle_0")
    # cape on Up (direction branch) + side view
    add("eq_cape_up", new_spec(eq_cape="Áo choàng dài", direction="Up"), "idle_0")
    add("eq_cape_left", new_spec(eq_cape="Áo choàng dài", direction="Left"), "idle_0")
    # everything off
    add("eq_none", new_spec(**{f"eq_{k}": "Không" for k in EQ_FIELDS}), "idle_0")
    # heavy loadout
    add("eq_heavy", new_spec(
        eq_head="Mũ giáp", eq_inner="Áo trong tối", eq_outer="Thể tu", eq_chest_armor="Giáp ngực nặng",
        eq_shoulder="Giáp vai nặng", eq_gloves="Bao tay giáp", eq_pants="Quần sáng", eq_boots="Ủng giáp",
        eq_belt="Đai giáp", eq_cape="Áo choàng dài", eq_main_hand="Đao", eq_off_hand="Khiên",
        eq_accessory="Hồ lô", effect="Glow", aura="Hỏa"), "idle_0")

    (SPRITES / "manifest.json").write_text(json.dumps(entries, indent=1, ensure_ascii=False), encoding="utf-8")
    print(f"sprites: {len(entries)} cases")


if __name__ == "__main__":
    gen_primitives()
    gen_sprites()

    # composite rounding cases (src over dst, straight alpha)
    def comp(base, over):
        b = Image.new("RGBA", (1, 1), base)
        o = Image.new("RGBA", (1, 1), over)
        b.alpha_composite(o)
        return b.getpixel((0, 0))

    cases = []
    for ba in (0, 1, 17, 64, 128, 200, 254, 255):
        for oa in (0, 1, 37, 128, 200, 255):
            cases.append({"base": [10, 20, 30, ba], "over": [200, 150, 100, oa],
                          "out": list(comp((10, 20, 30, ba), (200, 150, 100, oa)))})
    (FIXTURES / "composite_cases.json").write_text(json.dumps(cases), encoding="utf-8")
    print(f"composite: {len(cases)} cases")
