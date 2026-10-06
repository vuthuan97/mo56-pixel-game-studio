import sys, json
from pathlib import Path
sys.path.insert(0, r"D:\1.VIBECODE\PIXEL_GAME_STUDIO\legacy-reference\pixel_sprite_studio_v6\pixel_sprite_studio_v6")
from PIL import Image, ImageDraw, ImageOps
from sprite_studio.core.models import CharacterSpec
from sprite_studio.animation.poses import POSES
from sprite_studio.rendering.renderer import CharacterRenderer

out = Path(__file__).parent / "_probe"
out.mkdir(exist_ok=True)

# 1) Does Pillow 12 still reproduce the shipped sample? (default spec, idle_0, Down)
r = CharacterRenderer()
spec = CharacterSpec()
img = r.render(spec, POSES["idle_0"])
sample = Image.open(r"D:\1.VIBECODE\PIXEL_GAME_STUDIO\legacy-reference\pixel_sprite_studio_v6\pixel_sprite_studio_v6\samples\sample_package_v6\animations\Down\idle\idle_0.png").convert("RGBA")
diff = sum(1 for a, b in zip(img.getdata(), sample.getdata()) if a != b)
print("sample diff pixels:", diff, "/", img.width * img.height)

# 2) ellipse pixel extents for various inclusive bboxes
cases = [(0,0,0,0),(0,0,1,1),(0,0,2,2),(0,0,3,3),(0,0,4,4),(0,0,5,5),(0,0,6,6),(0,0,7,7),(0,0,8,8),
         (0,0,14,15),(0,0,15,14),(0,0,13,13),(0,0,9,14),(0,0,14,9),(0,0,10,11),(0,0,11,10),
         (2,3,16,20),(1,1,12,12),(0,0,17,13)]
ell = {}
for b in cases:
    im = Image.new("RGBA", (24, 24), (0,0,0,0))
    d = ImageDraw.Draw(im)
    d.ellipse(b, fill=(255,0,0,255))
    px = im.load()
    rows = {}
    for y in range(24):
        xs = [x for x in range(24) if px[x,y][3] > 0]
        if xs: rows[y] = [min(xs), max(xs)]
    ell[str(b)] = rows
(out / "ellipse.json").write_text(json.dumps(ell), encoding="utf-8")
print("ellipse cases:", len(ell))

# 3) rectangle inclusivity + line + polygon
im = Image.new("RGBA",(8,8),(0,0,0,0)); d = ImageDraw.Draw(im)
d.rectangle([1,1,3,4], fill=(255,255,255,255))
rect_px = [(x,y) for y in range(8) for x in range(8) if im.getpixel((x,y))[3]>0]
print("rect [1,1,3,4]:", rect_px)

im = Image.new("RGBA",(12,12),(0,0,0,0)); d = ImageDraw.Draw(im)
d.line([(2,2),(7,5)], fill=(255,255,255,255), width=1)
line1 = [(x,y) for y in range(12) for x in range(12) if im.getpixel((x,y))[3]>0]
d.line([(9,2),(9,8)], fill=(255,255,255,255), width=1)
line2 = [(x,y) for y in range(12) for x in range(12) if im.getpixel((x,y))[3]>0]
print("line diag:", line1)
print("line v after:", [(x,y) for x,y in line2 if (x,y) not in line1])

im = Image.new("RGBA",(16,16),(0,0,0,0)); d = ImageDraw.Draw(im)
d.polygon([(3,10),(8,5),(9,12)], fill=(255,255,255,255))
poly = [(x,y) for y in range(16) for x in range(16) if im.getpixel((x,y))[3]>0]
print("polygon tri:", poly)
(out / "primitives.json").write_text(json.dumps({"rect":rect_px,"line_diag":line1,"polygon":poly}), encoding="utf-8")

# 4) alpha_composite rounding table
def comp(base, over):
    b = Image.new("RGBA",(1,1),base); o = Image.new("RGBA",(1,1),over)
    b.alpha_composite(o)
    return b.getpixel((0,0))
tests = []
for ba in (0,1,17,64,128,200,254,255):
    for oa in (0,1,37,128,200,255):
        base=(10,20,30,ba); over=(200,150,100,oa)
        tests.append({"base":base,"over":over,"out":comp(base,over)})
(out / "composite.json").write_text(json.dumps(tests), encoding="utf-8")
print("composite cases:", len(tests))

# 5) grayscale (ImageOps.grayscale on RGB, keep alpha)
strip = Image.new("RGBA",(16,1),(0,0,0,0)); d = ImageDraw.Draw(strip)
cols = [(0,0,0,255),(1,2,3,255),(34,58,130,255),(128,82,64,255),(196,140,48,255),(250,250,255,255),(255,255,255,255),(22,16,30,255),(255,240,225,150)]
for i,c in enumerate(cols): d.point((i,0), fill=c)
g = ImageOps.grayscale(strip.convert("RGB")).convert("RGBA"); g.putalpha(strip.getchannel("A"))
vals = [(c, g.getpixel((i,0))) for i,c in enumerate(cols)]
print("grayscale:")
for c,v in vals: print("  ", c, "->", v)
(out / "grayscale.json").write_text(json.dumps([[list(c), list(v)] for c,v in vals]), encoding="utf-8")

# 6) NEAREST resize mapping probes: which src px does each dst px come from
probe = Image.new("RGBA",(5,3),(0,0,0,0)); d = ImageDraw.Draw(probe)
for y in range(3):
    for x in range(5):
        d.point((x,y), fill=(x*40, y*80, 0, 255))
for (w,h) in [(10,6),(13,8),(50,30),(7,4),(3,2)]:
    up = probe.resize((w,h), Image.Resampling.NEAREST)
    m = []
    for y in range(h):
        m.append([up.getpixel((x,y))[0]//40 for x in range(w)])
    print(f"resize 5x3 -> {w}x{h} srcX map:", m)
