/**
 * Single world-spanning water plane. The river channel and pond are carved
 * below WATER_LEVEL in the heightfield, so one translucent plane renders
 * every water feature in a single draw call — no SSR, no clipmaps.
 */
import { WATER_LEVEL, WORLD_SIZE } from "./terrain";

export function Water() {
  return (
    <mesh position={[0, WATER_LEVEL, 0]} rotation-x={-Math.PI / 2}>
      <planeGeometry args={[WORLD_SIZE, WORLD_SIZE, 1, 1]} />
      <meshStandardMaterial
        color="#3d7ea6"
        transparent
        opacity={0.78}
        roughness={0.18}
        metalness={0.05}
      />
    </mesh>
  );
}
