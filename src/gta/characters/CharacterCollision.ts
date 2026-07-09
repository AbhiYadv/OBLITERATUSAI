import {
  makeBounds,
  type CollisionBounds,
  type CollisionKind,
  type CollisionRegistry,
} from "../collision/CollisionRegistry";

export const PEDESTRIAN_RADIUS = 0.34;
export const PEDESTRIAN_HALF_HEIGHT = 0.88;
export const PEDESTRIAN_PREFIX = "pedestrian:";

const PEDESTRIAN_OBSTACLE_KINDS: ReadonlySet<CollisionKind> = new Set([
  "player",
  "pedestrian",
  "moving-car",
  "parked-car",
  "bench",
  "streetlight",
  "traffic-signal",
  "bollard",
  "planter",
  "solid-prop",
  "building",
]);

export interface PedestrianCollisionState {
  id: string;
  x: number;
  y: number;
  z: number;
  yaw: number;
  speed: number;
}

export function pedestrianId(index: number): string {
  return `${PEDESTRIAN_PREFIX}${index}`;
}

export function pedestrianBounds(x: number, groundY: number, z: number): CollisionBounds {
  return makeBounds(
    x,
    groundY + PEDESTRIAN_HALF_HEIGHT,
    z,
    PEDESTRIAN_RADIUS,
    PEDESTRIAN_HALF_HEIGHT,
    PEDESTRIAN_RADIUS,
    0,
  );
}

export function shouldStopForPedestrianObstacle(
  registry: CollisionRegistry,
  state: PedestrianCollisionState,
): boolean {
  const forward = { x: Math.sin(state.yaw), z: Math.cos(state.yaw) };
  const hit = registry.raycastCorridor({
    origin: {
      x: state.x + forward.x * (PEDESTRIAN_RADIUS + 0.08),
      y: state.y + PEDESTRIAN_HALF_HEIGHT,
      z: state.z + forward.z * (PEDESTRIAN_RADIUS + 0.08),
    },
    direction: forward,
    length: Math.max(1.25, state.speed * 1.25 + PEDESTRIAN_RADIUS),
    halfWidth: PEDESTRIAN_RADIUS + 0.18,
    ignoreIds: new Set([state.id]),
    includeKinds: PEDESTRIAN_OBSTACLE_KINDS,
  });
  return Boolean(hit);
}
