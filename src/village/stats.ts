/**
 * Shared mutable runtime stats, written by scene systems each frame and
 * read by the DOM performance HUD. Also exposed as `window.__nexoVillage`
 * for tooling/tests (adapted from the LAAS hooks pattern).
 */
export interface VillageStats {
  seed: number;
  fps: number;
  frameMs: number;
  drawCalls: number;
  triangles: number;
  geometries: number;
  chunksBuilt: number;
  chunksPending: number;
  mode: "walk" | "fly" | "drive";
  x: number;
  y: number;
  z: number;
  grounded: boolean;
}

export const villageStats: VillageStats = {
  seed: 0,
  fps: 0,
  frameMs: 0,
  drawCalls: 0,
  triangles: 0,
  geometries: 0,
  chunksBuilt: 0,
  chunksPending: 0,
  mode: "walk",
  x: 0,
  y: 0,
  z: 0,
  grounded: true,
};

declare global {
  interface Window {
    __nexoVillage?: {
      stats: VillageStats;
      /** Debug/tooling hook: teleport the camera (switches to fly mode). */
      setPose?: (x: number, y: number, z: number, yaw: number, pitch: number) => void;
      /** Live position of one traffic truck, for tooling/screenshots. */
      debugTruck?: { x: number; z: number };
    };
  }
}

export function installVillageHooks(): void {
  window.__nexoVillage = { stats: villageStats };
}
