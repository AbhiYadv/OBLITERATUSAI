/**
 * Signalized intersections: four corner poles per crossing, each with a
 * three-lamp head. Everything is instanced (pole, housing, and one
 * instanced mesh per lamp color), so 180 signals cost five draw calls.
 * Lamp colors follow the shared city-wide cycle in signals.ts; instance
 * colors are rewritten only when the phase changes.
 */
import { useEffect, useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { ROAD_W, SLAB_TOP, STREET_XS, STREET_ZS } from "./cityPlan";
import { signalAt } from "./signals";
import { makeObject, registerObjects, worldCollisionRegistry } from "./collision/CollisionRegistry";

const POLE_H = 4.9;
const HEAD_Y = 4.3; // housing center height
const LIT = {
  red: new THREE.Color("#ff2a1a"),
  yellow: new THREE.Color("#ffb400"),
  green: new THREE.Color("#22d24a"),
};
const DARK = {
  red: new THREE.Color("#3a1210"),
  yellow: new THREE.Color("#392c08"),
  green: new THREE.Color("#0c2e14"),
};

interface Signal {
  x: number;
  z: number;
  rot: number;
  nsAxis: boolean; // controls north-south traffic
}

function buildSignals(): Signal[] {
  const out: Signal[] = [];
  const off = ROAD_W / 2 + 0.9;
  for (const x of STREET_XS) {
    for (const z of STREET_ZS) {
      // Two corners face the NS approaches, two face the EW approaches.
      out.push({ x: x + off, z: z + off, rot: 0, nsAxis: true });
      out.push({ x: x - off, z: z - off, rot: Math.PI, nsAxis: true });
      out.push({ x: x + off, z: z - off, rot: Math.PI / 2, nsAxis: false });
      out.push({ x: x - off, z: z + off, rot: -Math.PI / 2, nsAxis: false });
    }
  }
  return out;
}

export function TrafficLights() {
  const built = useMemo(() => {
    const signals = buildSignals();
    const n = signals.length;

    const poleGeo = new THREE.CylinderGeometry(0.07, 0.1, POLE_H, 8);
    poleGeo.translate(0, POLE_H / 2, 0);
    const metal = new THREE.MeshStandardMaterial({ color: "#2a2d31", roughness: 0.55, metalness: 0.5 });
    const poles = new THREE.InstancedMesh(poleGeo, metal, n);

    const headGeo = new THREE.BoxGeometry(0.34, 1.0, 0.24);
    headGeo.translate(0, HEAD_Y, 0.08);
    const headMat = new THREE.MeshStandardMaterial({ color: "#1c1e20", roughness: 0.7 });
    const heads = new THREE.InstancedMesh(headGeo, headMat, n);

    const lampGeo = new THREE.CircleGeometry(0.095, 12);
    const lampMeshes = [0, 1, 2].map((k) => {
      const geo = lampGeo.clone();
      geo.translate(0, HEAD_Y + 0.3 - k * 0.3, 0.205);
      const mat = new THREE.MeshBasicMaterial({ color: "#ffffff" });
      return new THREE.InstancedMesh(geo, mat, n);
    });
    lampGeo.dispose();

    const m = new THREE.Matrix4();
    const q = new THREE.Quaternion();
    const up = new THREE.Vector3(0, 1, 0);
    const one = new THREE.Vector3(1, 1, 1);
    const p = new THREE.Vector3();
    signals.forEach((s, i) => {
      q.setFromAxisAngle(up, s.rot);
      p.set(s.x, SLAB_TOP, s.z);
      m.compose(p, q, one);
      poles.setMatrixAt(i, m);
      heads.setMatrixAt(i, m);
      for (const lamp of lampMeshes) lamp.setMatrixAt(i, m);
    });
    poles.castShadow = true;
    for (const mesh of [poles, heads, ...lampMeshes]) {
      mesh.instanceMatrix.needsUpdate = true;
      mesh.frustumCulled = false;
    }
    return { signals, poles, heads, lampMeshes, metal, headMat };
  }, []);

  const lastPhase = useRef(-1);

  useFrame(({ clock }) => {
    const sig = signalAt(clock.elapsedTime);
    if (sig.phase === lastPhase.current) return;
    lastPhase.current = sig.phase;
    const [red, yellow, green] = built.lampMeshes;
    built.signals.forEach((s, i) => {
      const go = s.nsAxis ? sig.nsGo : sig.ewGo;
      const caution = s.nsAxis ? sig.nsYellow : sig.ewYellow;
      red.setColorAt(i, !go && !caution ? LIT.red : DARK.red);
      yellow.setColorAt(i, caution ? LIT.yellow : DARK.yellow);
      green.setColorAt(i, go ? LIT.green : DARK.green);
    });
    for (const lamp of built.lampMeshes) {
      if (lamp.instanceColor) lamp.instanceColor.needsUpdate = true;
    }
  });

  useEffect(() => {
    const unregisterSignals = registerObjects(
      worldCollisionRegistry,
      built.signals.map((signal, i) =>
        makeObject(
          `traffic-signal:${i}`,
          "traffic-signal",
          { x: signal.x, y: SLAB_TOP + POLE_H / 2, z: signal.z },
          { x: 0.24, y: POLE_H / 2, z: 0.24 },
          signal.rot,
        ),
      ),
    );
    return () => {
      unregisterSignals();
      built.poles.geometry.dispose();
      built.heads.geometry.dispose();
      built.metal.dispose();
      built.headMat.dispose();
      for (const lamp of built.lampMeshes) {
        lamp.geometry.dispose();
        (lamp.material as THREE.Material).dispose();
      }
    };
  }, [built]);

  return (
    <group>
      <primitive object={built.poles} />
      <primitive object={built.heads} />
      {built.lampMeshes.map((lamp, i) => (
        <primitive key={i} object={lamp} />
      ))}
    </group>
  );
}
