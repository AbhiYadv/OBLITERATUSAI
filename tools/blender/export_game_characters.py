"""Export prepared Nexo World game character GLBs.

Currently exports only the approved male player:
  public/assets/characters/man-player.glb
"""

from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[2]
MAN_BLEND = ROOT / "assets-source/characters/man/ManCharacter.blend"
MAN_GLB = ROOT / "public/assets/characters/man-player.glb"


def ensure_prepared_blend() -> None:
    if MAN_BLEND.exists():
        return
    import runpy

    runpy.run_path(str(ROOT / "tools/blender/prepare_man_character.py"), run_name="__main__")


def configure_export_scene() -> None:
    for obj in list(bpy.context.scene.objects):
        if obj.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(obj, do_unlink=True)
        elif obj.type == "MESH" and (not obj.material_slots or obj.name.lower().startswith("icosphere")):
            bpy.data.objects.remove(obj, do_unlink=True)
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            obj.hide_viewport = False
            obj.hide_render = False
    for action in list(bpy.data.actions):
        if action.name == "walk":
            action.use_fake_user = True
        else:
            bpy.data.actions.remove(action)


def export_man_player() -> None:
    ensure_prepared_blend()
    MAN_GLB.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(MAN_BLEND))
    configure_export_scene()
    bpy.ops.export_scene.gltf(
        filepath=str(MAN_GLB),
        export_format="GLB",
        export_yup=True,
        export_animations=True,
        export_animation_mode="ACTIONS",
        export_nla_strips=False,
        export_materials="EXPORT",
        export_image_format="AUTO",
    )
    print(f"WROTE {MAN_GLB} {MAN_GLB.stat().st_size}")


def main() -> None:
    export_man_player()


if __name__ == "__main__":
    main()
