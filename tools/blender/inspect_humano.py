"""Inspect approved Humano character source assets.

Run examples:
  blender --background --python tools/blender/inspect_humano.py -- assets-source/characters/humano
  blender --background --python tools/blender/inspect_humano.py -- assets-source/.../Humano_..._LOD1.blend

The script is read-only: it opens/imports source files and prints JSON.
"""

import json
import os
import sys
from pathlib import Path

import bpy
from mathutils import Vector


SUPPORTED_SCENES = {".blend", ".fbx", ".glb", ".gltf"}
TEXTURE_EXTS = {".jpg", ".jpeg", ".png", ".tif", ".tiff", ".webp"}


def argv_after_double_dash():
    if "--" not in sys.argv:
        return []
    return sys.argv[sys.argv.index("--") + 1 :]


def discover(root):
    root_path = Path(root)
    if root_path.is_file():
        return [root_path]
    return sorted(
        p for p in root_path.rglob("*")
        if p.is_file() and p.suffix.lower() in SUPPORTED_SCENES
    )


def inventory(root):
    root_path = Path(root)
    files = sorted(p for p in root_path.rglob("*") if p.is_file()) if root_path.is_dir() else [root_path]
    return {
        "root": str(root_path),
        "lod0_scene_files": [str(p) for p in files if "LOD0" in str(p) and p.suffix.lower() in {".blend", ".fbx"}],
        "lod1_scene_files": [str(p) for p in files if "LOD1" in str(p) and p.suffix.lower() in {".blend", ".fbx"}],
        "lod2_scene_files": [str(p) for p in files if "LOD2" in str(p) and p.suffix.lower() in {".blend", ".fbx"}],
        "texture_folders": sorted(str(p) for p in root_path.rglob("Maps") if p.is_dir()) if root_path.is_dir() else [],
        "textures": [str(p) for p in files if p.suffix.lower() in TEXTURE_EXTS],
        "licence_or_terms_links": [
            str(p) for p in files
            if "license" in p.name.lower() or "licence" in p.name.lower() or "terms" in p.name.lower()
        ],
        "source_links": [str(p) for p in files if p.suffix.lower() == ".url"],
    }


def load_scene(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    ext = path.suffix.lower()
    if ext == ".blend":
        bpy.ops.wm.open_mainfile(filepath=str(path))
    elif ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(path))
    elif ext in {".glb", ".gltf"}:
        bpy.ops.import_scene.gltf(filepath=str(path))
    else:
        raise ValueError(f"Unsupported scene file: {path}")


def material_summary(mat):
    image_paths = []
    if mat.use_nodes and mat.node_tree:
        for node in mat.node_tree.nodes:
            image = getattr(node, "image", None)
            if image:
                image_paths.append(bpy.path.abspath(image.filepath) if image.filepath else image.name)
    return {
        "name": mat.name,
        "use_nodes": mat.use_nodes,
        "node_count": len(mat.node_tree.nodes) if mat.use_nodes and mat.node_tree else 0,
        "image_paths": sorted(set(image_paths)),
    }


def object_world_corners(obj):
    return [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]


def scene_bounds(meshes):
    if not meshes:
        return None
    xs, ys, zs = [], [], []
    for obj in meshes:
        for corner in object_world_corners(obj):
            xs.append(corner.x)
            ys.append(corner.y)
            zs.append(corner.z)
    return {
        "min": [min(xs), min(ys), min(zs)],
        "max": [max(xs), max(ys), max(zs)],
        "size": [max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)],
    }


def inspect_scene(path):
    load_scene(path)
    bpy.context.view_layer.update()

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    meshes = []
    for obj in mesh_objects:
        meshes.append(
            {
                "name": obj.name,
                "vertex_count": len(obj.data.vertices),
                "polygon_count": len(obj.data.polygons),
                "material_slots": [slot.material.name for slot in obj.material_slots if slot.material],
                "has_armature_modifier": any(mod.type == "ARMATURE" for mod in obj.modifiers),
                "dimensions": list(obj.dimensions),
            }
        )

    armatures = []
    for obj in bpy.context.scene.objects:
        if obj.type != "ARMATURE":
            continue
        armatures.append(
            {
                "name": obj.name,
                "bone_count": len(obj.data.bones),
                "root_bones": [bone.name for bone in obj.data.bones if bone.parent is None],
                "action": obj.animation_data.action.name if obj.animation_data and obj.animation_data.action else None,
            }
        )

    animations = []
    for action in bpy.data.actions:
        frames = action.frame_range
        animations.append(
            {
                "name": action.name,
                "frame_start": frames[0],
                "frame_end": frames[1],
                "duration_seconds": (frames[1] - frames[0]) / bpy.context.scene.render.fps,
                "fcurve_count": action_fcurve_count(action),
            }
        )

    return {
        "path": str(path),
        "bytes": os.path.getsize(path),
        "scene_fps": bpy.context.scene.render.fps,
        "mesh_count": len(meshes),
        "total_vertices": sum(m["vertex_count"] for m in meshes),
        "total_polygons": sum(m["polygon_count"] for m in meshes),
        "bounds": scene_bounds(mesh_objects),
        "meshes": meshes,
        "armatures": armatures,
        "materials": [material_summary(mat) for mat in bpy.data.materials],
        "animations": animations,
        "cameras": [obj.name for obj in bpy.context.scene.objects if obj.type == "CAMERA"],
        "lights": [obj.name for obj in bpy.context.scene.objects if obj.type == "LIGHT"],
    }


def action_fcurve_count(action):
    curves = getattr(action, "fcurves", None)
    if curves is not None:
        return len(curves)
    layers = getattr(action, "layers", [])
    total = 0
    for layer in layers:
        for strip in getattr(layer, "strips", []):
            channelbag = getattr(strip, "channelbag", None)
            if channelbag is not None:
                total += len(getattr(channelbag, "fcurves", []))
    return total


def main():
    roots = argv_after_double_dash()
    if not roots:
        raise SystemExit("Pass one or more source files/directories after --")
    report = {
        "inventory": [inventory(root) for root in roots],
        "scenes": [],
    }
    for root in roots:
        for path in discover(root):
            report["scenes"].append(inspect_scene(path))
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
