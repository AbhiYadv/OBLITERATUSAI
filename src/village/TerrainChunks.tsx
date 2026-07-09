/**
 * Chunked terrain renderer with distance-based LOD.
 *
 * The 2048 m world is a fixed 16x16 grid of 128 m chunks. Every chunk is
 * always present (no streaming pop-in at this world size); only its mesh
 * resolution changes with distance to the player. Chunks get vertex skirts
 * so LOD transitions never show cracks. Builds run through a per-frame
 * time-budgeted queue to avoid hitches while walking.
 */
import { useEffect, useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import {
  CHUNKS_PER_SIDE,
  CHUNK_SIZE,
  HALF_WORLD,
  colorAt,
  heightAt,
  sampleTerrain,
} from "./terrain";
import { villageStats } from "./stats";

const LOD_SEGMENTS = [64, 32, 16, 8];
const LOD_DISTANCE = [230, 470, 900]; // beyond the last -> coarsest LOD
const CHUNK_COUNT = CHUNKS_PER_SIDE * CHUNKS_PER_SIDE;

function lodForDistance(dist: number): number {
  for (let i = 0; i < LOD_DISTANCE.length; i++) {
    if (dist < LOD_DISTANCE[i]) return i;
  }
  return LOD_SEGMENTS.length - 1;
}

function buildChunkGeometry(ci: number, cj: number, lod: number): THREE.BufferGeometry {
  const segments = LOD_SEGMENTS[lod];
  const n = segments + 1;
  const step = CHUNK_SIZE / segments;
  const x0 = ci * CHUNK_SIZE - HALF_WORLD;
  const z0 = cj * CHUNK_SIZE - HALF_WORLD;
  const gridVerts = n * n;
  const skirtVerts = 4 * n;
  const total = gridVerts + skirtVerts;

  const positions = new Float32Array(total * 3);
  const normals = new Float32Array(total * 3);
  const colors = new Float32Array(total * 3);
  const eps = Math.min(step * 0.5, 2);
  const rgb = [0, 0, 0];

  for (let j = 0; j < n; j++) {
    for (let i = 0; i < n; i++) {
      const x = x0 + i * step;
      const z = z0 + j * step;
      const s = sampleTerrain(x, z);
      const h = s.height;
      const dhx = (heightAt(x + eps, z) - h) / eps;
      const dhz = (heightAt(x, z + eps) - h) / eps;
      const v = (j * n + i) * 3;
      positions[v] = x;
      positions[v + 1] = h;
      positions[v + 2] = z;
      const invLen = 1 / Math.sqrt(dhx * dhx + 1 + dhz * dhz);
      normals[v] = -dhx * invLen;
      normals[v + 1] = invLen;
      normals[v + 2] = -dhz * invLen;
      colorAt(s, x, z, Math.sqrt(dhx * dhx + dhz * dhz), rgb);
      colors[v] = rgb[0];
      colors[v + 1] = rgb[1];
      colors[v + 2] = rgb[2];
    }
  }

  const indices: number[] = [];
  for (let j = 0; j < segments; j++) {
    for (let i = 0; i < segments; i++) {
      const a = j * n + i;
      const b = a + 1;
      const c = a + n;
      const d = c + 1;
      indices.push(a, c, b, b, c, d);
    }
  }

  // Skirts: duplicate each border vertex, dropped down, to hide LOD cracks.
  const skirtDepth = 3 + step;
  let cursor = gridVerts;
  const addSkirt = (edge: number[], flip: boolean) => {
    const base = cursor;
    for (let k = 0; k < edge.length; k++) {
      const src = edge[k] * 3;
      const dst = (base + k) * 3;
      positions[dst] = positions[src];
      positions[dst + 1] = positions[src + 1] - skirtDepth;
      positions[dst + 2] = positions[src + 2];
      normals[dst] = normals[src];
      normals[dst + 1] = normals[src + 1];
      normals[dst + 2] = normals[src + 2];
      colors[dst] = colors[src];
      colors[dst + 1] = colors[src + 1];
      colors[dst + 2] = colors[src + 2];
    }
    for (let k = 0; k < edge.length - 1; k++) {
      const v0 = edge[k];
      const v1 = edge[k + 1];
      const s0 = base + k;
      const s1 = base + k + 1;
      if (flip) {
        indices.push(v0, s0, v1, v1, s0, s1);
      } else {
        indices.push(v0, v1, s0, v1, s1, s0);
      }
    }
    cursor += edge.length;
  };

  const north: number[] = [];
  const south: number[] = [];
  const west: number[] = [];
  const east: number[] = [];
  for (let k = 0; k < n; k++) {
    north.push(k); // j = 0, i ascending
    south.push(segments * n + k); // j = segments
    west.push(k * n); // i = 0, j ascending
    east.push(k * n + segments); // i = segments
  }
  addSkirt(north, false);
  addSkirt(east, false);
  addSkirt(south, true);
  addSkirt(west, true);

  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute("position", new THREE.BufferAttribute(positions, 3));
  geometry.setAttribute("normal", new THREE.BufferAttribute(normals, 3));
  geometry.setAttribute("color", new THREE.BufferAttribute(colors, 3));
  geometry.setIndex(indices);
  geometry.computeBoundingSphere();
  return geometry;
}

interface TerrainChunksProps {
  playerPos: React.RefObject<THREE.Vector3>;
}

export function TerrainChunks({ playerPos }: TerrainChunksProps) {
  const groupRef = useRef<THREE.Group>(null);

  const material = useMemo(
    () => new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.95, metalness: 0 }),
    [],
  );

  const state = useMemo(
    () => ({
      builtLod: new Int8Array(CHUNK_COUNT).fill(-1),
      queuedLod: new Int8Array(CHUNK_COUNT).fill(-1),
      meshes: new Array<THREE.Mesh | null>(CHUNK_COUNT).fill(null),
      queue: [] as { idx: number; lod: number; dist: number }[],
      lastEvalChunkX: Number.NaN,
      lastEvalChunkZ: Number.NaN,
      builtCount: 0,
    }),
    [],
  );

  useEffect(() => {
    return () => {
      for (const mesh of state.meshes) mesh?.geometry.dispose();
      material.dispose();
    };
  }, [state, material]);

  useFrame(() => {
    const group = groupRef.current;
    const pos = playerPos.current;
    if (!group || !pos) return;

    // Re-evaluate desired LODs when the player enters a new chunk.
    const pcx = Math.floor((pos.x + HALF_WORLD) / CHUNK_SIZE);
    const pcz = Math.floor((pos.z + HALF_WORLD) / CHUNK_SIZE);
    if (pcx !== state.lastEvalChunkX || pcz !== state.lastEvalChunkZ) {
      state.lastEvalChunkX = pcx;
      state.lastEvalChunkZ = pcz;
      state.queue.length = 0;
      state.queuedLod.fill(-1);
      for (let cj = 0; cj < CHUNKS_PER_SIDE; cj++) {
        for (let ci = 0; ci < CHUNKS_PER_SIDE; ci++) {
          const idx = cj * CHUNKS_PER_SIDE + ci;
          const cx = ci * CHUNK_SIZE - HALF_WORLD + CHUNK_SIZE / 2;
          const cz = cj * CHUNK_SIZE - HALF_WORLD + CHUNK_SIZE / 2;
          const dist = Math.hypot(cx - pos.x, cz - pos.z);
          const lod = lodForDistance(dist);
          if (lod !== state.builtLod[idx]) {
            state.queue.push({ idx, lod, dist });
            state.queuedLod[idx] = lod;
          }
        }
      }
      state.queue.sort((a, b) => a.dist - b.dist);
    }

    // Build with a time budget: generous during initial load, small after.
    const initialLoad = state.builtCount < CHUNK_COUNT;
    const budgetMs = initialLoad ? 24 : 7;
    const start = performance.now();
    while (state.queue.length > 0 && performance.now() - start < budgetMs) {
      const job = state.queue.shift()!;
      if (state.queuedLod[job.idx] !== job.lod) continue; // superseded
      state.queuedLod[job.idx] = -1;
      const ci = job.idx % CHUNKS_PER_SIDE;
      const cj = Math.floor(job.idx / CHUNKS_PER_SIDE);
      const geometry = buildChunkGeometry(ci, cj, job.lod);
      const old = state.meshes[job.idx];
      if (old) {
        old.geometry.dispose();
        old.geometry = geometry;
      } else {
        const mesh = new THREE.Mesh(geometry, material);
        mesh.receiveShadow = true;
        mesh.matrixAutoUpdate = false;
        group.add(mesh);
        state.meshes[job.idx] = mesh;
        state.builtCount++;
      }
      state.builtLod[job.idx] = job.lod;
    }

    villageStats.chunksBuilt = state.builtCount;
    villageStats.chunksPending = state.queue.length;
  });

  return <group ref={groupRef} />;
}
