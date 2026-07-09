import type { CollisionBounds, CollisionObject, CollisionVec2, CollisionVec3 } from "./CollisionTypes";

export interface CollisionRegistryReader {
  queryArea(bounds: CollisionBounds): CollisionObject[];
}

export interface PlayerCollisionRequest {
  current: CollisionVec3;
  desired: CollisionVec3;
  radius: number;
  halfHeight: number;
  ignoreKinds: ReadonlySet<string>;
}

export interface PlayerCollisionResult {
  position: CollisionVec3;
  collided: boolean;
}

interface LocalPoint {
  x: number;
  z: number;
}

interface SweepHit {
  t: number;
  normal: CollisionVec2;
}

const EPS = 0.0001;
const MAX_SLIDES = 3;

export function resolveHorizontalCapsule(
  registry: CollisionRegistryReader,
  request: PlayerCollisionRequest,
): PlayerCollisionResult {
  const query = boundsAroundSegment(request.current, request.desired, request.radius, request.halfHeight);
  const candidates = registry
    .queryArea(query)
    .filter((obj) => obj.solid && !request.ignoreKinds.has(obj.kind) && verticallyOverlaps(obj.bounds, request));

  let current = { ...request.current };
  let desired = { ...request.desired };
  let collided = false;

  for (let slide = 0; slide < MAX_SLIDES; slide++) {
    let nearest: { object: CollisionObject; hit: SweepHit } | null = null;
    for (const obj of candidates) {
      const hit = sweepAgainstBounds(current, desired, obj.bounds, request.radius);
      if (hit && (!nearest || hit.t < nearest.hit.t)) nearest = { object: obj, hit };
    }
    if (!nearest) {
      const pushed = resolveStaticOverlaps(desired, candidates, request.radius);
      return { position: { x: pushed.x, y: request.desired.y, z: pushed.z }, collided: collided || pushed.collided };
    }

    collided = true;
    const moveX = desired.x - current.x;
    const moveZ = desired.z - current.z;
    const contact = {
      x: current.x + moveX * Math.max(0, nearest.hit.t - EPS),
      y: request.desired.y,
      z: current.z + moveZ * Math.max(0, nearest.hit.t - EPS),
    };
    const remain = {
      x: desired.x - contact.x,
      z: desired.z - contact.z,
    };
    const intoNormal = remain.x * nearest.hit.normal.x + remain.z * nearest.hit.normal.z;
    const slideMove = intoNormal < 0
      ? {
          x: remain.x - nearest.hit.normal.x * intoNormal,
          z: remain.z - nearest.hit.normal.z * intoNormal,
        }
      : remain;
    current = contact;
    desired = {
      x: contact.x + slideMove.x,
      y: request.desired.y,
      z: contact.z + slideMove.z,
    };
  }

  const pushed = resolveStaticOverlaps(desired, candidates, request.radius);
  return { position: { x: pushed.x, y: request.desired.y, z: pushed.z }, collided: true };
}

function boundsAroundSegment(
  current: CollisionVec3,
  desired: CollisionVec3,
  radius: number,
  halfHeight: number,
): CollisionBounds {
  const minX = Math.min(current.x, desired.x) - radius;
  const maxX = Math.max(current.x, desired.x) + radius;
  const minZ = Math.min(current.z, desired.z) - radius;
  const maxZ = Math.max(current.z, desired.z) + radius;
  const minY = Math.min(current.y, desired.y) - halfHeight;
  const maxY = Math.max(current.y, desired.y) + halfHeight;
  return {
    center: { x: (minX + maxX) / 2, y: (minY + maxY) / 2, z: (minZ + maxZ) / 2 },
    halfExtents: { x: (maxX - minX) / 2, y: (maxY - minY) / 2, z: (maxZ - minZ) / 2 },
    yaw: 0,
    aabb: { minX, maxX, minY, maxY, minZ, maxZ },
  };
}

function verticallyOverlaps(bounds: CollisionBounds, request: PlayerCollisionRequest): boolean {
  const minY = Math.min(request.current.y, request.desired.y) - request.halfHeight;
  const maxY = Math.max(request.current.y, request.desired.y) + request.halfHeight;
  return bounds.aabb.maxY >= minY && bounds.aabb.minY <= maxY;
}

function sweepAgainstBounds(
  current: CollisionVec3,
  desired: CollisionVec3,
  bounds: CollisionBounds,
  radius: number,
): SweepHit | null {
  const start = toLocal(current, bounds);
  const end = toLocal(desired, bounds);
  if (insideExpanded(start, bounds, radius)) return staticHit(start, bounds, radius);

  const dx = end.x - start.x;
  const dz = end.z - start.z;
  if (Math.abs(dx) < EPS && Math.abs(dz) < EPS) return null;

  const hx = bounds.halfExtents.x + radius;
  const hz = bounds.halfExtents.z + radius;
  let tEnter = 0;
  let tExit = 1;
  let normal: CollisionVec2 = { x: 0, z: 0 };

  const xHit = axisSweep(start.x, dx, hx, { x: dx > 0 ? -1 : 1, z: 0 });
  if (!xHit) return null;
  if (xHit.enter > tEnter) {
    tEnter = xHit.enter;
    normal = xHit.normal;
  }
  tExit = Math.min(tExit, xHit.exit);

  const zHit = axisSweep(start.z, dz, hz, { x: 0, z: dz > 0 ? -1 : 1 });
  if (!zHit) return null;
  if (zHit.enter > tEnter) {
    tEnter = zHit.enter;
    normal = zHit.normal;
  }
  tExit = Math.min(tExit, zHit.exit);

  if (tEnter > tExit || tEnter < 0 || tEnter > 1) return null;
  return { t: tEnter, normal: normalToWorld(normal, bounds.yaw) };
}

function axisSweep(
  start: number,
  delta: number,
  half: number,
  normal: CollisionVec2,
): { enter: number; exit: number; normal: CollisionVec2 } | null {
  if (Math.abs(delta) < EPS) {
    if (start <= -half || start >= half) return null;
    return { enter: 0, exit: 1, normal };
  }
  const inv = 1 / delta;
  let enter = (-half - start) * inv;
  let exit = (half - start) * inv;
  if (enter > exit) [enter, exit] = [exit, enter];
  return { enter, exit, normal };
}

function resolveStaticOverlaps(
  point: CollisionVec3,
  candidates: CollisionObject[],
  radius: number,
): { x: number; z: number; collided: boolean } {
  let x = point.x;
  let z = point.z;
  let collided = false;
  for (let i = 0; i < MAX_SLIDES; i++) {
    let pushed = false;
    for (const obj of candidates) {
      const local = toLocal({ x, y: point.y, z }, obj.bounds);
      const hit = staticHit(local, obj.bounds, radius);
      if (!hit) continue;
      x += hit.normal.x * 0.025;
      z += hit.normal.z * 0.025;
      const resolved = pushOutOfBounds({ x, z }, obj.bounds, radius, hit.normal);
      x = resolved.x;
      z = resolved.z;
      collided = true;
      pushed = true;
    }
    if (!pushed) break;
  }
  return { x, z, collided };
}

function staticHit(local: LocalPoint, bounds: CollisionBounds, radius: number): SweepHit | null {
  const hx = bounds.halfExtents.x + radius;
  const hz = bounds.halfExtents.z + radius;
  if (!insideExpanded(local, bounds, radius)) return null;
  const left = local.x + hx;
  const right = hx - local.x;
  const near = local.z + hz;
  const far = hz - local.z;
  const min = Math.min(left, right, near, far);
  const normal =
    min === left ? { x: -1, z: 0 }
    : min === right ? { x: 1, z: 0 }
    : min === near ? { x: 0, z: -1 }
    : { x: 0, z: 1 };
  return { t: 0, normal: normalToWorld(normal, bounds.yaw) };
}

function pushOutOfBounds(
  point: CollisionVec2,
  bounds: CollisionBounds,
  radius: number,
  normal: CollisionVec2,
): CollisionVec2 {
  const local = toLocal({ x: point.x, y: 0, z: point.z }, bounds);
  const localNormal = normalToLocal(normal, bounds.yaw);
  const hx = bounds.halfExtents.x + radius;
  const hz = bounds.halfExtents.z + radius;
  const out = { ...local };
  if (Math.abs(localNormal.x) > Math.abs(localNormal.z)) out.x = localNormal.x < 0 ? -hx : hx;
  else out.z = localNormal.z < 0 ? -hz : hz;
  return toWorld(out, bounds);
}

function insideExpanded(local: LocalPoint, bounds: CollisionBounds, radius: number): boolean {
  return (
    local.x > -bounds.halfExtents.x - radius &&
    local.x < bounds.halfExtents.x + radius &&
    local.z > -bounds.halfExtents.z - radius &&
    local.z < bounds.halfExtents.z + radius
  );
}

function toLocal(point: CollisionVec3, bounds: CollisionBounds): LocalPoint {
  const dx = point.x - bounds.center.x;
  const dz = point.z - bounds.center.z;
  const c = Math.cos(-bounds.yaw);
  const s = Math.sin(-bounds.yaw);
  return { x: dx * c - dz * s, z: dx * s + dz * c };
}

function toWorld(local: LocalPoint, bounds: CollisionBounds): CollisionVec2 {
  const c = Math.cos(bounds.yaw);
  const s = Math.sin(bounds.yaw);
  return {
    x: bounds.center.x + local.x * c - local.z * s,
    z: bounds.center.z + local.x * s + local.z * c,
  };
}

function normalToWorld(normal: CollisionVec2, yaw: number): CollisionVec2 {
  const c = Math.cos(yaw);
  const s = Math.sin(yaw);
  return { x: normal.x * c - normal.z * s, z: normal.x * s + normal.z * c };
}

function normalToLocal(normal: CollisionVec2, yaw: number): CollisionVec2 {
  const c = Math.cos(-yaw);
  const s = Math.sin(-yaw);
  return { x: normal.x * c - normal.z * s, z: normal.x * s + normal.z * c };
}
