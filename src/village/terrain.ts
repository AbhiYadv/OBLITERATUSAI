/**
 * Analytic 2 km heightfield for the Nexo Village world.
 *
 * The terrain is a pure function of (seed, x, z): rolling meadows, a hill
 * zone in the northeast, a flattened central village clearing, a carved
 * river along the west side, a pond near the village, and dirt paths that
 * radiate out to each zone. Height is sampled per-vertex at chunk build
 * time and per-frame for walk collision, so everything stays deterministic
 * without storing a giant grid.
 */
import { fbm, mix, smoothstep } from "./seed";

export const WORLD_SIZE = 2048;
export const HALF_WORLD = WORLD_SIZE / 2;
export const CHUNK_SIZE = 128;
export const CHUNKS_PER_SIDE = WORLD_SIZE / CHUNK_SIZE;

export const WATER_LEVEL = 14;
export const VILLAGE_HEIGHT = 18;
export const VILLAGE_FLAT_RADIUS = 110;
export const VILLAGE_BLEND_RADIUS = 175;
export const PLAYER_BOUND = HALF_WORLD - 16;

export type Vec2 = readonly [number, number];

/** Named zone centers, used by terrain masks, paths, and scatter. */
export const ZONES = {
  village: [0, 0] as Vec2,
  farm: [360, 440] as Vec2,
  meadow: [-560, -40] as Vec2,
  forestNorth: [-140, -660] as Vec2,
  forestSouthwest: [-620, 540] as Vec2,
  hills: [640, -520] as Vec2,
  pond: [-180, 150] as Vec2,
} as const;

export const POND_RADIUS = 42;

/** Downtown district for the GTA scene: a riverfront rectangle in the west
 * meadow. Only active when the scene opts in via setCityEnabled(). */
export const CITY = { minX: -500, maxX: -200, minZ: -350, maxZ: 350, y: 17 };

let cityEnabled = false;
export function setCityEnabled(v: boolean): void {
  cityEnabled = v;
}

/** 1 inside the city rectangle, smooth falloff over ~45 m outside it. */
export function cityMaskAt(x: number, z: number): number {
  if (!cityEnabled) return 0;
  const dx = Math.max(CITY.minX - x, x - CITY.maxX, 0);
  const dz = Math.max(CITY.minZ - z, z - CITY.maxZ, 0);
  return 1 - smoothstep(0, 45, Math.hypot(dx, dz));
}

/** River runs north (-z) to south (+z) along the west side. */
export const RIVER: Vec2[] = [
  [-620, -1024],
  [-660, -780],
  [-700, -520],
  [-660, -260],
  [-580, -40],
  [-560, 200],
  [-620, 460],
  [-700, 720],
  [-680, 1024],
];

/** Village paths: each polyline starts at the plaza and heads to a zone,
 * continuing past it as an outer exploration path. */
export const PATHS: Vec2[][] = [
  // farm, continuing southeast
  [[0, 0], [120, 160], [240, 300], [360, 440], [520, 640], [700, 860]],
  // meadow, continuing west toward the river
  [[0, 0], [-140, -20], [-320, -40], [-560, -60], [-820, -80]],
  // north forest
  [[0, 0], [-40, -160], [-100, -380], [-150, -640], [-180, -880]],
  // hills, continuing northeast
  [[0, 0], [160, -120], [340, -280], [540, -440], [760, -620], [910, -750]],
  // pond
  [[0, 0], [-90, 70], [-168, 138]],
  // pond onward to the southwest forest
  [[-168, 138], [-320, 250], [-470, 380], [-600, 500]],
];

function distSqToSegment(px: number, pz: number, a: Vec2, b: Vec2): number {
  const abx = b[0] - a[0];
  const abz = b[1] - a[1];
  const apx = px - a[0];
  const apz = pz - a[1];
  const len = abx * abx + abz * abz;
  const t = len > 0 ? Math.min(1, Math.max(0, (apx * abx + apz * abz) / len)) : 0;
  const dx = apx - abx * t;
  const dz = apz - abz * t;
  return dx * dx + dz * dz;
}

export function distToPolyline(points: Vec2[], x: number, z: number): number {
  let best = Infinity;
  for (let i = 0; i < points.length - 1; i++) {
    const d = distSqToSegment(x, z, points[i], points[i + 1]);
    if (d < best) best = d;
  }
  return Math.sqrt(best);
}

export function distToPaths(x: number, z: number): number {
  let best = Infinity;
  for (const path of PATHS) {
    const d = distToPolyline(path, x, z);
    if (d < best) best = d;
  }
  return best;
}

function radialMask(x: number, z: number, center: Vec2, inner: number, outer: number): number {
  const dx = x - center[0];
  const dz = z - center[1];
  return 1 - smoothstep(inner, outer, Math.sqrt(dx * dx + dz * dz));
}

export interface TerrainSample {
  height: number;
  villageMask: number;
  hillMask: number;
  meadowMask: number;
  forestMask: number;
  farmMask: number;
  cityMask: number;
  pathDist: number;
  riverDist: number;
}

// Sub-seeds keep each noise field independent.
let S = 0;
export function setTerrainSeed(seed: number): void {
  S = seed >>> 0;
}

export function sampleTerrain(x: number, z: number): TerrainSample {
  const villageMask = radialMask(x, z, ZONES.village, VILLAGE_FLAT_RADIUS, VILLAGE_BLEND_RADIUS);
  const hillMask = radialMask(x, z, ZONES.hills, 300, 560);
  const meadowMask = radialMask(x, z, ZONES.meadow, 240, 430);
  const farmMask = radialMask(x, z, ZONES.farm, 140, 260);
  const forestMask = Math.max(
    radialMask(x, z, ZONES.forestNorth, 230, 340),
    radialMask(x, z, ZONES.forestSouthwest, 190, 300),
  );
  const cityMask = cityMaskAt(x, z);
  const pathDist = distToPaths(x, z);
  const riverDist = distToPolyline(RIVER, x, z);

  // Rolling macro relief.
  let h = 16 + fbm(S + 11, x, z, 4, 1 / 750) * 20;
  // Meadow stays gentle.
  h = mix(h, 15.5 + fbm(S + 11, x, z, 3, 1 / 750) * 5, meadowMask * 0.7);
  // Hill zone rises up to ~75 m.
  if (hillMask > 0.001) {
    const hillNoise = fbm(S + 23, x, z, 3, 1 / 300) * 0.5 + 0.55;
    h += hillMask * hillNoise * 62;
  }
  // Fine detail bumps, suppressed in the clearing, farm, and on paths.
  const pathFlat = 1 - smoothstep(2.5, 7, pathDist);
  let detail = fbm(S + 37, x, z, 2, 1 / 55) * 1.5;
  detail *= (1 - villageMask * 0.9) * (1 - pathFlat * 0.85) * (1 - farmMask * 0.6) * (1 - cityMask);
  h += detail;
  // Flatten the downtown district (GTA scene only; mask is 0 otherwise).
  if (cityMask > 0.001) h = mix(h, CITY.y, cityMask);
  // Soft floor keeps open land above water level: only the river and pond
  // (carved below) may dip under WATER_LEVEL, so the world has exactly one
  // river and one pond rather than incidental lakes in noise dips.
  if (h < 15.4) h = 15.4 - (15.4 - h) * 0.06;
  // Flatten the central village clearing.
  h = mix(h, VILLAGE_HEIGHT, villageMask);
  // Gentle rim at the world edge to signal the boundary.
  const edge = smoothstep(880, 1012, Math.max(Math.abs(x), Math.abs(z)));
  h += edge * edge * 38;

  // Carve the river channel below water level.
  const riverHalfWidth = 12 + fbm(S + 53, x, z, 2, 1 / 140) * 4;
  const bank = 1 - smoothstep(riverHalfWidth, riverHalfWidth + 34, riverDist);
  if (bank > 0.001) {
    const bed = 10.4 + fbm(S + 61, x, z, 2, 1 / 40) * 0.5;
    h = mix(h, Math.min(h, bed), bank);
  }
  // Carve the pond near the village.
  const pondMask = radialMask(x, z, ZONES.pond, POND_RADIUS * 0.62, POND_RADIUS + 22);
  if (pondMask > 0.001) {
    h = mix(h, 11.2, pondMask);
  }

  return { height: h, villageMask, hillMask, meadowMask, forestMask, farmMask, cityMask, pathDist, riverDist };
}

/** Walkable ground height — the collision probe used by the player. */
export function heightAt(x: number, z: number): number {
  return sampleTerrain(x, z).height;
}

const C_GRASS = [0.396, 0.596, 0.286];
const C_MEADOW = [0.576, 0.686, 0.333];
const C_FOREST = [0.282, 0.435, 0.216];
const C_FARM = [0.545, 0.494, 0.302];
const C_VILLAGE = [0.573, 0.545, 0.369];
const C_PATH = [0.612, 0.498, 0.341];
const C_SAND = [0.76, 0.698, 0.502];
const C_ROCK = [0.553, 0.553, 0.565];
const C_CONCRETE = [0.44, 0.44, 0.45];

/** Per-vertex terrain color from the sample; slope is rise over run. */
export function colorAt(
  s: TerrainSample,
  x: number,
  z: number,
  slope: number,
  out: number[],
): void {
  const tint = fbm(S + 71, x, z, 2, 1 / 90) * 0.06;
  let r = C_GRASS[0];
  let g = C_GRASS[1];
  let b = C_GRASS[2];
  const lerp3 = (c: number[], t: number) => {
    r = mix(r, c[0], t);
    g = mix(g, c[1], t);
    b = mix(b, c[2], t);
  };
  lerp3(C_MEADOW, s.meadowMask * 0.85);
  lerp3(C_FOREST, s.forestMask * 0.8);
  lerp3(C_FARM, s.farmMask * 0.55);
  lerp3(C_VILLAGE, s.villageMask * 0.55);
  // Rock on steep slopes and high peaks.
  lerp3(C_ROCK, smoothstep(0.75, 1.15, slope) * 0.85);
  lerp3(C_ROCK, smoothstep(62, 82, s.height) * 0.6);
  // Sand along water margins.
  lerp3(C_SAND, (1 - smoothstep(WATER_LEVEL - 0.6, WATER_LEVEL + 1.4, s.height)) * 0.9);
  // Dirt paths.
  lerp3(C_PATH, 1 - smoothstep(1.8, 4.2, s.pathDist));
  // City ground reads as concrete; road/sidewalk meshes sit on top.
  lerp3(C_CONCRETE, s.cityMask * 0.95);
  out[0] = Math.max(0, r + tint);
  out[1] = Math.max(0, g + tint);
  out[2] = Math.max(0, b + tint);
}
