"""Inspect Nexo World vehicle GLB assets for drivable-car suitability.

Read-only. Prints JSON for hierarchy, mesh dimensions, materials, textures,
animations, and wheel-origin evidence.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_FILES = [
    ROOT / "public/assets/vehicles/lowpoly-cars.glb",
    ROOT / "public/assets/vehicles/truck.glb",
]


def argv_after_double_dash() -> list[str]:
    if "--" not in sys.argv:
        return []
    return sys.argv[sys.argv.index("--") + 1 :]


def open_glb(path: Path) -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    bpy.context.view_layer.update()


def bounds_for_object(obj: bpy.types.Object) -> dict[str, list[float]]:
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    mins = [min(getattr(c, axis) for c in corners) for axis in ("x", "y", "z")]
    maxs = [max(getattr(c, axis) for c in corners) for axis in ("x", "y", "z")]
    return {
        "min": mins,
        "max": maxs,
        "size": [maxs[i] - mins[i] for i in range(3)],
    }


def image_size(image: bpy.types.Image) -> list[int] | None:
    try:
        return [int(image.size[0]), int(image.size[1])]
    except Exception:
        return None


def inspect_file(path: Path) -> dict:
    open_glb(path)
    meshes = []
    total_triangles = 0
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        polygons = len(obj.data.polygons)
        total_triangles += sum(max(1, len(poly.vertices) - 2) for poly in obj.data.polygons)
        bounds = bounds_for_object(obj)
        origin = list(obj.matrix_world.translation)
        center = [(bounds["min"][i] + bounds["max"][i]) * 0.5 for i in range(3)]
        meshes.append(
            {
                "object": obj.name,
                "mesh": obj.data.name,
                "materials": [slot.material.name for slot in obj.material_slots if slot.material],
                "vertices": len(obj.data.vertices),
                "polygons": polygons,
                "triangles": sum(max(1, len(poly.vertices) - 2) for poly in obj.data.polygons),
                "bounds": bounds,
                "origin": origin,
                "origin_to_bounds_center": [origin[i] - center[i] for i in range(3)],
            }
        )

    scene_bounds = None
    if meshes:
        mins = [min(mesh["bounds"]["min"][i] for mesh in meshes) for i in range(3)]
        maxs = [max(mesh["bounds"]["max"][i] for mesh in meshes) for i in range(3)]
        scene_bounds = {
            "min": mins,
            "max": maxs,
            "size": [maxs[i] - mins[i] for i in range(3)],
        }

    return {
        "file": str(path.relative_to(ROOT)),
        "bytes": path.stat().st_size,
        "objects": [
            {
                "name": obj.name,
                "type": obj.type,
                "parent": obj.parent.name if obj.parent else None,
                "children": [child.name for child in obj.children],
                "location": list(obj.location),
                "rotation_euler": list(obj.rotation_euler),
                "scale": list(obj.scale),
            }
            for obj in bpy.context.scene.objects
        ],
        "meshes": meshes,
        "scene_bounds": scene_bounds,
        "total_triangles": total_triangles,
        "materials": [
            {
                "name": mat.name,
                "blend_method": mat.blend_method,
                "use_nodes": mat.use_nodes,
            }
            for mat in bpy.data.materials
        ],
        "textures": [
            {
                "name": image.name,
                "filepath": image.filepath,
                "size": image_size(image),
            }
            for image in bpy.data.images
        ],
        "animations": [
            {
                "name": action.name,
                "frame_range": list(action.frame_range),
                "fcurves": len(getattr(action, "fcurves", [])),
            }
            for action in bpy.data.actions
        ],
    }


def main() -> None:
    args = [Path(arg) for arg in argv_after_double_dash()]
    files = [path if path.is_absolute() else ROOT / path for path in args] if args else DEFAULT_FILES
    report = {"vehicles": [inspect_file(path) for path in files]}
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
