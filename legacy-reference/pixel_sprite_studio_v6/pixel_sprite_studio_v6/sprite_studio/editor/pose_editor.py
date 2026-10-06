from dataclasses import replace
from pathlib import Path
import json
EDITABLE_FIELDS=["body_dx","body_dy","head_dx","head_dy","left_arm","right_arm","left_leg","right_leg","hair_state","weapon_state","torso_tilt"]
def resolve_pose(base_pose,overrides):
    data={k:v for k,v in (overrides or {}).items() if k in EDITABLE_FIELDS}
    return replace(base_pose,**data)
class PoseOverrideStore:
    def __init__(self): self.data={}
    def get(self,name): return dict(self.data.get(name,{}))
    def set_field(self,name,field,value): self.data.setdefault(name,{})[field]=value
    def reset_pose(self,name): self.data.pop(name,None)
    def save(self,path): Path(path).write_text(json.dumps(self.data,ensure_ascii=False,indent=2),encoding="utf-8")
    def load(self,path): self.data=json.loads(Path(path).read_text(encoding="utf-8"))
