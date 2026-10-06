from pathlib import Path
import json
from sprite_studio.animation.poses import ANIMATIONS

def export_godot_spriteframes(package_dir,animations,directions,fps=6):
    package_dir=Path(package_dir); gd=package_dir/"godot"; gd.mkdir(parents=True,exist_ok=True)
    ext=[]; ids={}; rid=1
    for direction in directions:
        for anim in animations:
            for frame in ANIMATIONS[anim]:
                eid=f"{rid}_{direction}_{anim}_{frame}"; ids[(direction,anim,frame)]=eid
                rel=f"../animations/{direction}/{anim}/{frame}.png"
                ext.append(f'[ext_resource type="Texture2D" path="{rel}" id="{eid}"]')
                rid+=1
    entries=[]
    for direction in directions:
        for anim in animations:
            fr=[]
            for frame in ANIMATIONS[anim]:
                eid=ids[(direction,anim,frame)]
                fr.append('{\n"duration": 1.0,\n"texture": ExtResource("'+eid+'")\n}')
            loop='false' if anim=='death' else 'true'
            entries.append('{\n"frames": ['+','.join(fr)+'],\n"loop": '+loop+',\n"name": &"'+anim+'_'+direction.lower()+'",\n"speed": '+str(float(fps))+'\n}')
    txt='[gd_resource type="SpriteFrames" load_steps='+str(rid)+' format=3]\n\n'+'\n'.join(ext)+'\n\n[resource]\nanimations = ['+',\n'.join(entries)+']\n'
    (gd/'sprite_frames.tres').write_text(txt,encoding='utf-8')
    gdscript='extends AnimatedSprite2D\n\nfunc play_move(direction: String) -> void:\n    play("walk_" + direction)\n\nfunc play_idle(direction: String) -> void:\n    play("idle_" + direction)\n'
    (gd/'character_sprite_example.gd').write_text(gdscript,encoding='utf-8')
    (gd/'godot_export.json').write_text(json.dumps({"directions":directions,"animations":animations,"fps":fps},ensure_ascii=False,indent=2),encoding='utf-8')
    (gd/'README_GODOT.md').write_text('# Godot Export\n\nAssign `sprite_frames.tres` to an AnimatedSprite2D. Animation names use `<animation>_<direction>`.\n',encoding='utf-8')
    return gd
