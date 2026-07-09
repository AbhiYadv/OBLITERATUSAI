import { CityAsset } from "../assets/CityAsset";
import { LetterSign } from "../Signs";
import { GLASS } from "./facades";

/**
 * Procedural stand-in for the HQ tower. Explicit fallback only — replaced by
 * /assets/city/nexo-labs-hq.glb once the manifest entry is installed.
 */
function NexoLabsHqFallback() {
  const towerW = 15;
  const towerD = 13;
  const floors = 9;
  const floorHeight = 3.5;
  const towerH = floors * floorHeight;
  const podiumH = 4.5;

  return (
    <group position={[-26, 0, -16.5]}>
      {/* podium with recessed glass entrance */}
      <mesh position={[0, podiumH / 2, 0]} castShadow receiveShadow>
        <boxGeometry args={[18, podiumH, 16]} />
        <meshStandardMaterial color="#b9b4aa" roughness={0.8} />
      </mesh>
      <mesh position={[0, 1.9, 8.01]}>
        <boxGeometry args={[9, 3.4, 0.2]} />
        <meshStandardMaterial {...GLASS} />
      </mesh>
      {/* entrance canopy */}
      <mesh position={[0, 3.8, 9.1]} castShadow>
        <boxGeometry args={[10, 0.25, 2.4]} />
        <meshStandardMaterial color="#2c3138" roughness={0.5} />
      </mesh>
      <LetterSign text="NEXO LABS" fontSize={0.75} position={[0, 4.35, 9.15]} />

      {/* glass tower */}
      <group position={[0, podiumH, 0]}>
        <mesh position={[0, towerH / 2, 0]} castShadow>
          <boxGeometry args={[towerW, towerH, towerD]} />
          <meshStandardMaterial {...GLASS} />
        </mesh>
        {/* floor bands */}
        {Array.from({ length: floors + 1 }).map((_, i) => (
          <mesh key={i} position={[0, i * floorHeight, 0]}>
            <boxGeometry args={[towerW + 0.18, 0.32, towerD + 0.18]} />
            <meshStandardMaterial color="#ccd2d6" metalness={0.35} roughness={0.5} />
          </mesh>
        ))}
        {/* corner columns */}
        {[
          [-towerW / 2, -towerD / 2],
          [towerW / 2, -towerD / 2],
          [-towerW / 2, towerD / 2],
          [towerW / 2, towerD / 2],
        ].map(([cx, cz]) => (
          <mesh key={`${cx}-${cz}`} position={[cx, towerH / 2, cz]}>
            <boxGeometry args={[0.45, towerH, 0.45]} />
            <meshStandardMaterial color="#c4cad0" metalness={0.4} roughness={0.45} />
          </mesh>
        ))}
        {/* crown signage on two faces */}
        <LetterSign text="NEXO LABS" fontSize={1.35} position={[0, towerH - 2.2, towerD / 2 + 0.25]} />
        <LetterSign
          text="NEXO LABS"
          fontSize={1.35}
          position={[towerW / 2 + 0.25, towerH - 2.2, 0]}
          rotationY={Math.PI / 2}
        />
        {/* rooftop mechanical box + mast */}
        <mesh position={[2.5, towerH + 1, -2]} castShadow>
          <boxGeometry args={[6, 2, 5]} />
          <meshStandardMaterial color="#9aa0a6" roughness={0.8} />
        </mesh>
        <mesh position={[-4, towerH + 2.2, 3]}>
          <cylinderGeometry args={[0.08, 0.08, 4.4, 8]} />
          <meshStandardMaterial color="#c8ccd2" />
        </mesh>
      </group>
    </group>
  );
}

export function NexoLabsHQ() {
  return <CityAsset assetId="nexo-labs-hq" fallback={<NexoLabsHqFallback />} />;
}
