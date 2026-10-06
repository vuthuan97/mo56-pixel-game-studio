
from pathlib import Path
import tkinter as tk
from tkinter import ttk, filedialog, messagebox
from PIL import Image, ImageTk, ImageDraw
import random

from sprite_studio.core.models import CharacterSpec, SkillSpec, AIConfig
from sprite_studio.core.catalog import BODY_OPTIONS, EQUIPMENT_LIBRARY, SLOT_LABELS, GAME_BG, CLASS_TEMPLATES
from sprite_studio.animation.poses import POSES, ANIMATIONS, CLASS_ANIMATION_MAP
from sprite_studio.rendering.renderer import CharacterRenderer
from sprite_studio.exporting.exporter import Exporter
from sprite_studio.ai.config import save_ai_config, load_ai_config
from sprite_studio.editor.pose_editor import PoseOverrideStore, resolve_pose
from sprite_studio.editor.anchors import AnchorStore
from sprite_studio.editor.art_qa import analyze_sprite
from sprite_studio.assets.library import AssetLibrary
from sprite_studio.godot.exporter import export_godot_spriteframes

class SpriteStudioApp:
    def __init__(self,root):
        self.root=root
        root.title("Pixel Sprite Studio v5")
        root.geometry("1440x880")
        root.minsize(1280,780)

        self.spec=CharacterSpec()
        self.skill=SkillSpec()
        self.ai=AIConfig()
        self.renderer=CharacterRenderer()
        self.exporter=Exporter(self.renderer)
        self.pose_overrides=PoseOverrideStore()
        self.anchors=AnchorStore()
        self.renderer.set_anchor_store(self.anchors)
        self.exporter.set_pose_overrides(self.pose_overrides)
        self.asset_library=AssetLibrary(Path(__file__).resolve().parents[2]/"asset_library")

        self.vars={k:tk.StringVar(value=str(v)) for k,v in self.spec.to_dict().items() if k!="equipment"}
        self.eq_vars={k:tk.StringVar(value=v) for k,v in self.spec.equipment.items()}

        self.pose_var=tk.StringVar(value="idle_0")
        self.anim_var=tk.StringVar(value="idle")
        self.preview_mode=tk.StringVar(value="Normal")
        self.preview_scale=tk.StringVar(value="10x")
        self.game_bg=tk.StringVar(value="Cỏ")
        self.game_preview=tk.BooleanVar(value=False)
        self.onion=tk.BooleanVar(value=False)
        self.fps=tk.IntVar(value=6)
        self.playing=False
        self.play_index=0
        self.after_id=None

        self.anim_export={a:tk.BooleanVar(value=(a in ["idle","walk","attack","hurt","cast","death"])) for a in ANIMATIONS}
        self.dir_export={d:tk.BooleanVar(value=True) for d in ["Down","Up","Left","Right"]}
        self.include_layers=tk.BooleanVar(value=False)

        self.skill_vars={
            "skill_id":tk.StringVar(value=self.skill.skill_id),
            "name":tk.StringVar(value=self.skill.name),
            "animation":tk.StringVar(value=self.skill.animation),
            "effect":tk.StringVar(value=self.skill.effect),
            "element":tk.StringVar(value=self.skill.element),
            "hit_frame":tk.IntVar(value=self.skill.hit_frame),
        }
        self.ai_vars={
            "provider":tk.StringVar(value=self.ai.provider),
            "model":tk.StringVar(value=self.ai.model),
            "base_url":tk.StringVar(value=self.ai.base_url),
            "api_key_env":tk.StringVar(value=self.ai.api_key_env),
        }
        self.timeline_vars={"body_dx":tk.IntVar(value=0),"body_dy":tk.IntVar(value=0),"head_dx":tk.IntVar(value=0),"head_dy":tk.IntVar(value=0),"left_arm":tk.StringVar(value="down"),"right_arm":tk.StringVar(value="down"),"left_leg":tk.StringVar(value="neutral"),"right_leg":tk.StringVar(value="neutral"),"hair_state":tk.StringVar(value="still"),"weapon_state":tk.StringVar(value="idle")}
        self.anchor_name=tk.StringVar(value="head"); self.anchor_x=tk.IntVar(value=16); self.anchor_y=tk.IntVar(value=12)
        self.library_slot=tk.StringVar(value="outer")

        self.photo=None
        self._build()
        self.apply_class_template(initial=True)
        self.refresh()

    def combo(self,parent,label,var,values,refresh=True):
        ttk.Label(parent,text=label).pack(anchor="w")
        cb=ttk.Combobox(parent,textvariable=var,values=values,state="readonly")
        cb.pack(fill="x",pady=(0,6))
        if refresh: cb.bind("<<ComboboxSelected>>",lambda e:self.refresh())
        return cb

    def _build(self):
        outer=ttk.Frame(self.root,padding=8)
        outer.pack(fill="both",expand=True)

        left=ttk.Frame(outer)
        left.pack(side="left",fill="y")

        nb=ttk.Notebook(left,width=440,height=780)
        nb.pack(fill="y")

        # Body
        f=ttk.Frame(nb,padding=10); nb.add(f,text="Cơ thể")
        ttk.Label(f,text="v5: nâng chất lượng body/hair/equipment + class motion.",foreground="#555").pack(anchor="w",pady=(0,8))
        ttk.Label(f,text="Tên").pack(anchor="w")
        ttk.Entry(f,textvariable=self.vars["name"]).pack(fill="x",pady=(0,8))
        class_cb=self.combo(f,"Class",self.vars["class_type"],BODY_OPTIONS["class_type"],refresh=False)
        class_cb.bind("<<ComboboxSelected>>",lambda e:self.apply_class_template())
        for label,key in [
            ("Giới tính","gender"),("Dáng người","body_type"),("Màu da","skin_tone"),
            ("Dáng đầu","head_shape"),("Mắt","eye_style"),("Mũi","nose_style"),
            ("Miệng","mouth_style"),("Kiểu tóc","hair_style"),("Màu tóc","hair_color"),
            ("Hướng top-down","direction"),("Theme/Skin","skin_variant")
        ]:
            self.combo(f,label,self.vars[key],BODY_OPTIONS[key])
        ttk.Button(f,text="Áp dụng class template",command=self.apply_class_template).pack(fill="x",pady=(8,2))
        ttk.Button(f,text="Preview class animation",command=self.select_class_animation).pack(fill="x",pady=2)

        # Equipment
        f=ttk.Frame(nb,padding=10); nb.add(f,text="Trang bị")
        ttk.Label(f,text="'Không' = không mặc / không trang bị.",foreground="#555").pack(anchor="w",pady=(0,8))
        c=tk.Canvas(f,height=640,highlightthickness=0)
        sb=ttk.Scrollbar(f,orient="vertical",command=c.yview)
        inner=ttk.Frame(c)
        inner.bind("<Configure>",lambda e:c.configure(scrollregion=c.bbox("all")))
        c.create_window((0,0),window=inner,anchor="nw")
        c.configure(yscrollcommand=sb.set)
        c.pack(side="left",fill="both",expand=True); sb.pack(side="right",fill="y")
        for slot,label in SLOT_LABELS.items():
            self.combo(inner,label,self.eq_vars[slot],EQUIPMENT_LIBRARY[slot])
        ttk.Separator(inner).pack(fill="x",pady=8)
        ttk.Button(inner,text="Cởi toàn bộ",command=self.unequip_all).pack(fill="x",pady=2)
        ttk.Button(inner,text="Random trang bị",command=self.random_equipment).pack(fill="x",pady=2)
        ttk.Button(inner,text="Import PNG overlay 32x46",command=self.import_overlay).pack(fill="x",pady=2)
        ttk.Button(inner,text="Xóa custom overlay",command=self.clear_overlay).pack(fill="x",pady=2)

        # Animation
        f=ttk.Frame(nb,padding=10); nb.add(f,text="Animation")
        self.combo(f,"Animation",self.anim_var,list(ANIMATIONS),refresh=False)
        ttk.Label(f,text="Pose/frame").pack(anchor="w")
        pcb=ttk.Combobox(f,textvariable=self.pose_var,values=list(POSES),state="readonly")
        pcb.pack(fill="x",pady=(0,6)); pcb.bind("<<ComboboxSelected>>",lambda e:self.refresh())
        row=ttk.Frame(f); row.pack(fill="x",pady=4)
        ttk.Button(row,text="◀",command=self.prev_frame).pack(side="left",expand=True,fill="x",padx=(0,2))
        ttk.Button(row,text="▶",command=self.next_frame).pack(side="left",expand=True,fill="x",padx=2)
        ttk.Button(row,text="Play/Stop",command=self.toggle_play).pack(side="left",expand=True,fill="x",padx=(2,0))
        ttk.Label(f,text="FPS").pack(anchor="w")
        ttk.Spinbox(f,from_=1,to=24,textvariable=self.fps,width=8).pack(anchor="w",pady=(0,6))
        ttk.Checkbutton(f,text="Onion skin frame trước",variable=self.onion,command=self.refresh).pack(anchor="w")
        ttk.Separator(f).pack(fill="x",pady=8)
        ttk.Label(f,text="Animation cần export").pack(anchor="w")
        for a,v in self.anim_export.items():
            ttk.Checkbutton(f,text=a,variable=v).pack(anchor="w")

        # Effect/Skill
        f=ttk.Frame(nb,padding=10); nb.add(f,text="Effect / Skill")
        self.combo(f,"Aura",self.vars["aura"],BODY_OPTIONS["aura"])
        self.combo(f,"Effect",self.vars["effect"],BODY_OPTIONS["effect"])
        self.combo(f,"Element",self.vars["element"],BODY_OPTIONS["element"])
        ttk.Separator(f).pack(fill="x",pady=8)
        ttk.Label(f,text="Skill ID").pack(anchor="w"); ttk.Entry(f,textvariable=self.skill_vars["skill_id"]).pack(fill="x",pady=(0,6))
        ttk.Label(f,text="Tên skill").pack(anchor="w"); ttk.Entry(f,textvariable=self.skill_vars["name"]).pack(fill="x",pady=(0,6))
        self.combo(f,"Animation skill",self.skill_vars["animation"],list(ANIMATIONS),refresh=False)
        self.combo(f,"Effect skill",self.skill_vars["effect"],BODY_OPTIONS["effect"],refresh=False)
        self.combo(f,"Element skill",self.skill_vars["element"],BODY_OPTIONS["element"],refresh=False)
        ttk.Label(f,text="Hit frame").pack(anchor="w"); ttk.Spinbox(f,from_=0,to=20,textvariable=self.skill_vars["hit_frame"]).pack(anchor="w")

        # AI
        f=ttk.Frame(nb,padding=10); nb.add(f,text="AI Config")
        ttk.Label(f,text="Scaffold AI config.",foreground="#555").pack(anchor="w",pady=(0,8))
        for label,key in [("Provider","provider"),("Model","model"),("Base URL","base_url"),("API key env","api_key_env")]:
            ttk.Label(f,text=label).pack(anchor="w"); ttk.Entry(f,textvariable=self.ai_vars[key]).pack(fill="x",pady=(0,6))
        ttk.Label(f,text="Prompt template").pack(anchor="w")
        self.ai_prompt=tk.Text(f,height=10,wrap="word")
        self.ai_prompt.pack(fill="both",expand=True)
        self.ai_prompt.insert("1.0",self.ai.prompt_template)
        ttk.Button(f,text="Lưu AI config",command=self.save_ai).pack(fill="x",pady=(6,2))
        ttk.Button(f,text="Mở AI config",command=self.load_ai).pack(fill="x",pady=2)

        # Timeline editor
        f=ttk.Frame(nb,padding=10); nb.add(f,text="Timeline")
        ttk.Label(f,text="Chỉnh pose/frame hiện tại",foreground="#555").pack(anchor="w",pady=(0,8))
        for label,key in [("Body X","body_dx"),("Body Y","body_dy"),("Head X","head_dx"),("Head Y","head_dy")]:
            row=ttk.Frame(f); row.pack(fill="x",pady=2); ttk.Label(row,text=label,width=12).pack(side="left"); ttk.Spinbox(row,from_=-6,to=6,textvariable=self.timeline_vars[key],width=8).pack(side="left")
        for label,key,vals in [("Tay trái","left_arm",["down","up","forward","back"]),("Tay phải","right_arm",["down","up","forward","back"]),("Chân trái","left_leg",["neutral","forward","back"]),("Chân phải","right_leg",["neutral","forward","back"]),("Tóc","hair_state",["still","left","right"]),("Vũ khí","weapon_state",["idle","ready","raise","slash","recover","back"])]: self.combo(f,label,self.timeline_vars[key],vals,refresh=False)
        ttk.Button(f,text="Load pose vào editor",command=self.load_pose_to_editor).pack(fill="x",pady=(8,2)); ttk.Button(f,text="Áp dụng override",command=self.apply_timeline_override).pack(fill="x",pady=2); ttk.Button(f,text="Reset override pose",command=self.reset_pose_override).pack(fill="x",pady=2); ttk.Button(f,text="Lưu overrides JSON",command=self.save_pose_overrides).pack(fill="x",pady=(8,2)); ttk.Button(f,text="Mở overrides JSON",command=self.load_pose_overrides).pack(fill="x",pady=2)

        # Anchor editor
        f=ttk.Frame(nb,padding=10); nb.add(f,text="Anchors")
        self.combo(f,"Anchor",self.anchor_name,["head","chest","left_hand","right_hand","left_foot","right_foot","weapon","accessory"],refresh=False)
        row=ttk.Frame(f); row.pack(fill="x",pady=4); ttk.Label(row,text="X").pack(side="left"); ttk.Spinbox(row,from_=0,to=31,textvariable=self.anchor_x,width=8).pack(side="left",padx=(4,12)); ttk.Label(row,text="Y").pack(side="left"); ttk.Spinbox(row,from_=0,to=45,textvariable=self.anchor_y,width=8).pack(side="left",padx=4)
        ttk.Button(f,text="Load anchor",command=self.load_anchor).pack(fill="x",pady=2); ttk.Button(f,text="Apply anchor",command=self.apply_anchor).pack(fill="x",pady=2); ttk.Button(f,text="Anchor Debug preview",command=self.preview_anchor_debug).pack(fill="x",pady=2); ttk.Button(f,text="Reset anchors",command=self.reset_anchors).pack(fill="x",pady=(8,2)); ttk.Button(f,text="Lưu anchors JSON",command=self.save_anchors).pack(fill="x",pady=2); ttk.Button(f,text="Mở anchors JSON",command=self.load_anchors).pack(fill="x",pady=2)

        # Asset library
        f=ttk.Frame(nb,padding=10); nb.add(f,text="Asset Library")
        self.combo(f,"Slot library",self.library_slot,list(EQUIPMENT_LIBRARY),refresh=False); self.asset_list=tk.Listbox(f,height=18); self.asset_list.pack(fill="both",expand=True,pady=(4,6)); ttk.Button(f,text="Refresh library",command=self.refresh_asset_library).pack(fill="x",pady=2); ttk.Button(f,text="Import PNG vào library",command=self.import_library_asset).pack(fill="x",pady=2); ttk.Button(f,text="Apply asset đã chọn",command=self.apply_library_asset).pack(fill="x",pady=2)

        # Art QA
        f=ttk.Frame(nb,padding=10); nb.add(f,text="Art QA")
        self.qa_text=tk.Text(f,height=20,wrap="word"); self.qa_text.pack(fill="both",expand=True); ttk.Button(f,text="Analyze current sprite",command=self.run_art_qa).pack(fill="x",pady=4)

        # Export
        f=ttk.Frame(nb,padding=10); nb.add(f,text="Export")
        ttk.Label(f,text="Hướng cần xuất").pack(anchor="w")
        for d,v in self.dir_export.items():
            ttk.Checkbutton(f,text=d,variable=v).pack(anchor="w")
        ttk.Checkbutton(f,text="Xuất từng layer PNG",variable=self.include_layers).pack(anchor="w",pady=(8,4))
        ttk.Separator(f).pack(fill="x",pady=8)
        ttk.Button(f,text="Save character JSON",command=self.save_character).pack(fill="x",pady=3)
        ttk.Button(f,text="Load character JSON",command=self.load_character).pack(fill="x",pady=3)
        ttk.Button(f,text="Validate",command=self.validate).pack(fill="x",pady=3)
        ttk.Button(f,text="Xuất pose hiện tại",command=self.export_pose).pack(fill="x",pady=(10,3))
        ttk.Button(f,text="Xuất full sprite package",command=self.export_package).pack(fill="x",pady=3)
        ttk.Button(f,text="Xuất package + Godot",command=self.export_package_godot).pack(fill="x",pady=3)

        # Preview
        right=ttk.Frame(outer)
        right.pack(side="right",fill="both",expand=True,padx=(10,0))

        bar=ttk.Frame(right); bar.pack(fill="x")
        ttk.Label(bar,text="Preview").pack(side="left")
        m=ttk.Combobox(bar,textvariable=self.preview_mode,values=["Normal","Native 1x","Grayscale","Silhouette","Part Debug","Anchor Debug"],state="readonly",width=16)
        m.pack(side="left",padx=6); m.bind("<<ComboboxSelected>>",lambda e:self.refresh())
        ttk.Label(bar,text="Scale").pack(side="left")
        sc=ttk.Combobox(bar,textvariable=self.preview_scale,values=["1x","2x","4x","10x"],state="readonly",width=6)
        sc.pack(side="left",padx=6); sc.bind("<<ComboboxSelected>>",lambda e:self.refresh())
        ttk.Checkbutton(bar,text="Game preview",variable=self.game_preview,command=self.refresh).pack(side="left",padx=6)
        bg=ttk.Combobox(bar,textvariable=self.game_bg,values=list(GAME_BG),state="readonly",width=8)
        bg.pack(side="left"); bg.bind("<<ComboboxSelected>>",lambda e:self.refresh())
        ttk.Button(bar,text="Random Body",command=self.random_body).pack(side="left",padx=4)
        ttk.Button(bar,text="Reset",command=self.reset).pack(side="left",padx=4)

        pf=ttk.LabelFrame(right,text="Preview",padding=8); pf.pack(fill="both",expand=True,pady=(8,0))
        self.canvas=tk.Canvas(pf,bg="#202230",highlightthickness=0)
        self.canvas.pack(fill="both",expand=True)
        self.info=ttk.Label(right,text="")
        self.info.pack(anchor="w",pady=(6,0))

    def sync(self):
        data={k:v.get() for k,v in self.vars.items()}
        data["equipment"]={k:v.get() for k,v in self.eq_vars.items()}
        self.spec=CharacterSpec.from_dict(data)
        self.skill=SkillSpec(
            skill_id=self.skill_vars["skill_id"].get(),
            name=self.skill_vars["name"].get(),
            animation=self.skill_vars["animation"].get(),
            effect=self.skill_vars["effect"].get(),
            element=self.skill_vars["element"].get(),
            hit_frame=int(self.skill_vars["hit_frame"].get()),
        )
        self.ai=AIConfig(
            provider=self.ai_vars["provider"].get(),
            model=self.ai_vars["model"].get(),
            base_url=self.ai_vars["base_url"].get(),
            api_key_env=self.ai_vars["api_key_env"].get(),
            prompt_template=self.ai_prompt.get("1.0","end").strip(),
        )

    def apply_class_template(self, initial=False):
        c=self.vars["class_type"].get()
        tpl=CLASS_TEMPLATES.get(c,{})
        for k,v in tpl.items():
            if k in self.eq_vars:
                self.eq_vars[k].set(v)
            elif k in self.vars:
                self.vars[k].set(v)
        class_anim=CLASS_ANIMATION_MAP.get(c)
        if class_anim:
            self.anim_var.set(class_anim if not initial else "idle")
            frames=ANIMATIONS[class_anim]
            self.pose_var.set(frames[0] if not initial else "idle_0")
            self.skill_vars["animation"].set(class_anim)
            self.skill_vars["effect"].set(tpl.get("effect","Slash"))
            self.skill_vars["element"].set(tpl.get("element","Kim"))
        self.refresh()

    def select_class_animation(self):
        anim=CLASS_ANIMATION_MAP.get(self.vars["class_type"].get(),"idle")
        self.anim_var.set(anim)
        self.pose_var.set(ANIMATIONS[anim][0])
        self.refresh()

    def refresh(self):
        self.sync()
        pose=resolve_pose(POSES[self.pose_var.get()],self.pose_overrides.get(self.pose_var.get()))
        mode=self.preview_mode.get()
        render_mode="Normal" if mode=="Native 1x" else mode
        img=self.renderer.render(self.spec,pose,render_mode)

        if self.onion.get():
            anim=self.anim_var.get()
            frames=ANIMATIONS.get(anim,[])
            if self.pose_var.get() in frames and frames:
                idx=frames.index(self.pose_var.get())
                prev=frames[idx-1] if idx>0 else frames[-1]
                pimg=self.renderer.render(self.spec,resolve_pose(POSES[prev],self.pose_overrides.get(prev)),"Normal")
                fade=Image.new("RGBA",pimg.size,(0,0,0,0))
                fade=Image.blend(fade,pimg,0.22)
                fade.alpha_composite(img)
                img=fade

        scale_map={"1x":1,"2x":2,"4x":4,"10x":10}
        scale=1 if mode=="Native 1x" else scale_map[self.preview_scale.get()]
        big=img.resize((img.width*scale,img.height*scale),Image.Resampling.NEAREST)

        cw=max(self.canvas.winfo_width(),850)
        ch=max(self.canvas.winfo_height(),680)

        if self.game_preview.get():
            bgc=GAME_BG[self.game_bg.get()]
            viewport=Image.new("RGBA",(360,640),bgc)
            vd=ImageDraw.Draw(viewport)
            for y in range(0,640,32):
                vd.line([(0,y),(360,y)],fill=(bgc[0]//2,bgc[1]//2,bgc[2]//2,70))
            for x in range(0,360,32):
                vd.line([(x,0),(x,640)],fill=(bgc[0]//2,bgc[1]//2,bgc[2]//2,70))
            pscale=4
            main=img.resize((img.width*pscale,img.height*pscale),Image.Resampling.NEAREST)
            viewport.alpha_composite(main,((360-main.width)//2,320-main.height//2))
            for ox,oy in [(70,220),(250,250),(90,420),(250,430)]:
                npc=self.renderer.render(self.spec,POSES["idle_0"],"Silhouette")
                npc=npc.resize((npc.width*2,npc.height*2),Image.Resampling.NEAREST)
                viewport.alpha_composite(npc,(ox,oy))
            big=viewport

        bg=Image.new("RGBA",(cw,ch),(32,34,48,255))
        bg.alpha_composite(big,((cw-big.width)//2,(ch-big.height)//2))
        self.photo=ImageTk.PhotoImage(bg)
        self.canvas.delete("all")
        self.canvas.create_image(0,0,anchor="nw",image=self.photo)

        worn=sum(1 for v in self.spec.equipment.values() if v!="Không")
        self.info.config(text=f"{self.spec.name} • {self.spec.class_type} • {self.spec.direction} • {self.pose_var.get()} • Trang bị: {worn} slot")

    def toggle_play(self):
        self.playing=not self.playing
        if self.playing:self._tick()
        elif self.after_id:
            self.root.after_cancel(self.after_id); self.after_id=None

    def _tick(self):
        if not self.playing:return
        frames=ANIMATIONS[self.anim_var.get()]
        self.pose_var.set(frames[self.play_index%len(frames)])
        self.play_index+=1
        self.refresh()
        delay=max(40,int(1000/max(1,self.fps.get())))
        self.after_id=self.root.after(delay,self._tick)

    def prev_frame(self):
        frames=ANIMATIONS[self.anim_var.get()]
        cur=self.pose_var.get()
        idx=frames.index(cur) if cur in frames else 0
        self.pose_var.set(frames[(idx-1)%len(frames)])
        self.refresh()

    def next_frame(self):
        frames=ANIMATIONS[self.anim_var.get()]
        cur=self.pose_var.get()
        idx=frames.index(cur) if cur in frames else -1
        self.pose_var.set(frames[(idx+1)%len(frames)])
        self.refresh()

    def unequip_all(self):
        for v in self.eq_vars.values(): v.set("Không")
        self.refresh()

    def random_body(self):
        keys=["gender","class_type","body_type","skin_tone","head_shape","eye_style","nose_style","mouth_style","hair_style","hair_color","direction","skin_variant"]
        for k in keys: self.vars[k].set(random.choice(BODY_OPTIONS[k]))
        self.apply_class_template()

    def random_equipment(self):
        for k,vals in EQUIPMENT_LIBRARY.items():
            self.eq_vars[k].set(random.choice(vals))
        self.refresh()

    def import_overlay(self):
        win=tk.Toplevel(self.root); win.title("Import overlay")
        slot=tk.StringVar(value=list(EQUIPMENT_LIBRARY)[0])
        ttk.Label(win,text="Slot").pack(padx=10,pady=(10,2))
        ttk.Combobox(win,textvariable=slot,values=list(EQUIPMENT_LIBRARY),state="readonly").pack(padx=10)
        def choose():
            path=filedialog.askopenfilename(filetypes=[("PNG","*.png")])
            if path:
                self.renderer.set_custom_overlay(slot.get(),path)
                self.refresh(); win.destroy()
        ttk.Button(win,text="Chọn PNG",command=choose).pack(padx=10,pady=10)

    def clear_overlay(self):
        self.renderer.clear_custom_overlays()
        self.refresh()

    def reset(self):
        self.spec=CharacterSpec()
        for k,v in self.spec.to_dict().items():
            if k=="equipment":
                for sk,sv in v.items(): self.eq_vars[sk].set(sv)
            elif k in self.vars: self.vars[k].set(v)
        self.pose_var.set("idle_0")
        self.anim_var.set("idle")
        self.preview_mode.set("Normal")
        self.preview_scale.set("10x")
        self.game_preview.set(False)
        self.refresh()

    def save_character(self):
        self.sync()
        path=filedialog.asksaveasfilename(defaultextension=".json",filetypes=[("JSON","*.json")])
        if path: self.spec.save_json(path)

    def load_character(self):
        path=filedialog.askopenfilename(filetypes=[("JSON","*.json")])
        if not path:return
        self.spec=CharacterSpec.load_json(path)
        for k,v in self.spec.to_dict().items():
            if k=="equipment":
                for sk,sv in v.items():
                    if sk in self.eq_vars: self.eq_vars[sk].set(sv)
            elif k in self.vars:
                self.vars[k].set(v)
        self.refresh()

    def validate(self):
        self.sync()
        issues=[]
        if self.spec.class_type=="Kiếm tu" and self.spec.equipment["main_hand"]=="Không":
            issues.append("Kiếm tu nên có vũ khí chính.")
        if self.spec.class_type=="Đan tu" and self.spec.equipment["off_hand"]=="Không":
            issues.append("Đan tu nên có hồ lô hoặc item tay phụ.")
        if self.spec.direction=="Up" and self.spec.eye_style!="Bình tĩnh":
            issues.append("Up direction không hiển thị mặt; eye style không có tác dụng.")
        if not any(v.get() for v in self.anim_export.values()):
            issues.append("Chưa chọn animation để export.")
        msg="Không phát hiện vấn đề lớn." if not issues else "\n".join("• "+x for x in issues)
        messagebox.showinfo("Validation",msg)

    def export_pose(self):
        self.sync()
        path=filedialog.asksaveasfilename(defaultextension=".png",filetypes=[("PNG","*.png")],initialfile=self.pose_var.get()+".png")
        if path:self.renderer.render(self.spec,resolve_pose(POSES[self.pose_var.get()],self.pose_overrides.get(self.pose_var.get()))).save(path)

    def export_package(self):
        self.sync()
        anims=[a for a,v in self.anim_export.items() if v.get()]
        dirs=[d for d,v in self.dir_export.items() if v.get()]
        if not anims or not dirs:
            messagebox.showwarning("Thiếu lựa chọn","Cần chọn ít nhất 1 animation và 1 direction.")
            return
        out=filedialog.askdirectory(title="Chọn thư mục xuất")
        if not out:return
        dest=Path(out)/self.spec.name.replace(" ","_")
        self.exporter.export_package(self.spec,anims,dirs,dest,self.include_layers.get(),self.skill,self.ai)
        messagebox.showinfo("Hoàn tất",f"Đã xuất:\n{dest}")

    def load_pose_to_editor(self):
        p=resolve_pose(POSES[self.pose_var.get()],self.pose_overrides.get(self.pose_var.get()))
        for k,v in self.timeline_vars.items(): v.set(getattr(p,k))
        self.refresh()
    def apply_timeline_override(self):
        n=self.pose_var.get()
        for k,v in self.timeline_vars.items(): self.pose_overrides.set_field(n,k,v.get())
        self.refresh()
    def reset_pose_override(self): self.pose_overrides.reset_pose(self.pose_var.get()); self.load_pose_to_editor()
    def save_pose_overrides(self):
        p=filedialog.asksaveasfilename(defaultextension=".json",filetypes=[("JSON","*.json")],initialfile="pose_overrides.json")
        if p:self.pose_overrides.save(p)
    def load_pose_overrides(self):
        p=filedialog.askopenfilename(filetypes=[("JSON","*.json")])
        if p:self.pose_overrides.load(p); self.load_pose_to_editor()
    def load_anchor(self):
        x,y=self.anchors.get(self.vars["direction"].get(),self.anchor_name.get()); self.anchor_x.set(x); self.anchor_y.set(y)
    def apply_anchor(self): self.anchors.set(self.vars["direction"].get(),self.anchor_name.get(),self.anchor_x.get(),self.anchor_y.get()); self.preview_mode.set("Anchor Debug"); self.refresh()
    def preview_anchor_debug(self): self.preview_mode.set("Anchor Debug"); self.refresh()
    def reset_anchors(self): self.anchors.reset(); self.load_anchor(); self.refresh()
    def save_anchors(self):
        p=filedialog.asksaveasfilename(defaultextension=".json",filetypes=[("JSON","*.json")],initialfile="anchors.json")
        if p:self.anchors.save(p)
    def load_anchors(self):
        p=filedialog.askopenfilename(filetypes=[("JSON","*.json")])
        if p:self.anchors.load(p); self.load_anchor(); self.refresh()
    def refresh_asset_library(self):
        self.asset_list.delete(0,"end")
        for a in self.asset_library.list_assets(self.library_slot.get()): self.asset_list.insert("end",a["id"])
    def import_library_asset(self):
        p=filedialog.askopenfilename(filetypes=[("PNG","*.png")])
        if p:self.asset_library.import_png(self.library_slot.get(),p); self.refresh_asset_library()
    def apply_library_asset(self):
        sel=self.asset_list.curselection()
        if not sel:return
        aid=self.asset_list.get(sel[0])
        for a in self.asset_library.list_assets(self.library_slot.get()):
            if a["id"]==aid:self.renderer.set_custom_overlay(self.library_slot.get(),str(a["path"]));break
        self.refresh()
    def run_art_qa(self):
        self.sync(); pose=resolve_pose(POSES[self.pose_var.get()],self.pose_overrides.get(self.pose_var.get())); info=analyze_sprite(self.renderer.render(self.spec,pose,"Normal")); lines=[f"Palette count: {info['palette_count']}",f"Visible size: {info['visible_size'][0]}x{info['visible_size'][1]}",f"Value range: {info['value_range']}",""]
        lines += (["Warnings:"]+["• "+w for w in info["warnings"]]) if info["warnings"] else ["Không phát hiện cảnh báo art lớn."]
        self.qa_text.delete("1.0","end"); self.qa_text.insert("1.0","\n".join(lines))
    def export_package_godot(self):
        self.sync(); anims=[a for a,v in self.anim_export.items() if v.get()]; dirs=[d for d,v in self.dir_export.items() if v.get()]
        if not anims or not dirs: messagebox.showwarning("Thiếu lựa chọn","Cần chọn ít nhất 1 animation và 1 direction."); return
        out=filedialog.askdirectory(title="Chọn thư mục xuất")
        if not out:return
        dest=Path(out)/self.spec.name.replace(" ","_"); self.exporter.export_package(self.spec,anims,dirs,dest,self.include_layers.get(),self.skill,self.ai); export_godot_spriteframes(dest,anims,dirs,self.fps.get()); messagebox.showinfo("Hoàn tất",f"Đã xuất package + Godot:\n{dest}")

    def save_ai(self):
        self.sync()
        path=filedialog.asksaveasfilename(defaultextension=".json",filetypes=[("JSON","*.json")],initialfile="ai_config.json")
        if path: save_ai_config(self.ai,path)

    def load_ai(self):
        path=filedialog.askopenfilename(filetypes=[("JSON","*.json")])
        if not path:return
        self.ai=load_ai_config(path)
        for k in self.ai_vars: self.ai_vars[k].set(getattr(self.ai,k))
        self.ai_prompt.delete("1.0","end")
        self.ai_prompt.insert("1.0",self.ai.prompt_template)
