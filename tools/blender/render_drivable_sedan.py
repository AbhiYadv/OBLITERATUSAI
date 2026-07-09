"""Render neutral visual QA views for the drivable sedan asset."""

from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
BLEND = ROOT / "assets-source/vehicles/drivable-sedan/DrivableSedan.blend"
REPORT_JSON = ROOT / "assets-source/vehicles/drivable-sedan/asset-report.json"
RENDER_DIR = ROOT / "assets-source/vehicles/drivable-sedan/renders"


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_scene() -> None:
    bpy.ops.wm.open_mainfile(filepath=str(BLEND))
    RENDER_DIR.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 48
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 1400
    scene.render.resolution_y = 950
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    if scene.world is None:
        scene.world = bpy.data.worlds.new("RenderOnly.World")
    scene.world.color = (0.78, 0.8, 0.82)

    # Hide runtime collision proxy in visual renders; it remains in the exported asset.
    collision = bpy.data.objects.get("Collision.Body")
    if collision:
        collision.hide_render = True

    bpy.ops.mesh.primitive_plane_add(size=9, location=(0, 0, -0.002))
    ground = bpy.context.object
    ground.name = "RenderOnly.Ground"
    mat = bpy.data.materials.new("RenderOnly.Ground.Mat")
    mat.diffuse_color = (0.55, 0.56, 0.56, 1)
    ground.data.materials.append(mat)

    bpy.ops.object.light_add(type="AREA", location=(0, 0, 5.8))
    key = bpy.context.object
    key.name = "RenderOnly.AreaKey"
    key.data.energy = 480
    key.data.size = 5.5
    bpy.ops.object.light_add(type="SUN", location=(0, 0, 4))
    sun = bpy.context.object
    sun.name = "RenderOnly.Sun"
    sun.data.energy = 1.0
    sun.rotation_euler = (math.radians(42), 0, math.radians(35))

    bpy.ops.object.camera_add(location=(4.2, 5.4, 2.4))
    camera = bpy.context.object
    camera.name = "RenderOnly.Camera"
    camera.data.lens = 42
    scene.camera = camera


def create_human_reference() -> bpy.types.Object:
    mat = bpy.data.materials.new("RenderOnly.HumanReference.Mat")
    mat.diffuse_color = (0.2, 0.22, 0.24, 1)
    group = bpy.data.objects.new("RenderOnly.HumanReference", None)
    bpy.context.collection.objects.link(group)
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.13, depth=0.82, location=(-1.55, -1.78, 0.93))
    body = bpy.context.object
    body.name = "RenderOnly.HumanReference.Body"
    body.data.materials.append(mat)
    body.parent = group
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.13, location=(-1.55, -1.78, 1.46))
    head = bpy.context.object
    head.name = "RenderOnly.HumanReference.Head"
    head.data.materials.append(mat)
    head.parent = group
    for side in (-1, 1):
        bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=0.045, depth=0.82, location=(-1.55 + side * 0.09, -1.78, 0.32))
        leg = bpy.context.object
        leg.name = f"RenderOnly.HumanReference.Leg.{side}"
        leg.data.materials.append(mat)
        leg.parent = group
        bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=0.035, depth=0.62, location=(-1.55 + side * 0.19, -1.78, 0.98), rotation=(0.18, 0, 0))
        arm = bpy.context.object
        arm.name = f"RenderOnly.HumanReference.Arm.{side}"
        arm.data.materials.append(mat)
        arm.parent = group
    group.hide_render = True
    for child in group.children:
        child.hide_render = True
    return group


def set_door(angle_degrees: float) -> None:
    door = bpy.data.objects.get("Door.Driver")
    if door:
        door.rotation_euler[2] = math.radians(angle_degrees)


def set_front_steer(angle_degrees: float) -> None:
    for name, sign in (("SteerPivot.FL", 1), ("SteerPivot.FR", 1)):
        obj = bpy.data.objects.get(name)
        if obj:
            obj.rotation_euler[2] = math.radians(angle_degrees * sign)


def reset_pose() -> None:
    set_door(0)
    set_front_steer(0)
    for obj in bpy.context.scene.objects:
        if obj.name.startswith("Glass."):
            obj.hide_render = False
    human = bpy.data.objects.get("RenderOnly.HumanReference")
    if human:
        human.hide_render = True
        for child in human.children:
            child.hide_render = True


def render(
    name: str,
    camera_pos: tuple[float, float, float],
    target: tuple[float, float, float],
    *,
    door: float = 0,
    steer: float = 0,
    human: bool = False,
    lens: float = 42,
    hide_for_render: tuple[str, ...] = (),
) -> str:
    reset_pose()
    set_door(door)
    set_front_steer(steer)
    for object_name in hide_for_render:
        obj = bpy.data.objects.get(object_name)
        if obj:
            obj.hide_render = True
    human_ref = bpy.data.objects.get("RenderOnly.HumanReference")
    if human_ref:
        human_ref.hide_render = not human
        for child in human_ref.children:
            child.hide_render = not human

    camera = bpy.context.scene.camera
    camera.location = camera_pos
    camera.data.lens = lens
    look_at(camera, Vector(target))
    out = RENDER_DIR / name
    bpy.context.scene.render.filepath = str(out)
    bpy.ops.render.render(write_still=True)
    return str(out.relative_to(ROOT))


def main() -> None:
    setup_scene()
    create_human_reference()
    renders = [
        render("01-front-three-quarter.png", (3.7, 5.1, 2.2), (0, 0, 0.85)),
        render("02-rear-three-quarter.png", (-3.8, -5.0, 2.15), (0, -0.15, 0.85)),
        render("03-left-side.png", (-5.2, 0.0, 1.65), (0, 0, 0.82), lens=55),
        render("04-front.png", (0, 5.9, 1.55), (0, 0.35, 0.82), lens=55),
        render("05-interior-through-windshield.png", (0.05, 1.32, 1.36), (-0.2, -0.08, 0.92), lens=42),
        render("06-driver-door-open.png", (-3.3, 2.9, 1.8), (-0.55, 0.1, 0.92), door=-68),
        render("07-front-wheels-steered.png", (3.0, 4.2, 1.65), (0.35, 1.2, 0.55), steer=25, lens=58),
        render("08-human-scale-reference.png", (-5.8, 1.1, 1.8), (-0.35, -0.45, 0.92), door=-25, human=True, lens=42),
    ]

    report = json.loads(REPORT_JSON.read_text(encoding="utf-8")) if REPORT_JSON.exists() else {}
    report["visual_qa_renders"] = renders
    report["visual_qa_notes"] = {
        "lighting": "neutral area and sun lighting on plain grey ground",
        "temporary_human_reference": "created only inside render script and not saved/exported",
        "interior_render_override": "none; the windshield now has a real opening behind it in the source model.",
        "door_open_render_angle_degrees": -68,
        "front_wheel_steer_render_angle_degrees": 25,
    }
    REPORT_JSON.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({"renders": renders}, indent=2))


if __name__ == "__main__":
    main()
