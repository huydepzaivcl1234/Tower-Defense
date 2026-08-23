# Blender 4.x/5.x
# Main Base / Core Building generator for the Tower Defense project.
# Builds a game-ready hero asset inspired by the approved cyan-crystal / gold-bronze / cool-stone concept.
# Front of the building is -Y. Z is up. Target height is ~18 m.

import bpy
import math
import os
from pathlib import Path
from mathutils import Vector

# -----------------------------------------------------------------------------
# CONFIG
# -----------------------------------------------------------------------------
ASSET_NAME = "MainBase"
TARGET_HEIGHT_M = 18.0
PLATFORM_RADIUS = 8.2
DRUM_RADIUS = 5.15
FRONT_Y = -5.05

# Keep these material names stable: the Unity setup tool maps them by name.
MAT_STONE = "MAT_Stone"
MAT_GOLD = "MAT_Gold"
MAT_CRYSTAL = "MAT_Crystal"
MAT_BANNER = "MAT_Banner"
MAT_MOSS = "MAT_Moss"
MAT_PORTAL = "MAT_Portal"


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials):
        pass


def set_principled_input(bsdf, names, value):
    for name in names:
        sock = bsdf.inputs.get(name)
        if sock is not None:
            sock.default_value = value
            return True
    return False


def make_material(name, base_color, metallic=0.0, roughness=0.5, emission=None, emission_strength=0.0, transmission=0.0, ior=1.5):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    if bsdf is None:
        bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    set_principled_input(bsdf, ["Base Color"], base_color)
    set_principled_input(bsdf, ["Metallic"], metallic)
    set_principled_input(bsdf, ["Roughness"], roughness)
    set_principled_input(bsdf, ["IOR"], ior)
    set_principled_input(bsdf, ["Transmission Weight", "Transmission"], transmission)
    if emission is not None:
        set_principled_input(bsdf, ["Emission Color", "Emission"], emission)
        set_principled_input(bsdf, ["Emission Strength"], emission_strength)
    return mat


def build_materials():
    # Cool light stone, antique bronze-gold, cyan crystal, navy cloth.
    stone = make_material(MAT_STONE, (0.46, 0.51, 0.56, 1), 0.0, 0.72)
    gold = make_material(MAT_GOLD, (0.47, 0.26, 0.065, 1), 0.92, 0.26)
    crystal = make_material(MAT_CRYSTAL, (0.02, 0.42, 0.80, 1), 0.0, 0.12,
                            emission=(0.0, 0.72, 1.0, 1), emission_strength=5.0,
                            transmission=0.35, ior=1.65)
    banner = make_material(MAT_BANNER, (0.018, 0.055, 0.14, 1), 0.0, 0.62)
    moss = make_material(MAT_MOSS, (0.07, 0.19, 0.075, 1), 0.0, 0.88)
    portal = make_material(MAT_PORTAL, (0.008, 0.08, 0.22, 1), 0.0, 0.16,
                           emission=(0.0, 0.48, 1.0, 1), emission_strength=8.0,
                           transmission=0.15, ior=1.35)
    return {
        MAT_STONE: stone,
        MAT_GOLD: gold,
        MAT_CRYSTAL: crystal,
        MAT_BANNER: banner,
        MAT_MOSS: moss,
        MAT_PORTAL: portal,
    }


def assign_mat(obj, mat):
    if obj.type == 'MESH':
        obj.data.materials.clear()
        obj.data.materials.append(mat)


def finish_mesh(obj, bevel=0.06, smooth=True):
    if obj.type != 'MESH':
        return obj
    if bevel > 0:
        mod = obj.modifiers.new("Bevel", 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = 'ANGLE'
    if smooth:
        for p in obj.data.polygons:
            p.use_smooth = True
    return obj


def add_box(name, loc, scale, mat, rot=(0, 0, 0), bevel=0.06):
    bpy.ops.mesh.primitive_cube_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (scale[0] * 0.5, scale[1] * 0.5, scale[2] * 0.5)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign_mat(obj, mat)
    return finish_mesh(obj, bevel)


def add_cylinder(name, loc, radius, depth, vertices, mat, rot=(0, 0, 0), bevel=0.05):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    assign_mat(obj, mat)
    return finish_mesh(obj, bevel)


def add_cone(name, loc, radius1, radius2, depth, vertices, mat, rot=(0, 0, 0), bevel=0.03):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    assign_mat(obj, mat)
    return finish_mesh(obj, bevel)


def add_curve_tube(name, points, radius, mat, cyclic=False):
    curve = bpy.data.curves.new(name + "_Curve", 'CURVE')
    curve.dimensions = '3D'
    curve.resolution_u = 2
    curve.bevel_depth = radius
    curve.bevel_resolution = 3
    spline = curve.splines.new('BEZIER')
    spline.bezier_points.add(len(points) - 1)
    for bp, co in zip(spline.bezier_points, points):
        bp.co = co
        bp.handle_left_type = 'AUTO'
        bp.handle_right_type = 'AUTO'
    spline.use_cyclic_u = cyclic
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def convert_curves_to_mesh():
    for obj in list(bpy.context.scene.objects):
        if obj.type != 'CURVE':
            continue
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.convert(target='MESH')
        obj.select_set(False)
        finish_mesh(obj, 0.0, True)


def add_crystal(name, loc, radius, height, mat, sides=6):
    # Hand-built faceted crystal: pointed top, broad middle, tapered bottom.
    z0 = -height * 0.48
    z1 = -height * 0.18
    z2 = height * 0.18
    z3 = height * 0.50
    verts = []
    rings = [(z0, radius * 0.46), (z1, radius), (z2, radius * 0.86)]
    for z, r in rings:
        for i in range(sides):
            a = 2 * math.pi * i / sides
            # Tiny alternating radius offset gives less-perfect, more gem-like facets.
            rr = r * (1.0 + (0.045 if i % 2 == 0 else -0.025))
            verts.append((math.cos(a) * rr, math.sin(a) * rr, z))
    top_idx = len(verts)
    verts.append((0, 0, z3))
    faces = []
    for ring in range(2):
        start = ring * sides
        nxt = (ring + 1) * sides
        for i in range(sides):
            j = (i + 1) % sides
            faces.append((start + i, start + j, nxt + j, nxt + i))
    top_ring = 2 * sides
    for i in range(sides):
        j = (i + 1) % sides
        faces.append((top_ring + i, top_ring + j, top_idx))
    # bottom cap
    faces.append(tuple(reversed(tuple(range(sides)))))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    assign_mat(obj, mat)
    finish_mesh(obj, 0.015, False)
    return obj


def add_shield(name, loc, width, height, thickness, face_mat, trim_mat):
    # Shield silhouette in local XZ, front faces -Y.
    pts = [
        (-0.50, 0.42), (-0.42, 0.72), (0.0, 0.88), (0.42, 0.72), (0.50, 0.42),
        (0.40, -0.28), (0.0, -0.90), (-0.40, -0.28)
    ]
    verts = [(x * width, -thickness * 0.5, z * height) for x, z in pts] + [(x * width, thickness * 0.5, z * height) for x, z in pts]
    n = len(pts)
    faces = [tuple(range(n)), tuple(reversed(tuple(range(n, n * 2))))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    assign_mat(obj, face_mat)
    finish_mesh(obj, 0.08, True)

    # Gold outline: tube following shield perimeter.
    outline_pts = [(loc[0] + x * width, loc[1] - thickness * 0.56, loc[2] + z * height) for x, z in pts]
    add_curve_tube(name + "_GoldBorder", outline_pts, 0.10, trim_mat, cyclic=True)
    return obj


def add_banner(name, x, y, z, side, banner_mat, gold_mat):
    # Cloth panel mesh with gentle wave. Gold pole and top/bottom trim.
    width, height = 1.55, 3.2
    cols, rows = 4, 8
    verts, faces = [], []
    for r in range(rows + 1):
        vz = z - height * (r / rows)
        for c in range(cols + 1):
            u = c / cols
            vx = x + (u - 0.5) * width
            vy = y + 0.10 * math.sin(u * math.pi * 2 + r * 0.35) * side
            verts.append((vx, vy, vz))
    for r in range(rows):
        for c in range(cols):
            a = r * (cols + 1) + c
            b = a + 1
            d = (r + 1) * (cols + 1) + c
            e = d + 1
            faces.append((a, b, e, d))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    assign_mat(obj, banner_mat)
    solid = obj.modifiers.new("ClothThickness", 'SOLIDIFY')
    solid.thickness = 0.025
    finish_mesh(obj, 0.01, True)

    add_cylinder(name + "_Pole", (x, y + 0.08 * side, z + 0.15), 0.055, width + 0.9, 12, gold_mat,
                 rot=(0, math.radians(90), 0), bevel=0.02)
    add_box(name + "_GoldTop", (x, y - 0.025, z + 0.01), (width, 0.06, 0.08), gold_mat, bevel=0.02)
    return obj


def add_pine(name, loc, scale, stone_mat, moss_mat):
    trunk = add_cylinder(name + "_Trunk", (loc[0], loc[1], loc[2] + 0.8 * scale), 0.13 * scale, 1.6 * scale, 10, stone_mat, bevel=0.02)
    # Reuse vegetation palette via moss material.
    add_cone(name + "_NeedlesA", (loc[0], loc[1], loc[2] + 1.45 * scale), 0.95 * scale, 0.0, 1.65 * scale, 12, moss_mat, bevel=0.01)
    add_cone(name + "_NeedlesB", (loc[0], loc[1], loc[2] + 2.15 * scale), 0.72 * scale, 0.0, 1.45 * scale, 12, moss_mat, bevel=0.01)


def smart_uv_all():
    # Basic UVs suitable for prototype PBR materials and later replacement by production UVs.
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH' or len(obj.data.polygons) == 0:
            continue
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        try:
            bpy.ops.object.mode_set(mode='EDIT')
            bpy.ops.mesh.select_all(action='SELECT')
            bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.025)
            bpy.ops.object.mode_set(mode='OBJECT')
        except Exception:
            try:
                bpy.ops.object.mode_set(mode='OBJECT')
            except Exception:
                pass
        obj.select_set(False)


def build_asset():
    clear_scene()
    mats = build_materials()
    stone, gold = mats[MAT_STONE], mats[MAT_GOLD]
    crystal, banner, moss, portal = mats[MAT_CRYSTAL], mats[MAT_BANNER], mats[MAT_MOSS], mats[MAT_PORTAL]

    root = bpy.data.objects.new(ASSET_NAME, None)
    bpy.context.collection.objects.link(root)

    # ------------------------------------------------------------------
    # 1) PLATFORM / COBBLESTONE RIM
    # ------------------------------------------------------------------
    platform = add_cylinder("Base_Platform", (0, 0, 0.45), PLATFORM_RADIUS, 0.9, 24, stone, bevel=0.12)
    platform.parent = root
    add_cylinder("Base_InnerRise", (0, 0, 0.95), 6.9, 0.55, 24, stone, bevel=0.10).parent = root

    for i in range(28):
        a = 2 * math.pi * i / 28
        r = 7.55 + 0.12 * math.sin(i * 1.7)
        x, y = math.cos(a) * r, math.sin(a) * r
        stone_block = add_box(
            f"RimStone_{i:02d}",
            (x, y, 0.88 + 0.04 * math.sin(i)),
            (1.35, 0.82, 0.52), stone,
            rot=(0.03 * math.sin(i), 0.02 * math.cos(i), a + math.pi * 0.5), bevel=0.12)
        stone_block.parent = root

    # Moss patches around the base.
    for i in range(14):
        a = 2 * math.pi * i / 14 + 0.17
        r = 6.5 + 0.35 * math.sin(i * 2.1)
        patch = add_cylinder(f"Moss_{i:02d}", (math.cos(a) * r, math.sin(a) * r, 1.25), 0.42 + 0.12 * (i % 3), 0.08, 10, moss, bevel=0.03)
        patch.scale.y = 0.6
        patch.parent = root

    # ------------------------------------------------------------------
    # 2) STAIRS / APPROACH / MOSAIC
    # ------------------------------------------------------------------
    for i in range(5):
        z = 1.15 + i * 0.26
        y = -7.0 + i * 0.56
        w = 5.2 - i * 0.42
        step = add_box(f"FrontStep_{i}", (0, y, z), (w, 1.15, 0.32), stone, bevel=0.08)
        step.parent = root

    mosaic_blue = add_box("Mosaic_BlueDiamond", (0, -5.72, 2.49), (1.6, 1.6, 0.055), crystal,
                          rot=(0, 0, math.radians(45)), bevel=0.025)
    mosaic_blue.parent = root
    for sx in (-1, 1):
        wing = add_box(f"Mosaic_GoldWing_{sx}", (sx * 1.18, -5.72, 2.50), (1.45, 0.22, 0.07), gold,
                       rot=(0, 0, math.radians(18 * sx)), bevel=0.025)
        wing.parent = root

    # ------------------------------------------------------------------
    # 3) MAIN OCTAGONAL DRUM + STONE BAND DETAIL
    # ------------------------------------------------------------------
    drum = add_cylinder("Main_Drum", (0, 0, 5.0), DRUM_RADIUS, 6.7, 8, stone, rot=(0, 0, math.radians(22.5)), bevel=0.16)
    drum.parent = root
    lower_band = add_cylinder("Gold_LowerBand", (0, 0, 2.45), 5.23, 0.22, 8, gold, rot=(0, 0, math.radians(22.5)), bevel=0.04)
    lower_band.parent = root
    upper_band = add_cylinder("Gold_UpperBand", (0, 0, 7.72), 5.23, 0.24, 8, gold, rot=(0, 0, math.radians(22.5)), bevel=0.04)
    upper_band.parent = root

    # Visible mortar/stone courses as shallow radial blocks around the drum.
    for row in range(5):
        z = 3.15 + row * 0.92
        count = 16
        offset = (row % 2) * (math.pi / count)
        for i in range(count):
            a = 2 * math.pi * i / count + offset
            r = 5.16
            x, y = math.cos(a) * r, math.sin(a) * r
            blk = add_box(f"WallBlock_{row}_{i:02d}", (x, y, z), (1.40, 0.34, 0.68), stone,
                          rot=(0, 0, a + math.pi * 0.5), bevel=0.07)
            blk.parent = root

    # Four prominent corner turrets.
    turret_angles = [45, 135, 225, 315]
    for idx, deg in enumerate(turret_angles):
        a = math.radians(deg)
        x, y = math.cos(a) * 5.0, math.sin(a) * 5.0
        tower = add_cylinder(f"CornerTower_{idx}", (x, y, 5.35), 1.15, 6.9, 8, stone, rot=(0, 0, a), bevel=0.12)
        tower.parent = root
        cap = add_cylinder(f"CornerTowerGoldCap_{idx}", (x, y, 8.78), 1.26, 0.25, 8, gold, rot=(0, 0, a), bevel=0.04)
        cap.parent = root
        c = add_crystal(f"CornerCrystal_{idx}", (x, y, 9.55), 0.42, 1.55, crystal)
        c.parent = root
        # Gold pedestal under crystal.
        ped = add_cylinder(f"CornerCrystalPedestal_{idx}", (x, y, 9.00), 0.55, 0.45, 8, gold, bevel=0.05)
        ped.parent = root

    # ------------------------------------------------------------------
    # 4) FRONT PORTAL + GOLD GOTHIC ARCH + CREST
    # ------------------------------------------------------------------
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, location=(0, FRONT_Y - 0.05, 4.65))
    portal_obj = bpy.context.object
    portal_obj.name = "Portal_Energy"
    portal_obj.scale = (1.62, 0.13, 2.15)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign_mat(portal_obj, portal)
    portal_obj.parent = root

    arch_pts = [
        (-2.05, FRONT_Y - 0.32, 2.55), (-2.05, FRONT_Y - 0.32, 5.15),
        (-1.72, FRONT_Y - 0.32, 6.12), (-0.92, FRONT_Y - 0.32, 6.92),
        (0.0, FRONT_Y - 0.32, 7.55), (0.92, FRONT_Y - 0.32, 6.92),
        (1.72, FRONT_Y - 0.32, 6.12), (2.05, FRONT_Y - 0.32, 5.15),
        (2.05, FRONT_Y - 0.32, 2.55)
    ]
    arch = add_curve_tube("Entrance_GoldArch", arch_pts, 0.24, gold, cyclic=False)
    arch.parent = root
    for sx in (-1, 1):
        col = add_box(f"Entrance_GoldColumn_{sx}", (sx * 2.05, FRONT_Y - 0.30, 3.7), (0.43, 0.38, 2.8), gold, bevel=0.08)
        col.parent = root

    crest = add_shield("Faction_Crest", (0, FRONT_Y - 0.47, 7.45), 2.15, 1.65, 0.18, crystal, gold)
    crest.parent = root
    # Simple rune/monogram made from gold bars, intentionally abstract and reusable.
    for xoff, ang in [(-0.34, 22), (0.34, -22), (0, 90)]:
        rune = add_box(f"Faction_Rune_{xoff}_{ang}", (xoff, FRONT_Y - 0.60, 7.55), (0.17, 0.10, 1.35), gold,
                       rot=(0, 0, math.radians(ang)), bevel=0.035)
        rune.parent = root

    # ------------------------------------------------------------------
    # 5) CROWN / CORE
    # ------------------------------------------------------------------
    crown_base = add_cylinder("Crown_StoneSeat", (0, 0, 8.55), 3.65, 1.20, 12, stone, bevel=0.10)
    crown_base.parent = root
    crown_gold = add_cylinder("Crown_GoldRing", (0, 0, 9.15), 3.70, 0.28, 16, gold, bevel=0.04)
    crown_gold.parent = root

    # Curved gothic ribs around the central crystal.
    rib_count = 8
    for i in range(rib_count):
        a = 2 * math.pi * i / rib_count
        start = Vector((math.cos(a) * 3.35, math.sin(a) * 3.35, 9.2))
        mid = Vector((math.cos(a) * 4.05, math.sin(a) * 4.05, 12.2))
        end = Vector((math.cos(a) * 0.55, math.sin(a) * 0.55, 15.0))
        rib = add_curve_tube(f"Crown_Rib_{i:02d}", [start, mid, end], 0.16, gold)
        rib.parent = root

    core = add_crystal("Crystal_Core", (0, 0, 12.35), 1.75, 6.20, crystal, sides=7)
    core.parent = root

    # Smaller roofline crystal accents.
    for i in range(6):
        a = 2 * math.pi * i / 6 + math.radians(30)
        x, y = math.cos(a) * 3.35, math.sin(a) * 3.35
        ped = add_cylinder(f"RoofCrystalPedestal_{i}", (x, y, 9.55), 0.42, 0.35, 8, gold, bevel=0.04)
        ped.parent = root
        c = add_crystal(f"RoofCrystal_{i}", (x, y, 10.2 + 0.18 * (i % 2)), 0.30, 1.45 + 0.2 * (i % 2), crystal)
        c.parent = root

    spire = add_cone("Top_GoldSpire", (0, 0, 16.35), 0.56, 0.0, 2.55, 6, gold, bevel=0.03)
    spire.parent = root

    # ------------------------------------------------------------------
    # 6) SYMMETRIC BANNERS + PINES
    # ------------------------------------------------------------------
    add_banner("Banner_L", -5.8, -1.7, 8.1, -1, banner, gold).parent = root
    add_banner("Banner_R", 5.8, -1.7, 8.1, 1, banner, gold).parent = root
    add_pine("Pine_L", (-6.15, -5.35, 1.12), 0.82, stone, moss)
    add_pine("Pine_R", (6.15, -5.35, 1.12), 0.82, stone, moss)

    # A few small gold finials for silhouette readability.
    for x in (-4.5, 4.5):
        finial = add_cone(f"FrontFinial_{x}", (x, -4.15, 9.2), 0.26, 0.0, 1.25, 6, gold, bevel=0.025)
        finial.parent = root

    # Convert crown/crest outline curves to mesh so FBX import is deterministic.
    convert_curves_to_mesh()

    # Parent any unparented generated mesh to root.
    for obj in bpy.context.scene.objects:
        if obj != root and obj.parent is None and obj.type in {'MESH', 'CURVE'}:
            obj.parent = root

    smart_uv_all()

    # Pivot/root stays at world origin / base center.
    root.location = (0, 0, 0)
    bpy.context.view_layer.objects.active = root
    root.select_set(True)
    return root


def setup_preview_camera_and_lights():
    # Studio preview only. These are excluded from FBX export selection.
    bpy.ops.object.light_add(type='AREA', location=(-9.0, -11.0, 18.0))
    key = bpy.context.object
    key.name = "PREVIEW_Key"
    key.data.energy = 1800
    key.data.shape = 'DISK'
    key.data.size = 8.0
    key.rotation_euler = (math.radians(32), 0, math.radians(-38))

    bpy.ops.object.light_add(type='AREA', location=(8.0, 5.0, 10.0))
    fill = bpy.context.object
    fill.name = "PREVIEW_Fill"
    fill.data.energy = 700
    fill.data.size = 10.0

    bpy.ops.object.camera_add(location=(19.5, -22.5, 17.5))
    cam = bpy.context.object
    cam.name = "PREVIEW_Camera"
    direction = Vector((0, 0, 7.0)) - cam.location
    cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
    cam.data.lens = 55
    bpy.context.scene.camera = cam


def export_for_unity(root):
    script_path = Path(__file__).resolve()
    project_root = script_path.parents[3]
    out_dir = project_root / "Assets" / "Models" / "MainBase"
    out_dir.mkdir(parents=True, exist_ok=True)
    fbx_path = out_dir / "MainBase.fbx"
    blend_path = script_path.parent / "MainBase.blend"

    # Save editable Blender source.
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

    # Export only the root hierarchy, no preview camera/lights.
    bpy.ops.object.select_all(action='DESELECT')
    root.select_set(True)
    for child in root.children_recursive:
        if child.type in {'MESH', 'EMPTY'}:
            child.select_set(True)
    bpy.context.view_layer.objects.active = root

    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        object_types={'EMPTY', 'MESH'},
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z',
        axis_up='Y',
        add_leaf_bones=False,
        bake_anim=False,
        mesh_smooth_type='FACE',
        use_mesh_modifiers=True,
        path_mode='AUTO'
    )
    print(f"[MainBase] Saved Blender source: {blend_path}")
    print(f"[MainBase] Exported Unity FBX: {fbx_path}")


def main():
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    root = build_asset()
    setup_preview_camera_and_lights()
    export_for_unity(root)
    print(f"[MainBase] Done. Approx target height: {TARGET_HEIGHT_M:.1f} m")


if __name__ == "__main__":
    main()
