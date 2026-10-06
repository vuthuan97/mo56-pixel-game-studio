
from pathlib import Path
import json
from sprite_studio.core.models import AIConfig

def save_ai_config(config, path):
    Path(path).write_text(json.dumps(config.to_dict(), ensure_ascii=False, indent=2), encoding="utf-8")

def load_ai_config(path):
    data=json.loads(Path(path).read_text(encoding="utf-8"))
    return AIConfig(**{k:v for k,v in data.items() if k in AIConfig.__dataclass_fields__})
