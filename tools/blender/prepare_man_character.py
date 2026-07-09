"""Prepare the approved Humano LOD1 male player source blend.

Creates:
  assets-source/characters/man/ManCharacter.blend

The original Humano package is not modified.
"""

from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
SOURCE_BLEND = ROOT / "assets-source/characters/humano/Humano_Anim_045-3745-W5/Humano_Anim_045-3745-W5_LOD1/Humano_Anim_045-3745-W5_01_LOD1.blend"
MAPS_DIR = ROOT / "assets-source/characters/humano/Humano_Anim_045-3745-W5/Humano_Anim_045-3745-W5_LOD1/Maps"
OUT_BLEND = ROOT / "assets-source/characters/man/ManCharacter.blend"


def require_file(path: Path) -> None:
    if not path.exists():
        raise FileNotFoundError(path)


def validate_scene() -> None:
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if len(meshes) != 1:
        raise RuntimeError(f"Expected exactly one player mesh, found {len(meshes)}")
    if len(armatures) != 1:
        raise RuntimeError(f"Expected exactly one armature, found {len(armatures)}")
    if len(armatures[0].data.bones) < 20:
        raise RuntimeError(f"Skeleton has too few bones: {len(armatures[0].data.bones)}")
    if not bpy.data.actions:
        raise RuntimeError("No animation actions found in Humano source")


def clean_scene() -> None:
    for obj in list(bpy.context.scene.objects):
        if obj.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(obj, do_unlink=True)
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
        if obj.type == "MESH":
            obj.name = "Nexo_Man_Player_LOD1"
            obj.data.name = "Nexo_Man_Player_LOD1_Mesh"
            obj.location.z -= min((obj.matrix_world @ Vector(corner)).z for corner in obj.bound_box)
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            for slot in obj.material_slots:
                if slot.material:
                    slot.material.name = "Nexo_Man_Player_2K"
        elif obj.type == "ARMATURE":
            obj.name = "Nexo_Man_Player_Skeleton"
            obj.data.name = "Nexo_Man_Player_Skeleton_Data"
            obj.show_in_front = False


def rename_animation() -> None:
    for action in bpy.data.actions:
        if "Key" in action.name:
            action.name = "HumanoShapeKeys_Source"
        else:
            action.name = "walk"


def make_paths_relative() -> None:
    for image in bpy.data.images:
        if image.filepath:
            image.filepath = bpy.path.relpath(str(Path(bpy.path.abspath(image.filepath))))


def annotate_scene() -> None:
    bpy.context.scene["nexo_source"] = str(SOURCE_BLEND.relative_to(ROOT))
    bpy.context.scene["nexo_lod"] = "LOD1"
    bpy.context.scene["nexo_texture_set"] = str(MAPS_DIR.relative_to(ROOT))
    bpy.context.scene["nexo_animation_inventory"] = "walk only; idle/run/jump/fall/landing/turn/gesture clips absent"


def main() -> None:
    require_file(SOURCE_BLEND)
    require_file(MAPS_DIR / "Humano_Anim_045-3745-W5_Color01_2K.jpg")
    OUT_BLEND.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    validate_scene()
    clean_scene()
    rename_animation()
    make_paths_relative()
    annotate_scene()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND))
    print(f"WROTE {OUT_BLEND}")


if __name__ == "__main__":
    main()
