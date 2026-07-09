import { CollisionRegistry, makeObject } from "../src/gta/collision/CollisionRegistry.ts";
import {
  PEDESTRIAN_RADIUS,
  pedestrianBounds,
  shouldStopForPedestrianObstacle,
} from "../src/gta/characters/CharacterCollision.ts";

function expect(condition: boolean, label: string): void {
  if (!condition) throw new Error(label);
}

const registry = new CollisionRegistry(8);
const selfId = "pedestrian:test:0";
registry.register({
  id: selfId,
  kind: "pedestrian",
  solid: true,
  dynamic: true,
  bounds: pedestrianBounds(0, 0.15, 0),
});
registry.register(
  makeObject(
    "player:main",
    "player",
    { x: 0, y: 1.05, z: 2.4 },
    { x: 0.42, y: 0.9, z: 0.42 },
    0,
    true,
  ),
);

expect(
  shouldStopForPedestrianObstacle(registry, {
    id: selfId,
    x: 0,
    y: 0.15,
    z: 0,
    yaw: 0,
    speed: 1.3,
  }),
  "pedestrian should stop for player directly ahead",
);

registry.unregister("player:main");
registry.register({
  id: "pedestrian:test:1",
  kind: "pedestrian",
  solid: true,
  dynamic: true,
  bounds: pedestrianBounds(0.2, 0.15, 1.4),
});

expect(
  shouldStopForPedestrianObstacle(registry, {
    id: selfId,
    x: 0,
    y: 0.15,
    z: 0,
    yaw: 0,
    speed: 1.3,
  }),
  "pedestrian should maintain spacing from another pedestrian ahead",
);

registry.unregister("pedestrian:test:1");
registry.register(
  makeObject(
    "traffic:moving:0",
    "moving-car",
    { x: 0.1, y: 0.9, z: 2.2 },
    { x: 1.0, y: 0.9, z: 2.2 },
    0,
    true,
  ),
);

expect(
  shouldStopForPedestrianObstacle(registry, {
    id: selfId,
    x: 0,
    y: 0.15,
    z: 0,
    yaw: 0,
    speed: 1.3,
  }),
  "pedestrian should stop before walking into a vehicle",
);

const bounds = pedestrianBounds(5, 0.15, -3);
expect(bounds.halfExtents.x === PEDESTRIAN_RADIUS, "pedestrian radius should stay compact");
expect(bounds.halfExtents.z === PEDESTRIAN_RADIUS, "pedestrian depth should stay compact");

console.log("pedestrian collision behavior checks passed");
