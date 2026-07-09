import { Billboard, Text } from "@react-three/drei";

interface LabelSpec {
  text: string;
  position: [number, number, number];
  /** shrink labels that sit close to the camera */
  scale?: number;
}

/** Small pill labels above key places. Environment-first: kept compact. */
const LABELS: LabelSpec[] = [
  { text: "Nexo Labs HQ", position: [-26, 39.5, -16.5] },
  { text: "Worker Bureau", position: [-6, 20.5, -14.5] },
  { text: "MCP Marketplace", position: [28, 15.2, -15.5] },
  { text: "Preview Theater", position: [52, 14.6, -14.5] },
  { text: "Authentication Center", position: [40, 24, -33] },
  { text: "Notification Board", position: [14, 4.6, 6.3], scale: 0.6 },
];

function PillLabel({ text, position, scale = 1 }: LabelSpec) {
  const width = text.length * 0.34 + 0.9;
  return (
    <Billboard position={position} scale={scale}>
      <mesh>
        <boxGeometry args={[width, 1.05, 0.06]} />
        <meshStandardMaterial color="#14181d" roughness={0.5} transparent opacity={0.92} />
      </mesh>
      <Text position={[0, 0, 0.06]} fontSize={0.58} anchorX="center" anchorY="middle">
        {text}
        <meshBasicMaterial toneMapped={false} color="#f3f6f9" />
      </Text>
    </Billboard>
  );
}

export function Labels() {
  return (
    <>
      {LABELS.map((label) => (
        <PillLabel key={label.text} {...label} />
      ))}
    </>
  );
}
