/**
 * Deterministic downtown plan for the GTA scene: street grid, block slabs,
 * building lots, traffic loop routes, streetlights, crosswalks, and parked
 * cars. Everything derives from the world seed so the city is identical
 * every run.
 */
import { mulberry32 } from "../village/seed";
import { CITY } from "../village/terrain";

export const ROAD_W = 11; // asphalt width between block slabs
export const SLAB_H = 0.15; // sidewalk slab height above road level
export const ROAD_Y = CITY.y + 0.02;
export const SLAB_TOP = CITY.y + SLAB_H;
export const LANE_OFFSET = 2.75; // right-hand lane offset from street centerline

export const STREET_XS = [-500, -425, -350, -275, -200];
export const STREET_ZS = [-350, -262.5, -175, -87.5, 0, 87.5, 175, 262.5, 350];

const CITY_CENTER_X = -350;
const CITY_CENTER_Z = 0;

export interface BlockRect {
  minX: number;
  maxX: number;
  minZ: number;
  maxZ: number;
}

export interface BuildingSpec {
  x: number;
  z: number;
  w: number; // extent along x
  d: number; // extent along z
  h: number;
  variant: number; // facade texture index 0..3
  storefront: boolean;
  acUnits: Array<[number, number]>; // roof positions relative to center
  waterTower: [number, number] | null;
  /** Rendered as an active construction site instead of a finished tower. */
  construction?: boolean;
}

export interface CarRoute {
  points: Float32Array; // x,z pairs, densely resampled
  cumLen: Float32Array; // cumulative length per point
  totalLen: number;
}

export interface StreetLight {
  x: number;
  z: number;
  rot: number; // yaw so the head faces the road
}

export interface Decal {
  x: number;
  z: number;
  rot: number; // 0 = stripe long axis along x
}

export interface ParkedCar {
  x: number;
  z: number;
  yaw: number;
}

export interface CityPlan {
  blocks: BlockRect[];
  buildings: BuildingSpec[];
  routes: CarRoute[];
  lights: StreetLight[];
  dashes: Decal[];
  crosswalkStripes: Decal[];
  parked: ParkedCar[];
}

function downtownFactor(x: number, z: number): number {
  const d = Math.hypot((x - CITY_CENTER_X) / 160, (z - CITY_CENTER_Z) / 360);
  return Math.max(0, 1 - d);
}

function makeBuildings(rand: () => number, blocks: BlockRect[]): BuildingSpec[] {
  const out: BuildingSpec[] = [];
  for (const b of blocks) {
    const cx = (b.minX + b.maxX) / 2;
    const cz = (b.minZ + b.maxZ) / 2;
    const t = downtownFactor(cx, cz);
    // Buildable area: slab inset by a 3.5 m sidewalk ring.
    const innerW = b.maxX - b.minX - 7;
    const innerD = b.maxZ - b.minZ - 7;

    const addBuilding = (x: number, z: number, w: number, d: number, h: number) => {
      const glassy = h > 45;
      const variant = glassy
        ? (rand() < 0.7 ? 3 : 1)
        : Math.floor(rand() * 3); // 0 beige, 1 precast, 2 brick
      const acUnits: Array<[number, number]> = [];
      const nAc = h > 20 ? 1 + Math.floor(rand() * 3) : 0;
      for (let k = 0; k < nAc; k++) {
        acUnits.push([(rand() - 0.5) * w * 0.55, (rand() - 0.5) * d * 0.55]);
      }
      const waterTower: [number, number] | null =
        variant === 2 && h < 40 && rand() < 0.35
          ? [(rand() - 0.5) * w * 0.4, (rand() - 0.5) * d * 0.4]
          : null;
      out.push({ x, z, w, d, h, variant, storefront: rand() < 0.75, acUnits, waterTower });
    };

    if (t > 0.55 && rand() < 0.45) {
      // Single downtown tower on a plaza.
      const w = innerW * (0.55 + rand() * 0.2);
      const d = innerD * (0.5 + rand() * 0.2);
      const h = 45 + t * 40 + rand() * 25;
      addBuilding(cx, cz, w, d, h);
    } else {
      // 2x2 lots, most occupied.
      const lotW = innerW / 2;
      const lotD = innerD / 2;
      for (let i = 0; i < 2; i++) {
        for (let j = 0; j < 2; j++) {
          if (rand() < 0.14) continue; // empty lot / plaza
          const lx = b.minX + 3.5 + lotW * (i + 0.5);
          const lz = b.minZ + 3.5 + lotD * (j + 0.5);
          const w = lotW * (0.72 + rand() * 0.2);
          const d = lotD * (0.72 + rand() * 0.2);
          const h = 9 + rand() * 13 + t * (18 + rand() * 25);
          addBuilding(lx, lz, w, d, h);
        }
      }
    }
  }
  return out;
}

/** Offset a rectangular loop to the right-hand lane and resample it densely
 * with rounded corners. */
function makeRoute(corners: Array<[number, number]>): CarRoute {
  const n = corners.length;
  const offsetPts: Array<[number, number]> = [];
  for (let i = 0; i < n; i++) {
    const a = corners[i];
    const b = corners[(i + 1) % n];
    const dx = b[0] - a[0];
    const dz = b[1] - a[1];
    const len = Math.hypot(dx, dz) || 1;
    // Right of travel in the xz plane: (dz, -dx)/len.
    const ox = (dz / len) * LANE_OFFSET;
    const oz = (-dx / len) * LANE_OFFSET;
    offsetPts.push([a[0] + ox, a[1] + oz], [b[0] + ox, b[1] + oz]);
  }
  const loop: Array<[number, number]> = [];
  for (let i = 0; i < n; i++) {
    const prevEnd = offsetPts[((i + n - 1) % n) * 2 + 1];
    const currStart = offsetPts[i * 2];
    loop.push([(prevEnd[0] + currStart[0]) / 2, (prevEnd[1] + currStart[1]) / 2]);
  }
  // Rounded corners: cut each corner with a quadratic bezier.
  const R = 7;
  const dense: number[] = [];
  for (let i = 0; i < n; i++) {
    const p0 = loop[(i + n - 1) % n];
    const p1 = loop[i];
    const p2 = loop[(i + 1) % n];
    const inV = [p1[0] - p0[0], p1[1] - p0[1]];
    const outV = [p2[0] - p1[0], p2[1] - p1[1]];
    const inLen = Math.hypot(inV[0], inV[1]) || 1;
    const outLen = Math.hypot(outV[0], outV[1]) || 1;
    const rIn = Math.min(R, inLen * 0.4);
    const rOut = Math.min(R, outLen * 0.4);
    const a: [number, number] = [p1[0] - (inV[0] / inLen) * rIn, p1[1] - (inV[1] / inLen) * rIn];
    const c: [number, number] = [p1[0] + (outV[0] / outLen) * rOut, p1[1] + (outV[1] / outLen) * rOut];
    const prevC: [number, number] = i === 0
      ? a
      : [dense[dense.length - 2], dense[dense.length - 1]];
    const straightLen = Math.hypot(a[0] - prevC[0], a[1] - prevC[1]);
    const straightSteps = Math.max(1, Math.round(straightLen / 3));
    for (let k = 1; k <= straightSteps; k++) {
      dense.push(
        prevC[0] + ((a[0] - prevC[0]) * k) / straightSteps,
        prevC[1] + ((a[1] - prevC[1]) * k) / straightSteps,
      );
    }
    for (let k = 1; k <= 6; k++) {
      const u = k / 6;
      const iu = 1 - u;
      dense.push(
        iu * iu * a[0] + 2 * iu * u * p1[0] + u * u * c[0],
        iu * iu * a[1] + 2 * iu * u * p1[1] + u * u * c[1],
      );
    }
  }
  const points = new Float32Array(dense);
  const count = points.length / 2;
  const cumLen = new Float32Array(count);
  let total = 0;
  for (let i = 1; i < count; i++) {
    total += Math.hypot(
      points[i * 2] - points[(i - 1) * 2],
      points[i * 2 + 1] - points[(i - 1) * 2 + 1],
    );
    cumLen[i] = total;
  }
  total += Math.hypot(points[0] - points[(count - 1) * 2], points[1] - points[(count - 1) * 2 + 1]);
  return { points, cumLen, totalLen: total };
}

export function buildCityPlan(seed: number): CityPlan {
  const rand = mulberry32(seed ^ 0xc17f0a);

  const blocks: BlockRect[] = [];
  for (let i = 0; i < STREET_XS.length - 1; i++) {
    for (let j = 0; j < STREET_ZS.length - 1; j++) {
      blocks.push({
        minX: STREET_XS[i] + ROAD_W / 2,
        maxX: STREET_XS[i + 1] - ROAD_W / 2,
        minZ: STREET_ZS[j] + ROAD_W / 2,
        maxZ: STREET_ZS[j + 1] - ROAD_W / 2,
      });
    }
  }

  const buildings = makeBuildings(rand, blocks);
  // Two downtown mid-rises become active construction sites (concept art:
  // tower crane + concrete frame next to finished towers).
  const candidates = buildings.filter(
    (b) => b.h > 28 && b.h < 58 && downtownFactor(b.x, b.z) > 0.3,
  );
  if (candidates.length > 1) candidates[1].construction = true;
  if (candidates.length > 4) candidates[4].construction = true;

  // Traffic loops around random super-blocks, half in each direction.
  const routes: CarRoute[] = [];
  for (let r = 0; r < 10; r++) {
    const i0 = Math.floor(rand() * (STREET_XS.length - 1));
    const i1 = Math.min(STREET_XS.length - 1, i0 + 1 + Math.floor(rand() * 2));
    const j0 = Math.floor(rand() * (STREET_ZS.length - 2));
    const j1 = Math.min(STREET_ZS.length - 1, j0 + 1 + Math.floor(rand() * 3));
    const x0 = STREET_XS[i0];
    const x1 = STREET_XS[i1];
    const z0 = STREET_ZS[j0];
    const z1 = STREET_ZS[j1];
    const cw: Array<[number, number]> = [[x0, z0], [x1, z0], [x1, z1], [x0, z1]];
    routes.push(makeRoute(r % 2 === 0 ? cw : [...cw].reverse()));
  }

  // Streetlights along both street families, alternating sides.
  const lights: StreetLight[] = [];
  for (const x of STREET_XS) {
    for (let z = CITY.minZ + 20, k = 0; z < CITY.maxZ - 10; z += 45, k++) {
      const side = k % 2 === 0 ? 1 : -1;
      lights.push({ x: x + side * (ROAD_W / 2 + 0.7), z, rot: side > 0 ? Math.PI / 2 : -Math.PI / 2 });
    }
  }
  for (const z of STREET_ZS) {
    for (let x = CITY.minX + 20, k = 0; x < CITY.maxX - 10; x += 45, k++) {
      const side = k % 2 === 0 ? 1 : -1;
      lights.push({ x, z: z + side * (ROAD_W / 2 + 0.7), rot: side > 0 ? Math.PI : 0 });
    }
  }

  // Lane dashes, skipped near intersections.
  const dashes: Decal[] = [];
  const nearAny = (v: number, arr: number[], r: number) => arr.some((s) => Math.abs(v - s) < r);
  for (const x of STREET_XS) {
    for (let z = CITY.minZ + 6; z < CITY.maxZ - 6; z += 6) {
      if (nearAny(z, STREET_ZS, 9)) continue;
      dashes.push({ x, z, rot: Math.PI / 2 });
    }
  }
  for (const z of STREET_ZS) {
    for (let x = CITY.minX + 6; x < CITY.maxX - 6; x += 6) {
      if (nearAny(x, STREET_XS, 9)) continue;
      dashes.push({ x, z, rot: 0 });
    }
  }

  // Crosswalk stripes at every intersection, on all four approaches.
  const crosswalkStripes: Decal[] = [];
  for (const x of STREET_XS) {
    for (const z of STREET_ZS) {
      for (let s = -2; s <= 2; s++) {
        const off = s * 0.95;
        crosswalkStripes.push({ x: x + off, z: z - ROAD_W / 2 - 1.4, rot: Math.PI / 2 });
        crosswalkStripes.push({ x: x + off, z: z + ROAD_W / 2 + 1.4, rot: Math.PI / 2 });
        crosswalkStripes.push({ x: x - ROAD_W / 2 - 1.4, z: z + off, rot: 0 });
        crosswalkStripes.push({ x: x + ROAD_W / 2 + 1.4, z: z + off, rot: 0 });
      }
    }
  }

  // Parked cars along random curbs.
  const parked: ParkedCar[] = [];
  for (let k = 0; k < 26; k++) {
    if (rand() < 0.5) {
      const x = STREET_XS[Math.floor(rand() * STREET_XS.length)];
      const z = CITY.minZ + 25 + rand() * (CITY.maxZ - CITY.minZ - 50);
      if (STREET_ZS.some((s) => Math.abs(z - s) < 12)) continue;
      const side = rand() < 0.5 ? 1 : -1;
      parked.push({ x: x + side * (ROAD_W / 2 - 1.15), z, yaw: side > 0 ? Math.PI : 0 });
    } else {
      const z = STREET_ZS[Math.floor(rand() * STREET_ZS.length)];
      const x = CITY.minX + 25 + rand() * (CITY.maxX - CITY.minX - 50);
      if (STREET_XS.some((s) => Math.abs(x - s) < 12)) continue;
      const side = rand() < 0.5 ? 1 : -1;
      parked.push({ x, z: z + side * (ROAD_W / 2 - 1.15), yaw: side > 0 ? Math.PI / 2 : -Math.PI / 2 });
    }
  }

  return { blocks, buildings, routes, lights, dashes, crosswalkStripes, parked };
}

/** Ground height inside the city: slab tops on blocks, asphalt on streets.
 * Returns -Infinity outside the city rectangle. */
export function cityGroundAt(x: number, z: number, plan: CityPlan): number {
  if (x < CITY.minX - 2 || x > CITY.maxX + 2 || z < CITY.minZ - 2 || z > CITY.maxZ + 2) {
    return -Infinity;
  }
  for (const b of plan.blocks) {
    if (x >= b.minX && x <= b.maxX && z >= b.minZ && z <= b.maxZ) return SLAB_TOP;
  }
  return ROAD_Y;
}
