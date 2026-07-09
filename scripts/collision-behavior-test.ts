import {
  CollisionRegistry,
  makeBounds,
  resolveHorizontalCapsule,
  type CollisionObject,
} from "../src/gta/collision/CollisionRegistry.ts";

function expectClose(actual: number, expected: number, label: string): void {
  if (Math.abs(actual - expected) > 0.001) {
    throw new Error(`${label}: expected ${expected}, received ${actual}`);
  }
}

function expect(condition: boolean, label: string): void {
  if (!condition) throw new Error(label);
}

const registry = new CollisionRegistry(8);
const wall: CollisionObject = {
  id: "wall",
  kind: "building",
  bounds: makeBounds(2, 0, 0, 2, 3, 8, 0),
  solid: true,
};
registry.register(wall);

const blocked = resolveHorizontalCapsule(registry, {
  current: { x: -2, y: 0, z: 0 },
  desired: { x: 2.2, y: 0, z: 0 },
  radius: 0.5,
  halfHeight: 0.9,
  ignoreKinds: new Set(),
});
expect(blocked.collided, "player crossing a solid object should collide");
expectClose(blocked.position.x, -0.5, "player should stop at expanded wall edge");

const sliding = resolveHorizontalCapsule(registry, {
  current: { x: -2, y: 0, z: 2.6 },
  desired: { x: 2.2, y: 0, z: 2.6 },
  radius: 0.5,
  halfHeight: 0.9,
  ignoreKinds: new Set(),
});
expect(sliding.collided, "diagonal edge contact should collide");
expect(sliding.position.z > 2.55, "slide should preserve tangent movement");

registry.update("wall", makeBounds(20, 0, 0, 2, 3, 8, 0));
expect(
  registry.queryArea(makeBounds(0, 0, 0, 3, 2, 3, 0)).length === 0,
  "updated object should leave its old spatial cells",
);
expect(
  registry.raycastCorridor({
    origin: { x: 15, y: 0, z: 0 },
    direction: { x: 1, z: 0 },
    length: 10,
    halfWidth: 1.3,
    ignoreIds: new Set(),
  })?.id === "wall",
  "corridor query should find forward obstacles",
);

registry.unregister("wall");
expect(registry.queryArea(makeBounds(20, 0, 0, 3, 2, 3, 0)).length === 0, "unregister should remove bounds");

console.log("collision behavior checks passed");
