/**
 * Terrain-conforming dirt path ribbons, the central plaza disc, and a small
 * stone well as the village-center landmark. Path color is also blended
 * into the terrain vertex colors; these ribbons sit a few centimetres above
 * ground to read clearly at distance.
 */
import { useEffect, useMemo } from "react";
import * as THREE from "three";
import { PATHS, VILLAGE_HEIGHT, cityMaskAt, heightAt, type Vec2 } from "./terrain";

const PATH_WIDTH = 2.8;
const SAMPLE_STEP = 3.5;
const LIFT = 0.07;

function buildRibbon(points: Vec2[], positions: number[], indices: number[]): void {
  // Resample the polyline at a fixed step.
  const samples: Array<[number, number]> = [];
  for (let i = 0; i < points.length - 1; i++) {
    const [ax, az] = points[i];
    const [bx, bz] = points[i + 1];
    const len = Math.hypot(bx - ax, bz - az);
    const steps = Math.max(1, Math.ceil(len / SAMPLE_STEP));
    for (let k = 0; k < steps; k++) {
      const t = k / steps;
      samples.push([ax + (bx - ax) * t, az + (bz - az) * t]);
    }
  }
  samples.push([points[points.length - 1][0], points[points.length - 1][1]]);
  if (samples.length < 2) return;

  const base = positions.length / 3;
  const inCity: boolean[] = [];
  for (let i = 0; i < samples.length; i++) {
    const [x, z] = samples[i];
    const [px, pz] = samples[Math.max(0, i - 1)];
    const [nx, nz] = samples[Math.min(samples.length - 1, i + 1)];
    let dx = nx - px;
    let dz = nz - pz;
    const dl = Math.hypot(dx, dz) || 1;
    dx /= dl;
    dz /= dl;
    // Perpendicular in the ground plane.
    const ox = -dz * (PATH_WIDTH / 2);
    const oz = dx * (PATH_WIDTH / 2);
    const lx = x + ox;
    const lz = z + oz;
    const rx = x - ox;
    const rz = z - oz;
    positions.push(lx, heightAt(lx, lz) + LIFT, lz, rx, heightAt(rx, rz) + LIFT, rz);
    inCity.push(cityMaskAt(x, z) > 0.35);
  }
  for (let i = 0; i < samples.length - 1; i++) {
    // Dirt paths end at the city edge; streets take over inside.
    if (inCity[i] || inCity[i + 1]) continue;
    const a = base + i * 2;
    indices.push(a, a + 2, a + 1, a + 1, a + 2, a + 3);
  }
}

export function Paths() {
  const geometry = useMemo(() => {
    const positions: number[] = [];
    const indices: number[] = [];
    for (const path of PATHS) buildRibbon(path, positions, indices);
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.Float32BufferAttribute(positions, 3));
    geo.setIndex(indices);
    geo.computeVertexNormals();
    geo.computeBoundingSphere();
    return geo;
  }, []);

  useEffect(() => () => geometry.dispose(), [geometry]);

  return (
    <group>
      <mesh geometry={geometry} receiveShadow>
        <meshStandardMaterial color="#9c7f57" roughness={1} />
      </mesh>
      {/* Central plaza */}
      <mesh
        position={[0, VILLAGE_HEIGHT + 0.05, 0]}
        rotation-x={-Math.PI / 2}
        receiveShadow
      >
        <circleGeometry args={[15, 36]} />
        <meshStandardMaterial color="#a89372" roughness={1} />
      </mesh>
      <VillageWell />
    </group>
  );
}

/** Simple stone well at the plaza center — an orientation landmark only. */
function VillageWell() {
  const y = VILLAGE_HEIGHT;
  return (
    <group position={[0, y, 0]}>
      <mesh position={[0, 0.5, 0]} castShadow receiveShadow>
        <cylinderGeometry args={[1.15, 1.25, 1, 12]} />
        <meshStandardMaterial color="#8b8b90" roughness={0.9} />
      </mesh>
      {[-0.95, 0.95].map((x) => (
        <mesh key={x} position={[x, 1.7, 0]} castShadow>
          <boxGeometry args={[0.14, 1.6, 0.14]} />
          <meshStandardMaterial color="#6b4a30" roughness={0.9} />
        </mesh>
      ))}
      <mesh position={[0, 2.75, 0]} rotation-y={Math.PI / 4} castShadow>
        <coneGeometry args={[1.7, 0.9, 4]} />
        <meshStandardMaterial color="#7a4f33" roughness={0.9} />
      </mesh>
    </group>
  );
}
