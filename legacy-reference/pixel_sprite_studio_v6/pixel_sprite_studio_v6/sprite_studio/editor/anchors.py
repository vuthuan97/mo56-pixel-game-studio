from pathlib import Path
import json
DEFAULT_ANCHORS={
"Down":{"head":[16,12],"chest":[16,26],"left_hand":[9,31],"right_hand":[23,31],"left_foot":[12,42],"right_foot":[20,42],"weapon":[4,29],"accessory":[20,31]},
"Up":{"head":[16,12],"chest":[16,26],"left_hand":[10,30],"right_hand":[22,30],"left_foot":[12,42],"right_foot":[20,42],"weapon":[4,28],"accessory":[19,31]},
"Left":{"head":[15,12],"chest":[15,26],"left_hand":[8,30],"right_hand":[21,30],"left_foot":[11,42],"right_foot":[19,42],"weapon":[4,29],"accessory":[19,31]},
"Right":{"head":[17,12],"chest":[17,26],"left_hand":[11,30],"right_hand":[24,30],"left_foot":[13,42],"right_foot":[21,42],"weapon":[5,29],"accessory":[21,31]}}
class AnchorStore:
    def __init__(self): self.data=json.loads(json.dumps(DEFAULT_ANCHORS))
    def get(self,direction,name): return tuple(self.data[direction][name])
    def set(self,direction,name,x,y): self.data.setdefault(direction,{})[name]=[int(x),int(y)]
    def reset(self): self.data=json.loads(json.dumps(DEFAULT_ANCHORS))
    def save(self,path): Path(path).write_text(json.dumps(self.data,ensure_ascii=False,indent=2),encoding="utf-8")
    def load(self,path): self.data=json.loads(Path(path).read_text(encoding="utf-8"))
