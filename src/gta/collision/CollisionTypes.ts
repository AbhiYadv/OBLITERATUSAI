export type CollisionKind =
  | "player"
  | "pedestrian"
  | "building"
  | "moving-car"
  | "parked-car"
  | "bench"
  | "streetlight"
  | "traffic-signal"
  | "bollard"
  | "kiosk"
  | "planter"
  | "solid-prop";

export interface CollisionVec2 {
  x: number;
  z: number;
}

export interface CollisionVec3 extends CollisionVec2 {
  y: number;
}

export interface CollisionAabb {
  minX: number;
  maxX: number;
  minY: number;
  maxY: number;
  minZ: number;
  maxZ: number;
}

export interface CollisionBounds {
  center: CollisionVec3;
  halfExtents: CollisionVec3;
  yaw: number;
  aabb: CollisionAabb;
}

export interface CollisionObject {
  id: string;
  kind: CollisionKind;
  bounds: CollisionBounds;
  solid: boolean;
  dynamic?: boolean;
}

export interface CorridorQuery {
  origin: CollisionVec3;
  direction: CollisionVec2;
  length: number;
  halfWidth: number;
  ignoreIds: ReadonlySet<string>;
  includeKinds?: ReadonlySet<CollisionKind>;
}

export interface CorridorHit {
  id: string;
  object: CollisionObject;
  distance: number;
}
