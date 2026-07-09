"""Export the prepared drivable sedan GLB."""

from __future__ import annotations

import runpy
from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[2]
BLEND = ROOT / "assets-source/vehicles/drivable-sedan/DrivableSedan.blend"
GLB = ROOT / "public/assets/vehicles/drivable-sedan.glb"


def ensure_blend() -> None:
    if BLEND.exists():
        return
    runpy.run_path(str(ROOT / "tools/blender/prepare_drivable_sedan.py"), run_name="__main__")


def main() -> None:
    ensure_blend()
    GLB.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(BLEND))
    for obj in bpy.context.scene.objects:
        if obj.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(obj, do_unlink=True)
    bpy.ops.export_scene.gltf(
        filepath=str(GLB),
        export_format="GLB",
        export_yup=True,
        export_animations=False,
        export_materials="EXPORT",
        export_image_format="AUTO",
        export_apply=True,
    )
    print(f"WROTE {GLB} {GLB.stat().st_size}")


if __name__ == "__main__":
    main()
