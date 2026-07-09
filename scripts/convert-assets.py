"""One-off Blender conversion of user-supplied vehicle/character packs to GLB.

Run:  blender --background --python scripts/convert-assets.py

Sources (user Downloads):
  64-truck/untitled_quardfaced.obj + Truck.png  -> vehicles/truck.glb
  Low Poly Cars (Free)_blender/LowPolyCars.blend -> vehicles/lowpoly-cars.glb
  VADER/VADER.blend (Auto-Rig Pro rig)           -> characters/vader.glb
"""
import os

import bpy

DL = os.path.expanduser("~/Downloads")
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "public", "assets")


def export_glb(path, **kwargs):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=path,
        export_format="GLB",
        export_yup=True,
        **kwargs,
    )
    print("WROTE", path, os.path.getsize(path))


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


# ---- 1) Truck: OBJ + palette texture -------------------------------------
reset()
bpy.ops.wm.obj_import(filepath=os.path.join(DL, "64-truck", "untitled_quardfaced.obj"))
img = bpy.data.images.load(os.path.join(DL, "64-truck", "Truck.png"))
for mat in bpy.data.materials:
    mat.use_nodes = True
    bsdf = next((n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        continue
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = img
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
export_glb(os.path.join(OUT, "vehicles", "truck.glb"))

# ---- 2) Low-poly car pack: export the whole scene ------------------------
bpy.ops.wm.open_mainfile(filepath=os.path.join(DL, "Low Poly Cars (Free)_blender", "LowPolyCars.blend"))
print("CAR OBJECTS:", [o.name for o in bpy.context.scene.objects if o.type == "MESH"])
export_glb(os.path.join(OUT, "vehicles", "lowpoly-cars.glb"))

# ---- 3) VADER: rigged character with animations --------------------------
# Keep only char_grp -> rig -> body meshes; drop the studio scene and the
# Auto-Rig Pro control-shape meshes; bake only the mixamo walk clip.
bpy.ops.wm.open_mainfile(filepath=os.path.join(DL, "VADER", "VADER.blend"))


def remove_recursive(obj):
    for child in list(obj.children):
        remove_recursive(child)
    bpy.data.objects.remove(obj, do_unlink=True)


for name in ("Camera", "FLOOR", "FOCUS", "ENV", "Sun"):
    if name in bpy.data.objects:
        remove_recursive(bpy.data.objects[name])
if "cs_grp" in bpy.data.objects:
    remove_recursive(bpy.data.objects["cs_grp"])

rig = bpy.data.objects["rig"]
mixamo = next(a for a in bpy.data.actions if "mixamo" in a.name)
if rig.animation_data is None:
    rig.animation_data_create()
rig.animation_data.action = mixamo
for action in list(bpy.data.actions):
    if action is not mixamo:
        bpy.data.actions.remove(action)

export_glb(os.path.join(OUT, "characters", "vader.glb"), export_animations=True)

print("ALL DONE")
