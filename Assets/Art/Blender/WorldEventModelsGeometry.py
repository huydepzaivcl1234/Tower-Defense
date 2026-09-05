#!/usr/bin/env python3
"""Generate three stylized Unity-ready OBJ models for the Tower Defense world events.

The generated geometry is deterministic, Z-up, centered at the origin, uses metres,
and is intentionally split into named objects/material groups for easy Blender edits.
"""

from __future__ import annotations

import math
from pathlib import Path


TAU = math.tau


class Obj:
    def __init__(self, name: str):
        self.name = name
        self.v = []
        self.vn = []
        self.faces = []

    def mesh(self, name, verts, faces, material, smooth=True):
        base = len(self.v)
        self.v.extend(verts)
        self.faces.append(("o", name))
        self.faces.append(("usemtl", material))
        self.faces.append(("s", "1" if smooth else "off"))
        for face in faces:
            self.faces.append(("f", tuple(base + i + 1 for i in face)))

    def write(self, path: Path, mtl_name: str):
        lines = [f"# {self.name}", f"mtllib {mtl_name}"]
        lines += [f"v {x:.6f} {y:.6f} {z:.6f}" for x, y, z in self.v]
        for kind, payload in self.faces:
            if kind == "f":
                lines.append("f " + " ".join(str(i) for i in payload))
            else:
                lines.append(f"{kind} {payload}")
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def transform(verts, loc=(0, 0, 0), scale=(1, 1, 1), rot=(0, 0, 0)):
    cx, sx = math.cos(rot[0]), math.sin(rot[0])
    cy, sy = math.cos(rot[1]), math.sin(rot[1])
    cz, sz = math.cos(rot[2]), math.sin(rot[2])
    out = []
    for x, y, z in verts:
        x, y, z = x * scale[0], y * scale[1], z * scale[2]
        y, z = y * cx - z * sx, y * sx + z * cx
        x, z = x * cy + z * sy, -x * sy + z * cy
        x, y = x * cz - y * sz, x * sz + y * cz
        out.append((x + loc[0], y + loc[1], z + loc[2]))
    return out


def cylinder(seg=24, r=1.0, h=1.0):
    v = []
    for z in (-h / 2, h / 2):
        for i in range(seg):
            a = TAU * i / seg
            v.append((math.cos(a) * r, math.sin(a) * r, z))
    v += [(0, 0, -h / 2), (0, 0, h / 2)]
    f = []
    for i in range(seg):
        j = (i + 1) % seg
        f.append((i, j, seg + j, seg + i))
        f.append((2 * seg, j, i))
        f.append((2 * seg + 1, seg + i, seg + j))
    return v, f


def cone(seg=20, r1=1.0, r2=0.0, h=1.0):
    v = []
    for z, r in ((-h / 2, r1), (h / 2, r2)):
        for i in range(seg):
            a = TAU * i / seg
            v.append((math.cos(a) * r, math.sin(a) * r, z))
    v += [(0, 0, -h / 2), (0, 0, h / 2)]
    f = []
    for i in range(seg):
        j = (i + 1) % seg
        f.append((i, j, seg + j, seg + i))
        f.append((2 * seg, j, i))
        if r2 > 0:
            f.append((2 * seg + 1, seg + i, seg + j))
    return v, f


def uv_sphere(seg=20, rings=12):
    v = [(0, 0, 1)]
    for j in range(1, rings):
        p = math.pi * j / rings
        for i in range(seg):
            a = TAU * i / seg
            v.append((math.sin(p) * math.cos(a), math.sin(p) * math.sin(a), math.cos(p)))
    v.append((0, 0, -1))
    f = []
    for i in range(seg):
        f.append((0, 1 + i, 1 + (i + 1) % seg))
    for j in range(rings - 2):
        row, nxt = 1 + j * seg, 1 + (j + 1) * seg
        for i in range(seg):
            k = (i + 1) % seg
            f.append((row + i, nxt + i, nxt + k, row + k))
    bot = len(v) - 1
    row = 1 + (rings - 2) * seg
    for i in range(seg):
        f.append((bot, row + (i + 1) % seg, row + i))
    return v, f


def jagged_sphere(seg=18, rings=11, seed=0):
    v, f = uv_sphere(seg, rings)
    out = []
    for idx, (x, y, z) in enumerate(v):
        wobble = 1 + 0.105 * math.sin(idx * 12.9898 + seed * 4.17) + 0.055 * math.sin(idx * 3.71)
        out.append((x * wobble, y * wobble, z * wobble))
    return out, f


def torus(major=1.0, minor=0.08, seg=32, tube=8):
    v, f = [], []
    for i in range(seg):
        a = TAU * i / seg
        for j in range(tube):
            b = TAU * j / tube
            rr = major + minor * math.cos(b)
            v.append((rr * math.cos(a), rr * math.sin(a), minor * math.sin(b)))
    for i in range(seg):
        for j in range(tube):
            ni, nj = (i + 1) % seg, (j + 1) % tube
            f.append((i * tube + j, ni * tube + j, ni * tube + nj, i * tube + nj))
    return v, f


def box():
    v = [(-.5,-.5,-.5),(.5,-.5,-.5),(.5,.5,-.5),(-.5,.5,-.5),
         (-.5,-.5,.5),(.5,-.5,.5),(.5,.5,.5),(-.5,.5,.5)]
    f = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return v, f


def crystal(sides=6):
    v = []
    for z, r in ((-.43,.45),(-.20,1.0),(.23,.82)):
        for i in range(sides):
            a = TAU * i / sides
            v.append((math.cos(a) * r, math.sin(a) * r, z))
    v.append((0,0,.55))
    f = []
    for row in range(2):
        for i in range(sides):
            j=(i+1)%sides
            f.append((row*sides+i,row*sides+j,(row+1)*sides+j,(row+1)*sides+i))
    for i in range(sides):
        j=(i+1)%sides
        f.append((2*sides+i,2*sides+j,3*sides))
    f.append(tuple(reversed(range(sides))))
    return v,f


def add(obj, primitive, name, mat, loc=(0,0,0), scale=(1,1,1), rot=(0,0,0), smooth=True):
    v,f=primitive
    obj.mesh(name, transform(v,loc,scale,rot), f, mat, smooth)


def build_paw():
    o=Obj("DogCatRain_LuckyPawDrop")
    add(o,cylinder(40,1,.16),"LuckyCoin","AntiqueGold",scale=(.78,.78,1),loc=(0,0,0))
    add(o,torus(.60,.045,40,8),"CoinTrim","PaleGold",loc=(0,0,.095),scale=(1,1,.65))
    add(o,cylinder(32,1,.035),"CoinInset","DeepGold",loc=(0,0,.098),scale=(.52,.52,1))
    s=uv_sphere(18,10)
    add(o,s,"PawPad","PaleGold",loc=(0,-.10,.145),scale=(.24,.30,.055))
    toes=[(-.28,.17,.115,.115),(-.10,.28,.12,.13),(.10,.28,.12,.13),(.28,.17,.115,.115)]
    for i,(x,y,sx,sy) in enumerate(toes):
        add(o,s,f"Toe_{i+1}","PaleGold",loc=(x,y,.145),scale=(sx,sy,.05))
    # Dog/cat ear crest and small cyan lucky gem give a lively, readable silhouette.
    c=cone(5,1,0,.45)
    add(o,c,"Ear_Left","PaleGold",loc=(-.34,.57,.12),scale=(.19,.16,1),rot=(0,.28,-.10),smooth=False)
    add(o,c,"Ear_Right","PaleGold",loc=(.34,.57,.12),scale=(.19,.16,1),rot=(0,-.28,.10),smooth=False)
    gem=crystal(6)
    add(o,gem,"LuckyCrystal","CyanCrystal",loc=(0,-.47,.18),scale=(.13,.13,.28),rot=(.12,0,0),smooth=False)
    # Cloud curls behind the coin.
    for i,(x,y,z,sc) in enumerate([(-.46,-.38,-.06,.28),(-.18,-.50,-.08,.34),(.18,-.50,-.08,.34),(.46,-.38,-.06,.28)]):
        add(o,s,f"Cloud_{i+1}","CloudBlue",loc=(x,y,z),scale=(sc,sc*.66,sc*.52))
    for i,a in enumerate((0,math.pi/2)):
        add(o,box(),f"Sparkle_{i+1}","CyanCrystal",loc=(.52,-.20,.18),scale=(.035,.035,.28),rot=(0,a,math.pi/4),smooth=False)
    return o


def build_meteor():
    o=Obj("MeteorShower_Meteor")
    rock=jagged_sphere(22,13,7)
    add(o,rock,"MeteorCore","MeteorRock",scale=(.78,.72,.95),rot=(.16,.31,.08),smooth=False)
    # Interlocking chunks create a less spherical, more dangerous silhouette.
    chunks=[(.54,.08,.08,.32),(-.48,.18,-.10,.29),(.14,-.50,-.20,.30),(-.16,-.31,.50,.26),(.26,.34,-.52,.28),(-.42,-.20,.24,.25)]
    for i,(x,y,z,scl) in enumerate(chunks):
        add(o,jagged_sphere(12,7,20+i),f"RockChunk_{i+1}","MeteorRock",loc=(x,y,z),scale=(scl,scl*.82,scl*.9),rot=(i*.31,i*.47,i*.19),smooth=False)
    # Magma seams and glowing impact nose.
    shard=crystal(5)
    seams=[(.30,-.50,.05,.34),(-.30,-.40,.24,.30),(.42,.10,-.25,.28),(-.44,.12,-.20,.26),(.08,.42,-.42,.25)]
    for i,(x,y,z,ln) in enumerate(seams):
        add(o,shard,f"MagmaSeam_{i+1}","Magma",loc=(x,y,z),scale=(.065,.055,ln),rot=(i*.7,.35+i*.24,.2*i),smooth=False)
    add(o,uv_sphere(18,10),"ImpactCore","HotMagma",loc=(0,0,-.70),scale=(.36,.34,.27))
    # Layered flame trail, pointing upward while the meteor falls toward -Z.
    add(o,cone(18,.52,.18,1.25),"FlameOuter","FlameOrange",loc=(0,0,.98),scale=(1,1,1),smooth=False)
    add(o,cone(16,.30,.05,1.65),"FlameInner","HotMagma",loc=(0,0,1.34),scale=(1,1,1),smooth=False)
    for i,a in enumerate((0,TAU/3,2*TAU/3)):
        add(o,cone(12,.16,0,.88),f"FlameWisp_{i+1}","FlameOrange",loc=(math.cos(a)*.34,math.sin(a)*.34,.82),scale=(1,1,1),rot=(math.sin(a)*.20,-math.cos(a)*.20,0),smooth=False)
    return o


def build_holy():
    o=Obj("HolyLight_Shrine")
    # Floating octagonal altar with stepped antique-gold trim.
    add(o,cylinder(12,1,.22),"BaseLower","CoolStone",loc=(0,0,.05),scale=(2.30,2.30,1),smooth=False)
    add(o,cylinder(12,1,.12),"BaseGold","AntiqueGold",loc=(0,0,.22),scale=(2.03,2.03,1),smooth=False)
    add(o,cylinder(12,1,.22),"BaseInner","IvoryStone",loc=(0,0,.37),scale=(1.68,1.68,1),smooth=False)
    add(o,torus(1.42,.075,40,8),"BaseHalo","CyanCrystal",loc=(0,0,.53))
    gem=crystal(7)
    add(o,gem,"HolyCrystal","CyanCrystal",loc=(0,0,1.55),scale=(.62,.62,1.85),smooth=False)
    add(o,uv_sphere(18,10),"CrystalHeart","HolyWhite",loc=(0,0,1.55),scale=(.27,.27,.38))
    # Curved-looking guardian prongs built from angled ivory/gold segments.
    for i in range(4):
        a=i*TAU/4
        x,y=math.cos(a)*.82,math.sin(a)*.82
        add(o,box(),f"Prong_{i+1}","IvoryStone",loc=(x,y,1.26),scale=(.18,.18,1.20),rot=(math.sin(a)*-.22,math.cos(a)*.22,a),smooth=False)
        add(o,crystal(5),f"ProngTip_{i+1}","AntiqueGold",loc=(x*1.08,y*1.08,1.91),scale=(.16,.16,.36),rot=(0,0,a),smooth=False)
    # Three independent halos make the asset readable from every camera angle.
    add(o,torus(1.58,.075,44,8),"HaloVertical_A","AntiqueGold",loc=(0,0,1.78),rot=(math.pi/2,0,0))
    add(o,torus(1.38,.06,40,8),"HaloVertical_B","CyanCrystal",loc=(0,0,1.78),rot=(0,math.pi/2,math.pi/4))
    add(o,torus(1.08,.055,36,8),"HaloCrown","HolyWhite",loc=(0,0,3.05),rot=(0,0,0))
    # Floating runic shards.
    for i in range(10):
        a=i*TAU/10+.12
        rr=2.02+(i%2)*.16
        add(o,crystal(5),f"RuneShard_{i+1}","CyanCrystal" if i%2==0 else "AntiqueGold",
            loc=(math.cos(a)*rr,math.sin(a)*rr,1.02+(i%2)*.52),scale=(.13,.13,.42),rot=(.16,0,-a),smooth=False)
    # Translucent light column and radiant ground rays.
    add(o,cylinder(28,1,5.2),"HolyBeamOuter","HolyBeam",loc=(0,0,3.35),scale=(.64,.64,1))
    add(o,cylinder(24,1,5.7),"HolyBeamInner","HolyWhite",loc=(0,0,3.52),scale=(.22,.22,1))
    for i in range(12):
        a=i*TAU/12
        add(o,box(),f"GroundRay_{i+1}","AntiqueGold",loc=(math.cos(a)*1.48,math.sin(a)*1.48,.57),scale=(1.35,.055,.035),rot=(0,0,a),smooth=False)
    add(o,gem,"CrownCrystal","HolyWhite",loc=(0,0,3.45),scale=(.30,.30,.72),smooth=False)
    return o


MTL = """# Tower Defense world-event materials
newmtl AntiqueGold
Kd 0.50 0.27 0.055
Ks 0.70 0.52 0.20
Ns 180
illum 2
newmtl PaleGold
Kd 0.92 0.67 0.18
Ks 0.90 0.78 0.40
Ns 220
illum 2
newmtl DeepGold
Kd 0.29 0.12 0.025
Ks 0.45 0.25 0.08
Ns 120
illum 2
newmtl CyanCrystal
Kd 0.02 0.50 0.82
Ke 0.00 0.55 1.00
Ks 0.70 0.95 1.00
Ns 300
d 0.86
illum 2
newmtl CloudBlue
Kd 0.46 0.58 0.79
Ks 0.15 0.23 0.38
Ns 70
illum 2
newmtl MeteorRock
Kd 0.075 0.050 0.060
Ks 0.10 0.08 0.08
Ns 18
illum 2
newmtl Magma
Kd 0.95 0.075 0.008
Ke 1.00 0.12 0.005
Ks 0.55 0.16 0.04
Ns 140
illum 2
newmtl HotMagma
Kd 1.00 0.42 0.025
Ke 1.00 0.35 0.015
Ks 1.00 0.52 0.12
Ns 220
illum 2
newmtl FlameOrange
Kd 0.98 0.16 0.01
Ke 1.00 0.12 0.005
d 0.80
illum 2
newmtl CoolStone
Kd 0.29 0.36 0.44
Ks 0.13 0.18 0.23
Ns 55
illum 2
newmtl IvoryStone
Kd 0.72 0.74 0.70
Ks 0.28 0.30 0.27
Ns 80
illum 2
newmtl HolyWhite
Kd 1.00 0.93 0.63
Ke 0.95 0.78 0.34
Ks 1.00 0.98 0.82
Ns 320
illum 2
newmtl HolyBeam
Kd 0.30 0.72 1.00
Ke 0.08 0.48 1.00
d 0.18
illum 2
"""


def main():
    script_path = Path(__file__).resolve()
    # In-repo location: Assets/Art/Blender/WorldEventModelsGeometry.py
    if len(script_path.parents) >= 4 and script_path.parent.name == "Blender":
        out = script_path.parents[3] / "Assets" / "Models" / "WorldEvents"
    else:
        out = script_path.parent / "generated_world_event_models"
    out.mkdir(parents=True,exist_ok=True)
    mtl="WorldEventMaterials.mtl"
    (out/mtl).write_text(MTL,encoding="utf-8")
    models=[build_paw(),build_meteor(),build_holy()]
    for model in models:
        model.write(out/f"{model.name}.obj",mtl)
        print(f"{model.name}: {len(model.v)} vertices -> {out/model.name}.obj")


if __name__=="__main__":
    main()

