import { CityAsset } from "../assets/CityAsset";
import { LetterSign } from "../Signs";
import { GLASS } from "./facades";

/**
 * Procedural stand-in for the theater. Explicit fallback only — replaced by
 * /assets/city/preview-theater.glb once the manifest entry is installed.
 */
function PreviewTheaterFallback() {
  const w = 16;
  const d = 12;
  const h = 12;

  return (
    // north row east of the marketplace, entrance faces the road
    <group position={[52, 0, -14.5]}>
      <mesh position={[0, h / 2, 0]} castShadow receiveShadow>
        <boxGeometry args={[w, h, d]} />
        <meshStandardMaterial color="#26282d" roughness={0.7} />
      </mesh>
      {/* parapet cap */}
      <mesh position={[0, h + 0.15, 0]}>
        <boxGeometry args={[w + 0.4, 0.3, d + 0.4]} />
        <meshStandardMaterial color="#34373d" roughness={0.7} />
      </mesh>
      {/* horizontal reveal bands so side and rear walls read as architecture */}
      {[3.5, 7, 10.5].map((y) => (
        <mesh key={`band-${y}`} position={[0, y, 0]}>
          <boxGeometry args={[w + 0.12, 0.25, d + 0.12]} />
          <meshStandardMaterial color="#3a3e45" roughness={0.6} />
        </mesh>
      ))}
      {/* corner pilasters */}
      {[
        [-w / 2, -d / 2],
        [w / 2, -d / 2],
        [-w / 2, d / 2],
        [w / 2, d / 2],
      ].map(([cx, cz]) => (
        <mesh key={`pil-${cx}-${cz}`} position={[cx, h / 2, cz]}>
          <boxGeometry args={[0.5, h, 0.5]} />
          <meshStandardMaterial color="#1d1f24" roughness={0.6} />
        </mesh>
      ))}
      {/* rooftop unit */}
      <mesh position={[-3, h + 0.8, -2]} castShadow>
        <boxGeometry args={[4, 1.6, 3]} />
        <meshStandardMaterial color="#4a4e55" roughness={0.8} />
      </mesh>
      {/* vertical accent fins */}
      {[-6.5, 6.5].map((x) => (
        <mesh key={x} position={[x, h / 2, d / 2 + 0.1]}>
          <boxGeometry args={[0.5, h, 0.3]} />
          <meshStandardMaterial
            color="#8f7bd8"
            emissive="#6d55c4"
            emissiveIntensity={0.5}
            roughness={0.4}
          />
        </mesh>
      ))}
      {/* marquee canopy */}
      <mesh position={[0, 4.4, d / 2 + 1.3]} castShadow>
        <boxGeometry args={[11, 0.3, 2.6]} />
        <meshStandardMaterial color="#17181c" roughness={0.5} />
      </mesh>
      <mesh position={[0, 4.15, d / 2 + 2.55]}>
        <boxGeometry args={[11, 0.5, 0.12]} />
        <meshStandardMaterial
          color="#efe6ff"
          emissive="#cdbcf5"
          emissiveIntensity={0.8}
          roughness={0.3}
        />
      </mesh>
      <LetterSign text="PREVIEW THEATER" fontSize={0.95} color="#e9ddff" position={[0, 6.4, d / 2 + 0.2]} />
      {/* entrance glass */}
      <mesh position={[0, 1.8, d / 2 + 0.06]}>
        <boxGeometry args={[7, 3.4, 0.12]} />
        <meshStandardMaterial {...GLASS} />
      </mesh>
      {/* poster boxes */}
      {[-5, 5].map((x) => (
        <group key={x} position={[x, 2.2, d / 2 + 0.1]}>
          <mesh>
            <boxGeometry args={[1.7, 2.5, 0.14]} />
            <meshStandardMaterial color="#0f1013" roughness={0.5} />
          </mesh>
          <mesh position={[0, 0, 0.08]}>
            <planeGeometry args={[1.4, 2.2]} />
            <meshStandardMaterial
              color={x < 0 ? "#4f7fb8" : "#b86a4f"}
              emissive={x < 0 ? "#2c4a70" : "#703a2c"}
              emissiveIntensity={0.4}
            />
          </mesh>
        </group>
      ))}
    </group>
  );
}

export function PreviewTheater() {
  return <CityAsset assetId="preview-theater" fallback={<PreviewTheaterFallback />} />;
}
