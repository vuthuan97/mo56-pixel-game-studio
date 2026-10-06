from pathlib import Path
import shutil,json
class AssetLibrary:
    def __init__(self,base_dir): self.base_dir=Path(base_dir); self.base_dir.mkdir(parents=True,exist_ok=True)
    def slot_dir(self,slot): d=self.base_dir/slot; d.mkdir(parents=True,exist_ok=True); return d
    def import_png(self,slot,source_path,asset_id=None):
        src=Path(source_path); aid=asset_id or src.stem; dst=self.slot_dir(slot)/(aid+".png"); shutil.copy2(src,dst)
        dst.with_suffix(".json").write_text(json.dumps({"id":aid,"slot":slot,"file":dst.name,"canvas":[32,46]},ensure_ascii=False,indent=2),encoding="utf-8")
        return dst
    def list_assets(self,slot): return [{"id":p.stem,"path":p,"slot":slot} for p in sorted(self.slot_dir(slot).glob("*.png"))]
