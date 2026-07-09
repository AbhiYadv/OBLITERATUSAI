import { CityAsset } from "../assets/CityAsset";
import { LetterSign } from "../Signs";
import { GLASS, PunchedFacades } from "./facades";

/**
 * Procedural stand-in for the bureau. Explicit fallback only — replaced by
 * /assets/city/worker-bureau.glb once the manifest entry is installed.
 */
function WorkerBureauFallback() {
  const w = 10;
  const d = 12;
  const floors = 5;
  const floorHeight = 3.4;
  const h = floors * floorHeight + 1;

  return (
    <group position={[-6, 0, -14.5]}>
      <mesh position={[0, h / 2, 0]} castShadow receiveShadow>
        <boxGeometry args={[w, h, d]} />
        <meshStandardMaterial color="#cfc9be" roughness={0.85} />
      </mesh>
      <PunchedFacades w={w} d={d} floors={floors} colsX={3} colsZ={4} floorHeight={floorHeight} />
      {/* parapet */}
      <mesh position={[0, h + 0.15, 0]}>
        <boxGeometry args={[w + 0.3, 0.3, d + 0.3]} />
        <meshStandardMaterial color="#b0aca2" roughness={0.8} />
      </mesh>
      {/* entrance portal facing the street */}
      <mesh position={[0, 1.7, d / 2 + 0.05]}>
        <boxGeometry args={[3.6, 3, 0.25]} />
        <meshStandardMaterial color="#2c3138" roughness={0.5} />
      </mesh>
      <mesh position={[0, 1.5, d / 2 + 0.2]}>
        <boxGeometry args={[2.2, 2.6, 0.1]} />
        <meshStandardMaterial {...GLASS} />
      </mesh>
      <LetterSign text="WORKER BUREAU" fontSize={0.55} position={[0, 3.7, d / 2 + 0.22]} />
    </group>
  );
}

export function WorkerBureau() {
  return <CityAsset assetId="worker-bureau" fallback={<WorkerBureauFallback />} />;
}
