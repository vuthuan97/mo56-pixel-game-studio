
from pathlib import Path
from PIL import Image
import json
from sprite_studio.animation.poses import POSES, ANIMATIONS

class Exporter:
    def __init__(self, renderer):
        self.renderer = renderer
        self.pose_overrides=None

    def set_pose_overrides(self,store): self.pose_overrides=store
    def _pose(self,name):
        from sprite_studio.animation.poses import POSES
        if self.pose_overrides is None: return POSES[name]
        from sprite_studio.editor.pose_editor import resolve_pose
        return resolve_pose(POSES[name],self.pose_overrides.get(name))

    def export_package(self, spec, animations, directions, out_dir, include_layers=False, skill=None, ai_config=None):
        out=Path(out_dir); out.mkdir(parents=True, exist_ok=True)
        original_direction=spec.direction
        metadata={
            "character": spec.to_dict(),
            "animations": {a: ANIMATIONS[a] for a in animations},
            "directions": directions,
        }
        if skill is not None: metadata["skill"]=skill.to_dict()
        if ai_config is not None: metadata["ai_config"]=ai_config.to_dict()

        for direction in directions:
            spec.direction=direction
            for anim in animations:
                adir=out/"animations"/direction/anim
                adir.mkdir(parents=True, exist_ok=True)
                for frame in ANIMATIONS[anim]:
                    img=self.renderer.render(spec, self._pose(frame))
                    img.save(adir/f"{frame}.png")
                    if include_layers:
                        ldir=out/"layers"/direction/anim/frame
                        ldir.mkdir(parents=True, exist_ok=True)
                        for name,layer in self.renderer.render_layers(spec, self._pose(frame)):
                            layer.save(ldir/f"{name}.png")

        for direction in directions:
            spec.direction=direction
            cols=max(len(ANIMATIONS[a]) for a in animations)
            sheet=Image.new("RGBA",(32*cols,46*len(animations)),(0,0,0,0))
            for row,anim in enumerate(animations):
                for col,frame in enumerate(ANIMATIONS[anim]):
                    sheet.alpha_composite(self.renderer.render(spec, self._pose(frame)), (col*32,row*46))
            sheet.save(out/f"spritesheet_{direction.lower()}.png")

        spec.direction=original_direction
        (out/"package.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
        return out
