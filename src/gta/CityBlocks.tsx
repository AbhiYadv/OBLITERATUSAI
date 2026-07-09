/**
 * Textured city buildings, merged aggressively for draw-call budget:
 * one merged wall mesh per facade variant (4), one merged storefront
 * strip mesh, one merged roof mesh, plus instanced roof props (AC units,
 * water towers). ~100 buildings render in about 9 draw calls.
 */
import { useEffect, useMemo } from "react";
import * as THREE from "three";
import { mergeGeometries } from "three/examples/jsm/utils/BufferGeometryUtils.js";
import { mulberry32 } from "../village/seed";
import { SLAB_TOP, STREET_XS, STREET_ZS, type BuildingSpec, type CityPlan } from "./cityPlan";
import { boundsFromPlanBuilding, registerObjects, worldCollisionRegistry } from "./collision/CollisionRegistry";
import {
  FACADE_TILE_M,
  STOREFRONT_H,
  STOREFRONT_TILE_M,
  makeFacadeTextures,
} from "./facades";

/** Recessed ground-floor entrances with frames and projecting canopies, so
 * buildings read as enterable structures instead of sealed boxes. Each
 * entrance faces the nearest street. Returns three merged geometries. */
function buildEntrances(buildings: BuildingSpec[]): {
  recess: THREE.BufferGeometry;
  frame: THREE.BufferGeometry;
  canopy: THREE.BufferGeometry;
} {
  const recessParts: THREE.BufferGeometry[] = [];
  const frameParts: THREE.BufferGeometry[] = [];
  const canopyParts: THREE.BufferGeometry[] = [];
  const push = (
    arr: THREE.BufferGeometry[],
    w: number, h: number, d: number,
    x: number, y: number, z: number,
  ) => {
    const g = new THREE.BoxGeometry(w, h, d);
    g.translate(x, y, z);
    arr.push(g);
  };

  const doorW = 2.5;
  const doorH = 3.0;
  const y0 = SLAB_TOP;
  for (const b of buildings) {
    if (b.construction || b.h < 8) continue;
    // Nearest street on each axis, and which way it lies.
    let bestX = Infinity, sxDir = 1;
    for (const sx of STREET_XS) {
      const dd = Math.abs(b.x - sx);
      if (dd < bestX) { bestX = dd; sxDir = Math.sign(sx - b.x) || 1; }
    }
    let bestZ = Infinity, szDir = 1;
    for (const sz of STREET_ZS) {
      const dd = Math.abs(b.z - sz);
      if (dd < bestZ) { bestZ = dd; szDir = Math.sign(sz - b.z) || 1; }
    }
    if (bestX <= bestZ) {
      // Entrance on the ±x face.
      const fx = b.x + sxDir * (b.w / 2);
      push(recessParts, 0.5, doorH, doorW, fx - sxDir * 0.24, y0 + doorH / 2, b.z);
      push(frameParts, 0.16, 0.26, doorW + 0.5, fx + sxDir * 0.03, y0 + doorH + 0.13, b.z);
      push(frameParts, 0.16, doorH + 0.26, 0.22, fx + sxDir * 0.03, y0 + (doorH + 0.26) / 2, b.z - doorW / 2 - 0.15);
      push(frameParts, 0.16, doorH + 0.26, 0.22, fx + sxDir * 0.03, y0 + (doorH + 0.26) / 2, b.z + doorW / 2 + 0.15);
      push(canopyParts, 1.4, 0.14, doorW + 1.1, fx + sxDir * 0.7, y0 + doorH + 0.45, b.z);
    } else {
      // Entrance on the ±z face.
      const fz = b.z + szDir * (b.d / 2);
      push(recessParts, doorW, doorH, 0.5, b.x, y0 + doorH / 2, fz - szDir * 0.24);
      push(frameParts, doorW + 0.5, 0.26, 0.16, b.x, y0 + doorH + 0.13, fz + szDir * 0.03);
      push(frameParts, 0.22, doorH + 0.26, 0.16, b.x - doorW / 2 - 0.15, y0 + (doorH + 0.26) / 2, fz + szDir * 0.03);
      push(frameParts, 0.22, doorH + 0.26, 0.16, b.x + doorW / 2 + 0.15, y0 + (doorH + 0.26) / 2, fz + szDir * 0.03);
      push(canopyParts, doorW + 1.1, 0.14, 1.4, b.x, y0 + doorH + 0.45, fz + szDir * 0.7);
    }
  }
  const merge = (arr: THREE.BufferGeometry[]) => {
    const g = mergeGeometries(arr, false)!;
    for (const p of arr) p.dispose();
    return g;
  };
  return { recess: merge(recessParts), frame: merge(frameParts), canopy: merge(canopyParts) };
}

class WallBatch {
  positions: number[] = [];
  normals: number[] = [];
  uvs: number[] = [];
  colors: number[] = [];
  indices: number[] = [];

  /** Wall from (ax,az) to (bx,bz); vertices ordered clockwise-from-above
   * around the building so normals face outward. `tint` varies per
   * building to break up texture repetition. */
  pushWall(
    ax: number,
    az: number,
    bx: number,
    bz: number,
    y0: number,
    y1: number,
    uRep: number,
    vRep: number,
    tint = 1,
  ): void {
    const len = Math.hypot(bx - ax, bz - az) || 1;
    const nx = (bz - az) / len;
    const nz = -(bx - ax) / len;
    const base = this.positions.length / 3;
    this.positions.push(ax, y0, az, bx, y0, bz, bx, y1, bz, ax, y1, az);
    for (let k = 0; k < 4; k++) {
      this.normals.push(-nx, 0, -nz);
      this.colors.push(tint, tint, tint);
    }
    this.uvs.push(0, 0, uRep, 0, uRep, vRep, 0, vRep);
    this.indices.push(base, base + 1, base + 2, base, base + 2, base + 3);
  }

  toGeometry(): THREE.BufferGeometry {
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.Float32BufferAttribute(this.positions, 3));
    geo.setAttribute("normal", new THREE.Float32BufferAttribute(this.normals, 3));
    geo.setAttribute("uv", new THREE.Float32BufferAttribute(this.uvs, 2));
    geo.setAttribute("color", new THREE.Float32BufferAttribute(this.colors, 3));
    geo.setIndex(this.indices);
    geo.computeBoundingSphere();
    return geo;
  }
}

function wallsAround(
  batch: WallBatch,
  b: BuildingSpec,
  y0: number,
  y1: number,
  grow: number,
  tileM: number,
  vRep?: number,
  tint = 1,
): void {
  const w2 = b.w / 2 + grow;
  const d2 = b.d / 2 + grow;
  const corners: Array<[number, number]> = [
    [b.x - w2, b.z - d2],
    [b.x - w2, b.z + d2],
    [b.x + w2, b.z + d2],
    [b.x + w2, b.z - d2],
  ];
  for (let i = 0; i < 4; i++) {
    const a = corners[i];
    const c = corners[(i + 1) % 4];
    const len = Math.hypot(c[0] - a[0], c[1] - a[1]);
    batch.pushWall(a[0], a[1], c[0], c[1], y0, y1, len / tileM, vRep ?? (y1 - y0) / tileM, tint);
  }
}

/** Soft ground-contact shading: a gradient skirt quad per wall, fading
 * from dark at the base outward, so buildings sit on the ground instead of
 * floating on it. */
function buildContactAO(buildings: BuildingSpec[]): THREE.BufferGeometry {
  const positions: number[] = [];
  const uvs: number[] = [];
  const indices: number[] = [];
  const W = 1.15;
  for (const b of buildings) {
    const w2 = b.w / 2 + 0.06;
    const d2 = b.d / 2 + 0.06;
    const corners: Array<[number, number]> = [
      [b.x - w2, b.z - d2],
      [b.x - w2, b.z + d2],
      [b.x + w2, b.z + d2],
      [b.x + w2, b.z - d2],
    ];
    for (let i = 0; i < 4; i++) {
      const a = corners[i];
      const c = corners[(i + 1) % 4];
      const len = Math.hypot(c[0] - a[0], c[1] - a[1]) || 1;
      const nx = ((c[1] - a[1]) / len) * -W;
      const nz = (-(c[0] - a[0]) / len) * -W;
      const base = positions.length / 3;
      const y = SLAB_TOP + 0.03;
      positions.push(a[0], y, a[1], c[0], y, c[1], c[0] + nx, y, c[1] + nz, a[0] + nx, y, a[1] + nz);
      uvs.push(0, 0, 1, 0, 1, 1, 0, 1);
      indices.push(base, base + 2, base + 1, base, base + 3, base + 2);
    }
  }
  const geo = new THREE.BufferGeometry();
  geo.setAttribute("position", new THREE.Float32BufferAttribute(positions, 3));
  geo.setAttribute("uv", new THREE.Float32BufferAttribute(uvs, 2));
  geo.setIndex(indices);
  geo.computeBoundingSphere();
  return geo;
}

function makeAOTexture(): THREE.CanvasTexture {
  const canvas = document.createElement("canvas");
  canvas.width = 4;
  canvas.height = 64;
  const ctx = canvas.getContext("2d")!;
  const grad = ctx.createLinearGradient(0, 64, 0, 0);
  grad.addColorStop(0, "rgba(0,0,0,0.5)");
  grad.addColorStop(1, "rgba(0,0,0,0)");
  ctx.fillStyle = grad;
  ctx.fillRect(0, 0, 4, 64);
  return new THREE.CanvasTexture(canvas);
}

export function CityBlocks({ plan, seed }: { plan: CityPlan; seed: number }) {
  const built = useMemo(() => {
    const textures = makeFacadeTextures(seed);
    const facadeBatches = [new WallBatch(), new WallBatch(), new WallBatch(), new WallBatch()];
    const storefrontBatches = textures.storefronts.map(() => new WallBatch());
    const roofBatch = new WallBatch();

    const acSpots: Array<[number, number, number]> = [];
    const towerSpots: Array<[number, number, number]> = [];
    const antennaSpots: Array<[number, number, number]> = [];

    const tintRand = mulberry32(seed ^ 0xb17d);
    for (const b of plan.buildings) {
      // Per-building brightness variation breaks up texture repetition.
      const tint = 0.82 + tintRand() * 0.3;
      const brand = Math.floor(tintRand() * storefrontBatches.length);
      if (b.construction) continue; // rendered by the Construction site
      wallsAround(facadeBatches[b.variant], b, SLAB_TOP, SLAB_TOP + b.h, 0, FACADE_TILE_M, undefined, tint);
      if (b.storefront && b.h > 8) {
        wallsAround(storefrontBatches[brand], b, SLAB_TOP + 0.02, SLAB_TOP + STOREFRONT_H, 0.05, STOREFRONT_TILE_M, 1);
      }
      // Parapet lip crowning the roof edge.
      wallsAround(roofBatch, b, SLAB_TOP + b.h - 0.1, SLAB_TOP + b.h + 0.75, 0.16, 10, 1);
      if (b.h > 55) {
        antennaSpots.push([b.x + b.w * 0.18, SLAB_TOP + b.h, b.z - b.d * 0.12]);
      }
      // Roof cap.
      const y = SLAB_TOP + b.h;
      const base = roofBatch.positions.length / 3;
      const w2 = b.w / 2;
      const d2 = b.d / 2;
      roofBatch.positions.push(
        b.x - w2, y, b.z - d2,
        b.x + w2, y, b.z - d2,
        b.x + w2, y, b.z + d2,
        b.x - w2, y, b.z + d2,
      );
      for (let k = 0; k < 4; k++) {
        roofBatch.normals.push(0, 1, 0);
        roofBatch.colors.push(1, 1, 1);
      }
      roofBatch.uvs.push(0, 0, 1, 0, 1, 1, 0, 1);
      roofBatch.indices.push(base, base + 2, base + 1, base, base + 3, base + 2);

      for (const [ox, oz] of b.acUnits) acSpots.push([b.x + ox, y, b.z + oz]);
      if (b.waterTower) towerSpots.push([b.x + b.waterTower[0], y, b.z + b.waterTower[1]]);
    }

    const facadeGeos = facadeBatches.map((b) => b.toGeometry());
    const facadeMats = textures.facades.map(
      (map, i) =>
        new THREE.MeshStandardMaterial({
          map,
          vertexColors: true,
          roughness: i === 3 ? 0.35 : 0.9,
          metalness: i === 3 ? 0.35 : 0,
        }),
    );
    const storefrontGeos = storefrontBatches.map((b) => b.toGeometry());
    const storefrontMats = textures.storefronts.map(
      (map) => new THREE.MeshStandardMaterial({ map, roughness: 0.6 }),
    );
    const roofGeo = roofBatch.toGeometry();
    const roofMat = new THREE.MeshStandardMaterial({ color: "#3f4044", roughness: 0.95 });

    // Ground-contact ambient occlusion skirt.
    const aoGeo = buildContactAO(plan.buildings.filter((b) => !b.construction));
    const aoTex = makeAOTexture();
    const aoMat = new THREE.MeshBasicMaterial({
      map: aoTex,
      transparent: true,
      depthWrite: false,
      color: "#000000",
    });

    // Roof props.
    const acGeo = new THREE.BoxGeometry(1.7, 1.1, 1.7);
    acGeo.translate(0, 0.55, 0);
    const acMat = new THREE.MeshStandardMaterial({ color: "#8f9499", roughness: 0.8 });
    const ac = new THREE.InstancedMesh(acGeo, acMat, Math.max(1, acSpots.length));
    const tankGeo = new THREE.CylinderGeometry(1.5, 1.7, 3.2, 10);
    tankGeo.translate(0, 2.6, 0);
    const tankMat = new THREE.MeshStandardMaterial({ color: "#6e5138", roughness: 0.9 });
    const tank = new THREE.InstancedMesh(tankGeo, tankMat, Math.max(1, towerSpots.length));
    const capGeo = new THREE.ConeGeometry(1.8, 1.2, 10);
    capGeo.translate(0, 4.7, 0);
    const capMat = new THREE.MeshStandardMaterial({ color: "#54402d", roughness: 0.9 });
    const cap = new THREE.InstancedMesh(capGeo, capMat, Math.max(1, towerSpots.length));
    const antennaGeo = new THREE.CylinderGeometry(0.05, 0.14, 7.5, 5);
    antennaGeo.translate(0, 3.75, 0);
    const antennaMat = new THREE.MeshStandardMaterial({ color: "#43464b", roughness: 0.5, metalness: 0.6 });
    const antenna = new THREE.InstancedMesh(antennaGeo, antennaMat, Math.max(1, antennaSpots.length));

    const m = new THREE.Matrix4();
    acSpots.forEach((p, i) => ac.setMatrixAt(i, m.makeTranslation(p[0], p[1], p[2])));
    towerSpots.forEach((p, i) => {
      tank.setMatrixAt(i, m.makeTranslation(p[0], p[1], p[2]));
      cap.setMatrixAt(i, m.makeTranslation(p[0], p[1], p[2]));
    });
    antennaSpots.forEach((p, i) => antenna.setMatrixAt(i, m.makeTranslation(p[0], p[1], p[2])));
    ac.count = acSpots.length;
    tank.count = towerSpots.length;
    cap.count = towerSpots.length;
    antenna.count = antennaSpots.length;
    ac.instanceMatrix.needsUpdate = true;
    tank.instanceMatrix.needsUpdate = true;
    cap.instanceMatrix.needsUpdate = true;
    antenna.instanceMatrix.needsUpdate = true;
    ac.castShadow = tank.castShadow = cap.castShadow = antenna.castShadow = true;

    // Recessed street entrances with frames and canopies.
    const entrances = buildEntrances(plan.buildings);
    const entryRecessMat = new THREE.MeshStandardMaterial({
      color: "#0d1418",
      roughness: 0.15,
      metalness: 0.4,
      envMapIntensity: 1.1,
    });
    const entryFrameMat = new THREE.MeshStandardMaterial({ color: "#c9c6bd", roughness: 0.8 });
    const entryCanopyMat = new THREE.MeshStandardMaterial({
      color: "#2b3a47",
      roughness: 0.4,
      metalness: 0.6,
    });

    return {
      textures, facadeGeos, facadeMats, storefrontGeos, storefrontMats, roofGeo, roofMat,
      aoGeo, aoTex, aoMat, ac, tank, cap, antenna,
      entrances, entryRecessMat, entryFrameMat, entryCanopyMat,
    };
  }, [plan, seed]);

  useEffect(() => {
    const unregisterBuildings = registerObjects(
      worldCollisionRegistry,
      plan.buildings.map((building, i) => boundsFromPlanBuilding(`building:${i}`, building, SLAB_TOP)),
    );
    return () => {
      unregisterBuildings();
      built.textures.dispose();
      for (const g of built.facadeGeos) g.dispose();
      for (const mat of built.facadeMats) mat.dispose();
      for (const g of built.storefrontGeos) g.dispose();
      for (const mat of built.storefrontMats) mat.dispose();
      built.roofGeo.dispose();
      built.roofMat.dispose();
      built.aoGeo.dispose();
      built.aoTex.dispose();
      built.aoMat.dispose();
      built.entrances.recess.dispose();
      built.entrances.frame.dispose();
      built.entrances.canopy.dispose();
      built.entryRecessMat.dispose();
      built.entryFrameMat.dispose();
      built.entryCanopyMat.dispose();
      for (const inst of [built.ac, built.tank, built.cap, built.antenna]) {
        inst.geometry.dispose();
        (inst.material as THREE.Material).dispose();
      }
    };
  }, [built, plan]);

  return (
    <group>
      {built.facadeGeos.map((geo, i) => (
        <mesh key={i} geometry={geo} material={built.facadeMats[i]} castShadow receiveShadow />
      ))}
      {built.storefrontGeos.map((geo, i) => (
        <mesh key={`s${i}`} geometry={geo} material={built.storefrontMats[i]} />
      ))}
      <mesh geometry={built.roofGeo} material={built.roofMat} />
      <mesh geometry={built.entrances.recess} material={built.entryRecessMat} />
      <mesh geometry={built.entrances.frame} material={built.entryFrameMat} castShadow />
      <mesh geometry={built.entrances.canopy} material={built.entryCanopyMat} castShadow />
      <mesh geometry={built.aoGeo} material={built.aoMat} renderOrder={1} />
      <primitive object={built.ac} />
      <primitive object={built.tank} />
      <primitive object={built.cap} />
      <primitive object={built.antenna} />
    </group>
  );
}
