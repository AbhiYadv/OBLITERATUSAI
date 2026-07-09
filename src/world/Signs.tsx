import { Text } from "@react-three/drei";

interface LetterSignProps {
  text: string;
  fontSize?: number;
  color?: string;
  position: [number, number, number];
  rotationY?: number;
}

/** Stand-off lettering, reads like backlit channel letters. */
export function LetterSign({
  text,
  fontSize = 1,
  color = "#eef4f8",
  position,
  rotationY = 0,
}: LetterSignProps) {
  return (
    <Text
      position={position}
      rotation-y={rotationY}
      fontSize={fontSize}
      anchorX="center"
      anchorY="middle"
      letterSpacing={0.06}
    >
      {text}
      <meshBasicMaterial toneMapped={false} color={color} />
    </Text>
  );
}

interface BandSignProps {
  text: string;
  width: number;
  height?: number;
  band?: string;
  color?: string;
  fontSize?: number;
  position: [number, number, number];
  rotationY?: number;
}

/** Dark fascia band with lettering, typical storefront signage. */
export function BandSign({
  text,
  width,
  height = 1,
  band = "#14171b",
  color = "#e8edf2",
  fontSize = 0.5,
  position,
  rotationY = 0,
}: BandSignProps) {
  return (
    <group position={position} rotation-y={rotationY}>
      <mesh castShadow>
        <boxGeometry args={[width, height, 0.24]} />
        <meshStandardMaterial color={band} roughness={0.6} />
      </mesh>
      <Text
        position={[0, 0, 0.14]}
        fontSize={fontSize}
        anchorX="center"
        anchorY="middle"
        letterSpacing={0.08}
      >
        {text}
        <meshBasicMaterial toneMapped={false} color={color} />
      </Text>
    </group>
  );
}
