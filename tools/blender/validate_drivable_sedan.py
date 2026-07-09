"""Validate the prepared and exported drivable sedan asset."""

from __future__ import annotations

import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
BLEND = ROOT / "assets-source/vehicles/drivable-sedan/DrivableSedan.blend"
GLB = ROOT / "public/assets/vehicles/drivable-sedan.glb"
REPORT_JSON = ROOT / "assets-source/vehicles/drivable-sedan/asset-report.json"

REQUIRED_NODES = [
    "Body",
    "Door.Driver",
    "Door.Passenger",
    "Door.Rear.Left",
    "Door.Rear.Right",
    "Wheel.FL",
    "Wheel.FR",
    "Wheel.RL",
    "Wheel.RR",
    "SteerPivot.FL",
    "SteerPivot.FR",
    "Glass.Windshield",
    "Glass.Rear",
    "Glass.Side.Driver",
    "Glass.Side.Passenger",
    "Glass.Side.Rear.Left",
    "Glass.Side.Rear.Right",
    "Interior.Dashboard",
    "Interior.SteeringWheel",
    "Interior.Seat.Driver",
    "Interior.Seat.Passenger",
    "Light.Head.Left",
    "Light.Head.Right",
    "Light.Brake.Left",
    "Light.Brake.Right",
    "Marker.DriverDoor",
    "Marker.Seat.Driver",
    "Marker.Exit.Left",
    "Marker.Camera.Chase",
    "Collision.Body",
]


def bounds(obj: bpy.types.Object) -> dict:
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    mins = [min(getattr(c, axis) for c in corners) for axis in ("x", "y", "z")]
    maxs = [max(getattr(c, axis) for c in corners) for axis in ("x", "y", "z")]
    return {"min": mins, "max": maxs, "size": [maxs[i] - mins[i] for i in range(3)]}


def triangle_count() -> int:
    return sum(
        sum(max(1, len(poly.vertices) - 2) for poly in obj.data.polygons)
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
    )


def assert_true(condition: bool, message: str, failures: list[str]) -> None:
    if not condition:
        failures.append(message)


def validate_current_scene(label: str) -> dict:
    failures: list[str] = []
    names = [obj.name for obj in bpy.context.scene.objects]
    for node in REQUIRED_NODES:
        assert_true(node in names, f"{label}: missing required node {node}", failures)
        assert_true(names.count(node) == 1, f"{label}: duplicate required node {node}", failures)

    for name in ["Marker.DriverDoor", "Marker.Seat.Driver", "Marker.Exit.Left", "Marker.Camera.Chase"]:
        obj = bpy.data.objects.get(name)
        assert_true(obj is not None and obj.type == "EMPTY", f"{label}: {name} must be an empty marker", failures)

    assert_true(not [obj for obj in bpy.context.scene.objects if obj.type in {"CAMERA", "LIGHT"}], f"{label}: cameras/lights should not be present", failures)

    for glass_name in [
        "Glass.Windshield",
        "Glass.Rear",
        "Glass.Side.Driver",
        "Glass.Side.Passenger",
        "Glass.Side.Rear.Left",
        "Glass.Side.Rear.Right",
    ]:
        obj = bpy.data.objects.get(glass_name)
        mat = obj.material_slots[0].material if obj and obj.material_slots else None
        assert_true(mat is not None and mat.blend_method in {"BLEND", "HASHED"}, f"{label}: {glass_name} material must be transparent", failures)

    driver = bpy.data.objects.get("Door.Driver")
    if driver:
        assert_true(abs(driver.location.x + 0.895) < 0.04, f"{label}: Door.Driver origin x not at hinge", failures)
        assert_true(abs(driver.location.y - 0.55) < 0.04, f"{label}: Door.Driver origin y not at hinge", failures)

    wheel_names = ["Wheel.FL", "Wheel.FR", "Wheel.RL", "Wheel.RR"]
    wheel_origins = []
    for name in wheel_names:
        obj = bpy.data.objects.get(name)
        if obj:
            wheel_origins.append(tuple(round(v, 3) for v in obj.matrix_world.translation))
    assert_true(len(set(wheel_origins)) == 4, f"{label}: wheel origins must be distinct", failures)

    for pivot in ["SteerPivot.FL", "SteerPivot.FR"]:
        assert_true(bpy.data.objects.get(pivot) is not None, f"{label}: missing {pivot}", failures)

    assert_true(bpy.data.objects.get("Collision.Body") is not None, f"{label}: missing collision proxy", failures)

    tris = triangle_count()
    assert_true(tris < 40000, f"{label}: triangle budget exceeded: {tris}", failures)
    assert_true(len(bpy.data.materials) <= 12, f"{label}: material budget too high: {len(bpy.data.materials)}", failures)

    render_meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.name != "Collision.Body"]
    mins = [min(bounds(obj)["min"][i] for obj in render_meshes) for i in range(3)]
    maxs = [max(bounds(obj)["max"][i] for obj in render_meshes) for i in range(3)]
    dims = {"width": maxs[0] - mins[0], "length": maxs[1] - mins[1], "height": maxs[2] - mins[2]}
    assert_true(4.2 <= dims["length"] <= 4.7, f"{label}: length out of range {dims['length']:.3f}", failures)
    assert_true(1.7 <= dims["width"] <= 1.98, f"{label}: width out of plausible range {dims['width']:.3f}", failures)
    assert_true(1.35 <= dims["height"] <= 1.6, f"{label}: height out of range {dims['height']:.3f}", failures)

    return {
        "label": label,
        "passed": not failures,
        "failures": failures,
        "triangle_count": tris,
        "material_count": len(bpy.data.materials),
        "dimensions_source_axes_meters": dims,
        "nodes": sorted(names),
        "wheel_origins": wheel_origins,
    }


def main() -> None:
    if not BLEND.exists():
        raise FileNotFoundError(BLEND)
    if not GLB.exists():
        raise FileNotFoundError(GLB)

    bpy.ops.wm.open_mainfile(filepath=str(BLEND))
    blend_result = validate_current_scene("blend")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(GLB))
    bpy.context.view_layer.update()
    glb_result = validate_current_scene("glb")

    report = json.loads(REPORT_JSON.read_text(encoding="utf-8")) if REPORT_JSON.exists() else {}
    report["validation"] = {
        "blend": blend_result,
        "glb_reload": glb_result,
        "passed": blend_result["passed"] and glb_result["passed"],
    }
    REPORT_JSON.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report["validation"], indent=2))
    if not report["validation"]["passed"]:
        sys.exit(1)


if __name__ == "__main__":
    main()
