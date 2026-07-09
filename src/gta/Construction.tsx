/**
 * Active construction sites (concept art: concrete frame towers with tower
 * cranes). For each building flagged `construction` in the plan: open
 * concrete floor slabs on a column grid, a yellow tower crane with jib and
 * hook, and an orange/white barrier fence with traffic cones.
 * Three instanced draws: colored boxes, columns, cones.
 */
import { useEffect, useMemo } from "react";
import * as THREE from "three";
import { SLAB_TOP, type CityPlan } from "./cityPlan";

const FLOOR_H = 3.4;

interface BoxInst {
  x: number;
  y: number;
  z: number;
  sx: number;
  sy: number;
  sz: number;
  rot: number;
  color: string;
}

export function Construction({ plan }: { plan: CityPlan }) {
  const built = useMemo(() => {
    const sites = plan.buildings.filter((b) => b.construction);
    const boxes: BoxInst[] = [];
    const columns: Array<{ x: number; y: number; z: number; h: number }> = [];
    const cones: Array<{ x: number; z: number }> = [];

    for (const b of sites) {
      const frameH = Math.max(3, Math.round((b.h * 0.6) / FLOOR_H)) * FLOOR_H;
      const floors = frameH / FLOOR_H;
      // Floor slabs.
      for (let f = 0; f <= floors; f++) {
        boxes.push({
          x: b.x,
          y: SLAB_TOP + f * FLOOR_H,
          z: b.z,
          sx: b.w,
          sy: 0.26,
          sz: b.d,
          rot: 0,
          color: "#b8b5ae",
        });
      }
      // Column grid.
      const nx = Math.max(2, Math.round(b.w / 6));
      const nz = Math.max(2, Math.round(b.d / 6));
      for (let i = 0; i <= nx; i++) {
        for (let j = 0; j <= nz; j++) {
          const cx = b.x - b.w / 2 + (b.w * i) / nx;
          const cz = b.z - b.d / 2 + (b.d * j) / nz;
          for (let f = 0; f < floors; f++) {
            columns.push({ x: cx, y: SLAB_TOP + f * FLOOR_H, z: cz, h: FLOOR_H });
          }
        }
      }
      // Tower crane beside the lot.
      const mastX = b.x + b.w / 2 + 4.5;
      const mastZ = b.z - b.d / 2 - 2.5;
      const mastH = frameH + 14;
      boxes.push({ x: mastX, y: SLAB_TOP, z: mastZ, sx: 1.1, sy: mastH, sz: 1.1, rot: 0, color: "#e8b23a" });
      const jibYaw = Math.atan2(b.x - mastX, b.z - mastZ);
      const jibLen = 24;
      boxes.push({
        x: mastX + Math.sin(jibYaw) * (jibLen / 2),
        y: SLAB_TOP + mastH,
        z: mastZ + Math.cos(jibYaw) * (jibLen / 2),
        sx: 0.55,
        sy: 0.7,
        sz: jibLen,
        rot: jibYaw,
        color: "#e8b23a",
      });
      boxes.push({
        x: mastX - Math.sin(jibYaw) * 4.5,
        y: SLAB_TOP + mastH,
        z: mastZ - Math.cos(jibYaw) * 4.5,
        sx: 0.55,
        sy: 0.7,
        sz: 9,
        rot: jibYaw,
        color: "#e8b23a",
      });
      boxes.push({
        x: mastX - Math.sin(jibYaw) * 7.5,
        y: SLAB_TOP + mastH - 1,
        z: mastZ - Math.cos(jibYaw) * 7.5,
        sx: 1.6,
        sy: 1.4,
        sz: 1.2,
        rot: jibYaw,
        color: "#8e8e93",
      });
      // Cab.
      boxes.push({ x: mastX, y: SLAB_TOP + mastH - 2.2, z: mastZ, sx: 1.8, sy: 1.8, sz: 1.8, rot: jibYaw, color: "#d99a24" });
      // Hook cable + hook block, two-thirds along the jib.
      const hx = mastX + Math.sin(jibYaw) * jibLen * 0.66;
      const hz = mastZ + Math.cos(jibYaw) * jibLen * 0.66;
      const cableLen = mastH - frameH - 3;
      boxes.push({ x: hx, y: SLAB_TOP + mastH - cableLen, z: hz, sx: 0.06, sy: cableLen, sz: 0.06, rot: 0, color: "#26282c" });
      boxes.push({ x: hx, y: SLAB_TOP + mastH - cableLen - 0.7, z: hz, sx: 0.7, sy: 0.7, sz: 0.7, rot: jibYaw, color: "#8e8e93" });

      // Barrier fence around the lot (alternating orange/white).
      const fx0 = b.x - b.w / 2 - 2;
      const fx1 = b.x + b.w / 2 + 2;
      const fz0 = b.z - b.d / 2 - 2;
      const fz1 = b.z + b.d / 2 + 2;
      let k = 0;
      for (let x = fx0; x < fx1; x += 2.1, k++) {
        boxes.push({ x: x + 1, y: SLAB_TOP, z: fz0, sx: 2, sy: 1, sz: 0.09, rot: 0, color: k % 2 ? "#e2601a" : "#e8e6e0" });
        boxes.push({ x: x + 1, y: SLAB_TOP, z: fz1, sx: 2, sy: 1, sz: 0.09, rot: 0, color: k % 2 ? "#e8e6e0" : "#e2601a" });
      }
      for (let z = fz0; z < fz1; z += 2.1, k++) {
        boxes.push({ x: fx0, y: SLAB_TOP, z: z + 1, sx: 0.09, sy: 1, sz: 2, rot: 0, color: k % 2 ? "#e2601a" : "#e8e6e0" });
        boxes.push({ x: fx1, y: SLAB_TOP, z: z + 1, sx: 0.09, sy: 1, sz: 2, rot: 0, color: k % 2 ? "#e8e6e0" : "#e2601a" });
      }
      // Cones by the gate corner.
      for (let c = 0; c < 6; c++) {
        cones.push({ x: fx0 + 1 + c * 1.1, z: fz0 - 1.2 });
      }
    }

    const boxGeo = new THREE.BoxGeometry(1, 1, 1);
    boxGeo.translate(0, 0.5, 0);
    const boxMat = new THREE.MeshStandardMaterial({ color: "#ffffff", roughness: 0.85 });
    const boxMesh = new THREE.InstancedMesh(boxGeo, boxMat, Math.max(1, boxes.length));
    {
      const m = new THREE.Matrix4();
      const q = new THREE.Quaternion();
      const up = new THREE.Vector3(0, 1, 0);
      const s = new THREE.Vector3();
      const p = new THREE.Vector3();
      const color = new THREE.Color();
      boxes.forEach((bx, i) => {
        q.setFromAxisAngle(up, bx.rot);
        s.set(bx.sx, bx.sy, bx.sz);
        p.set(bx.x, bx.y, bx.z);
        m.compose(p, q, s);
        boxMesh.setMatrixAt(i, m);
        boxMesh.setColorAt(i, color.set(bx.color));
      });
      boxMesh.count = boxes.length;
      boxMesh.instanceMatrix.needsUpdate = true;
      if (boxMesh.instanceColor) boxMesh.instanceColor.needsUpdate = true;
      boxMesh.castShadow = true;
      boxMesh.frustumCulled = false;
    }

    const colGeo = new THREE.CylinderGeometry(0.16, 0.16, 1, 8);
    colGeo.translate(0, 0.5, 0);
    const colMat = new THREE.MeshStandardMaterial({ color: "#afaca5", roughness: 0.9 });
    const colMesh = new THREE.InstancedMesh(colGeo, colMat, Math.max(1, columns.length));
    {
      const m = new THREE.Matrix4();
      columns.forEach((c, i) => {
        m.makeScale(1, c.h, 1);
        m.setPosition(c.x, c.y, c.z);
        colMesh.setMatrixAt(i, m);
      });
      colMesh.count = columns.length;
      colMesh.instanceMatrix.needsUpdate = true;
      colMesh.castShadow = true;
      colMesh.frustumCulled = false;
    }

    const coneGeo = new THREE.ConeGeometry(0.2, 0.55, 8);
    coneGeo.translate(0, 0.275, 0);
    const coneMat = new THREE.MeshStandardMaterial({ color: "#e2601a", roughness: 0.7 });
    const coneMesh = new THREE.InstancedMesh(coneGeo, coneMat, Math.max(1, cones.length));
    {
      const m = new THREE.Matrix4();
      cones.forEach((c, i) => {
        m.identity();
        m.setPosition(c.x, SLAB_TOP, c.z);
        coneMesh.setMatrixAt(i, m);
      });
      coneMesh.count = cones.length;
      coneMesh.instanceMatrix.needsUpdate = true;
      coneMesh.frustumCulled = false;
    }

    return { boxMesh, colMesh, coneMesh, boxMat, colMat, coneMat };
  }, [plan]);

  useEffect(() => {
    return () => {
      for (const mesh of [built.boxMesh, built.colMesh, built.coneMesh]) mesh.geometry.dispose();
      built.boxMat.dispose();
      built.colMat.dispose();
      built.coneMat.dispose();
    };
  }, [built]);

  return (
    <group>
      <primitive object={built.boxMesh} />
      <primitive object={built.colMesh} />
      <primitive object={built.coneMesh} />
    </group>
  );
}
