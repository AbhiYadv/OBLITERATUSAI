import { SpatialHash } from "./SpatialHash";
import { recordBaselineCollisionQuery } from "../debug/PerformanceProbe";
import type {
  CollisionAabb,
  CollisionBounds,
  CollisionKind,
  CollisionObject,
  CollisionVec3,
  CorridorHit,
  CorridorQuery,
} from "./CollisionTypes";
export type { CollisionBounds, CollisionKind, CollisionObject, CorridorHit } from "./CollisionTypes";
export { resolveHorizontalCapsule } from "./PlayerCollision";

export class CollisionRegistry {
  private readonly objects = new Map<string, CollisionObject>();
  private readonly hash: SpatialHash;

  constructor(cellSize = 12) {
    this.hash = new SpatialHash(cellSize);
  }

  register(object: CollisionObject): void {
    this.objects.set(object.id, object);
    this.hash.insert(object.id, object.bounds.aabb);
  }

  update(id: string, bounds: CollisionBounds): void {
    const object = this.objects.get(id);
    if (!object) return;
    object.bounds = bounds;
    this.hash.insert(id, bounds.aabb);
  }

  unregister(id: string): void {
    this.objects.delete(id);
    this.hash.remove(id);
  }

  unregisterPrefix(prefix: string): void {
    for (const id of [...this.objects.keys()]) {
      if (id.startsWith(prefix)) this.unregister(id);
    }
  }

  countByPrefix(prefix: string): number {
    let count = 0;
    for (const id of this.objects.keys()) {
      if (id.startsWith(prefix)) count++;
    }
    return count;
  }

  queryArea(bounds: CollisionBounds): CollisionObject[] {
    recordBaselineCollisionQuery();
    return this.hash
      .query(bounds.aabb)
      .map((id) => this.objects.get(id))
      .filter((object): object is CollisionObject => Boolean(object))
      .filter((object) => aabbIntersects(bounds.aabb, object.bounds.aabb));
  }

  raycastCorridor(query: CorridorQuery): CorridorHit | null {
    const dirLen = Math.hypot(query.direction.x, query.direction.z);
    if (dirLen < 0.0001 || query.length <= 0) return null;
    const dir = { x: query.direction.x / dirLen, z: query.direction.z / dirLen };
    const side = { x: -dir.z, z: dir.x };
    const center = {
      x: query.origin.x + dir.x * query.length * 0.5,
      y: query.origin.y,
      z: query.origin.z + dir.z * query.length * 0.5,
    };
    const queryBounds = makeBounds(
      center.x,
      center.y,
      center.z,
      Math.abs(dir.x) * query.length * 0.5 + Math.abs(side.x) * query.halfWidth,
      4,
      Math.abs(dir.z) * query.length * 0.5 + Math.abs(side.z) * query.halfWidth,
      0,
    );
    const hits = this.queryArea(queryBounds)
      .filter((object) => object.solid && !query.ignoreIds.has(object.id))
      .filter((object) => !query.includeKinds || query.includeKinds.has(object.kind))
      .map((object) => corridorHit(object, query.origin, dir, side, query.length, query.halfWidth))
      .filter((hit): hit is CorridorHit => Boolean(hit))
      .sort((a, b) => a.distance - b.distance);
    return hits[0] ?? null;
  }
}

export const worldCollisionRegistry = new CollisionRegistry();

export function makeBounds(
  x: number,
  y: number,
  z: number,
  halfX: number,
  halfY: number,
  halfZ: number,
  yaw = 0,
): CollisionBounds {
  const c = Math.abs(Math.cos(yaw));
  const s = Math.abs(Math.sin(yaw));
  const aabbHalfX = c * halfX + s * halfZ;
  const aabbHalfZ = s * halfX + c * halfZ;
  return {
    center: { x, y, z },
    halfExtents: { x: halfX, y: halfY, z: halfZ },
    yaw,
    aabb: {
      minX: x - aabbHalfX,
      maxX: x + aabbHalfX,
      minY: y - halfY,
      maxY: y + halfY,
      minZ: z - aabbHalfZ,
      maxZ: z + aabbHalfZ,
    },
  };
}

export function registerObjects(registry: CollisionRegistry, objects: CollisionObject[]): () => void {
  for (const object of objects) registry.register(object);
  return () => {
    for (const object of objects) registry.unregister(object.id);
  };
}

export function boundsFromPlanBuilding(
  id: string,
  building: { x: number; z: number; w: number; d: number; h: number },
  groundY: number,
): CollisionObject {
  return {
    id,
    kind: "building",
    solid: true,
    bounds: makeBounds(
      building.x,
      groundY + building.h / 2,
      building.z,
      building.w / 2,
      building.h / 2,
      building.d / 2,
      0,
    ),
  };
}

export function makeObject(
  id: string,
  kind: CollisionKind,
  center: CollisionVec3,
  halfExtents: CollisionVec3,
  yaw = 0,
  dynamic = false,
): CollisionObject {
  return {
    id,
    kind,
    solid: true,
    dynamic,
    bounds: makeBounds(center.x, center.y, center.z, halfExtents.x, halfExtents.y, halfExtents.z, yaw),
  };
}

function corridorHit(
  object: CollisionObject,
  origin: CollisionVec3,
  dir: { x: number; z: number },
  side: { x: number; z: number },
  length: number,
  halfWidth: number,
): CorridorHit | null {
  const aabb = object.bounds.aabb;
  const center = object.bounds.center;
  const dx = center.x - origin.x;
  const dz = center.z - origin.z;
  const forward = dx * dir.x + dz * dir.z;
  const lateral = dx * side.x + dz * side.z;
  const aabbHalfX = (aabb.maxX - aabb.minX) / 2;
  const aabbHalfZ = (aabb.maxZ - aabb.minZ) / 2;
  const forwardRadius = Math.abs(dir.x) * aabbHalfX + Math.abs(dir.z) * aabbHalfZ;
  const sideRadius = Math.abs(side.x) * aabbHalfX + Math.abs(side.z) * aabbHalfZ;
  if (forward + forwardRadius < 0 || forward - forwardRadius > length) return null;
  if (Math.abs(lateral) > halfWidth + sideRadius) return null;
  return { id: object.id, object, distance: Math.max(0, forward - forwardRadius) };
}

function aabbIntersects(a: CollisionAabb, b: CollisionAabb): boolean {
  return (
    a.minX <= b.maxX &&
    a.maxX >= b.minX &&
    a.minY <= b.maxY &&
    a.maxY >= b.minY &&
    a.minZ <= b.maxZ &&
    a.maxZ >= b.minZ
  );
}
