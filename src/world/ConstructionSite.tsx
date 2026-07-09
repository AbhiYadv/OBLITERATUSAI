import { Text } from "@react-three/drei";

/**
 * Authentication Center construction site.
 * Second row behind the marketplace/theater, so the crane and the partially
 * built frame rise above the street-front rooflines (reference composition).
 */
const SITE_X_START = 30;
const SITE_X_END = 48;
const SITE_FRONT_Z = -26;
const SITE_DEPTH = 16;
const SITE_CENTER_X = (SITE_X_START + SITE_X_END) / 2;
const SITE_CENTER_Z = SITE_FRONT_Z - SITE_DEPTH / 2;

const STEEL_YELLOW = "#e8a13a";
const SCAFFOLD = "#c77b30";
const CONCRETE = "#b5b0a6";

/** Partially built concrete frame: slab grid, columns, core, rebar. */
function PartialStructure() {
  const columns: Array<[number, number]> = [];
  for (let x = -4; x <= 4; x += 4) {
    for (let z = -3.5; z <= 3.5; z += 3.5) {
      columns.push([x, z]);
    }
  }
  return (
    <group position={[36, 0, -34]}>
      {/* foundation slab */}
      <mesh position={[0, 0.25, 0]} receiveShadow>
        <boxGeometry args={[12, 0.5, 10]} />
        <meshStandardMaterial color={CONCRETE} roughness={0.95} />
      </mesh>
      {/* columns, three lifts */}
      {columns.map(([x, z]) => (
        <mesh key={`${x}-${z}`} position={[x, 5.9, z]} castShadow>
          <boxGeometry args={[0.5, 11.2, 0.5]} />
          <meshStandardMaterial color="#c2bdb3" roughness={0.9} />
        </mesh>
      ))}
      {/* floor slabs */}
      {[4, 7.8, 11.6].map((y) => (
        <mesh key={y} position={[0, y, 0]} castShadow receiveShadow>
          <boxGeometry args={[12, 0.35, 10]} />
          <meshStandardMaterial color={CONCRETE} roughness={0.95} />
        </mesh>
      ))}
      {/* service core rising past the top slab */}
      <mesh position={[3.5, 6.5, -2.5]} castShadow>
        <boxGeometry args={[3, 13, 3]} />
        <meshStandardMaterial color="#a8a49a" roughness={0.9} />
      </mesh>
      {/* rebar sticking out of the top slab */}
      {[-4.5, -3.9, -3.3, -2.7].map((x) => (
        <mesh key={x} position={[x, 12.7, 4.2]}>
          <cylinderGeometry args={[0.03, 0.03, 1.8, 6]} />
          <meshStandardMaterial color="#7a4a2a" roughness={0.8} />
        </mesh>
      ))}
    </group>
  );
}

/** Scaffolding wall on the street side of the structure. */
function Scaffolding() {
  const xs = [-6, -4, -2, 0, 2, 4, 6];
  return (
    <group position={[36, 0, -28.4]}>
      {xs.map((x) => (
        <mesh key={x} position={[x, 6.2, 0]} castShadow>
          <cylinderGeometry args={[0.06, 0.06, 12.4, 8]} />
          <meshStandardMaterial color={SCAFFOLD} roughness={0.6} />
        </mesh>
      ))}
      {[1.6, 3.5, 5.4, 7.3, 9.2, 11.1].map((y) => (
        <mesh key={y} position={[0, y, 0]}>
          <boxGeometry args={[12.6, 0.09, 0.09]} />
          <meshStandardMaterial color={SCAFFOLD} roughness={0.6} />
        </mesh>
      ))}
      {/* work planks */}
      {[4.2, 8].map((y) => (
        <mesh key={`p-${y}`} position={[0, y, 0.35]} castShadow>
          <boxGeometry args={[12.4, 0.08, 0.7]} />
          <meshStandardMaterial color="#9c8258" roughness={0.9} />
        </mesh>
      ))}
      {/* orange debris netting */}
      <mesh position={[0, 6, 0.75]}>
        <planeGeometry args={[12.4, 11]} />
        <meshStandardMaterial color="#e8641b" transparent opacity={0.32} side={2} />
      </mesh>
    </group>
  );
}

/** Tower crane rising above the street-front rooflines. */
function TowerCrane() {
  const mastH = 19;
  return (
    <group position={[44, 0, -32]}>
      <mesh position={[0, 0.4, 0]} castShadow>
        <boxGeometry args={[3.2, 0.8, 3.2]} />
        <meshStandardMaterial color={CONCRETE} roughness={0.9} />
      </mesh>
      {/* mast segments, alternating twist to suggest lattice */}
      {Array.from({ length: 9 }).map((_, i) => (
        <mesh key={i} position={[0, 1.9 + i * 2, 0]} rotation-y={i % 2 === 0 ? 0 : 0.12} castShadow>
          <boxGeometry args={[1, 2, 1]} />
          <meshStandardMaterial color={STEEL_YELLOW} roughness={0.55} />
        </mesh>
      ))}
      {/* project banner on the mast, facing the street */}
      <mesh position={[0, 10, 0.62]}>
        <planeGeometry args={[5, 0.9]} />
        <meshStandardMaterial color="#f2efe8" roughness={0.7} />
      </mesh>
      <Text position={[0, 10, 0.65]} fontSize={0.42} anchorX="center" letterSpacing={0.03}>
        AUTH CENTER — 45%
        <meshBasicMaterial toneMapped={false} color="#c2571b" />
      </Text>
      {/* slewing unit and cab */}
      <mesh position={[0, mastH + 0.5, 0]} castShadow>
        <boxGeometry args={[1.4, 1, 1.4]} />
        <meshStandardMaterial color={STEEL_YELLOW} roughness={0.55} />
      </mesh>
      <mesh position={[0.9, mastH + 0.6, 0]} castShadow>
        <boxGeometry args={[0.9, 1.1, 1]} />
        <meshStandardMaterial color="#2f3338" roughness={0.5} />
      </mesh>
      {/* jib toward the structure, counter-jib behind */}
      <mesh position={[-6.5, mastH + 1.2, 0]} castShadow>
        <boxGeometry args={[13.5, 0.55, 0.7]} />
        <meshStandardMaterial color={STEEL_YELLOW} roughness={0.55} />
      </mesh>
      <mesh position={[3, mastH + 1.2, 0]} castShadow>
        <boxGeometry args={[4.5, 0.55, 0.9]} />
        <meshStandardMaterial color={STEEL_YELLOW} roughness={0.55} />
      </mesh>
      <mesh position={[4.6, mastH + 0.5, 0]} castShadow>
        <boxGeometry args={[1.2, 1.4, 1.6]} />
        <meshStandardMaterial color="#8f8b83" roughness={0.85} />
      </mesh>
      {/* tower head and tie cables */}
      <mesh position={[0, mastH + 2.6, 0]} castShadow>
        <coneGeometry args={[0.55, 2, 4]} />
        <meshStandardMaterial color={STEEL_YELLOW} roughness={0.55} />
      </mesh>
      <mesh position={[-5.5, mastH + 2.1, 0]} rotation-z={-0.28}>
        <cylinderGeometry args={[0.02, 0.02, 11.5, 4]} />
        <meshStandardMaterial color="#3a3d42" />
      </mesh>
      <mesh position={[2.4, mastH + 2.1, 0]} rotation-z={0.75}>
        <cylinderGeometry args={[0.02, 0.02, 4.6, 4]} />
        <meshStandardMaterial color="#3a3d42" />
      </mesh>
      {/* hook line with a hanging beam */}
      <mesh position={[-9.5, mastH - 2.8, 0]}>
        <cylinderGeometry args={[0.02, 0.02, 7.6, 4]} />
        <meshStandardMaterial color="#3a3d42" />
      </mesh>
      <mesh position={[-9.5, mastH - 6.8, 0]} castShadow>
        <boxGeometry args={[2.6, 0.3, 0.3]} />
        <meshStandardMaterial color="#8f8b83" roughness={0.7} />
      </mesh>
    </group>
  );
}

/** Perimeter hoarding panels with posts; gap at the gate. */
function Fencing() {
  const panels: Array<{ x: number; z: number; rot: number }> = [];
  // front line along the site edge, leaving a gate near the west end
  for (let x = SITE_X_START + 5; x <= SITE_X_END - 1; x += 2) {
    panels.push({ x, z: SITE_FRONT_Z - 0.1, rot: 0 });
  }
  // side returns
  for (let z = SITE_FRONT_Z - 2; z >= SITE_FRONT_Z - SITE_DEPTH; z -= 2) {
    panels.push({ x: SITE_X_START, z, rot: Math.PI / 2 });
    panels.push({ x: SITE_X_END, z, rot: Math.PI / 2 });
  }
  return (
    <group>
      {panels.map(({ x, z, rot }, i) => (
        <group key={i} position={[x, 0, z]} rotation-y={rot}>
          <mesh position={[0, 1.1, 0]} castShadow>
            <boxGeometry args={[2, 2.2, 0.06]} />
            <meshStandardMaterial color="#d8dad9" roughness={0.8} />
          </mesh>
          <mesh position={[0, 1.85, 0]}>
            <boxGeometry args={[2, 0.12, 0.1]} />
            <meshStandardMaterial color="#e8641b" roughness={0.6} />
          </mesh>
          <mesh position={[-0.95, 1.1, 0]}>
            <boxGeometry args={[0.1, 2.2, 0.1]} />
            <meshStandardMaterial color="#5b6470" roughness={0.6} />
          </mesh>
        </group>
      ))}
      {/* water-filled barriers at the gate */}
      {[31.5, 34].map((x, i) => (
        <mesh key={x} position={[x, 0.55, SITE_FRONT_Z + 1]} castShadow>
          <boxGeometry args={[2.4, 1.1, 0.5]} />
          <meshStandardMaterial color={i % 2 === 0 ? "#e8641b" : "#e6e3dc"} roughness={0.7} />
        </mesh>
      ))}
    </group>
  );
}

/** Material laydown: pallets, blocks, pipes, site cabin. */
function Materials() {
  return (
    <group>
      {/* block pallets */}
      {[0, 1].map((i) => (
        <group key={i} position={[32 + i * 2.2, 0, -39.5]}>
          <mesh position={[0, 0.1, 0]}>
            <boxGeometry args={[1.6, 0.14, 1.2]} />
            <meshStandardMaterial color="#a9835a" roughness={0.9} />
          </mesh>
          <mesh position={[0, 0.6, 0]} castShadow>
            <boxGeometry args={[1.4, 0.85, 1]} />
            <meshStandardMaterial color="#b9b5ad" roughness={0.95} />
          </mesh>
        </group>
      ))}
      {/* pipe stack */}
      {[0, 1, 2].map((i) => (
        <mesh
          key={i}
          position={[33, 0.25 + (i === 2 ? 0.42 : 0), -41.2 + (i === 1 ? 0.55 : 0)]}
          rotation-z={Math.PI / 2}
          castShadow
        >
          <cylinderGeometry args={[0.24, 0.24, 4.5, 10]} />
          <meshStandardMaterial color="#7f8b93" metalness={0.5} roughness={0.4} />
        </mesh>
      ))}
      {/* site cabin */}
      <group position={[46, 0, -40]}>
        <mesh position={[0, 1.4, 0]} castShadow>
          <boxGeometry args={[4, 2.6, 2.2]} />
          <meshStandardMaterial color="#e6e3dc" roughness={0.7} />
        </mesh>
        <mesh position={[-1.2, 1.15, 1.12]}>
          <boxGeometry args={[0.8, 1.9, 0.05]} />
          <meshStandardMaterial color="#4b535c" roughness={0.6} />
        </mesh>
      </group>
    </group>
  );
}

/** Gate signboard with project name and progress. */
function ProgressSign() {
  return (
    <group position={[32.5, 0, SITE_FRONT_Z + 0.7]}>
      {[-1.5, 1.5].map((x) => (
        <mesh key={x} position={[x, 1.2, 0]} castShadow>
          <boxGeometry args={[0.12, 2.4, 0.12]} />
          <meshStandardMaterial color="#5b6470" roughness={0.6} />
        </mesh>
      ))}
      <mesh position={[0, 1.9, 0.05]} castShadow>
        <boxGeometry args={[3.6, 1.6, 0.1]} />
        <meshStandardMaterial color="#f2efe8" roughness={0.7} />
      </mesh>
      <Text position={[0, 2.25, 0.12]} fontSize={0.3} anchorX="center" letterSpacing={0.04}>
        AUTHENTICATION CENTER
        <meshBasicMaterial toneMapped={false} color="#23262b" />
      </Text>
      <Text position={[0, 1.72, 0.12]} fontSize={0.26} anchorX="center" letterSpacing={0.04}>
        UNDER CONSTRUCTION
        <meshBasicMaterial toneMapped={false} color="#c2571b" />
      </Text>
      <Text position={[0, 1.32, 0.12]} fontSize={0.34} anchorX="center">
        45%
        <meshBasicMaterial toneMapped={false} color="#c2571b" />
      </Text>
    </group>
  );
}

export function ConstructionSite() {
  return (
    <group>
      {/* dirt pad */}
      <mesh position={[SITE_CENTER_X, 0.03, SITE_CENTER_Z]} receiveShadow>
        <boxGeometry args={[SITE_X_END - SITE_X_START, 0.06, SITE_DEPTH]} />
        <meshStandardMaterial color="#8a7a63" roughness={1} />
      </mesh>
      <PartialStructure />
      <Scaffolding />
      <TowerCrane />
      <Fencing />
      <Materials />
      <ProgressSign />
    </group>
  );
}
