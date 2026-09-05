"""Blender 4.x/5.x packager for the three Tower Defense world-event models.

Run this file from Blender's Scripting workspace after the OBJ/MTL files are in
Assets/Models/WorldEvents. It creates one editable .blend and one Unity FBX per
model without changing any scene, prefab, ScriptableObject, or gameplay code.
"""

import bpy
from pathlib import Path


MODEL_NAMES = (
    "DogCatRain_LuckyPawDrop",
    "MeteorShower_Meteor",
    "HolyLight_Shrine",
)


def project_root():
    return Path(__file__).resolve().parents[3]


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def import_obj(path):
    if hasattr(bpy.ops.wm, "obj_import"):
        bpy.ops.wm.obj_import(filepath=str(path), forward_axis="NEGATIVE_Z", up_axis="Y")
    else:
        bpy.ops.import_scene.obj(filepath=str(path), axis_forward="-Z", axis_up="Y")


def set_socket(bsdf, names, value):
    for name in names:
        socket = bsdf.inputs.get(name)
        if socket is not None:
            socket.default_value = value
            return


def tune_materials():
    emission = {
        "CyanCrystal": ((0.0, 0.55, 1.0, 1.0), 5.0),
        "Magma": ((1.0, 0.08, 0.005, 1.0), 6.0),
        "HotMagma": ((1.0, 0.34, 0.015, 1.0), 8.0),
        "FlameOrange": ((1.0, 0.12, 0.005, 1.0), 5.5),
        "HolyWhite": ((1.0, 0.82, 0.36, 1.0), 6.0),
        "HolyBeam": ((0.05, 0.50, 1.0, 1.0), 3.5),
    }
    metallic = {"AntiqueGold": 0.88, "PaleGold": 0.74, "DeepGold": 0.68}
    for mat in bpy.data.materials:
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf is None:
            continue
        set_socket(bsdf, ("Metallic",), metallic.get(mat.name, 0.0))
        set_socket(bsdf, ("Roughness",), 0.24 if mat.name in metallic else 0.52)
        if mat.name in emission:
            color, strength = emission[mat.name]
            set_socket(bsdf, ("Emission Color", "Emission"), color)
            set_socket(bsdf, ("Emission Strength",), strength)
        if mat.name == "HolyBeam":
            set_socket(bsdf, ("Alpha",), 0.18)
            if hasattr(mat, "surface_render_method"):
                mat.surface_render_method = "DITHERED"


def package_model(name, obj_path, source_dir, model_dir):
    clear_scene()
    import_obj(obj_path)
    tune_materials()

    imported = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    root = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(root)
    root.location = (0, 0, 0)
    for obj in imported:
        obj.parent = root
        for polygon in obj.data.polygons:
            polygon.use_smooth = not ("Rock" in obj.name or "Shard" in obj.name or "Crystal" in obj.name)

    blend_path = source_dir / f"{name}.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for obj in imported:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    fbx_path = model_dir / f"{name}.fbx"
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        path_mode="AUTO",
    )
    print(f"[{name}] source={blend_path}")
    print(f"[{name}] unity={fbx_path}")


def main():
    root = project_root()
    model_dir = root / "Assets" / "Models" / "WorldEvents"
    source_dir = root / "Assets" / "Art" / "Blender" / "WorldEvents"
    source_dir.mkdir(parents=True, exist_ok=True)
    missing = [str(model_dir / f"{name}.obj") for name in MODEL_NAMES if not (model_dir / f"{name}.obj").exists()]
    if missing:
        raise FileNotFoundError("Missing model source(s):\n" + "\n".join(missing))
    for name in MODEL_NAMES:
        package_model(name, model_dir / f"{name}.obj", source_dir, model_dir)
    print("World-event Blender pack complete.")


if __name__ == "__main__":
    main()

