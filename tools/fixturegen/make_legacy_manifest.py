"""Generate tools/legacy_checksums.txt — integrity manifest of legacy-reference/.

The legacy prototype is the read-only source of truth for golden-master tests.
This manifest lets any later run verify nothing in it changed. `__pycache__`
is excluded (bytecode regenerates when Python imports the modules); the zip
snapshot is recorded as a single hash line.

Run from repo root:  python tools/fixturegen/make_legacy_manifest.py
"""
import hashlib
import os

ROOT = "legacy-reference"
OUTPUT = "tools/legacy_checksums.txt"


def sha256(path: str) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 16), b""):
            h.update(chunk)
    return h.hexdigest()


def main() -> None:
    lines = []
    for root, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d != "__pycache__"]
        for name in sorted(files):
            path = os.path.join(root, name).replace("\\", "/")
            if path.endswith(".zip"):
                continue
            lines.append(f"{sha256(path)}  {path}")
    lines.sort()

    zip_hash = sha256(os.path.join(ROOT, "pixel_sprite_studio_v6.zip"))
    header = [
        "# Checksum manifest of legacy-reference (source of truth, READ-ONLY).",
        "# Regenerate: python tools/fixturegen/make_legacy_manifest.py  |  Verify: any sha256 tool",
        f"# {zip_hash}  pixel_sprite_studio_v6.zip  (snapshot of the same tree)",
    ]
    with open(OUTPUT, "w", encoding="utf-8", newline="\n") as out:
        out.write("\n".join(header + lines) + "\n")
    print(f"manifest entries: {len(lines)}")


if __name__ == "__main__":
    main()
