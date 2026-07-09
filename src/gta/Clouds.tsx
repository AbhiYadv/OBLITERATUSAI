/**
 * Lightweight sky detail: seeded billboard cloud sprites on one instanced
 * mesh (single draw call), drifting slowly. Deliberately not volumetric —
 * the LAAS cloud stack is excluded by design.
 */
import { useEffect, useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { mulberry32 } from "../village/seed";

const COUNT = 46;
const WRAP = 1600;

function makeCloudTexture(): THREE.CanvasTexture {
  const size = 128;
  const canvas = document.createElement("canvas");
  canvas.width = size;
  canvas.height = size;
  const ctx = canvas.getContext("2d")!;
  const blob = (x: number, y: number, r: number, a: number) => {
    const g = ctx.createRadialGradient(x, y, 0, x, y, r);
    g.addColorStop(0, `rgba(255,255,255,${a})`);
    g.addColorStop(0.65, `rgba(250,250,252,${a * 0.5})`);
    g.addColorStop(1, "rgba(255,255,255,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, size, size);
  };
  blob(64, 70, 52, 0.85);
  blob(42, 62, 34, 0.7);
  blob(88, 60, 30, 0.75);
  blob(64, 52, 26, 0.6);
  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  return tex;
}

interface CloudState {
  x: number;
  y: number;
  z: number;
  w: number;
  h: number;
}

export function CloudLayer({ seed }: { seed: number }) {
  const built = useMemo(() => {
    const rand = mulberry32(seed ^ 0xc10bd5);
    const clouds: CloudState[] = [];
    for (let i = 0; i < COUNT; i++) {
      const w = 110 + rand() * 190;
      clouds.push({
        x: (rand() * 2 - 1) * WRAP,
        y: 300 + rand() * 190,
        z: (rand() * 2 - 1) * WRAP,
        w,
        h: w * (0.32 + rand() * 0.16),
      });
    }
    const texture = makeCloudTexture();
    const material = new THREE.MeshBasicMaterial({
      map: texture,
      transparent: true,
      opacity: 0.82,
      depthWrite: false,
      fog: false,
    });
    const mesh = new THREE.InstancedMesh(new THREE.PlaneGeometry(1, 1), material, COUNT);
    mesh.frustumCulled = false;
    mesh.renderOrder = -1; // behind scene transparents like water
    return { clouds, mesh, texture, material };
  }, [seed]);

  const tmp = useRef({ m: new THREE.Matrix4(), p: new THREE.Vector3(), s: new THREE.Vector3() });

  useFrame(({ camera }, delta) => {
    const t = tmp.current;
    const dt = Math.min(delta, 0.1);
    for (let i = 0; i < built.clouds.length; i++) {
      const c = built.clouds[i];
      c.x += 1.6 * dt; // slow easterly drift
      if (c.x > WRAP) c.x = -WRAP;
      t.p.set(c.x, c.y, c.z);
      t.s.set(c.w, c.h, 1);
      t.m.compose(t.p, camera.quaternion, t.s);
      built.mesh.setMatrixAt(i, t.m);
    }
    built.mesh.instanceMatrix.needsUpdate = true;
  });

  useEffect(() => {
    return () => {
      built.mesh.geometry.dispose();
      built.material.dispose();
      built.texture.dispose();
    };
  }, [built]);

  return <primitive object={built.mesh} />;
}
