"""Create the prepared Nexo World drivable sedan Blender source.

Implements the approved redesign in
assets-source/vehicles/drivable-sedan/redesign-brief.md: a single lofted
body shell with a closed greenhouse, integrated roof and A/B/C pillars,
shaped bonnet and boot, real door apertures with matching door skins,
inset glass, proper wheel arches, and a lightweight believable interior.
The original vehicle GLBs are not modified; they remain provenance only.
"""

from __future__ import annotations

import json
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
SOURCE_CAR_GLB = ROOT / "public/assets/vehicles/lowpoly-cars.glb"
SOURCE_TRUCK_GLB = ROOT / "public/assets/vehicles/truck.glb"
OUT_DIR = ROOT / "assets-source/vehicles/drivable-sedan"
OUT_BLEND = OUT_DIR / "DrivableSedan.blend"
REPORT_JSON = OUT_DIR / "asset-report.json"

FORWARD_AXIS = "+Y in Blender source; exported glTF faces -Z after Blender Y-up conversion"
MAX_DRIVER_DOOR_ANGLE_DEG = 68

# Wheel geometry (brief section 5).
WHEEL_Z = 0.325
TYRE_RADIUS = 0.325
TYRE_WIDTH = 0.215
TRACK_HALF = 0.77
AXLE_FRONT_Y = 1.34
AXLE_REAR_Y = -1.34
ARCH_RADIUS = 0.41

# Door apertures / hinges (brief section 4.4).
DOOR_APERTURE_FRONT_Y = 0.62
DOOR_APERTURE_REAR_Y = -1.175
HINGE_X = 0.865
FRONT_HINGE_Y = 0.62
FRONT_DOOR_TAIL_Y = -0.415
REAR_HINGE_Y = -0.45
REAR_DOOR_TAIL_Y = -1.165

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
    "Interior.Seat.Rear",
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

# Body loft stations, front to rear. Columns are:
# y, half width at z=0.15 (wl), z=0.34 (wr), z=0.62 (wd),
# shoulder half width (ws) at shoulder height (zs),
# beltline half width (wb) at beltline height (zb).
STATIONS = [
    (2.23, 0.600, 0.660, 0.710, 0.740, 0.680, 0.700, 0.800),
    (2.06, 0.720, 0.780, 0.830, 0.855, 0.710, 0.800, 0.840),
    (1.74, 0.780, 0.845, 0.885, 0.900, 0.740, 0.845, 0.875),
    (1.64, 0.785, 0.850, 0.885, 0.902, 0.750, 0.848, 0.885),
    (1.54, 0.788, 0.852, 0.888, 0.904, 0.755, 0.849, 0.893),
    (1.44, 0.790, 0.854, 0.889, 0.905, 0.760, 0.850, 0.900),
    (1.34, 0.790, 0.855, 0.890, 0.905, 0.770, 0.850, 0.908),
    (1.24, 0.790, 0.854, 0.889, 0.905, 0.775, 0.850, 0.916),
    (1.14, 0.788, 0.852, 0.888, 0.904, 0.780, 0.849, 0.924),
    (1.04, 0.785, 0.850, 0.885, 0.902, 0.790, 0.848, 0.930),
    (0.94, 0.780, 0.845, 0.880, 0.900, 0.800, 0.845, 0.938),
    (0.72, 0.780, 0.840, 0.875, 0.895, 0.840, 0.855, 0.955),
    (0.62, 0.780, 0.840, 0.875, 0.895, 0.850, 0.858, 0.960),
    (0.10, 0.780, 0.840, 0.875, 0.900, 0.860, 0.862, 0.968),
    (-0.42, 0.780, 0.840, 0.875, 0.900, 0.865, 0.862, 0.975),
    (-0.95, 0.775, 0.835, 0.870, 0.895, 0.870, 0.858, 0.982),
    (-1.06, 0.770, 0.830, 0.868, 0.893, 0.870, 0.856, 0.985),
    (-1.175, 0.770, 0.828, 0.865, 0.890, 0.875, 0.854, 0.990),
    (-1.25, 0.768, 0.826, 0.863, 0.888, 0.877, 0.852, 0.997),
    (-1.34, 0.765, 0.823, 0.860, 0.885, 0.880, 0.850, 1.005),
    (-1.43, 0.763, 0.820, 0.858, 0.883, 0.882, 0.848, 1.012),
    (-1.54, 0.760, 0.818, 0.855, 0.880, 0.885, 0.845, 1.025),
    (-1.64, 0.750, 0.810, 0.845, 0.870, 0.890, 0.835, 1.050),
    (-1.74, 0.740, 0.800, 0.835, 0.862, 0.890, 0.827, 1.045),
    (-1.96, 0.710, 0.770, 0.800, 0.830, 0.880, 0.795, 1.000),
    (-2.23, 0.600, 0.660, 0.700, 0.730, 0.800, 0.680, 0.875),
]

BONNET_MIN_Y = 0.55  # bonnet top patch extends under the windshield base
BOOT_MAX_Y = -1.64  # boot deck patch starts at the rear-glass base


def reset_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def make_material(
    name: str,
    color: tuple[float, float, float, float],
    *,
    metallic: float = 0.0,
    roughness: float = 0.5,
    alpha: float | None = None,
    emission: tuple[float, float, float] | None = None,
    emission_strength: float = 0.0,
    clearcoat: float = 0.0,
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if clearcoat > 0:
            for input_name in ("Coat Weight", "Clearcoat", "Coat"):
                if input_name in bsdf.inputs:
                    bsdf.inputs[input_name].default_value = clearcoat
                    break
        if alpha is not None:
            bsdf.inputs["Alpha"].default_value = alpha
            mat.blend_method = "BLEND"
            mat.show_transparent_back = True
        if emission is not None:
            bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
            bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat


def smooth_shade(obj: bpy.types.Object, angle_degrees: float = 42.0) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    try:
        bpy.ops.object.shade_auto_smooth(angle=math.radians(angle_degrees))
    except Exception:
        bpy.ops.object.shade_smooth()


def mesh_object(
    name: str,
    verts: list[tuple[float, float, float]],
    faces: list[tuple[int, ...]],
    material: bpy.types.Material,
    *,
    dedupe: bool = True,
) -> bpy.types.Object:
    mesh = bpy.data.meshes.new(f"{name}.Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    if dedupe:
        bm = bmesh.new()
        bm.from_mesh(mesh)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0006)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(mesh)
        bm.free()
        mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def create_box_object(
    name: str,
    location: tuple[float, float, float],
    size: tuple[float, float, float],
    material: bpy.types.Material,
    *,
    rotation: tuple[float, float, float] = (0, 0, 0),
    bevel_width: float = 0.014,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}.Mesh"
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.rotation_euler = rotation
    obj.data.materials.append(material)
    if bevel_width > 0:
        bevel = obj.modifiers.new(f"{name}.Bevel", "BEVEL")
        bevel.width = bevel_width
        bevel.segments = 1
    return obj


def create_beam(
    name: str,
    p1: tuple[float, float, float],
    p2: tuple[float, float, float],
    width: float,
    thickness: float,
    material: bpy.types.Material,
) -> bpy.types.Object:
    a, b = Vector(p1), Vector(p2)
    direction = b - a
    mid = (a + b) / 2
    bpy.ops.mesh.primitive_cube_add(size=1, location=tuple(mid))
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}.Mesh"
    obj.dimensions = (width, direction.length, thickness)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.rotation_euler = direction.to_track_quat("Y", "Z").to_euler()
    obj.data.materials.append(material)
    return obj


def parent_keep_world(child: bpy.types.Object, parent: bpy.types.Object) -> None:
    child.parent = parent
    child.matrix_parent_inverse = parent.matrix_world.inverted()


def join_into(target: bpy.types.Object, parts: list[bpy.types.Object]) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    target.select_set(True)
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.join()


# --- profile helpers -------------------------------------------------------


def arch_z(y: float) -> float:
    """Height of the wheel-arch opening rim at longitudinal position y."""
    best = 0.0
    for yc in (AXLE_FRONT_Y, AXLE_REAR_Y):
        dy = y - yc
        if abs(dy) < ARCH_RADIUS:
            best = max(best, WHEEL_Z + math.sqrt(ARCH_RADIUS**2 - dy * dy))
    return best


def profile_at(y: float) -> dict:
    ys = [s[0] for s in STATIONS]
    y = min(max(y, ys[-1]), ys[0])
    for i in range(len(STATIONS) - 1):
        y0, y1 = STATIONS[i][0], STATIONS[i + 1][0]
        if y1 <= y <= y0:
            t = 0.0 if y0 == y1 else (y0 - y) / (y0 - y1)
            row = [STATIONS[i][k] + t * (STATIONS[i + 1][k] - STATIONS[i][k]) for k in range(8)]
            return {
                "y": y,
                "levels": [
                    (row[1], 0.15),
                    (row[2], 0.34),
                    (row[3], 0.62),
                    (row[4], row[5]),
                    (row[6], row[7]),
                ],
                "wb": row[6],
                "zb": row[7],
            }
    raise ValueError(f"y {y} outside station table")


def side_x_at(levels: list[tuple[float, float]], z: float) -> float:
    if z <= levels[0][1]:
        return levels[0][0]
    for (w0, z0), (w1, z1) in zip(levels, levels[1:]):
        if z0 <= z <= z1:
            t = 0.0 if z1 == z0 else (z - z0) / (z1 - z0)
            return w0 + t * (w1 - w0)
    return levels[-1][0]


def ring_points(profile: dict) -> list[tuple[float, float]]:
    """Side ring (half width, z) with wheel-arch clamping applied."""
    az = arch_z(profile["y"])
    pts = []
    for w, z in profile["levels"]:
        if az > 0.0 and z < az:
            pts.append((side_x_at(profile["levels"], az), az))
        else:
            pts.append((w, z))
    return pts


def bonnet_crown(y: float) -> tuple[float, float]:
    t = (2.23 - y) / (2.23 - BONNET_MIN_Y)
    c2 = 0.035 + 0.030 * max(0.0, min(1.0, t))
    return 0.65 * c2, c2


# --- body shell ------------------------------------------------------------


def create_body_shell(material: bpy.types.Material) -> bpy.types.Object:
    verts: list[tuple[float, float, float]] = []
    faces: list[tuple[int, ...]] = []

    left: list[list[int]] = []
    right: list[list[int]] = []
    bonnet_rows: list[tuple[int, list[int]]] = []  # (station index, interior indices)
    boot_rows: list[tuple[int, list[int]]] = []

    def add(v: tuple[float, float, float]) -> int:
        verts.append(v)
        return len(verts) - 1

    ys = [s[0] for s in STATIONS]
    for si, y in enumerate(ys):
        profile = profile_at(y)
        ring = ring_points(profile)
        left.append([add((-w, y, z)) for w, z in ring])
        right.append([add((w, y, z)) for w, z in ring])
        wb, zb = profile["wb"], profile["zb"]
        if y >= DOOR_APERTURE_FRONT_Y - 1e-6:
            c1, c2 = bonnet_crown(y)
            bonnet_rows.append((si, [add((-0.55 * wb, y, zb + c1)), add((0.0, y, zb + c2)), add((0.55 * wb, y, zb + c1))]))
        if y <= BOOT_MAX_Y + 1e-6:
            boot_rows.append((si, [add((-0.55 * wb, y, zb + 0.02)), add((0.0, y, zb + 0.03)), add((0.55 * wb, y, zb + 0.02))]))

    # Extra bonnet row tucked under the windshield base so the cowl is sealed.
    cowl = profile_at(BONNET_MIN_Y)
    c1, c2 = bonnet_crown(BONNET_MIN_Y)
    cowl_edge_l = add((-cowl["wb"], BONNET_MIN_Y, cowl["zb"]))
    cowl_edge_r = add((cowl["wb"], BONNET_MIN_Y, cowl["zb"]))
    cowl_row = [add((-0.55 * cowl["wb"], BONNET_MIN_Y, cowl["zb"] + c1)), add((0.0, BONNET_MIN_Y, cowl["zb"] + c2)), add((0.55 * cowl["wb"], BONNET_MIN_Y, cowl["zb"] + c1))]

    def in_aperture(ya: float, yb: float) -> bool:
        lo, hi = DOOR_APERTURE_REAR_Y - 1e-6, DOOR_APERTURE_FRONT_Y + 1e-6
        return lo <= ya <= hi and lo <= yb <= hi

    for i in range(len(ys) - 1):
        la, lb = left[i], left[i + 1]
        ra, rb = right[i], right[i + 1]
        aperture = in_aperture(ys[i], ys[i + 1])
        for lvl in range(4):
            if aperture and lvl >= 1:
                continue
            faces.append((la[lvl], lb[lvl], lb[lvl + 1], la[lvl + 1]))
            faces.append((ra[lvl + 1], rb[lvl + 1], rb[lvl], ra[lvl]))
        faces.append((la[0], ra[0], rb[0], lb[0]))  # underbody strip

    def patch_faces(rows: list[tuple[int, list[int]]]) -> None:
        for (sa, ia), (sb, ib) in zip(rows, rows[1:]):
            ra = [left[sa][4], *ia, right[sa][4]]
            rb = [left[sb][4], *ib, right[sb][4]]
            for k in range(4):
                faces.append((ra[k], rb[k], rb[k + 1], ra[k + 1]))

    patch_faces(bonnet_rows)
    # Bridge the last bonnet station to the extra cowl row.
    last_si, last_int = bonnet_rows[-1]
    ra = [left[last_si][4], *last_int, right[last_si][4]]
    rb = [cowl_edge_l, *cowl_row, cowl_edge_r]
    for k in range(4):
        faces.append((ra[k], rb[k], rb[k + 1], ra[k + 1]))
    patch_faces(boot_rows)

    # Nose and tail caps.
    nose_int = bonnet_rows[0][1]
    faces.append((*left[0], *nose_int, *reversed(right[0])))
    tail_int = boot_rows[-1][1]
    faces.append((*reversed(left[-1]), *[i for i in reversed(tail_int)], *right[-1]))

    body = mesh_object("Body", verts, faces, material)
    return body


def create_roof(material: bpy.types.Material) -> bpy.types.Object:
    rows = [
        (0.06, 0.630, 1.425),
        (-0.30, 0.640, 1.438),
        (-0.70, 0.638, 1.433),
        (-1.00, 0.625, 1.415),
        (-1.20, 0.605, 1.372),
    ]
    verts: list[tuple[float, float, float]] = []
    faces: list[tuple[int, ...]] = []
    for y, we, ze in rows:
        verts.extend(
            [
                (-we, y, ze),
                (-0.55 * we, y, ze + 0.020),
                (0.0, y, ze + 0.028),
                (0.55 * we, y, ze + 0.020),
                (we, y, ze),
            ]
        )
    for i in range(len(rows) - 1):
        a, b = i * 5, (i + 1) * 5
        for k in range(4):
            faces.append((a + k, b + k, b + k + 1, a + k + 1))
    return mesh_object("Body.RoofPanel", verts, faces, material, dedupe=False)


def create_sail_panels(material: bpy.types.Material) -> list[bpy.types.Object]:
    panels = []
    for name, s in (("Body.Sail.Left", -1), ("Body.Sail.Right", 1)):
        pts = [
            (s * 0.855, -1.10, 0.988),
            (s * 0.632, -1.05, 1.402),
            (s * 0.605, -1.21, 1.370),
            (s * 0.840, -1.62, 1.055),
            (s * 0.850, -1.38, 1.015),
        ]
        panels.append(mesh_object(name, pts, [(0, 1, 2, 3, 4)], material, dedupe=False))
    return panels


def create_pillars(paint: bpy.types.Material, trim: bpy.types.Material) -> list[bpy.types.Object]:
    parts = []
    for side, s in (("Left", -1), ("Right", 1)):
        parts.append(create_beam(f"Body.Pillar.A.{side}", (s * 0.862, 0.63, 0.968), (s * 0.650, 0.07, 1.428), 0.095, 0.060, paint))
        parts.append(create_beam(f"Body.Pillar.B.{side}", (s * 0.862, -0.43, 0.975), (s * 0.640, -0.43, 1.433), 0.100, 0.050, trim))
        parts.append(create_beam(f"Body.Pillar.B.Lower.{side}", (s * 0.845, -0.4325, 0.37), (s * 0.862, -0.4325, 0.975), 0.055, 0.050, trim))
    return parts


def create_arch_liners(material: bpy.types.Material) -> list[bpy.types.Object]:
    liners = []
    for name, s, yc in (
        ("Body.ArchLiner.FL", -1, AXLE_FRONT_Y),
        ("Body.ArchLiner.FR", 1, AXLE_FRONT_Y),
        ("Body.ArchLiner.RL", -1, AXLE_REAR_Y),
        ("Body.ArchLiner.RR", 1, AXLE_REAR_Y),
    ):
        verts: list[tuple[float, float, float]] = []
        faces: list[tuple[int, ...]] = []
        steps = 9
        r = ARCH_RADIUS - 0.012
        for i in range(steps + 1):
            t = math.pi * i / steps
            yy = yc - r * math.cos(t)
            zz = WHEEL_Z + r * math.sin(t)
            verts.append((s * 0.58, yy, max(zz, 0.16)))
            verts.append((s * 0.90, yy, max(zz, 0.16)))
        for i in range(steps):
            a = i * 2
            faces.append((a, a + 2, a + 3, a + 1))
        cap = len(verts)
        verts.append((s * 0.58, yc, WHEEL_Z))
        for i in range(steps):
            faces.append((i * 2, cap, (i + 1) * 2))
        liners.append(mesh_object(name, verts, faces, material, dedupe=False))
    return liners


def create_mirrors(paint: bpy.types.Material, trim: bpy.types.Material) -> list[bpy.types.Object]:
    parts = []
    for side, s in (("Left", -1), ("Right", 1)):
        parts.append(create_box_object(f"Body.Mirror.{side}", (s * 0.90, 0.52, 1.05), (0.13, 0.09, 0.10), paint, bevel_width=0.02))
        parts.append(create_box_object(f"Body.Mirror.Stalk.{side}", (s * 0.855, 0.535, 1.015), (0.09, 0.05, 0.035), trim, bevel_width=0.008))
        parts.append(create_box_object(f"Body.Mirror.Glass.{side}", (s * 0.90, 0.472, 1.05), (0.11, 0.012, 0.082), trim, bevel_width=0.004))
    return parts


# --- doors and glass -------------------------------------------------------


def door_ring(y: float) -> list[tuple[float, float]]:
    profile = profile_at(y)
    az = arch_z(y)
    levels = [
        (profile["levels"][1][0], 0.345),
        profile["levels"][2],
        profile["levels"][3],
        (profile["wb"], profile["zb"]),
    ]
    pts = []
    for w, z in levels:
        if az > 0.0 and z < az:
            pts.append((side_x_at(profile["levels"], az), az))
        else:
            pts.append((w, z))
    return pts


def create_door(
    name: str,
    s: int,
    hinge_y: float,
    tail_y: float,
    sub_ys: list[float],
    paint: bpy.types.Material,
    trim: bpy.types.Material,
    interior: bpy.types.Material,
    frame_beams: list[tuple[tuple[float, float, float], tuple[float, float, float]]],
    handle_y: float,
) -> bpy.types.Object:
    hinge = Vector((s * HINGE_X, hinge_y, 0.66))
    verts: list[tuple[float, float, float]] = []
    faces: list[tuple[int, ...]] = []
    for y in sub_ys:
        ring = door_ring(y)
        base = len(verts)
        for w, z in ring:
            verts.append((s * w - hinge.x, y - hinge.y, z - hinge.z))
        if base > 0:
            a, b = base - 4, base
            for k in range(3):
                faces.append((a + k, b + k, b + k + 1, a + k + 1))
    mesh = bpy.data.meshes.new(f"{name}.Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0006)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    door = bpy.data.objects.new(name, mesh)
    door.location = hinge
    bpy.context.collection.objects.link(door)
    door.data.materials.append(paint)
    solidify = door.modifiers.new(f"{name}.Solidify", "SOLIDIFY")
    solidify.thickness = 0.02
    solidify.offset = -1
    bevel = door.modifiers.new(f"{name}.Bevel", "BEVEL")
    bevel.width = 0.012
    bevel.segments = 1
    door["hinge_axis"] = "local Z vertical in Blender source"
    door["max_open_angle_degrees"] = MAX_DRIVER_DOOR_ANGLE_DEG

    for i, (p1, p2) in enumerate(frame_beams):
        beam = create_beam(f"{name}.Frame.{i}", p1, p2, 0.040, 0.034, trim)
        parent_keep_world(beam, door)

    handle = create_box_object(f"{name}.Handle", (s * 0.884, handle_y, 0.905), (0.018, 0.15, 0.030), trim, bevel_width=0.005)
    parent_keep_world(handle, door)

    y_mid = (hinge_y + tail_y) / 2
    inner = create_box_object(f"{name}.InnerPanel", (s * 0.850, y_mid, 0.68), (0.016, abs(hinge_y - tail_y) - 0.05, 0.52), interior, bevel_width=0.01)
    parent_keep_world(inner, door)
    armrest = create_box_object(f"{name}.Armrest", (s * 0.828, y_mid, 0.72), (0.05, min(0.42, abs(hinge_y - tail_y) * 0.5), 0.04), interior, bevel_width=0.012)
    parent_keep_world(armrest, door)
    return door


def glass_from_rows(name: str, rows: list[list[tuple[float, float, float]]], material: bpy.types.Material) -> bpy.types.Object:
    verts: list[tuple[float, float, float]] = []
    faces: list[tuple[int, ...]] = []
    cols = len(rows[0])
    for row in rows:
        verts.extend(row)
    for i in range(len(rows) - 1):
        a, b = i * cols, (i + 1) * cols
        for k in range(cols - 1):
            faces.append((a + k, b + k, b + k + 1, a + k + 1))
    obj = mesh_object(name, verts, faces, material, dedupe=False)
    solidify = obj.modifiers.new(f"{name}.Solidify", "SOLIDIFY")
    solidify.thickness = 0.006
    obj["glass"] = True
    return obj


def create_glass_set(material: bpy.types.Material) -> dict[str, bpy.types.Object]:
    windshield = glass_from_rows(
        "Glass.Windshield",
        [
            [(-0.580, 0.64, 1.005), (-0.320, 0.64, 1.025), (0.0, 0.64, 1.033), (0.320, 0.64, 1.025), (0.580, 0.64, 1.005)],
            [(-0.600, 0.34, 1.215), (-0.335, 0.34, 1.238), (0.0, 0.34, 1.247), (0.335, 0.34, 1.238), (0.600, 0.34, 1.215)],
            [(-0.615, 0.055, 1.415), (-0.340, 0.055, 1.432), (0.0, 0.055, 1.440), (0.340, 0.055, 1.432), (0.615, 0.055, 1.415)],
        ],
        material,
    )
    rear = glass_from_rows(
        "Glass.Rear",
        [
            [(-0.565, -1.60, 1.085), (-0.310, -1.60, 1.100), (0.0, -1.60, 1.106), (0.310, -1.60, 1.100), (0.565, -1.60, 1.085)],
            [(-0.578, -1.19, 1.360), (-0.320, -1.19, 1.375), (0.0, -1.19, 1.381), (0.320, -1.19, 1.375), (0.578, -1.19, 1.360)],
        ],
        material,
    )
    sides = {}
    for name, s in (("Glass.Side.Driver", -1), ("Glass.Side.Passenger", 1)):
        sides[name] = glass_from_rows(
            name,
            [
                [(s * 0.860, 0.58, 0.985), (s * 0.860, -0.40, 0.988)],
                [(s * 0.648, 0.07, 1.418), (s * 0.640, -0.40, 1.425)],
            ],
            material,
        )
    for name, s in (("Glass.Side.Rear.Left", -1), ("Glass.Side.Rear.Right", 1)):
        sides[name] = glass_from_rows(
            name,
            [
                [(s * 0.858, -0.46, 0.990), (s * 0.852, -1.13, 0.995)],
                [(s * 0.642, -0.46, 1.427), (s * 0.630, -1.05, 1.395)],
            ],
            material,
        )
    return {"Glass.Windshield": windshield, "Glass.Rear": rear, **sides}


# --- wheels ----------------------------------------------------------------


def create_wheel(
    name: str,
    x: float,
    y: float,
    tyre_mat: bpy.types.Material,
    rim_mat: bpy.types.Material,
    dark_mat: bpy.types.Material,
    parent: bpy.types.Object | None,
) -> bpy.types.Object:
    s = 1 if x > 0 else -1
    bpy.ops.mesh.primitive_cylinder_add(vertices=36, radius=TYRE_RADIUS, depth=TYRE_WIDTH, location=(x, y, WHEEL_Z), rotation=(0, math.pi / 2, 0))
    tyre = bpy.context.object
    tyre.name = name
    tyre.data.name = f"{name}.Mesh"
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    tyre.data.materials.append(tyre_mat)
    tyre["spin_axis"] = "local X"
    bevel = tyre.modifiers.new(f"{name}.TyreBevel", "BEVEL")
    bevel.width = 0.05
    bevel.segments = 2
    smooth_shade(tyre)
    if parent:
        parent_keep_world(tyre, parent)

    def sub_cylinder(sub_name: str, radius: float, depth: float, x_offset: float, mat: bpy.types.Material, verts: int = 24) -> bpy.types.Object:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=(x + s * x_offset, y, WHEEL_Z), rotation=(0, math.pi / 2, 0))
        obj = bpy.context.object
        obj.name = sub_name
        obj.data.name = f"{sub_name}.Mesh"
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
        obj.data.materials.append(mat)
        smooth_shade(obj)
        parent_keep_world(obj, tyre)
        return obj

    sub_cylinder(f"{name}.Rim", 0.215, 0.050, 0.055, rim_mat)
    sub_cylinder(f"{name}.Barrel", 0.195, 0.160, 0.0, dark_mat, verts=18)
    sub_cylinder(f"{name}.Cap", 0.045, 0.028, 0.075, dark_mat, verts=12)
    for i in range(10):
        a = i * math.pi / 5 + (0.15 if i % 2 else 0.0)
        spoke = create_box_object(
            f"{name}.Spoke.{i}",
            (x + s * 0.062, y + 0.10 * math.sin(a), WHEEL_Z + 0.10 * math.cos(a)),
            (0.040, 0.034, 0.175),
            rim_mat,
            rotation=(a, 0, 0),
            bevel_width=0.004,
        )
        parent_keep_world(spoke, tyre)
    return tyre


# --- interior --------------------------------------------------------------


def create_seat(name: str, x: float, fabric: bpy.types.Material) -> bpy.types.Object:
    base = create_box_object(name, (x, -0.12, 0.615), (0.48, 0.52, 0.15), fabric, bevel_width=0.045)
    back = create_box_object(f"{name}.Back", (x, -0.335, 0.875), (0.48, 0.11, 0.50), fabric, rotation=(-0.18, 0, 0), bevel_width=0.035)
    head = create_box_object(f"{name}.Headrest", (x, -0.385, 1.15), (0.24, 0.075, 0.14), fabric, bevel_width=0.025)
    join_into(base, [back, head])
    base.name = name
    return base


def create_rear_bench(fabric: bpy.types.Material) -> bpy.types.Object:
    base = create_box_object("Interior.Seat.Rear", (0, -0.97, 0.585), (1.34, 0.52, 0.15), fabric, bevel_width=0.045)
    back = create_box_object("Interior.Seat.Rear.Back", (0, -1.16, 0.84), (1.34, 0.12, 0.44), fabric, rotation=(-0.14, 0, 0), bevel_width=0.035)
    hl = create_box_object("Interior.Seat.Rear.Head.L", (-0.34, -1.20, 1.07), (0.26, 0.075, 0.13), fabric, bevel_width=0.02)
    hr = create_box_object("Interior.Seat.Rear.Head.R", (0.34, -1.20, 1.07), (0.26, 0.075, 0.13), fabric, bevel_width=0.02)
    join_into(base, [back, hl, hr])
    base.name = "Interior.Seat.Rear"
    return base


def create_dashboard(dark: bpy.types.Material) -> bpy.types.Object:
    slab = create_box_object("Interior.Dashboard", (0, 0.51, 0.845), (1.58, 0.22, 0.27), dark, bevel_width=0.03)
    roll = create_box_object("Interior.Dashboard.Roll", (0, 0.47, 0.975), (1.50, 0.30, 0.05), dark, bevel_width=0.02)
    cowl = create_box_object("Interior.Dashboard.Cowl", (-0.40, 0.44, 1.00), (0.32, 0.16, 0.05), dark, bevel_width=0.015)
    screen = create_box_object("Interior.Dashboard.Screen", (0, 0.42, 1.00), (0.26, 0.02, 0.13), dark, rotation=(-0.15, 0, 0), bevel_width=0.006)
    join_into(slab, [roll, cowl, screen])
    slab.name = "Interior.Dashboard"
    return slab


def create_steering_wheel(dark: bpy.types.Material) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(major_radius=0.165, minor_radius=0.017, major_segments=24, minor_segments=8, location=(-0.40, 0.30, 0.975), rotation=(1.25, 0, 0))
    wheel = bpy.context.object
    wheel.name = "Interior.SteeringWheel"
    wheel.data.name = "Interior.SteeringWheel.Mesh"
    wheel.data.materials.append(dark)
    smooth_shade(wheel)
    hub = create_box_object("Interior.SteeringWheel.Hub", (-0.40, 0.30, 0.975), (0.07, 0.05, 0.07), dark, rotation=(1.25, 0, 0), bevel_width=0.01)
    spokes = []
    for i, a in enumerate((math.radians(200), math.radians(340), math.radians(90))):
        spoke = create_box_object(
            f"Interior.SteeringWheel.Spoke.{i}",
            (-0.40, 0.30, 0.975),
            (0.30 if i < 2 else 0.04, 0.02, 0.04 if i < 2 else 0.15),
            dark,
            rotation=(1.25, a if i >= 2 else 0, a if i < 2 else 0),
            bevel_width=0.004,
        )
        spokes.append(spoke)
    join_into(wheel, [hub, *spokes])
    wheel.name = "Interior.SteeringWheel"
    return wheel


def create_cabin_shell(dark: bpy.types.Material) -> list[bpy.types.Object]:
    floor = create_box_object("Interior.Floor", (0, -0.45, 0.44), (1.62, 2.15, 0.05), dark, bevel_width=0.01)
    hump = create_box_object("Interior.Floor.Hump", (0, -0.05, 0.50), (0.24, 1.30, 0.11), dark, bevel_width=0.015)
    sill_l = create_box_object("Interior.Floor.Sill.L", (-0.79, -0.28, 0.42), (0.07, 1.75, 0.15), dark, bevel_width=0.01)
    sill_r = create_box_object("Interior.Floor.Sill.R", (0.79, -0.28, 0.42), (0.07, 1.75, 0.15), dark, bevel_width=0.01)
    join_into(floor, [hump, sill_l, sill_r])
    floor.name = "Interior.Floor"
    firewall = create_box_object("Interior.Firewall", (0, 0.585, 0.71), (1.58, 0.05, 0.54), dark, bevel_width=0.01)
    shelf = create_box_object("Interior.ParcelShelf", (0, -1.38, 1.00), (1.46, 0.50, 0.04), dark, bevel_width=0.01)
    console = create_box_object("Interior.CenterConsole", (0, 0.02, 0.565), (0.22, 0.88, 0.17), dark, bevel_width=0.02)
    knob = create_box_object("Interior.CenterConsole.Knob", (0, 0.15, 0.675), (0.045, 0.045, 0.06), dark, bevel_width=0.012)
    join_into(console, [knob])
    console.name = "Interior.CenterConsole"
    return [floor, firewall, shelf, console]


# --- lights and trim -------------------------------------------------------


def create_lights_and_trim(mats: dict[str, bpy.types.Material]) -> None:
    trim = mats["Trim.Black"]
    for side, s in (("Left", -1), ("Right", 1)):
        rot = (0, 0, s * -math.radians(18))
        create_box_object(f"Trim.Socket.Head.{side}", (s * 0.53, 2.115, 0.775), (0.47, 0.06, 0.155), trim, rotation=rot, bevel_width=0.006)
        create_box_object(f"Light.Head.{side}", (s * 0.52, 2.14, 0.775), (0.44, 0.10, 0.125), mats["Headlight.Off"], rotation=rot, bevel_width=0.01)
        rot_rear = (0, 0, s * math.radians(15))
        create_box_object(f"Trim.Socket.Brake.{side}", (s * 0.51, -2.16, 0.80), (0.52, 0.06, 0.135), trim, rotation=rot_rear, bevel_width=0.006)
        create_box_object(f"Light.Brake.{side}", (s * 0.50, -2.185, 0.80), (0.48, 0.10, 0.115), mats["BrakeLight.Off"], rotation=rot_rear, bevel_width=0.01)

    create_box_object("Trim.Grille.Upper", (0, 2.225, 0.71), (0.92, 0.05, 0.08), trim, bevel_width=0.008)
    create_box_object("Trim.Intake.Lower", (0, 2.225, 0.375), (0.98, 0.05, 0.17), trim, bevel_width=0.01)
    create_box_object("Trim.Plate.Front", (0, 2.235, 0.55), (0.50, 0.04, 0.12), trim, bevel_width=0.006)
    create_box_object("Trim.Valance.Front", (0, 2.22, 0.225), (1.44, 0.055, 0.15), trim, bevel_width=0.012)
    create_box_object("Trim.Valance.Rear", (0, -2.22, 0.225), (1.42, 0.055, 0.15), trim, bevel_width=0.012)
    create_box_object("Trim.Plate.Rear", (0, -2.235, 0.58), (0.52, 0.04, 0.13), trim, bevel_width=0.006)


def create_empty(name: str, location: tuple[float, float, float], rotation: tuple[float, float, float] = (0, 0, 0), display_size: float = 0.18) -> bpy.types.Object:
    empty = bpy.data.objects.new(name, None)
    empty.empty_display_type = "PLAIN_AXES"
    empty.empty_display_size = display_size
    empty.location = location
    empty.rotation_euler = rotation
    bpy.context.collection.objects.link(empty)
    return empty


# --- assembly --------------------------------------------------------------


def create_assets() -> dict:
    reset_scene()
    mats = {
        "Body.Paint": make_material("Body.Paint", (0.10, 0.14, 0.19, 1), metallic=0.30, roughness=0.34, clearcoat=0.30),
        "Trim.Black": make_material("Trim.Black", (0.012, 0.013, 0.015, 1), roughness=0.60),
        "Glass.Transparent": make_material("Glass.Transparent", (0.10, 0.15, 0.18, 0.38), metallic=0.0, roughness=0.05, alpha=0.38),
        "Tyre.Rubber": make_material("Tyre.Rubber", (0.012, 0.012, 0.014, 1), roughness=0.85),
        "Rim.Metal": make_material("Rim.Metal", (0.42, 0.44, 0.46, 1), metallic=0.75, roughness=0.32),
        "Interior.Dark": make_material("Interior.Dark", (0.028, 0.030, 0.034, 1), roughness=0.68),
        "Seat.Fabric": make_material("Seat.Fabric", (0.055, 0.058, 0.062, 1), roughness=0.75),
        "Headlight.Off": make_material("Headlight.Off", (0.85, 0.90, 0.94, 1), roughness=0.15, emission=(0.75, 0.85, 1.0), emission_strength=0.0),
        "BrakeLight.Off": make_material("BrakeLight.Off", (0.42, 0.015, 0.012, 1), roughness=0.24, emission=(1.0, 0.04, 0.02), emission_strength=0.0),
        "Collision.Proxy": make_material("Collision.Proxy", (0.2, 0.9, 1.0, 0.10), roughness=0.8, alpha=0.10),
    }

    body = create_body_shell(mats["Body.Paint"])
    roof = create_roof(mats["Body.Paint"])
    sails = create_sail_panels(mats["Body.Paint"])
    pillars = create_pillars(mats["Body.Paint"], mats["Trim.Black"])
    liners = create_arch_liners(mats["Trim.Black"])
    mirrors = create_mirrors(mats["Body.Paint"], mats["Trim.Black"])
    join_into(body, [roof, *sails, *pillars, *liners, *mirrors])
    body.name = "Body"
    bevel = body.modifiers.new("Body.Bevel", "BEVEL")
    bevel.width = 0.02
    bevel.segments = 1
    body.modifiers.new("Body.WeightedNormals", "WEIGHTED_NORMAL")
    smooth_shade(body)

    # Doors. Driver side is Blender -X, passenger side +X.
    front_frames_l = [
        ((-0.868, 0.60, 0.975), (-0.652, 0.06, 1.425)),
        ((-0.652, 0.06, 1.425), (-0.647, -0.40, 1.432)),
        ((-0.868, -0.40, 0.978), (-0.647, -0.40, 1.432)),
    ]
    rear_frames_l = [
        ((-0.868, -0.46, 0.978), (-0.645, -0.46, 1.433)),
        ((-0.645, -0.46, 1.433), (-0.634, -1.06, 1.400)),
        ((-0.634, -1.06, 1.400), (-0.858, -1.15, 0.992)),
    ]

    def mirror_beams(beams):
        return [tuple(((-p[0], p[1], p[2]) for p in pair)) for pair in beams]

    front_sub = [FRONT_HINGE_Y, 0.36, 0.10, -0.16, FRONT_DOOR_TAIL_Y]
    rear_sub = [REAR_HINGE_Y, -0.63, -0.81, -0.95, -1.06, -1.10, REAR_DOOR_TAIL_Y]
    doors = {
        "Door.Driver": create_door("Door.Driver", -1, FRONT_HINGE_Y, FRONT_DOOR_TAIL_Y, front_sub, mats["Body.Paint"], mats["Trim.Black"], mats["Interior.Dark"], front_frames_l, -0.22),
        "Door.Passenger": create_door("Door.Passenger", 1, FRONT_HINGE_Y, FRONT_DOOR_TAIL_Y, front_sub, mats["Body.Paint"], mats["Trim.Black"], mats["Interior.Dark"], mirror_beams(front_frames_l), -0.22),
        "Door.Rear.Left": create_door("Door.Rear.Left", -1, REAR_HINGE_Y, REAR_DOOR_TAIL_Y, rear_sub, mats["Body.Paint"], mats["Trim.Black"], mats["Interior.Dark"], rear_frames_l, -1.00),
        "Door.Rear.Right": create_door("Door.Rear.Right", 1, REAR_HINGE_Y, REAR_DOOR_TAIL_Y, rear_sub, mats["Body.Paint"], mats["Trim.Black"], mats["Interior.Dark"], mirror_beams(rear_frames_l), -1.00),
    }

    glass = create_glass_set(mats["Glass.Transparent"])
    parent_keep_world(glass["Glass.Side.Driver"], doors["Door.Driver"])
    parent_keep_world(glass["Glass.Side.Passenger"], doors["Door.Passenger"])
    parent_keep_world(glass["Glass.Side.Rear.Left"], doors["Door.Rear.Left"])
    parent_keep_world(glass["Glass.Side.Rear.Right"], doors["Door.Rear.Right"])

    pivot_fl = create_empty("SteerPivot.FL", (-TRACK_HALF, AXLE_FRONT_Y, WHEEL_Z))
    pivot_fr = create_empty("SteerPivot.FR", (TRACK_HALF, AXLE_FRONT_Y, WHEEL_Z))
    create_wheel("Wheel.FL", -TRACK_HALF, AXLE_FRONT_Y, mats["Tyre.Rubber"], mats["Rim.Metal"], mats["Trim.Black"], pivot_fl)
    create_wheel("Wheel.FR", TRACK_HALF, AXLE_FRONT_Y, mats["Tyre.Rubber"], mats["Rim.Metal"], mats["Trim.Black"], pivot_fr)
    create_wheel("Wheel.RL", -TRACK_HALF, AXLE_REAR_Y, mats["Tyre.Rubber"], mats["Rim.Metal"], mats["Trim.Black"], None)
    create_wheel("Wheel.RR", TRACK_HALF, AXLE_REAR_Y, mats["Tyre.Rubber"], mats["Rim.Metal"], mats["Trim.Black"], None)

    create_dashboard(mats["Interior.Dark"])
    create_steering_wheel(mats["Interior.Dark"])
    create_seat("Interior.Seat.Driver", -0.40, mats["Seat.Fabric"])
    create_seat("Interior.Seat.Passenger", 0.40, mats["Seat.Fabric"])
    create_rear_bench(mats["Seat.Fabric"])
    create_cabin_shell(mats["Interior.Dark"])

    create_lights_and_trim(mats)

    create_empty("Marker.DriverDoor", (-1.45, 0.20, 0.02), rotation=(0, 0, -1.5708))
    create_empty("Marker.Seat.Driver", (-0.40, -0.12, 0.90))
    create_empty("Marker.Exit.Left", (-1.55, 0.0, 0.02), rotation=(0, 0, -1.5708))
    create_empty("Marker.Camera.Chase", (0, -6.2, 2.25), rotation=(1.12, 0, 0))

    collision = create_box_object("Collision.Body", (0, 0.0, 0.70), (1.80, 4.46, 1.06), mats["Collision.Proxy"], bevel_width=0)
    collision.display_type = "WIRE"
    collision.hide_render = True

    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            obj["drivable_sedan_node"] = True
        obj["forward_axis"] = FORWARD_AXIS

    bpy.context.scene["source_model_used"] = "redesign-brief.md procedural sedan; lowpoly-cars.glb::car9 retired as visual reference"
    bpy.context.scene["forward_axis"] = FORWARD_AXIS
    bpy.context.scene["driver_door_max_open_angle_degrees"] = MAX_DRIVER_DOOR_ANGLE_DEG
    bpy.context.scene["dimensions_meters"] = "length=4.46,width_body=1.81,width_with_mirrors=1.93,height=1.47,wheelbase=2.68"
    return {"materials": mats, "body": body}


def object_bounds(obj: bpy.types.Object) -> dict:
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    mins = [min(getattr(c, axis) for c in corners) for axis in ("x", "y", "z")]
    maxs = [max(getattr(c, axis) for c in corners) for axis in ("x", "y", "z")]
    return {"min": mins, "max": maxs, "size": [maxs[i] - mins[i] for i in range(3)]}


def collect_report() -> dict:
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    triangles = sum(sum(max(1, len(poly.vertices) - 2) for poly in obj.data.polygons) for obj in mesh_objects)
    mins = [min(object_bounds(obj)["min"][i] for obj in mesh_objects if obj.name != "Collision.Body") for i in range(3)]
    maxs = [max(object_bounds(obj)["max"][i] for obj in mesh_objects if obj.name != "Collision.Body") for i in range(3)]
    nodes = sorted(obj.name for obj in bpy.context.scene.objects)
    wheel_origins = {
        name: list(bpy.data.objects[name].matrix_world.translation)
        for name in ["Wheel.FL", "Wheel.FR", "Wheel.RL", "Wheel.RR"]
    }
    markers = {
        name: {
            "location": list(bpy.data.objects[name].location),
            "rotation_euler": list(bpy.data.objects[name].rotation_euler),
        }
        for name in ["Marker.DriverDoor", "Marker.Seat.Driver", "Marker.Exit.Left", "Marker.Camera.Chase"]
    }
    return {
        "source_model_used": "procedural redesign per assets-source/vehicles/drivable-sedan/redesign-brief.md",
        "redesign": {
            "brief": "assets-source/vehicles/drivable-sedan/redesign-brief.md",
            "construction": "single lofted shell with closed greenhouse; doors cut from the same profile function; arch openings clamped into the loft",
            "previous_construction_removed": [
                "floating roof slab",
                "exposed vertical pillar posts",
                "plank front/rear fascias",
                "rectangular slab doors",
                "thin wheel-arch rings",
                "wagon-style wheels",
            ],
        },
        "dimensions_meters": {
            "length": round(maxs[1] - mins[1], 3),
            "width_body": 1.81,
            "width_with_mirrors": round(maxs[0] - mins[0], 3),
            "height": round(maxs[2] - mins[2], 3),
            "wheelbase": 2.68,
            "ground_clearance": 0.15,
            "tyre_outer_diameter": 2 * TYRE_RADIUS,
        },
        "forward_axis": FORWARD_AXIS,
        "triangle_count": triangles,
        "material_count": len(bpy.data.materials),
        "texture_sizes": [],
        "node_inventory": nodes,
        "required_nodes": REQUIRED_NODES,
        "door_opening_axis": "Door.* local Z in Blender source, vertical hinge at object origin",
        "door_opening_angle_degrees": MAX_DRIVER_DOOR_ANGLE_DEG,
        "door_hinges": {
            "Door.Driver": [-HINGE_X, FRONT_HINGE_Y, 0.66],
            "Door.Passenger": [HINGE_X, FRONT_HINGE_Y, 0.66],
            "Door.Rear.Left": [-HINGE_X, REAR_HINGE_Y, 0.66],
            "Door.Rear.Right": [HINGE_X, REAR_HINGE_Y, 0.66],
        },
        "glass_parenting": {
            "Glass.Side.Driver": "Door.Driver",
            "Glass.Side.Passenger": "Door.Passenger",
            "Glass.Side.Rear.Left": "Door.Rear.Left",
            "Glass.Side.Rear.Right": "Door.Rear.Right",
        },
        "wheel_spin_axes": {name: "local X" for name in ["Wheel.FL", "Wheel.FR", "Wheel.RL", "Wheel.RR"]},
        "steering_axes": {
            "SteerPivot.FL": "local Z vertical in Blender source",
            "SteerPivot.FR": "local Z vertical in Blender source",
        },
        "wheel_origins": wheel_origins,
        "marker_transforms": markers,
        "collision_proxy": object_bounds(bpy.data.objects["Collision.Body"]),
        "remaining_limitations": [
            "Procedural reconstruction is game-fidelity, not scan-quality; panel curvature is faceted at close range.",
            "No animation clips are included; runtime will animate nodes directly.",
            "Collision.Body is exported with a transparent proxy material and should be hidden by the runtime loader.",
        ],
    }


def main() -> None:
    if not SOURCE_CAR_GLB.exists():
        raise FileNotFoundError(SOURCE_CAR_GLB)
    if not SOURCE_TRUCK_GLB.exists():
        raise FileNotFoundError(SOURCE_TRUCK_GLB)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    create_assets()
    report = collect_report()
    REPORT_JSON.write_text(json.dumps(report, indent=2), encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND))
    print(f"WROTE {OUT_BLEND}")
    print(f"WROTE {REPORT_JSON}")


if __name__ == "__main__":
    main()
