export function CityTerrain() {
  return (
    <group>
      {/* base ground */}
      <mesh rotation-x={-Math.PI / 2} receiveShadow>
        <planeGeometry args={[240, 180]} />
        <meshStandardMaterial color="#a09d95" roughness={0.95} />
      </mesh>
      {/* north block yard behind the building fronts */}
      <mesh rotation-x={-Math.PI / 2} position={[0, 0.008, -46]} receiveShadow>
        <planeGeometry args={[220, 75]} />
        <meshStandardMaterial color="#8b8880" roughness={0.95} />
      </mesh>
      {/* south plaza paving */}
      <mesh rotation-x={-Math.PI / 2} position={[0, 0.008, 36]} receiveShadow>
        <planeGeometry args={[220, 55]} />
        <meshStandardMaterial color="#aaa79f" roughness={0.95} />
      </mesh>
    </group>
  );
}
