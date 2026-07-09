/**
 * Sidewalk life from the concept art: real street trees in low planter
 * surrounds, benches, litter bins, crossing bollards, and the occasional
 * red café umbrella. Trees are the believable multi-part model from
 * treeBuilder (trunk + branches + leaf clusters), scaled to street size.
 * Everything is instanced — a dozen-odd draw calls for several hundred
 * objects.
 */
import { useEffect, useMemo } from "react";
import * as THREE from "three";
import { mulberry32 } from "../village/seed";
import { BARK_MATERIAL, FOLIAGE_MATERIAL, buildTreeVariants } from "../village/treeBuilder";
import { SLAB_TOP, type BlockRect, type CityPlan } from "./cityPlan";
import { makeObject, registerObjects, worldCollisionRegistry, type CollisionObject } from "./collision/CollisionRegistry";

const TREE_VARIANTS = 3;

interface Placement {
  x: number;
  z: number;
  rot: number;
  scale: number;
}

interface Layout {
  trees: Placement[];
  benches: Placement[];
  bins: Placement[];
  bollards: Placement[];
  umbrellas: Placement[];
  grass: Placement[];
}

function addGrassPatch(layout: Layout, rand: () => number, x: number, z: number, spread: number, count: number): void {
  for (let i = 0; i < count; i++) {
    layout.grass.push({
      x: x + (rand() - 0.5) * spread,
      z: z + (rand() - 0.5) * spread,
      rot: rand() * Math.PI * 2,
      scale: 0.65 + rand() * 0.75,
    });
  }
}

/** Walk one block edge, placing furniture on the sidewalk band. */
function fillEdge(
  layout: Layout,
  rand: () => number,
  ax: number,
  az: number,
  bx: number,
  bz: number,
): void {
  const len = Math.hypot(bx - ax, bz - az);
  const dx = (bx - ax) / len;
  const dz = (bz - az) / len;
  // Inward normal (block corners are traversed counter-clockwise here).
  const inx = -dz;
  const inz = dx;
  const rot = Math.atan2(dx, dz);
  let slot = Math.floor(rand() * 3);
  for (let t = 9; t < len - 9; t += 15) {
    const px = ax + dx * t + inx * 1.1;
    const pz = az + dz * t + inz * 1.1;
    const kind = slot++ % 5;
    if (kind === 2) {
      layout.benches.push({ x: px + inx * 1.1, z: pz + inz * 1.1, rot: rot + Math.PI, scale: 1 });
    } else if (kind === 4) {
      layout.bins.push({ x: px, z: pz, rot, scale: 1 });
    } else {
      // Street-tree scale: base tree is ~7.5 m, so 0.55-0.75 gives 4-5.5 m.
      layout.trees.push({ x: px, z: pz, rot: rand() * Math.PI * 2, scale: 0.55 + rand() * 0.2 });
      addGrassPatch(layout, rand, px, pz, 1.15, 3 + Math.floor(rand() * 4));
      if (rand() < 0.25) addGrassPatch(layout, rand, px + inx * 1.8, pz + inz * 1.8, 0.8, 2);
    }
  }
  // Continuous grass verge running along the curb side of the footpath, so
  // there is visible planting between the walkway and the road.
  for (let t = 5; t < len - 5; t += 1.6) {
    const gx = ax + dx * t + inx * 0.55;
    const gz = az + dz * t + inz * 0.55;
    addGrassPatch(layout, rand, gx, gz, 0.9, 2 + Math.floor(rand() * 2));
  }
  // Crossing bollards near both corners.
  for (const s of [2, 3.6, 5.2]) {
    layout.bollards.push({ x: ax + dx * s + inx * 0.55, z: az + dz * s + inz * 0.55, rot: 0, scale: 1 });
    layout.bollards.push({ x: bx - dx * s + inx * 0.55, z: bz - dz * s + inz * 0.55, rot: 0, scale: 1 });
  }
  // Occasional café umbrella.
  if (rand() < 0.18) {
    const t = 12 + rand() * (len - 24);
    layout.umbrellas.push({ x: ax + dx * t + inx * 2.6, z: az + dz * t + inz * 2.6, rot: rand(), scale: 1 });
  }
}

function edgesOf(b: BlockRect): Array<[number, number, number, number]> {
  return [
    [b.minX, b.minZ, b.maxX, b.minZ],
    [b.maxX, b.minZ, b.maxX, b.maxZ],
    [b.maxX, b.maxZ, b.minX, b.maxZ],
    [b.minX, b.maxZ, b.minX, b.minZ],
  ];
}

function fill(mesh: THREE.InstancedMesh, list: Placement[]): void {
  const m = new THREE.Matrix4();
  const q = new THREE.Quaternion();
  const up = new THREE.Vector3(0, 1, 0);
  const s = new THREE.Vector3();
  const p = new THREE.Vector3();
  list.forEach((item, i) => {
    q.setFromAxisAngle(up, item.rot);
    s.setScalar(item.scale);
    p.set(item.x, SLAB_TOP, item.z);
    m.compose(p, q, s);
    mesh.setMatrixAt(i, m);
  });
  mesh.count = list.length;
  mesh.instanceMatrix.needsUpdate = true;
}

export function StreetFurniture({ plan, seed }: { plan: CityPlan; seed: number }) {
  const built = useMemo(() => {
    const rand = mulberry32(seed ^ 0xf0a71a);
    const layout: Layout = { trees: [], benches: [], bins: [], bollards: [], umbrellas: [], grass: [] };
    for (const block of plan.blocks) {
      for (const [ax, az, bx, bz] of edgesOf(block)) fillEdge(layout, rand, ax, az, bx, bz);
    }

    const mk = (geo: THREE.BufferGeometry, mat: THREE.Material, list: Placement[], shadow = true) => {
      const mesh = new THREE.InstancedMesh(geo, mat, Math.max(1, list.length));
      fill(mesh, list);
      mesh.castShadow = shadow;
      mesh.frustumCulled = false;
      return mesh;
    };

    // Real street trees (bark + foliage per variant) sitting in low planters.
    const variants = buildTreeVariants(TREE_VARIANTS, seed ^ 0x5721);
    const barkMat = BARK_MATERIAL();
    const foliageMat = FOLIAGE_MATERIAL();
    const buckets: Placement[][] = Array.from({ length: TREE_VARIANTS }, () => []);
    layout.trees.forEach((t, i) => buckets[i % TREE_VARIANTS].push(t));
    const treeMeshes: THREE.InstancedMesh[] = [];
    variants.forEach((variant, v) => {
      const group = buckets[v];
      if (group.length === 0) return;
      const bark = new THREE.InstancedMesh(variant.bark, barkMat, group.length);
      const foliage = new THREE.InstancedMesh(variant.foliage, foliageMat, group.length);
      fill(bark, group);
      fill(foliage, group);
      bark.castShadow = true;
      foliage.castShadow = true;
      bark.frustumCulled = false;
      foliage.frustumCulled = false;
      treeMeshes.push(bark, foliage);
    });

    // Low square planter/tree-pit surround at each tree base.
    const planterGeo = new THREE.BoxGeometry(1.3, 0.32, 1.3);
    planterGeo.translate(0, 0.16, 0);
    const seatGeo = new THREE.BoxGeometry(1.75, 0.09, 0.55);
    seatGeo.translate(0, 0.46, 0);
    const backGeo = new THREE.BoxGeometry(1.75, 0.5, 0.07);
    backGeo.translate(0, 0.75, -0.26);
    const legGeo = new THREE.BoxGeometry(0.08, 0.46, 0.5);
    legGeo.translate(0, 0.23, 0);
    const binGeo = new THREE.CylinderGeometry(0.26, 0.23, 0.78, 12);
    binGeo.translate(0, 0.39, 0);
    const bollardGeo = new THREE.CylinderGeometry(0.085, 0.085, 0.8, 10);
    bollardGeo.translate(0, 0.4, 0);
    const poleGeo = new THREE.CylinderGeometry(0.035, 0.035, 2.35, 6);
    poleGeo.translate(0, 1.17, 0);
    const brellaGeo = new THREE.ConeGeometry(1.5, 0.55, 10);
    brellaGeo.translate(0, 2.4, 0);
    const grassGeo = new THREE.ConeGeometry(0.055, 0.35, 5);
    grassGeo.translate(0, 0.175, 0);

    const mats = {
      planter: new THREE.MeshStandardMaterial({ color: "#6b625a", roughness: 0.9 }),
      bench: new THREE.MeshStandardMaterial({ color: "#7a5c3e", roughness: 0.85 }),
      benchLeg: new THREE.MeshStandardMaterial({ color: "#3a3d40", roughness: 0.5, metalness: 0.5 }),
      bin: new THREE.MeshStandardMaterial({ color: "#2f5b45", roughness: 0.7, metalness: 0.3 }),
      bollard: new THREE.MeshStandardMaterial({ color: "#9aa2a8", roughness: 0.35, metalness: 0.7 }),
      pole: new THREE.MeshStandardMaterial({ color: "#4a4e52", roughness: 0.5, metalness: 0.4 }),
      brella: new THREE.MeshStandardMaterial({ color: "#b0342c", roughness: 0.75 }),
      grass: new THREE.MeshStandardMaterial({ color: "#3f7b42", roughness: 0.95 }),
    };

    const meshes = [
      mk(planterGeo, mats.planter, layout.trees, false),
      mk(seatGeo, mats.bench, layout.benches),
      mk(backGeo, mats.bench, layout.benches),
      mk(legGeo, mats.benchLeg, layout.benches, false),
      mk(binGeo, mats.bin, layout.bins, false),
      mk(bollardGeo, mats.bollard, layout.bollards, false),
      mk(poleGeo, mats.pole, layout.umbrellas),
      mk(brellaGeo, mats.brella, layout.umbrellas),
      mk(grassGeo, mats.grass, layout.grass, false),
    ];
    const colliders: CollisionObject[] = [
      ...layout.trees.map((item, i) =>
        makeObject(
          `street-furniture:planter:${i}`,
          "planter",
          { x: item.x, y: SLAB_TOP + 0.16, z: item.z },
          { x: 0.68, y: 0.16, z: 0.68 },
          item.rot,
        ),
      ),
      ...layout.benches.map((item, i) =>
        makeObject(
          `street-furniture:bench:${i}`,
          "bench",
          { x: item.x, y: SLAB_TOP + 0.42, z: item.z },
          { x: 0.96, y: 0.42, z: 0.42 },
          item.rot,
        ),
      ),
      ...layout.bollards.map((item, i) =>
        makeObject(
          `street-furniture:bollard:${i}`,
          "bollard",
          { x: item.x, y: SLAB_TOP + 0.4, z: item.z },
          { x: 0.16, y: 0.4, z: 0.16 },
          item.rot,
        ),
      ),
      ...layout.umbrellas.map((item, i) =>
        makeObject(
          `street-furniture:cafe-pole:${i}`,
          "solid-prop",
          { x: item.x, y: SLAB_TOP + 1.18, z: item.z },
          { x: 0.16, y: 1.18, z: 0.16 },
          item.rot,
        ),
      ),
    ];
    return { meshes, treeMeshes, variants, barkMat, foliageMat, mats, colliders };
  }, [plan, seed]);

  useEffect(() => {
    const unregisterFurniture = registerObjects(worldCollisionRegistry, built.colliders);
    return () => {
      unregisterFurniture();
      for (const mesh of built.meshes) mesh.geometry.dispose();
      for (const v of built.variants) {
        v.bark.dispose();
        v.foliage.dispose();
      }
      built.barkMat.dispose();
      built.foliageMat.dispose();
      for (const mat of Object.values(built.mats)) mat.dispose();
    };
  }, [built]);

  return (
    <group>
      {built.meshes.map((mesh, i) => (
        <primitive key={`f${i}`} object={mesh} />
      ))}
      {built.treeMeshes.map((mesh, i) => (
        <primitive key={`t${i}`} object={mesh} />
      ))}
    </group>
  );
}
