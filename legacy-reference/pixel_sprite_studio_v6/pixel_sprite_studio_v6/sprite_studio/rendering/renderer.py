
from __future__ import annotations
from PIL import Image, ImageDraw, ImageOps
from sprite_studio.core.catalog import SKIN_TONES, HAIR_PALETTES, PALETTES
from sprite_studio.effects.library import ELEMENT_COLORS

W,H=32,46
OUTLINE=(22,16,30,255); INNER=(52,54,78,255); WHITE=(250,250,255,255)
GOLD=(246,200,84,255); GOLD_D=(196,140,48,255); BROWN=(128,82,64,255)
BLADE=(236,246,255,255); BLADE_D=(150,190,236,255)
EYE=(34,58,130,255); EYE_L=(150,200,255,255); BLUSH=(255,150,160,255); MOUTH=(214,84,88,255)

def blank(): return Image.new("RGBA",(W,H),(0,0,0,0))
def rr(d,x,y,w,h,c): d.rectangle([x,y,x+w-1,y+h-1],fill=c)
def ee(d,x0,y0,x1,y1,c): d.ellipse([x0,y0,x1,y1],fill=c)
def pp(d,x,y,c): d.point((x,y),fill=c)

def outline(img):
    src=img.load(); out=img.copy(); dst=out.load()
    for y in range(H):
        for x in range(W):
            if src[x,y][3]==0:
                for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    nx,ny=x+dx,y+dy
                    if 0<=nx<W and 0<=ny<H and src[nx,ny][3]>0:
                        dst[x,y]=OUTLINE; break
    return out

def recolor(img,c):
    out=blank(); s=img.load(); o=out.load()
    for y in range(H):
        for x in range(W):
            if s[x,y][3]>0:o[x,y]=c
    return out

class CharacterRenderer:
    def __init__(self):
        self.custom_overlays={}
        self.anchor_store=None

    def set_anchor_store(self,store):
        self.anchor_store=store

    def set_custom_overlay(self,slot,path):
        self.custom_overlays[slot]=path

    def clear_custom_overlays(self):
        self.custom_overlays.clear()

    def palette(self,s):
        return PALETTES.get(s.skin_variant, PALETTES["Mặc định"])

    def _dir_offset(self,s):
        if s.direction=="Left": return -1
        if s.direction=="Right": return 1
        return 0

    def body(self,s,p):
        i=blank(); d=ImageDraw.Draw(i)
        skin,shadow=SKIN_TONES[s.skin_tone]
        x,w=(12,8) if s.body_type=="Mảnh" else (10,12) if s.body_type=="Đậm" else (11,10)
        bob=p.body_dy
        dx=self._dir_offset(s) + p.body_dx
        # direction-specific torso silhouettes
        if s.direction=="Up":
            rr(d,x-1+dx,22+bob,w+2,4,shadow)
            rr(d,x+dx,26+bob,w,8,skin)
            rr(d,x+1+dx,34+bob,w-2,4,skin)
        elif s.direction=="Down":
            rr(d,x-1+dx,22+bob,w+2,5,skin)
            rr(d,x+dx,27+bob,w,7,skin)
            rr(d,x+1+dx,33+bob,w-2,4,skin)
            rr(d,x+1+dx,35+bob,w-2,2,shadow)
        else:
            # side view narrower silhouette
            rr(d,x+1+dx,22+bob,w-1,4,skin)
            rr(d,x+dx,26+bob,w-2,8,skin)
            rr(d,x+1+dx,34+bob,w-3,4,skin)
            rr(d,x+1+dx,35+bob,w-4,2,shadow)
        # chest highlight
        rr(d,x+2+dx,24+bob,max(2,w-4),1,(255,240,225,150))
        return i

    def head(self,s,p):
        i=blank(); d=ImageDraw.Draw(i)
        skin,shadow=SKIN_TONES[s.skin_tone]
        dx=self._dir_offset(s)+p.head_dx; dy=p.head_dy+p.body_dy
        if s.head_shape=="Gọn":
            ee(d,9+dx,7+dy,22+dx,21+dy,skin)
        else:
            ee(d,8+dx,6+dy,23+dx,21+dy,skin)
        if s.direction=="Up":
            rr(d,10+dx,7+dy,10,2,(255,240,225,140))
        else:
            rr(d,11+dx,20+dy,10,2,shadow)
        return i

    def face(self,s,p):
        i=blank(); d=ImageDraw.Draw(i)
        if s.direction=="Up": return i
        dx=self._dir_offset(s)+p.head_dx; dy=p.head_dy+p.body_dy

        if s.direction in ("Left","Right"):
            ex=12 if s.direction=="Left" else 18
            rr(d,ex+dx,14+dy,2,3,EYE); pp(d,ex+dx,14+dy,EYE_L)
            if s.nose_style=="Chấm": pp(d,16+dx,17+dy,(210,150,140,255))
            if s.mouth_style!="Nghiêm": rr(d,15+dx,19+dy,1,1,MOUTH)
            return i

        if s.eye_style=="Lạnh lùng":
            rr(d,11+dx,15+dy,2,2,EYE); rr(d,18+dx,15+dy,2,2,EYE)
        elif s.eye_style=="Buồn ngủ":
            rr(d,11+dx,15+dy,2,1,EYE); rr(d,18+dx,15+dy,2,1,EYE)
        else:
            rr(d,11+dx,14+dy,2,3,EYE); pp(d,11+dx,14+dy,EYE_L)
            rr(d,18+dx,14+dy,2,3,EYE); pp(d,18+dx,14+dy,EYE_L)

        rr(d,9+dx,18+dy,2,1,BLUSH); rr(d,21+dx,18+dy,2,1,BLUSH)
        if s.nose_style=="Chấm": pp(d,16+dx,17+dy,(210,150,140,255))
        if s.mouth_style=="Cười":
            pp(d,15+dx,19+dy,MOUTH); pp(d,16+dx,20+dy,MOUTH); pp(d,17+dx,19+dy,MOUTH)
        elif s.mouth_style=="Nghiêm":
            rr(d,15+dx,19+dy,3,1,(130,70,70,255))
        else:
            rr(d,15+dx,19+dy,2,1,MOUTH)
        return i

    def hair_back(self,s,p):
        i=blank(); d=ImageDraw.Draw(i)
        base,dark,light=HAIR_PALETTES[s.hair_color]
        sway=1 if p.hair_state=="right" else -1 if p.hair_state=="left" else 0
        bob=p.body_dy; dx=self._dir_offset(s)
        ee(d,7+dx,4+bob,24+dx,20+bob,dark)

        hs=s.hair_style
        if hs in ("Tóc dài","Tóc xõa","Tóc mái dài"):
            rr(d,6+dx+sway,12,3,15,dark); rr(d,23+dx+sway,12,3,15,dark)
        elif hs=="Đuôi ngựa":
            tail_x = 24 if s.direction!="Left" else 5
            rr(d,23+dx,11,3,9,dark); rr(d,tail_x+sway,18,3,8,dark)
        elif hs=="Hai búi":
            ee(d,5+dx,5+bob,10+dx,10+bob,dark); ee(d,21+dx,5+bob,26+dx,10+bob,dark)
        else:
            rr(d,6+dx,12,3,10,dark); rr(d,23+dx,12,3,10,dark)
        return i

    def hair_front(self,s,p):
        i=blank(); d=ImageDraw.Draw(i)
        base,dark,light=HAIR_PALETTES[s.hair_color]
        dx=self._dir_offset(s)+p.head_dx; dy=p.head_dy+p.body_dy
        hs=s.hair_style

        if s.direction=="Up":
            ee(d,7+dx,3+dy,24+dx,14+dy,base)
            rr(d,9+dx,5+dy,12,2,light)
        elif s.direction in ("Left","Right"):
            ee(d,7+dx,3+dy,24+dx,13+dy,base)
            if s.direction=="Left":
                rr(d,8+dx,8+dy,5,8,base); rr(d,18+dx,10+dy,2,4,base)
            else:
                rr(d,19+dx,8+dy,5,8,base); rr(d,12+dx,10+dy,2,4,base)
            rr(d,11+dx,5+dy,4,1,light)
        else:
            ee(d,7+dx,3+dy,24+dx,13+dy,base)
            if hs=="Tóc lệch":
                rr(d,8+dx,8+dy,6,8,base); rr(d,18+dx,9+dy,4,5,base)
            elif hs=="Tóc mái dài":
                rr(d,8+dx,8+dy,4,8,base); rr(d,19+dx,8+dy,4,8,base)
            elif hs=="Tóc dựng":
                rr(d,10+dx,7+dy,3,6,base); rr(d,15+dx,6+dy,3,7,base); rr(d,20+dx,8+dy,2,5,base)
            else:
                rr(d,8+dx,9+dy,3,7,base); rr(d,20+dx,9+dy,3,5,base); rr(d,12+dx,10+dy,3,4,base)
            rr(d,11+dx,5+dy,4,1,light); rr(d,10+dx,6+dy,2,1,light)

        if hs=="Búi cao":
            ee(d,13+dx,0+dy,18+dx,5+dy,base); rr(d,14+dx,5+dy,3,1,GOLD)
        elif hs=="Đuôi ngựa":
            ee(d,12+dx,1+dy,19+dx,6+dy,base)
        elif hs=="Hai búi":
            ee(d,7+dx,2+dy,12+dx,7+dy,base); ee(d,20+dx,2+dy,25+dx,7+dy,base)
        elif hs=="Tóc võ sĩ":
            rr(d,12+dx,1+dy,8,3,base)
        return i

    def arm(self,s,p,side):
        i=blank(); d=ImageDraw.Draw(i)
        skin,shadow=SKIN_TONES[s.skin_tone]
        st=p.left_arm if side=="left" else p.right_arm; bob=p.body_dy; dx=self._dir_offset(s)+p.body_dx
        if side=="left":
            pts={"up":[(11,23+bob),(9,20+bob),(7,17+bob)], "forward":[(11,23+bob),(9,25+bob),(7,27+bob)],
                 "back":[(11,23+bob),(10,27+bob),(9,30+bob)], "down":[(11,23+bob),(10,27+bob),(9,31+bob)]}[st]
        else:
            pts={"up":[(20,23+bob),(22,20+bob),(24,17+bob)], "forward":[(20,23+bob),(22,25+bob),(24,27+bob)],
                 "back":[(20,23+bob),(21,27+bob),(22,30+bob)], "down":[(20,23+bob),(21,27+bob),(22,31+bob)]}[st]
        pts=[(x+dx,y) for x,y in pts]
        (sx,sy),(ex,ey),(hx,hy)=pts
        arm_color = shadow if st=="back" else skin
        rr(d,min(sx,ex),min(sy,ey),3,5,arm_color)
        rr(d,min(ex,hx),min(ey,hy),3,5,arm_color)
        rr(d,hx,hy,3,2,skin)
        return i

    def leg(self,s,p,side):
        i=blank(); d=ImageDraw.Draw(i)
        skin,shadow=SKIN_TONES[s.skin_tone]
        x=12 if side=="left" else 18
        st=p.left_leg if side=="left" else p.right_leg
        dx=(-1 if st=="back" else 1 if st=="forward" else 0) + self._dir_offset(s)+p.body_dx
        bob=p.body_dy
        rr(d,x+dx,35+bob,3,6,skin); rr(d,x+dx-1,41+bob,4,2,shadow)
        return i

    def equipment_layers(self,s,p):
        robe,robe_d,acc,acc_d=self.palette(s)
        bob=p.body_dy; dx=self._dir_offset(s)+p.body_dx
        layers=[]

        def mk(name):
            im=blank(); return name,im,ImageDraw.Draw(im)

        # cape behind
        if s.equipment["cape"]!="Không":
            name,im,d=mk("cape")
            long="dài" in s.equipment["cape"].lower()
            width=14 if s.direction in ("Down","Up") else 12
            rr(d,9+dx,23+bob,width,15 if long else 10,robe_d)
            if s.direction=="Up": rr(d,8+dx,22+bob,width+2,16 if long else 11,robe_d)
            layers.append((name,im))

        if s.equipment["pants"]!="Không":
            name,im,d=mk("pants")
            c=robe if s.equipment["pants"]=="Quần sáng" else robe_d
            rr(d,11+dx,34+bob,4,7,c); rr(d,17+dx,34+bob,4,7,c)
            layers.append((name,im))

        if s.equipment["boots"]!="Không":
            name,im,d=mk("boots")
            c=(75,66,92,255)
            if s.equipment["boots"]=="Ủng giáp": c=(130,135,155,255)
            elif s.equipment["boots"]=="Giày da": c=(88,62,52,255)
            rr(d,10+dx,39+bob,5,4,c); rr(d,17+dx,39+bob,5,4,c)
            layers.append((name,im))

        if s.equipment["inner"]!="Không":
            name,im,d=mk("inner")
            c=WHITE if s.equipment["inner"]=="Áo trong sáng" else INNER
            rr(d,11+dx,22+bob,10,10,c)
            layers.append((name,im))

        if s.equipment["outer"]!="Không":
            name,im,d=mk("outer")
            kind=s.equipment["outer"]
            rr(d,10+dx,24+bob,12,9,robe); rr(d,10+dx,31+bob,12,6,robe)
            if kind=="Kiếm tu":
                rr(d,10+dx,28+bob,12,2,acc); rr(d,13+dx,32+bob,2,5,robe_d); rr(d,18+dx,32+bob,2,5,robe_d)
            elif kind=="Đan tu":
                rr(d,9+dx,31+bob,14,6,robe); rr(d,12+dx,25+bob,8,2,acc)
                rr(d,9+dx,24+bob,2,11,acc_d)  # sash
            elif kind=="Phù tu":
                rr(d,14+dx,25+bob,3,5,(246,220,130,255)); pp(d,15+dx,27+bob,(190,80,60,255))
                rr(d,11+dx,24+bob,1,12,acc_d)
            elif kind=="Thể tu":
                rr(d,11+dx,24+bob,10,7,robe_d); rr(d,10+dx,31+bob,12,3,acc_d)
                rr(d,10+dx,24+bob,2,8,acc)
                rr(d,20+dx,24+bob,2,8,acc)
            elif kind=="Du hiệp":
                rr(d,10+dx,24+bob,12,2,acc_d); rr(d,11+dx,30+bob,10,2,acc)
                rr(d,9+dx,28+bob,3,7,robe_d)
            layers.append((name,im))

        if s.equipment["chest_armor"]!="Không":
            name,im,d=mk("chest_armor")
            c=(120,130,155,255) if "nhẹ" in s.equipment["chest_armor"] else (92,100,125,255)
            rr(d,12+dx,24+bob,8,6,c); rr(d,13+dx,25+bob,6,3,(170,180,205,255))
            layers.append((name,im))

        if s.equipment["shoulder"]!="Không":
            name,im,d=mk("shoulder")
            c=(150,155,180,255) if "nhẹ" in s.equipment["shoulder"] else (105,110,140,255)
            rr(d,8+dx,23+bob,5,3,c); rr(d,20+dx,23+bob,5,3,c)
            layers.append((name,im))

        if s.equipment["gloves"]!="Không":
            name,im,d=mk("gloves")
            c=(96,82,70,255) if "vải" in s.equipment["gloves"] else (120,125,150,255)
            rr(d,7+dx,30+bob,4,3,c); rr(d,22+dx,30+bob,4,3,c)
            layers.append((name,im))

        if s.equipment["belt"]!="Không":
            name,im,d=mk("belt")
            c=acc if s.equipment["belt"]!="Đai giáp" else (140,145,165,255)
            rr(d,11+dx,29+bob,10,2,c); rr(d,15+dx,29+bob,2,2,GOLD)
            if s.equipment["belt"]=="Đai ngọc": rr(d,16+dx,31+bob,1,2,(100,230,210,255))
            layers.append((name,im))

        if s.equipment["head"]!="Không":
            name,im,d=mk("head_equipment")
            item=s.equipment["head"]
            if item=="Băng trán":
                rr(d,8+dx,10+bob,15,1,WHITE)
            elif item=="Mũ vải":
                rr(d,9+dx,4+bob,14,4,robe)
            elif item=="Mũ giáp":
                rr(d,9+dx,4+bob,14,4,(120,125,150,255)); rr(d,11+dx,2+bob,10,2,(160,165,185,255))
            layers.append((name,im))

        if s.equipment["off_hand"]!="Không":
            name,im,d=mk("off_hand")
            item=s.equipment["off_hand"]
            if item=="Khiên":
                ee(d,23+dx,24+bob,29+dx,32+bob,(120,125,150,255))
            elif item=="Phù":
                rr(d,24+dx,24+bob,3,5,(246,220,130,255)); pp(d,25+dx,26+bob,(190,80,60,255))
            elif item=="Hồ lô":
                ee(d,23+dx,27+bob,27+dx,31+bob,(190,140,70,255)); rr(d,24+dx,25+bob,2,3,(170,110,50,255))
            layers.append((name,im))

        if s.equipment["accessory"]!="Không":
            name,im,d=mk("accessory")
            a=s.equipment["accessory"]
            if a=="Ngọc bội":
                rr(d,20+dx,31+bob,2,2,(100,230,210,255))
            elif a=="Khuyên tai":
                pp(d,8+dx,16+bob,GOLD); pp(d,23+dx,16+bob,GOLD)
            elif a=="Hồ lô":
                ee(d,23+dx,27+bob,27+dx,31+bob,(190,140,70,255)); rr(d,24+dx,25+bob,2,3,(170,110,50,255))
            layers.append((name,im))

        for slot,path in self.custom_overlays.items():
            try:
                im=Image.open(path).convert("RGBA")
                if im.size!=(W,H): im=im.resize((W,H),Image.Resampling.NEAREST)
                layers.append((f"custom_{slot}",im))
            except Exception:
                pass

        return layers

    def weapon(self,s,p):
        i=blank(); d=ImageDraw.Draw(i); w=s.equipment["main_hand"]
        if w=="Không": return i
        dx=self._dir_offset(s)+p.body_dx
        tilt = -1 if s.direction=="Left" else 1 if s.direction=="Right" else 0

        if p.weapon_state=="slash":
            base_x=4+dx
            for n in range(9):
                rr(d,base_x+n,9+n,2,2,BLADE_D); pp(d,base_x+n,9+n,BLADE)
            rr(d,13+dx,18,4,2,GOLD_D); rr(d,16+dx,19,2,5,BROWN); return i

        if p.weapon_state=="raise":
            rr(d,5+dx,6,2,18,BLADE_D); rr(d,5+dx,6,1,18,BLADE)
            rr(d,4+dx,23,4,2,GOLD_D); rr(d,5+dx,25,2,5,BROWN); return i

        if w=="Kiếm":
            rr(d,3+dx,10,2,19,BLADE_D); rr(d,3+dx,10,1,19,BLADE); rr(d,1+dx,29,6,2,GOLD_D); rr(d,3+dx,31,2,4,BROWN)
        elif w=="Đao":
            rr(d,3+dx,10,3,17,BLADE_D); rr(d,3+dx,10,1,17,BLADE); rr(d,2+dx,27,5,2,GOLD_D); rr(d,4+dx,29,2,5,BROWN)
        elif w=="Thương":
            rr(d,3+dx,6,1,30,BROWN); rr(d,2+dx,4,3,3,BLADE_D); pp(d,3+dx,3,BLADE)
        elif w=="Quạt":
            d.polygon([(3+dx,25),(8+dx,20),(9+dx,27)],fill=WHITE)
            d.line([(3+dx,25),(9+dx,27)],fill=GOLD_D); d.line([(3+dx,25),(8+dx,20)],fill=GOLD_D)
        elif w=="Trượng":
            rr(d,3+dx,7,1,29,BROWN); ee(d,1+dx,3,5+dx,7,GOLD); ee(d,2+dx,4,4+dx,6,(120,200,255,255))
        return i

    def effects(self,s,p):
        i=blank(); d=ImageDraw.Draw(i)
        c=ELEMENT_COLORS.get(s.element,(255,255,255,180))
        if s.aura!="Không":
            ac=ELEMENT_COLORS.get(s.aura,c)
            for x,y in [(5,16),(26,14),(4,28),(27,30),(8,37),(23,36)]:
                rr(d,x,y,2,2,ac)
        if s.effect=="Glow":
            for x,y in [(10,10),(22,12),(8,30),(24,32)]:
                ee(d,x,y,x+2,y+2,c)
        elif s.effect=="Spark":
            for x,y in [(5,8),(27,18),(6,35),(25,6)]:
                pp(d,x,y,c); pp(d,x+1,y,c); pp(d,x,y+1,c)
        elif s.effect=="Burst":
            for x,y in [(3,20),(27,20),(16,3),(16,40)]:
                rr(d,x,y,2,3,c)
        elif s.effect=="Slash":
            for n in range(7): pp(d,19+n,14+n,c)
        elif s.effect=="Heal":
            rr(d,15,12,2,8,c); rr(d,12,15,8,2,c)
            for x,y in [(9,18),(23,20),(12,28),(20,29)]: ee(d,x,y,x+2,y+2,c)
        elif s.effect=="Shield":
            ee(d,7,8,25,39,(c[0],c[1],c[2],80))
        return i

    def anchor_debug(self,s):
        i=blank(); d=ImageDraw.Draw(i)
        if not self.anchor_store: return i
        cols={"head":(255,80,80,255),"chest":(80,160,255,255),"left_hand":(80,220,120,255),"right_hand":(80,220,120,255),"left_foot":(180,90,240,255),"right_foot":(180,90,240,255),"weapon":(255,255,255,255),"accessory":(255,110,210,255)}
        for name,c in cols.items():
            x,y=self.anchor_store.get(s.direction,name); rr(d,x-1,y,3,1,c); rr(d,x,y-1,1,3,c)
        return i

    def render_layers(self,s,p):
        weapon=self.weapon(s,p)
        front_weapon=weapon if p.weapon_state in ("raise","slash") else blank()
        back_weapon=blank() if p.weapon_state in ("raise","slash") else weapon
        layers=[
            ("effect_back",self.effects(s,p)),
            ("hair_back",self.hair_back(s,p)),
            ("weapon_back",back_weapon),
            ("left_leg",self.leg(s,p,"left")),
            ("right_leg",self.leg(s,p,"right")),
            ("body",self.body(s,p)),
        ]
        layers.extend(self.equipment_layers(s,p))
        layers.extend([
            ("left_arm",self.arm(s,p,"left")),
            ("right_arm",self.arm(s,p,"right")),
            ("head",self.head(s,p)),
            ("face",self.face(s,p)),
            ("hair_front",self.hair_front(s,p)),
            ("weapon_front",front_weapon),
        ])
        return layers

    def render(self,s,p,mode="Normal"):
        debug={
            "hair_back":(255,80,80,255),"hair_front":(255,80,80,255),
            "body":(70,130,255,255),"head":(255,220,80,255),"face":(255,240,140,255),
            "left_arm":(80,220,120,255),"right_arm":(80,220,120,255),
            "left_leg":(180,90,240,255),"right_leg":(180,90,240,255),
            "weapon_back":(255,255,255,255),"weapon_front":(255,255,255,255),
        }
        out=blank()
        for name,layer in self.render_layers(s,p):
            if mode=="Part Debug":
                layer=recolor(layer,debug.get(name,(80,220,220,255)))
            out.alpha_composite(layer)
        if mode!="Part Debug":
            out=outline(out)
        if mode=="Anchor Debug": out.alpha_composite(self.anchor_debug(s))
        if mode=="Silhouette":
            out=recolor(out,(0,0,0,255))
        elif mode=="Grayscale":
            a=out.getchannel("A")
            g=ImageOps.grayscale(out.convert("RGB")).convert("RGBA")
            g.putalpha(a); out=g
        return out
