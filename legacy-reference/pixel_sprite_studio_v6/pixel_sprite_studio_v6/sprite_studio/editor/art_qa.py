from PIL import ImageOps
def analyze_sprite(img):
    rgba=img.convert("RGBA")
    colors=rgba.getcolors(maxcolors=100000) or []
    opaque=[(n,c) for n,c in colors if c[3]>0]
    alpha=rgba.getchannel("A")
    bbox=alpha.getbbox()
    visible=[bbox[2]-bbox[0],bbox[3]-bbox[1]] if bbox else [0,0]
    gray=ImageOps.grayscale(rgba.convert("RGB")); ap=alpha.load(); gp=gray.load(); vals=[]
    for y in range(rgba.height):
        for x in range(rgba.width):
            if ap[x,y]>0: vals.append(gp[x,y])
    vr=max(vals)-min(vals) if vals else 0
    warns=[]
    if len(opaque)>24: warns.append(f"Palette đang có {len(opaque)} màu; nên cân nhắc giảm.")
    if vr<90: warns.append("Độ tương phản sáng/tối thấp; sprite có thể khó đọc ở native 1x.")
    if visible[0]>30: warns.append("Silhouette gần chạm biên ngang.")
    if visible[1]>44: warns.append("Silhouette gần chạm biên dọc.")
    return {"palette_count":len(opaque),"visible_size":visible,"value_range":vr,"warnings":warns}
