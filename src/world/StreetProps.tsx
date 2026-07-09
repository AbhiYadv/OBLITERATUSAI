import { RoundedBox, Text } from "@react-three/drei";
import { SIDEWALK_TOP_Y } from "./layout";

const METAL_DARK = "#202327";

/* --------------------------------- greenery --------------------------------- */

function StreetTree({ position }: { position: [number, number, number] }) {
  return (
    <group position={position}>
      {/* planter pit frame */}
      <mesh position={[0, 0.06, 0]}>
        <boxGeometry args={[1.5, 0.12, 1.5]} />
        <meshStandardMaterial color="#8e8b85" roughness={0.9} />
      </mesh>
      <mesh position={[0, 1, 0]} castShadow>
        <cylinderGeometry args={[0.09, 0.13, 1.9, 8]} />
        <meshStandardMaterial color="#6b4f37" roughness={0.9} />
      </mesh>
      <mesh position={[0.12, 2.35, 0.05]} castShadow>
        <sphereGeometry args={[1, 12, 10]} />
        <meshStandardMaterial color="#4f7a45" roughness={0.95} />
      </mesh>
      <mesh position={[-0.25, 2.95, -0.12]} castShadow>
        <sphereGeometry args={[0.72, 12, 10]} />
        <meshStandardMaterial color="#5d8a4f" roughness={0.95} />
      </mesh>
    </group>
  );
}

function Planter({ position }: { position: [number, number, number] }) {
  return (
    <group position={position}>
      <mesh castShadow>
        <boxGeometry args={[1.8, 0.55, 0.7]} />
        <meshStandardMaterial color="#75726c" roughness={0.9} />
      </mesh>
      <mesh position={[0, 0.42, 0]} scale={[1, 0.55, 1]} castShadow>
        <sphereGeometry args={[0.62, 10, 8]} />
        <meshStandardMaterial color="#557b48" roughness={0.95} />
      </mesh>
    </group>
  );
}

/* -------------------------------- street metal ------------------------------- */

function StreetLight({
  position,
  side,
}: {
  position: [number, number, number];
  side: 1 | -1;
}) {
  return (
    <group position={position}>
      <mesh position={[0, 2.6, 0]} castShadow>
        <cylinderGeometry args={[0.07, 0.1, 5.2, 8]} />
        <meshStandardMaterial color={METAL_DARK} roughness={0.6} />
      </mesh>
      <mesh position={[0, 5.15, side * 0.8]}>
        <boxGeometry args={[0.09, 0.09, 1.7]} />
        <meshStandardMaterial color={METAL_DARK} roughness={0.6} />
      </mesh>
      <mesh position={[0, 5.05, side * 1.6]}>
        <boxGeometry args={[0.22, 0.12, 0.55]} />
        <meshStandardMaterial
          color="#fff3d6"
          emissive="#ffe9c4"
          emissiveIntensity={1.1}
          roughness={0.4}
        />
      </mesh>
    </group>
  );
}

function Bollard({ position }: { position: [number, number, number] }) {
  return (
    <group position={position}>
      <mesh position={[0, 0.4, 0]} castShadow>
        <cylinderGeometry args={[0.09, 0.1, 0.8, 10]} />
        <meshStandardMaterial color={METAL_DARK} metalness={0.4} roughness={0.5} />
      </mesh>
      <mesh position={[0, 0.72, 0]}>
        <cylinderGeometry args={[0.095, 0.095, 0.05, 10]} />
        <meshStandardMaterial color="#9aa0a6" metalness={0.6} roughness={0.35} />
      </mesh>
    </group>
  );
}

function Bench({ position, rotationY = 0 }: { position: [number, number, number]; rotationY?: number }) {
  return (
    <group position={position} rotation-y={rotationY}>
      {[-0.7, 0.7].map((x) => (
        <mesh key={x} position={[x, 0.22, 0]} castShadow>
          <boxGeometry args={[0.08, 0.44, 0.5]} />
          <meshStandardMaterial color={METAL_DARK} roughness={0.6} />
        </mesh>
      ))}
      {[0.4, 0.47].map((y, i) => (
        <mesh key={y} position={[0, y, i === 0 ? 0 : -0.03]} castShadow>
          <boxGeometry args={[1.8, 0.06, 0.5]} />
          <meshStandardMaterial color="#8a6d4d" roughness={0.8} />
        </mesh>
      ))}
      <mesh position={[0, 0.78, -0.26]} rotation-x={-0.22} castShadow>
        <boxGeometry args={[1.8, 0.5, 0.06]} />
        <meshStandardMaterial color="#8a6d4d" roughness={0.8} />
      </mesh>
    </group>
  );
}

/* ---------------------------------- vehicles --------------------------------- */

function Car({
  position,
  rotationY = 0,
  color,
  taxi = false,
}: {
  position: [number, number, number];
  rotationY?: number;
  color: string;
  taxi?: boolean;
}) {
  return (
    <group position={position} rotation-y={rotationY}>
      <RoundedBox args={[4.2, 0.7, 1.85]} radius={0.14} position={[0, 0.62, 0]} castShadow>
        <meshStandardMaterial color={color} metalness={0.5} roughness={0.3} />
      </RoundedBox>
      <RoundedBox args={[2.2, 0.62, 1.7]} radius={0.16} position={[-0.15, 1.2, 0]} castShadow>
        <meshStandardMaterial color="#1b2733" metalness={0.7} roughness={0.15} />
      </RoundedBox>
      {[
        [1.35, 0.82],
        [1.35, -0.82],
        [-1.35, 0.82],
        [-1.35, -0.82],
      ].map(([x, z]) => (
        <mesh key={`${x}-${z}`} position={[x, 0.34, z]} rotation-x={Math.PI / 2}>
          <cylinderGeometry args={[0.34, 0.34, 0.26, 14]} />
          <meshStandardMaterial color="#15161a" roughness={0.7} />
        </mesh>
      ))}
      {/* headlights and taillights */}
      {[0.55, -0.55].map((z) => (
        <mesh key={`h-${z}`} position={[2.1, 0.68, z]}>
          <boxGeometry args={[0.05, 0.14, 0.32]} />
          <meshStandardMaterial color="#f3f0dd" emissive="#efe9c8" emissiveIntensity={0.6} />
        </mesh>
      ))}
      {[0.55, -0.55].map((z) => (
        <mesh key={`t-${z}`} position={[-2.1, 0.68, z]}>
          <boxGeometry args={[0.05, 0.14, 0.32]} />
          <meshStandardMaterial color="#b03535" emissive="#8d1f1f" emissiveIntensity={0.5} />
        </mesh>
      ))}
      {taxi && (
        <mesh position={[-0.15, 1.62, 0]} castShadow>
          <boxGeometry args={[0.75, 0.22, 0.35]} />
          <meshStandardMaterial color="#f4f1e8" emissive="#d9d4bf" emissiveIntensity={0.4} />
        </mesh>
      )}
    </group>
  );
}

function Van({ position, rotationY = 0 }: { position: [number, number, number]; rotationY?: number }) {
  return (
    <group position={position} rotation-y={rotationY}>
      <RoundedBox args={[4.8, 1.9, 2.05]} radius={0.18} position={[0, 1.3, 0]} castShadow>
        <meshStandardMaterial color="#e8e9ea" metalness={0.35} roughness={0.4} />
      </RoundedBox>
      <mesh position={[1.95, 1.6, 0]}>
        <boxGeometry args={[0.95, 0.75, 1.9]} />
        <meshStandardMaterial color="#1b2733" metalness={0.7} roughness={0.15} />
      </mesh>
      {[
        [1.55, 0.95],
        [1.55, -0.95],
        [-1.55, 0.95],
        [-1.55, -0.95],
      ].map(([x, z]) => (
        <mesh key={`${x}-${z}`} position={[x, 0.38, z]} rotation-x={Math.PI / 2}>
          <cylinderGeometry args={[0.38, 0.38, 0.28, 14]} />
          <meshStandardMaterial color="#15161a" roughness={0.7} />
        </mesh>
      ))}
    </group>
  );
}

/* ----------------------------- notification kiosk ---------------------------- */

const NOTIFICATION_ROWS: Array<{ y: number; w: number; color: string }> = [
  { y: 3.1, w: 1.7, color: "#54d38a" },
  { y: 2.72, w: 1.45, color: "#4cc3e8" },
  { y: 2.34, w: 1.6, color: "#f0c53d" },
  { y: 1.96, w: 1.3, color: "#a58cf0" },
];

/** Freestanding digital information board on the south sidewalk. */
function NotificationBoard() {
  return (
    <group position={[14, SIDEWALK_TOP_Y, 6.3]} rotation-y={-0.25}>
      <mesh position={[0, 0.07, 0]} castShadow>
        <boxGeometry args={[1.9, 0.14, 0.8]} />
        <meshStandardMaterial color="#3a3d42" roughness={0.7} />
      </mesh>
      <mesh position={[0, 0.55, 0]} castShadow>
        <boxGeometry args={[0.3, 0.9, 0.26]} />
        <meshStandardMaterial color={METAL_DARK} roughness={0.55} />
      </mesh>
      <mesh position={[0, 2.55, 0]} castShadow>
        <boxGeometry args={[2.5, 3.1, 0.3]} />
        <meshStandardMaterial color="#17191d" metalness={0.4} roughness={0.45} />
      </mesh>
      {/* screen */}
      <mesh position={[0, 2.55, 0.17]}>
        <planeGeometry args={[2.15, 2.75]} />
        <meshStandardMaterial color="#0b0f14" emissive="#0d1420" emissiveIntensity={0.9} roughness={0.3} />
      </mesh>
      <Text position={[0, 3.6, 0.19]} fontSize={0.2} anchorX="center" letterSpacing={0.1}>
        NOTIFICATIONS
        <meshBasicMaterial toneMapped={false} color="#7ee2a8" />
      </Text>
      {NOTIFICATION_ROWS.map((row) => (
        <group key={row.y}>
          <mesh position={[-0.85, row.y, 0.18]}>
            <planeGeometry args={[0.14, 0.14]} />
            <meshBasicMaterial toneMapped={false} color={row.color} />
          </mesh>
          <mesh position={[-0.6 + row.w / 2, row.y, 0.18]}>
            <planeGeometry args={[row.w - 0.35, 0.09]} />
            <meshBasicMaterial toneMapped={false} color="#3d4754" />
          </mesh>
        </group>
      ))}
    </group>
  );
}

/* ---------------------------------- placement -------------------------------- */

const SOUTH_TREES = [-34, -22, -10, 22, 34];
const NORTH_TREES = [-38, -16, 45];
const NORTH_LIGHTS = [-40, -20, 14, 32, 50];
const SOUTH_LIGHTS = [-32, -12, 8, 28, 46];
const CROSSING_BOLLARDS = [-1, 0.1, 1.2, 2.3, 3.4];

export function StreetProps() {
  const walkY = SIDEWALK_TOP_Y;
  return (
    <group>
      {SOUTH_TREES.map((x) => (
        <StreetTree key={`ts-${x}`} position={[x, walkY, 6.7]} />
      ))}
      {NORTH_TREES.map((x) => (
        <StreetTree key={`tn-${x}`} position={[x, walkY, -6.7]} />
      ))}

      {NORTH_LIGHTS.map((x) => (
        <StreetLight key={`ln-${x}`} position={[x, walkY, -5.2]} side={1} />
      ))}
      {SOUTH_LIGHTS.map((x) => (
        <StreetLight key={`ls-${x}`} position={[x, walkY, 5.2]} side={-1} />
      ))}

      {CROSSING_BOLLARDS.map((x) => (
        <Bollard key={`bs-${x}`} position={[x, walkY, 5]} />
      ))}
      {CROSSING_BOLLARDS.map((x) => (
        <Bollard key={`bn-${x}`} position={[x, walkY, -5]} />
      ))}

      <Bench position={[-24, walkY, 7.4]} rotationY={Math.PI} />
      <Bench position={[28, walkY, 7.4]} rotationY={Math.PI} />

      {/* HQ plaza and theater planters */}
      <Planter position={[-32, 0.28, -7.2]} />
      <Planter position={[-20, 0.28, -7.2]} />
      <Planter position={[46, walkY, -7.3]} />
      <Planter position={[58, walkY, -7.3]} />

      {/* vehicles */}
      <Car position={[-4, 0.14, -1.4]} color="#f2b52a" taxi />
      <Car position={[-18, 0.14, -3.35]} color="#e8e9ea" />
      <Car position={[24, 0.14, 2.1]} rotationY={Math.PI} color="#3f6fa8" />
      <Van position={[40, 0.14, 3.3]} />

      <NotificationBoard />
    </group>
  );
}
