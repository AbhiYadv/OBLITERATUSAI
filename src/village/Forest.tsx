/**
 * Deterministic instanced vegetation: limited forest zones, sparse lone
 * trees, and rocks. Trees are believable multi-part models (trunk +
 * branches + irregular leaf clusters) from treeBuilder, split across a few
 * variants for species variation. One bark + one foliage InstancedMesh per
 * variant keeps the draw count low.
 */
import { useEffect, useMemo } from "react";
import * as THREE from "three";
import { mulberry32, valueNoise2 } from "./seed";
import { HALF_WORLD, WATER_LEVEL, ZONES, distToPolyline, RIVER, sampleTerrain } from "./terrain";
import { BARK_MATERIAL, FOLIAGE_MATERIAL, buildTreeVariants } from "./treeBuilder";

const MAX_TREES = 1500;
const MAX_ROCKS = 320;
const NUM_VARIANTS = 5;

interface Placement {
  x: number;
  y: number;
  z: number;
  rot: number;
  scale: number;
}

function scatterTrees(seed: number): Placement[] {
  const out: Placement[] = [];
  const zones: Array<{ cx: number; cz: number; r: number; attempts: number }> = [
    { cx: ZONES.forestNorth[0], cz: ZONES.forestNorth[1], r: 340, attempts: 2200 },
    { cx: ZONES.forestSouthwest[0], cz: ZONES.forestSouthwest[1], r: 300, attempts: 1500 },
  ];
  const rand = mulberry32(seed ^ 0x51ab3e);
  for (const zone of zones) {
    for (let i = 0; i < zone.attempts && out.length < MAX_TREES - 250; i++) {
      const ang = rand() * Math.PI * 2;
      const rad = Math.sqrt(rand()) * zone.r;
      const x = zone.cx + Math.cos(ang) * rad;
      const z = zone.cz + Math.sin(ang) * rad;
      if (Math.abs(x) > HALF_WORLD - 30 || Math.abs(z) > HALF_WORLD - 30) continue;
      const s = sampleTerrain(x, z);
      if (s.forestMask < 0.3 || s.villageMask > 0.05 || s.cityMask > 0.02) continue;
      if (s.pathDist < 6 || s.height < WATER_LEVEL + 1.2) continue;
      // Density falls off toward the zone edge for a natural treeline.
      if (rand() > s.forestMask * 0.75 + 0.15) continue;
      out.push({ x, y: s.height - 0.15, z, rot: rand() * Math.PI * 2, scale: 0.85 + rand() * 0.5 });
    }
  }
  // Sparse lone trees across the open world.
  for (let i = 0; i < 900 && out.length < MAX_TREES; i++) {
    const x = (rand() * 2 - 1) * (HALF_WORLD - 60);
    const z = (rand() * 2 - 1) * (HALF_WORLD - 60);
    if (valueNoise2(seed ^ 0x77f1, x / 210, z / 210) < 0.45) continue;
    const s = sampleTerrain(x, z);
    if (s.villageMask > 0.02 || s.farmMask > 0.4 || s.forestMask > 0.3 || s.cityMask > 0.02) continue;
    if (s.pathDist < 8 || s.height < WATER_LEVEL + 1.2 || s.height > 70) continue;
    if (rand() > 0.35) continue;
    out.push({ x, y: s.height - 0.15, z, rot: rand() * Math.PI * 2, scale: 0.95 + rand() * 0.55 });
  }
  return out;
}

function scatterRocks(seed: number): Placement[] {
  const out: Placement[] = [];
  const rand = mulberry32(seed ^ 0x0dd5c4);
  for (let i = 0; i < 1600 && out.length < MAX_ROCKS; i++) {
    const x = (rand() * 2 - 1) * (HALF_WORLD - 40);
    const z = (rand() * 2 - 1) * (HALF_WORLD - 40);
    const s = sampleTerrain(x, z);
    const nearRiver = distToPolyline(RIVER, x, z) < 55;
    if (s.hillMask < 0.25 && !nearRiver) continue;
    if (s.villageMask > 0.05 || s.pathDist < 5 || s.height < WATER_LEVEL - 1.5 || s.cityMask > 0.02) continue;
    if (rand() > 0.5) continue;
    out.push({ x, y: s.height - 0.3, z, rot: rand() * Math.PI * 2, scale: 0.5 + rand() * 1.7 });
  }
  return out;
}

function fillMatrices(mesh: THREE.InstancedMesh, placements: Placement[]): void {
  const m = new THREE.Matrix4();
  const q = new THREE.Quaternion();
  const up = new THREE.Vector3(0, 1, 0);
  const sv = new THREE.Vector3();
  const pv = new THREE.Vector3();
  for (let i = 0; i < placements.length; i++) {
    const p = placements[i];
    q.setFromAxisAngle(up, p.rot);
    sv.setScalar(p.scale);
    pv.set(p.x, p.y, p.z);
    m.compose(pv, q, sv);
    mesh.setMatrixAt(i, m);
  }
  mesh.instanceMatrix.needsUpdate = true;
}

export function Forest({ seed }: { seed: number }) {
  const built = useMemo(() => {
    const trees = scatterTrees(seed);
    const rockPlacements = scatterRocks(seed);
    const variants = buildTreeVariants(NUM_VARIANTS, seed);
    const barkMat = BARK_MATERIAL();
    const foliageMat = FOLIAGE_MATERIAL();

    // Bucket placements by variant so each variant geometry is instanced once.
    const buckets: Placement[][] = Array.from({ length: NUM_VARIANTS }, () => []);
    trees.forEach((t, i) => buckets[i % NUM_VARIANTS].push(t));

    const treeMeshes: THREE.InstancedMesh[] = [];
    variants.forEach((variant, v) => {
      const group = buckets[v];
      if (group.length === 0) return;
      const bark = new THREE.InstancedMesh(variant.bark, barkMat, group.length);
      const foliage = new THREE.InstancedMesh(variant.foliage, foliageMat, group.length);
      fillMatrices(bark, group);
      fillMatrices(foliage, group);
      bark.castShadow = true;
      foliage.castShadow = true;
      foliage.receiveShadow = true;
      treeMeshes.push(bark, foliage);
    });

    const rockGeo = new THREE.DodecahedronGeometry(1, 0);
    rockGeo.scale(1, 0.72, 1);
    const rockMat = new THREE.MeshStandardMaterial({ color: "#7d7d82", roughness: 0.95 });
    const rocks = new THREE.InstancedMesh(rockGeo, rockMat, rockPlacements.length);
    fillMatrices(rocks, rockPlacements);
    rocks.castShadow = true;
    rocks.receiveShadow = true;

    return { treeMeshes, variants, barkMat, foliageMat, rocks, rockGeo, rockMat };
  }, [seed]);

  useEffect(() => {
    return () => {
      for (const v of built.variants) {
        v.bark.dispose();
        v.foliage.dispose();
      }
      built.barkMat.dispose();
      built.foliageMat.dispose();
      built.rockGeo.dispose();
      built.rockMat.dispose();
    };
  }, [built]);

  return (
    <>
      {built.treeMeshes.map((mesh, i) => (
        <primitive key={i} object={mesh} />
      ))}
      <primitive object={built.rocks} />
    </>
  );
}
