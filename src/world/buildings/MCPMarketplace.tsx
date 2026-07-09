import { CityAsset } from "../assets/CityAsset";
import { BandSign, LetterSign } from "../Signs";
import { NORTH_FRONT_Z } from "../layout";

const GLASS = {
  color: "#22333f",
  metalness: 0.5,
  roughness: 0.16,
  emissive: "#1d2a35",
  emissiveIntensity: 0.5,
};

interface ShopSpec {
  name: string;
  accent: string;
}

const SHOPS: ShopSpec[] = [
  { name: "GITHUB SHOP", accent: "#d3dae2" },
  { name: "DATABASE SHOP", accent: "#4cc3e8" },
  { name: "DOCKER SHOP", accent: "#57a8ff" },
  { name: "BROWSER RESEARCH", accent: "#f0c53d" },
];

const BAY_WIDTH = 7;
const BLOCK_X_START = 14;
const BLOCK_WIDTH = BAY_WIDTH * SHOPS.length;
const BLOCK_DEPTH = 14;
const BLOCK_HEIGHT = 9;
const BLOCK_CENTER_X = BLOCK_X_START + BLOCK_WIDTH / 2;
const BLOCK_CENTER_Z = NORTH_FRONT_Z - BLOCK_DEPTH / 2;
const FRONT_Z = NORTH_FRONT_Z;

/** One ground-floor storefront bay: plinth, glass, mullions, door, sign, canopy. */
function StorefrontBay({ spec, x }: { spec: ShopSpec; x: number }) {
  return (
    <group position={[x, 0, FRONT_Z]}>
      {/* plinth */}
      <mesh position={[0, 0.25, 0.1]}>
        <boxGeometry args={[6.6, 0.5, 0.2]} />
        <meshStandardMaterial color="#3a3d42" roughness={0.7} />
      </mesh>
      {/* glazing */}
      <mesh position={[0, 1.95, 0.08]}>
        <boxGeometry args={[6.4, 2.9, 0.1]} />
        <meshStandardMaterial {...GLASS} />
      </mesh>
      {/* mullions */}
      {[-3.2, -1.1, 1.1, 3.2].map((mx) => (
        <mesh key={mx} position={[mx, 1.95, 0.14]}>
          <boxGeometry args={[0.1, 2.9, 0.14]} />
          <meshStandardMaterial color="#23262b" roughness={0.5} />
        </mesh>
      ))}
      {/* door leaf */}
      <mesh position={[0, 1.5, 0.15]}>
        <boxGeometry args={[1.5, 2.6, 0.08]} />
        <meshStandardMaterial color="#141d26" metalness={0.8} roughness={0.25} />
      </mesh>
      <mesh position={[0.55, 1.4, 0.22]}>
        <boxGeometry args={[0.06, 0.7, 0.06]} />
        <meshStandardMaterial color="#c8ccd2" metalness={0.7} roughness={0.3} />
      </mesh>
      {/* sign band */}
      <BandSign
        text={spec.name}
        width={6.8}
        color={spec.accent}
        fontSize={0.52}
        position={[0, 4, 0.12]}
      />
      {/* canopy */}
      <mesh position={[0, 3.35, 0.75]} castShadow>
        <boxGeometry args={[6.8, 0.12, 1.5]} />
        <meshStandardMaterial color="#2b2e33" roughness={0.6} />
      </mesh>
    </group>
  );
}

/**
 * Procedural stand-in for the marketplace block. Explicit fallback only —
 * replaced by /assets/city/mcp-marketplace-row.glb once installed (the sign
 * bands stay procedural so shop names remain data-driven).
 */
function MarketplaceFallback() {
  return (
    <group>
      {/* building shell */}
      <mesh position={[BLOCK_CENTER_X, BLOCK_HEIGHT / 2, BLOCK_CENTER_Z]} castShadow receiveShadow>
        <boxGeometry args={[BLOCK_WIDTH, BLOCK_HEIGHT, BLOCK_DEPTH]} />
        <meshStandardMaterial color="#85817a" roughness={0.85} />
      </mesh>
      {/* parapet */}
      <mesh position={[BLOCK_CENTER_X, BLOCK_HEIGHT + 0.15, BLOCK_CENTER_Z]}>
        <boxGeometry args={[BLOCK_WIDTH + 0.3, 0.3, BLOCK_DEPTH + 0.3]} />
        <meshStandardMaterial color="#65625c" roughness={0.85} />
      </mesh>
      {/* continuous upper-floor ribbon window */}
      <mesh position={[BLOCK_CENTER_X, 6.9, FRONT_Z + 0.05]}>
        <boxGeometry args={[BLOCK_WIDTH - 1.4, 1.7, 0.1]} />
        <meshStandardMaterial {...GLASS} />
      </mesh>
      {Array.from({ length: SHOPS.length * 4 + 1 }).map((_, i) => (
        <mesh
          key={i}
          position={[BLOCK_X_START + 0.7 + i * ((BLOCK_WIDTH - 1.4) / (SHOPS.length * 4)), 6.9, FRONT_Z + 0.11]}
        >
          <boxGeometry args={[0.08, 1.7, 0.1]} />
          <meshStandardMaterial color="#23262b" roughness={0.5} />
        </mesh>
      ))}
      {/* storefront bays */}
      {SHOPS.map((spec, i) => (
        <StorefrontBay key={spec.name} spec={spec} x={BLOCK_X_START + BAY_WIDTH * (i + 0.5)} />
      ))}
      {/* rooftop block signage */}
      <mesh position={[BLOCK_CENTER_X, BLOCK_HEIGHT + 1, FRONT_Z - 0.6]} castShadow>
        <boxGeometry args={[11, 1.3, 0.35]} />
        <meshStandardMaterial color="#17191d" roughness={0.6} />
      </mesh>
      <LetterSign
        text="MCP MARKETPLACE"
        fontSize={0.75}
        color="#cfe3f5"
        position={[BLOCK_CENTER_X, BLOCK_HEIGHT + 1, FRONT_Z - 0.4]}
      />
    </group>
  );
}

export function MCPMarketplace() {
  return <CityAsset assetId="mcp-marketplace" fallback={<MarketplaceFallback />} />;
}
