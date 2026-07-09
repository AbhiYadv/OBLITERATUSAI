/**
 * Street-level city furniture: one asphalt base plane, merged raised block
 * slabs (sidewalks), instanced lane dashes, crosswalk stripes, and
 * streetlights. Roughly 6 draw calls for the whole street network.
 */
import { useEffect, useMemo } from "react";
import * as THREE from "three";
import { CITY } from "../village/terrain";
import { ROAD_Y, SLAB_H, type CityPlan, type Decal } from "./cityPlan";
import { makeObject, registerObjects, worldCollisionRegistry } from "./collision/CollisionRegistry";

function fillDecals(mesh: THREE.InstancedMesh, decals: Decal[], y: number): void {
  const m = new THREE.Matrix4();
  const q = new THREE.Quaternion();
  const up = new THREE.Vector3(0, 1, 0);
  const one = new THREE.Vector3(1, 1, 1);
  const p = new THREE.Vector3();
  decals.forEach((d, i) => {
    q.setFromAxisAngle(up, d.rot);
    p.set(d.x, y, d.z);
    m.compose(p, q, one);
    mesh.setMatrixAt(i, m);
  });
  mesh.count = decals.length;
  mesh.instanceMatrix.needsUpdate = true;
}

export function CityRoads({ plan }: { plan: CityPlan }) {
  const built = useMemo(() => {
    // Merged sidewalk slabs, one box per block.
    const slabGeos: THREE.BufferGeometry[] = [];
    for (const b of plan.blocks) {
      const g = new THREE.BoxGeometry(b.maxX - b.minX, SLAB_H, b.maxZ - b.minZ);
      g.translate((b.minX + b.maxX) / 2, CITY.y + SLAB_H / 2, (b.minZ + b.maxZ) / 2);
      slabGeos.push(g);
    }
    const merged = new THREE.BufferGeometry();
    {
      const pos: number[] = [];
      const nor: number[] = [];
      const idx: number[] = [];
      for (const g of slabGeos) {
        const base = pos.length / 3;
        const p = g.getAttribute("position");
        const n = g.getAttribute("normal");
        for (let i = 0; i < p.count; i++) {
          pos.push(p.getX(i), p.getY(i), p.getZ(i));
          nor.push(n.getX(i), n.getY(i), n.getZ(i));
        }
        const gi = g.getIndex()!;
        for (let i = 0; i < gi.count; i++) idx.push(base + gi.getX(i));
        g.dispose();
      }
      merged.setAttribute("position", new THREE.Float32BufferAttribute(pos, 3));
      merged.setAttribute("normal", new THREE.Float32BufferAttribute(nor, 3));
      merged.setIndex(idx);
      merged.computeBoundingSphere();
    }
    const slabMat = new THREE.MeshStandardMaterial({ color: "#96938c", roughness: 0.95 });

    // Instanced paint decals.
    const paintMat = new THREE.MeshStandardMaterial({ color: "#d9d9cf", roughness: 0.8 });
    const dashGeo = new THREE.BoxGeometry(2.4, 0.02, 0.25);
    const dashes = new THREE.InstancedMesh(dashGeo, paintMat, Math.max(1, plan.dashes.length));
    fillDecals(dashes, plan.dashes, ROAD_Y + 0.015);
    const stripeGeo = new THREE.BoxGeometry(0.55, 0.02, 2.7);
    const stripes = new THREE.InstancedMesh(stripeGeo, paintMat, Math.max(1, plan.crosswalkStripes.length));
    fillDecals(stripes, plan.crosswalkStripes, ROAD_Y + 0.015);

    // Streetlights: pole + arm/head.
    const poleGeo = new THREE.CylinderGeometry(0.12, 0.17, 6.2, 8);
    poleGeo.translate(0, 3.1, 0);
    const poleMat = new THREE.MeshStandardMaterial({ color: "#2c2f33", roughness: 0.6, metalness: 0.4 });
    const poles = new THREE.InstancedMesh(poleGeo, poleMat, Math.max(1, plan.lights.length));
    const headGeo = new THREE.BoxGeometry(0.28, 0.14, 1.7);
    headGeo.translate(0, 6.15, -0.95); // arm reaches over the road
    const headMat = new THREE.MeshStandardMaterial({
      color: "#3a3d41",
      emissive: "#ffe9b0",
      emissiveIntensity: 0.55,
      roughness: 0.6,
    });
    const heads = new THREE.InstancedMesh(headGeo, headMat, Math.max(1, plan.lights.length));
    {
      const m = new THREE.Matrix4();
      const q = new THREE.Quaternion();
      const up = new THREE.Vector3(0, 1, 0);
      const one = new THREE.Vector3(1, 1, 1);
      const p = new THREE.Vector3();
      plan.lights.forEach((l, i) => {
        q.setFromAxisAngle(up, l.rot);
        p.set(l.x, CITY.y + SLAB_H, l.z);
        m.compose(p, q, one);
        poles.setMatrixAt(i, m);
        heads.setMatrixAt(i, m);
      });
      poles.count = heads.count = plan.lights.length;
      poles.instanceMatrix.needsUpdate = true;
      heads.instanceMatrix.needsUpdate = true;
      poles.castShadow = true;
      heads.castShadow = true;
    }

    return { merged, slabMat, dashes, stripes, poles, heads, paintMat };
  }, [plan]);

  useEffect(() => {
    const unregisterLights = registerObjects(
      worldCollisionRegistry,
      plan.lights.map((light, i) =>
        makeObject(
          `streetlight:${i}`,
          "streetlight",
          { x: light.x, y: CITY.y + SLAB_H + 3.1, z: light.z },
          { x: 0.22, y: 3.1, z: 0.22 },
          light.rot,
        ),
      ),
    );
    return () => {
      unregisterLights();
      built.merged.dispose();
      built.slabMat.dispose();
      built.paintMat.dispose();
      for (const inst of [built.dashes, built.stripes, built.poles, built.heads]) {
        inst.geometry.dispose();
      }
      (built.poles.material as THREE.Material).dispose();
      (built.heads.material as THREE.Material).dispose();
    };
  }, [built, plan]);

  return (
    <group>
      {/* Asphalt base across the whole district. */}
      <mesh
        position={[(CITY.minX + CITY.maxX) / 2, ROAD_Y, (CITY.minZ + CITY.maxZ) / 2]}
        rotation-x={-Math.PI / 2}
        receiveShadow
      >
        <planeGeometry args={[CITY.maxX - CITY.minX + 22, CITY.maxZ - CITY.minZ + 22, 1, 1]} />
        <meshStandardMaterial color="#3b3c41" roughness={0.98} />
      </mesh>
      <mesh geometry={built.merged} material={built.slabMat} castShadow receiveShadow />
      <primitive object={built.dashes} />
      <primitive object={built.stripes} />
      <primitive object={built.poles} />
      <primitive object={built.heads} />
    </group>
  );
}
