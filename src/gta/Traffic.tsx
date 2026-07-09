/**
 * Instanced traffic using the imported GLB vehicle packs:
 *  - lowpoly-cars.glb (car2/car9, each with 4 "whell" children) — tinted
 *    per instance, wheels spin, body rolls into corners.
 *  - truck.glb (64-truck OBJ conversion) — runs the loops at truck speed.
 * Vehicles follow right-hand-lane loops with rounded corners; parked cars
 * share the same instanced meshes. Draw calls stay fixed (~18) no matter
 * how many vehicles are on the road.
 */
import { useEffect, useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import { useGLTF } from "@react-three/drei";
import * as THREE from "three";
import { mulberry32 } from "../village/seed";
import { ROAD_W, ROAD_Y, STREET_XS, STREET_ZS, type CarRoute, type CityPlan } from "./cityPlan";
import { signalAt } from "./signals";
import { makeBounds, makeObject, worldCollisionRegistry } from "./collision/CollisionRegistry";
import type { CollisionKind } from "./collision/CollisionTypes";

const CAR_URL = "/assets/vehicles/lowpoly-cars.glb";
const TRUCK_URL = "/assets/vehicles/truck.glb";

const CAR_LEN = 4.3;
const TRUCK_LEN = 8.8;
const POD_LEN = 4.6;
const CARS_PER_ROUTE = 2;
const TINTS = ["#ffffff", "#ff9d94", "#9db8ff", "#ffe08a", "#9fe6ac", "#d9d9e2", "#c9a2e8"];
const TRAFFIC_PREFIX = "traffic:";
const TRAFFIC_OBSTACLE_KINDS: ReadonlySet<CollisionKind> = new Set([
  "player",
  "pedestrian",
  "moving-car",
  "parked-car",
]);

function firstMaterial(m: THREE.Material | THREE.Material[]): THREE.Material {
  return Array.isArray(m) ? m[0] : m;
}

interface VehicleTemplate {
  bodyParts: Array<{ geo: THREE.BufferGeometry; mat: THREE.Material }>;
  wheelGeo: THREE.BufferGeometry | null;
  wheelMat: THREE.Material | null;
  wheelOffsets: THREE.Vector3[];
  tintable: boolean;
}

/** Autonomous shuttle pod from the concept art: white rounded body, dark
 * glass band, low skirt, small wheels. Built from primitives so it slots
 * into the same template pipeline as the GLB vehicles. */
function makePodModel(): THREE.Group {
  const g = new THREE.Group();
  const white = new THREE.MeshStandardMaterial({ color: "#eef0f2", roughness: 0.28, metalness: 0.25 });
  const glass = new THREE.MeshStandardMaterial({
    color: "#12181f",
    roughness: 0.08,
    metalness: 0.6,
    envMapIntensity: 1.4,
  });
  const dark = new THREE.MeshStandardMaterial({ color: "#24272b", roughness: 0.55, metalness: 0.35 });
  const trim = new THREE.MeshStandardMaterial({ color: "#3a3d42", roughness: 0.4, metalness: 0.6 });
  const headMat = new THREE.MeshStandardMaterial({
    color: "#dfeaf5",
    emissive: "#eaf2ff",
    emissiveIntensity: 0.9,
    roughness: 0.3,
  });
  const tailMat = new THREE.MeshStandardMaterial({
    color: "#5a1512",
    emissive: "#ff2a20",
    emissiveIntensity: 0.8,
    roughness: 0.4,
  });

  const box = (
    w: number, h: number, d: number,
    x: number, y: number, z: number,
    mat: THREE.Material,
    name = "",
  ) => {
    const mesh = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat);
    mesh.position.set(x, y, z);
    mesh.name = name;
    g.add(mesh);
    return mesh;
  };

  // Lower body / chassis skirt.
  box(2.02, 0.62, 4.5, 0, 0.62, 0, dark, "pod_skirt");
  // Main cabin (slightly inset above skirt).
  box(1.98, 1.02, 4.34, 0, 1.34, 0, white, "pod_cabin");
  // Curved roof cap: flat panel plus a rounded ridge.
  box(1.78, 0.22, 4.0, 0, 1.96, 0, white, "pod_roof");
  const ridge = new THREE.Mesh(
    new THREE.CylinderGeometry(0.5, 0.5, 3.9, 12, 1, false, 0, Math.PI),
    white,
  );
  ridge.rotation.z = Math.PI / 2;
  ridge.rotation.y = Math.PI / 2;
  ridge.scale.set(1, 1, 0.28);
  ridge.position.set(0, 1.98, 0);
  g.add(ridge);
  // Wraparound glazing band.
  box(2.04, 0.86, 4.12, 0, 1.5, 0, glass, "pod_glassband");
  // Raked front & rear windshields capping the band.
  const wsF = box(1.9, 0.9, 0.14, 0, 1.52, 2.14, glass, "pod_ws_f");
  wsF.rotation.x = -0.22;
  const wsR = box(1.9, 0.9, 0.14, 0, 1.52, -2.14, glass, "pod_ws_r");
  wsR.rotation.x = 0.22;
  // Belt-line trim between glass and skirt.
  box(2.06, 0.1, 4.4, 0, 0.98, 0, trim, "pod_belt");
  // Door seam lines (recessed dark strips) each side.
  for (const sx of [-1.02, 1.02]) {
    box(0.04, 0.9, 0.05, sx, 1.4, 0.0, trim);
    box(0.04, 0.9, 0.05, sx, 1.4, 1.3, trim);
    box(0.04, 0.9, 0.05, sx, 1.4, -1.3, trim);
  }
  // Bumpers.
  box(1.96, 0.34, 0.22, 0, 0.55, 2.28, trim, "pod_bumper_f");
  box(1.96, 0.34, 0.22, 0, 0.55, -2.28, trim, "pod_bumper_r");
  // Head- and tail-light strips.
  box(1.5, 0.14, 0.06, 0, 0.95, 2.27, headMat, "pod_head");
  box(1.5, 0.14, 0.06, 0, 0.95, -2.27, tailMat, "pod_tail");

  // Wheel arches (dark fenders) + wheels with metal hubs.
  const wheelGeo = new THREE.CylinderGeometry(0.36, 0.36, 0.26, 14);
  wheelGeo.rotateZ(Math.PI / 2);
  const hubGeo = new THREE.CylinderGeometry(0.15, 0.15, 0.28, 8);
  hubGeo.rotateZ(Math.PI / 2);
  const wheelMat = new THREE.MeshStandardMaterial({ color: "#131315", roughness: 0.85 });
  const hubMat = new THREE.MeshStandardMaterial({ color: "#9aa0a6", roughness: 0.35, metalness: 0.8 });
  const spots: Array<[number, number]> = [
    [-0.86, 1.5],
    [0.86, 1.5],
    [-0.86, -1.5],
    [0.86, -1.5],
  ];
  spots.forEach(([x, z], i) => {
    // Fender arch over the wheel.
    box(0.5, 0.5, 1.0, x, 0.72, z, dark);
    const wheel = new THREE.Mesh(wheelGeo.clone(), wheelMat);
    wheel.position.set(x, 0.36, z);
    wheel.name = `whell_pod_${i}`;
    g.add(wheel);
    const hub = new THREE.Mesh(hubGeo.clone(), hubMat);
    hub.position.set(x, 0.36, z);
    hub.name = `whell_hub_${i}`;
    g.add(hub);
  });
  return g;
}

/** Bake a GLB vehicle into a normalized template: forward along +z, ground
 * at y=0, given length; wheel meshes (cars) split out for spinning. */
function buildTemplate(
  root: THREE.Object3D,
  targetLen: number,
  spinWheels: boolean,
  tintable = spinWheels,
): VehicleTemplate {
  root.updateMatrixWorld(true);
  const meshes: THREE.Mesh[] = [];
  root.traverse((o) => {
    if ((o as THREE.Mesh).isMesh) meshes.push(o as THREE.Mesh);
  });
  const wheelMeshes = spinWheels ? meshes.filter((m) => /whe+l|tyre|tire/i.test(m.name)) : [];
  const bodyMeshes = meshes.filter((m) => !wheelMeshes.includes(m));

  const box = new THREE.Box3().setFromObject(root);
  const size = box.getSize(new THREE.Vector3());
  const center = box.getCenter(new THREE.Vector3());
  const rotY = size.x > size.z ? Math.PI / 2 : 0; // long axis becomes +z
  const scale = targetLen / Math.max(size.x, size.z);
  const M = new THREE.Matrix4()
    .makeScale(scale, scale, scale)
    .multiply(new THREE.Matrix4().makeRotationY(rotY))
    .multiply(new THREE.Matrix4().makeTranslation(-center.x, -box.min.y, -center.z));

  const bake = (m: THREE.Mesh) =>
    m.geometry.clone().applyMatrix4(new THREE.Matrix4().multiplyMatrices(M, m.matrixWorld));

  const bodyParts = bodyMeshes.map((m) => ({ geo: bake(m), mat: firstMaterial(m.material) }));

  let wheelGeo: THREE.BufferGeometry | null = null;
  let wheelMat: THREE.Material | null = null;
  const wheelOffsets: THREE.Vector3[] = [];
  for (const wm of wheelMeshes) {
    const g = bake(wm);
    g.computeBoundingBox();
    const c = g.boundingBox!.getCenter(new THREE.Vector3());
    wheelOffsets.push(c);
    if (!wheelGeo) {
      g.translate(-c.x, -c.y, -c.z);
      wheelGeo = g;
      wheelMat = firstMaterial(wm.material);
    } else {
      g.dispose();
    }
  }
  return { bodyParts, wheelGeo, wheelMat, wheelOffsets, tintable };
}

interface Vehicle {
  template: number;
  slot: number; // instance index within its template
  route: CarRoute | null; // null = parked
  dist: number;
  speed: number;
  cruise: number; // preferred speed when the road is clear
  pointer: number;
  roll: number;
  prevYaw: number;
  spin: number;
}

const DRIVE_ACCEL = 4.5;
const BRAKE = 9;

function vehicleId(index: number, moving: boolean): string {
  return `${TRAFFIC_PREFIX}${moving ? "moving" : "parked"}:${index}`;
}

function vehicleDimensions(template: number): { halfWidth: number; halfHeight: number; halfLength: number } {
  if (template === 2) return { halfWidth: 1.25, halfHeight: 1.45, halfLength: TRUCK_LEN / 2 };
  if (template === 3) return { halfWidth: 1.08, halfHeight: 1.05, halfLength: POD_LEN / 2 };
  return { halfWidth: 1.02, halfHeight: 0.95, halfLength: CAR_LEN / 2 };
}

function vehicleBounds(template: number, x: number, z: number, yaw: number) {
  const dim = vehicleDimensions(template);
  return makeBounds(x, ROAD_Y + dim.halfHeight, z, dim.halfWidth, dim.halfHeight, dim.halfLength, yaw);
}

/** Distance to the stop line of the next signalized intersection along the
 * current axis of travel, or Infinity when none is ahead. */
function stopLineDistance(x: number, z: number, yaw: number): { ns: boolean; d: number } | null {
  const fx = Math.sin(yaw);
  const fz = Math.cos(yaw);
  let ns: boolean;
  if (Math.abs(fz) > 0.92) ns = true;
  else if (Math.abs(fx) > 0.92) ns = false;
  else return null; // mid-corner: never stop inside the intersection
  const lines = ns ? STREET_ZS : STREET_XS;
  const coord = ns ? z : x;
  const dir = Math.sign(ns ? fz : fx);
  let best = Infinity;
  for (const line of lines) {
    const stop = line - dir * (ROAD_W / 2 + 1.6);
    const d = (stop - coord) * dir;
    if (d > -1.5 && d < best) best = d;
  }
  return best === Infinity ? null : { ns, d: best };
}

function sampleRoute(route: CarRoute, dist: number, car: Vehicle, out: THREE.Vector3): number {
  const count = route.points.length / 2;
  const d = dist % route.totalLen;
  if (route.cumLen[car.pointer] > d) car.pointer = 0;
  while (car.pointer < count - 1 && route.cumLen[car.pointer + 1] < d) car.pointer++;
  const i = car.pointer;
  const j = (i + 1) % count;
  const segStart = route.cumLen[i];
  const segLen = (j === 0 ? route.totalLen : route.cumLen[j]) - segStart;
  const t = segLen > 0 ? (d - segStart) / segLen : 0;
  const x0 = route.points[i * 2];
  const z0 = route.points[i * 2 + 1];
  const x1 = route.points[j * 2];
  const z1 = route.points[j * 2 + 1];
  out.set(x0 + (x1 - x0) * t, ROAD_Y, z0 + (z1 - z0) * t);
  return Math.atan2(x1 - x0, z1 - z0);
}

function wrapAngle(a: number): number {
  while (a > Math.PI) a -= Math.PI * 2;
  while (a < -Math.PI) a += Math.PI * 2;
  return a;
}

export function Traffic({ plan, seed }: { plan: CityPlan; seed: number }) {
  const carGltf = useGLTF(CAR_URL);
  const truckGltf = useGLTF(TRUCK_URL);

  const built = useMemo(() => {
    const rand = mulberry32(seed ^ 0x7ea4c0de);
    const carRoot = carGltf.scene;
    const templates = [
      buildTemplate(carRoot.getObjectByName("car2") ?? carRoot, CAR_LEN, true),
      buildTemplate(carRoot.getObjectByName("car9") ?? carRoot, CAR_LEN, true),
      buildTemplate(truckGltf.scene, TRUCK_LEN, false),
      buildTemplate(makePodModel(), POD_LEN, true, false), // autonomous shuttle pod
    ];
    // Automotive material read: glossier paint with environment reflections.
    for (const tpl of templates) {
      for (const part of tpl.bodyParts) {
        const mat = part.mat as THREE.MeshStandardMaterial;
        if (mat.isMeshStandardMaterial) {
          mat.roughness = Math.min(mat.roughness, 0.38);
          mat.metalness = Math.max(mat.metalness, 0.3);
          mat.envMapIntensity = 1.2;
        }
      }
      const wheelMat = tpl.wheelMat as THREE.MeshStandardMaterial | null;
      if (wheelMat && wheelMat.isMeshStandardMaterial) wheelMat.roughness = 0.85;
    }

    // Fleet: every 5th moving vehicle is the truck; cars alternate models.
    const vehicles: Vehicle[] = [];
    const counts = [0, 0, 0, 0];
    let g = 0;
    for (const route of plan.routes) {
      for (let k = 0; k < CARS_PER_ROUTE; k++, g++) {
        // Mix: cars, every 5th a truck, every 3rd a shuttle pod.
        const template = g % 5 === 4 ? 2 : g % 3 === 1 ? 3 : g % 2;
        const cruise =
          template === 2 ? 6.5 + rand() * 2.5 : template === 3 ? 7 + rand() * 2 : 8.5 + rand() * 5;
        vehicles.push({
          template,
          slot: counts[template]++,
          route,
          dist: rand() * route.totalLen,
          speed: cruise,
          cruise,
          pointer: 0,
          roll: 0,
          prevYaw: 0,
          spin: rand() * 6,
        });
      }
    }
    const parkedStart = vehicles.length;
    plan.parked.forEach((p, k) => {
      const template = k % 2;
      vehicles.push({
        template,
        slot: counts[template]++,
        route: null,
        dist: 0,
        speed: 0,
        cruise: 0,
        pointer: 0,
        roll: 0,
        prevYaw: p.yaw,
        spin: 0,
      });
    });

    // Instanced meshes per template part.
    const bodyInstances = templates.map((t, ti) =>
      t.bodyParts.map((part) => {
        const inst = new THREE.InstancedMesh(part.geo, part.mat, Math.max(1, counts[ti]));
        inst.count = counts[ti];
        inst.castShadow = true;
        inst.frustumCulled = false;
        return inst;
      }),
    );
    const wheelInstances = templates.map((t, ti) => {
      if (!t.wheelGeo || counts[ti] === 0) return null;
      const inst = new THREE.InstancedMesh(
        t.wheelGeo,
        t.wheelMat!,
        Math.max(1, counts[ti] * t.wheelOffsets.length),
      );
      inst.count = counts[ti] * t.wheelOffsets.length;
      inst.castShadow = true;
      inst.frustumCulled = false;
      return inst;
    });

    // Per-instance tints over the palette texture (cars only).
    const color = new THREE.Color();
    for (const v of vehicles) {
      if (!templates[v.template].tintable) continue;
      color.set(TINTS[Math.floor(rand() * TINTS.length)]);
      for (const inst of bodyInstances[v.template]) inst.setColorAt(v.slot, color);
    }
    for (const insts of bodyInstances) {
      for (const inst of insts) if (inst.instanceColor) inst.instanceColor.needsUpdate = true;
    }

    // Static poses for parked vehicles.
    const m = new THREE.Matrix4();
    const q = new THREE.Quaternion();
    const up = new THREE.Vector3(0, 1, 0);
    const one = new THREE.Vector3(1, 1, 1);
    const pos = new THREE.Vector3();
    const wp = new THREE.Vector3();
    plan.parked.forEach((p, k) => {
      const v = vehicles[parkedStart + k];
      const t = templates[v.template];
      q.setFromAxisAngle(up, p.yaw);
      pos.set(p.x, ROAD_Y, p.z);
      m.compose(pos, q, one);
      for (const inst of bodyInstances[v.template]) inst.setMatrixAt(v.slot, m);
      const wheels = wheelInstances[v.template];
      if (wheels) {
        t.wheelOffsets.forEach((off, w) => {
          wp.copy(off).applyQuaternion(q).add(pos);
          m.compose(wp, q, one);
          wheels.setMatrixAt(v.slot * t.wheelOffsets.length + w, m);
        });
      }
    });

    return { templates, vehicles, bodyInstances, wheelInstances, movingCount: parkedStart };
  }, [plan, seed, carGltf, truckGltf]);

  useEffect(() => {
    const pos = new THREE.Vector3();
    built.vehicles.forEach((vehicle, i) => {
      const moving = i < built.movingCount;
      const yaw = moving ? sampleRoute(vehicle.route!, vehicle.dist, vehicle, pos) : vehicle.prevYaw;
      const x = moving ? pos.x : plan.parked[i - built.movingCount].x;
      const z = moving ? pos.z : plan.parked[i - built.movingCount].z;
      const dim = vehicleDimensions(vehicle.template);
      worldCollisionRegistry.register(
        makeObject(
          vehicleId(i, moving),
          moving ? "moving-car" : "parked-car",
          { x, y: ROAD_Y + dim.halfHeight, z },
          { x: dim.halfWidth, y: dim.halfHeight, z: dim.halfLength },
          yaw,
          moving,
        ),
      );
    });
    return () => worldCollisionRegistry.unregisterPrefix(TRAFFIC_PREFIX);
  }, [built, plan]);

  const tmp = useRef({
    m: new THREE.Matrix4(),
    pos: new THREE.Vector3(),
    q: new THREE.Quaternion(),
    qRoll: new THREE.Quaternion(),
    qSpin: new THREE.Quaternion(),
    one: new THREE.Vector3(1, 1, 1),
    wp: new THREE.Vector3(),
    up: new THREE.Vector3(0, 1, 0),
    xAxis: new THREE.Vector3(1, 0, 0),
    zAxis: new THREE.Vector3(0, 0, 1),
  });

  useFrame(({ clock }, rawDelta) => {
    const dt = Math.min(rawDelta, 0.05);
    const t = tmp.current;
    const { templates, vehicles, bodyInstances, wheelInstances, movingCount } = built;
    const sig = signalAt(clock.elapsedTime);

    for (let i = 0; i < movingCount; i++) {
      const v = vehicles[i];
      const tpl = templates[v.template];

      // Obey the signal ahead: ease to a stop at the line, resume on green.
      const yaw0 = sampleRoute(v.route!, v.dist, v, t.pos);
      let target = v.cruise;
      const dim = vehicleDimensions(v.template);
      const forward = { x: Math.sin(yaw0), z: Math.cos(yaw0) };
      const obstacle = worldCollisionRegistry.raycastCorridor({
        origin: {
          x: t.pos.x + forward.x * (dim.halfLength + 0.35),
          y: ROAD_Y + dim.halfHeight,
          z: t.pos.z + forward.z * (dim.halfLength + 0.35),
        },
        direction: forward,
        length: Math.max(12, (v.speed * v.speed) / (2 * BRAKE) + dim.halfLength + 12),
        halfWidth: dim.halfWidth + 0.28,
        ignoreIds: new Set([vehicleId(i, true)]),
        includeKinds: TRAFFIC_OBSTACLE_KINDS,
      });
      if (obstacle) {
        target = Math.min(target, Math.max(0, (obstacle.distance - 3.2) * 0.85));
      }
      const stop = stopLineDistance(t.pos.x, t.pos.z, yaw0);
      if (stop && stop.d < 24) {
        const go = stop.ns ? sig.nsGo : sig.ewGo;
        const caution = stop.ns ? sig.nsYellow : sig.ewYellow;
        if (!go && !caution) {
          target = Math.min(v.cruise, Math.max(0, (stop.d - 2.2) * 0.9));
        } else if (caution && stop.d > 8) {
          target = Math.min(v.cruise, Math.max(0, (stop.d - 2.2) * 0.9));
        }
      }
      v.speed += THREE.MathUtils.clamp(target - v.speed, -BRAKE * dt, DRIVE_ACCEL * dt);

      v.dist += v.speed * dt;
      const yaw = sampleRoute(v.route!, v.dist, v, t.pos);
      const yawRate = dt > 0 ? wrapAngle(yaw - v.prevYaw) / dt : 0;
      v.prevYaw = yaw;
      const targetRoll = THREE.MathUtils.clamp(-yawRate * v.speed * 0.006, -0.14, 0.14);
      v.roll += (targetRoll - v.roll) * Math.min(1, 8 * dt);
      v.spin += (v.speed / 0.34) * dt;

      t.q.setFromAxisAngle(t.up, yaw);
      t.qRoll.setFromAxisAngle(t.zAxis, v.roll);
      t.q.multiply(t.qRoll);
      t.m.compose(t.pos, t.q, t.one);
      for (const inst of bodyInstances[v.template]) inst.setMatrixAt(v.slot, t.m);
      worldCollisionRegistry.update(vehicleId(i, true), vehicleBounds(v.template, t.pos.x, t.pos.z, yaw));
      if (v.template === 2 && v.slot === 0 && window.__nexoVillage) {
        window.__nexoVillage.debugTruck = { x: t.pos.x, z: t.pos.z };
      }

      const wheels = wheelInstances[v.template];
      if (wheels) {
        t.qSpin.setFromAxisAngle(t.xAxis, v.spin);
        tpl.wheelOffsets.forEach((off, w) => {
          t.wp.copy(off).applyQuaternion(t.q).add(t.pos);
          const wq = t.q.clone().multiply(t.qSpin);
          t.m.compose(t.wp, wq, t.one);
          wheels.setMatrixAt(v.slot * tpl.wheelOffsets.length + w, t.m);
        });
      }
    }
    for (const insts of bodyInstances) {
      for (const inst of insts) inst.instanceMatrix.needsUpdate = true;
    }
    for (const wheels of wheelInstances) {
      if (wheels) wheels.instanceMatrix.needsUpdate = true;
    }
  });

  useEffect(() => {
    return () => {
      // Materials belong to the GLTF loader cache; only baked geometry is ours.
      for (const t of built.templates) {
        for (const part of t.bodyParts) part.geo.dispose();
        t.wheelGeo?.dispose();
      }
    };
  }, [built]);

  return (
    <group>
      {built.bodyInstances.flat().map((inst, i) => (
        <primitive key={`b${i}`} object={inst} />
      ))}
      {built.wheelInstances.map((inst, i) =>
        inst ? <primitive key={`w${i}`} object={inst} /> : null,
      )}
    </group>
  );
}

useGLTF.preload(CAR_URL);
useGLTF.preload(TRUCK_URL);
