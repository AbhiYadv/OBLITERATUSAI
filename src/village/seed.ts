/**
 * Deterministic seeding + noise for the village world.
 * All terrain, scatter, and layout derive from a single integer seed so a
 * given `?seed=` URL always produces the identical world.
 */

export const DEFAULT_SEED = 20260702;

export function parseSeed(): number {
  const raw = new URLSearchParams(window.location.search).get("seed");
  if (!raw) return DEFAULT_SEED;
  const n = Number.parseInt(raw, 10);
  return Number.isFinite(n) ? n >>> 0 : DEFAULT_SEED;
}

/** 32-bit integer avalanche hash. */
export function hashInt(x: number): number {
  x = Math.imul(x ^ (x >>> 16), 0x45d9f3b);
  x = Math.imul(x ^ (x >>> 16), 0x45d9f3b);
  return (x ^ (x >>> 16)) >>> 0;
}

/** Hash a 2D integer lattice point to [0, 1). */
export function hash2(seed: number, ix: number, iz: number): number {
  let h = seed ^ Math.imul(ix | 0, 0x27d4eb2d) ^ Math.imul(iz | 0, 0x165667b1);
  h = hashInt(h);
  return h / 4294967296;
}

/** Mulberry32 PRNG stream for deterministic scatter. */
export function mulberry32(seed: number): () => number {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const fade = (t: number) => t * t * (3 - 2 * t);

/** Smooth 2D value noise in [-1, 1]. */
export function valueNoise2(seed: number, x: number, z: number): number {
  const ix = Math.floor(x);
  const iz = Math.floor(z);
  const fx = fade(x - ix);
  const fz = fade(z - iz);
  const a = hash2(seed, ix, iz);
  const b = hash2(seed, ix + 1, iz);
  const c = hash2(seed, ix, iz + 1);
  const d = hash2(seed, ix + 1, iz + 1);
  const v = a + (b - a) * fx + (c - a) * fz + (a - b - c + d) * fx * fz;
  return v * 2 - 1;
}

/** Fractal Brownian motion, roughly [-1, 1]. */
export function fbm(
  seed: number,
  x: number,
  z: number,
  octaves: number,
  frequency: number,
): number {
  let sum = 0;
  let amp = 0.5;
  let norm = 0;
  let f = frequency;
  for (let i = 0; i < octaves; i++) {
    sum += valueNoise2(seed + i * 1013, x * f, z * f) * amp;
    norm += amp;
    amp *= 0.5;
    f *= 2.02;
  }
  return sum / norm;
}

export function smoothstep(edge0: number, edge1: number, x: number): number {
  const t = Math.min(1, Math.max(0, (x - edge0) / (edge1 - edge0)));
  return t * t * (3 - 2 * t);
}

export function mix(a: number, b: number, t: number): number {
  return a + (b - a) * t;
}
