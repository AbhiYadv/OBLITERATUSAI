const SKIN = "#c98e6d";
const HAIR = "#241b14";
const HOODIE = "#2a2d33";
const JEANS = "#33415e";
const SHOES = "#1c1d20";

interface HumanScaleCharacterProps {
  position?: [number, number, number];
  rotationY?: number;
}

/**
 * Lightweight human figure (~1.8m) in casual clothing, for scale and
 * atmosphere. Built from capsules/spheres with human proportions.
 */
export function HumanScaleCharacter({
  position = [0, 0, 0],
  rotationY = Math.PI,
}: HumanScaleCharacterProps) {
  return (
    <group position={position} rotation-y={rotationY}>
      {/* shoes */}
      {[-0.11, 0.11].map((x) => (
        <mesh key={`shoe-${x}`} position={[x, 0.06, 0.04]} castShadow>
          <boxGeometry args={[0.13, 0.11, 0.32]} />
          <meshStandardMaterial color={SHOES} roughness={0.7} />
        </mesh>
      ))}
      {/* legs */}
      {[-0.11, 0.11].map((x) => (
        <mesh key={`leg-${x}`} position={[x, 0.52, 0]} castShadow>
          <capsuleGeometry args={[0.085, 0.68, 4, 10]} />
          <meshStandardMaterial color={JEANS} roughness={0.85} />
        </mesh>
      ))}
      {/* hips */}
      <mesh position={[0, 0.96, 0]} castShadow>
        <capsuleGeometry args={[0.16, 0.1, 4, 10]} />
        <meshStandardMaterial color={JEANS} roughness={0.85} />
      </mesh>
      {/* torso (hoodie) */}
      <mesh position={[0, 1.26, 0]} castShadow>
        <capsuleGeometry args={[0.175, 0.42, 4, 12]} />
        <meshStandardMaterial color={HOODIE} roughness={0.9} />
      </mesh>
      {/* hood bunched at the neck */}
      <mesh position={[0, 1.5, -0.08]} castShadow>
        <sphereGeometry args={[0.12, 10, 8]} />
        <meshStandardMaterial color={HOODIE} roughness={0.9} />
      </mesh>
      {/* arms, relaxed at the sides */}
      {[-1, 1].map((side) => (
        <group key={`arm-${side}`} position={[side * 0.25, 1.36, 0]} rotation-z={side * 0.1}>
          <mesh position={[0, -0.26, 0]} castShadow>
            <capsuleGeometry args={[0.06, 0.46, 4, 8]} />
            <meshStandardMaterial color={HOODIE} roughness={0.9} />
          </mesh>
          {/* hands */}
          <mesh position={[side * 0.045, -0.56, 0]} castShadow>
            <sphereGeometry args={[0.055, 8, 8]} />
            <meshStandardMaterial color={SKIN} roughness={0.7} />
          </mesh>
        </group>
      ))}
      {/* neck and head */}
      <mesh position={[0, 1.56, 0]}>
        <cylinderGeometry args={[0.05, 0.06, 0.08, 8]} />
        <meshStandardMaterial color={SKIN} roughness={0.7} />
      </mesh>
      <mesh position={[0, 1.68, 0]} castShadow>
        <sphereGeometry args={[0.115, 14, 12]} />
        <meshStandardMaterial color={SKIN} roughness={0.7} />
      </mesh>
      {/* hair */}
      <mesh position={[0, 1.73, -0.02]} scale={[1, 0.82, 1.05]}>
        <sphereGeometry args={[0.115, 14, 10, 0, Math.PI * 2, 0, Math.PI * 0.62]} />
        <meshStandardMaterial color={HAIR} roughness={0.95} />
      </mesh>
    </group>
  );
}
