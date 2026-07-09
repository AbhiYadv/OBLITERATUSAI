import {
  CROSS_ROAD_CENTER_X,
  CROSS_ROAD_WIDTH,
  MAIN_ROAD_WIDTH,
  ROAD_LENGTH,
  SIDEWALK_WIDTH,
} from "./layout";

const ASPHALT = "#33373c";
const SIDEWALK = "#b6b3ac";
const MARKING = "#e6e6e2";

const crossWest = CROSS_ROAD_CENTER_X - CROSS_ROAD_WIDTH / 2;
const crossEast = CROSS_ROAD_CENTER_X + CROSS_ROAD_WIDTH / 2;

const laneDashes: number[] = [];
for (let x = -ROAD_LENGTH / 2 + 4; x < ROAD_LENGTH / 2; x += 7) {
  // keep the intersection clear of dashes
  if (x > crossWest - 4 && x < crossEast + 2) continue;
  laneDashes.push(x);
}

const crossRoadDashes: number[] = [];
for (let z = -14; z > -95; z -= 7) {
  crossRoadDashes.push(z);
}

/** Zebra crosswalk across the main road, centered at world x. */
function Crosswalk({ x }: { x: number }) {
  const stripes: number[] = [];
  for (let z = -3.5; z <= 3.5; z += 1.1) {
    stripes.push(z);
  }
  return (
    <group position={[x, 0, 0]}>
      {stripes.map((z) => (
        <mesh key={z} position={[0, 0.15, z]}>
          <boxGeometry args={[2.6, 0.02, 0.55]} />
          <meshStandardMaterial color={MARKING} roughness={0.85} />
        </mesh>
      ))}
    </group>
  );
}

/** Sidewalk slab with a visible curb face (sits higher than the road). */
function Sidewalk({
  x,
  z,
  width,
  depth,
}: {
  x: number;
  z: number;
  width: number;
  depth: number;
}) {
  return (
    <mesh position={[x, 0.17, z]} receiveShadow castShadow>
      <boxGeometry args={[width, 0.34, depth]} />
      <meshStandardMaterial color={SIDEWALK} roughness={0.9} />
    </mesh>
  );
}

export function Roads() {
  const northSidewalkZ = -(MAIN_ROAD_WIDTH / 2 + SIDEWALK_WIDTH / 2);
  const southSidewalkZ = MAIN_ROAD_WIDTH / 2 + SIDEWALK_WIDTH / 2;

  return (
    <group>
      {/* main road slab, east-west */}
      <mesh position={[0, 0.08, 0]} receiveShadow>
        <boxGeometry args={[ROAD_LENGTH, 0.12, MAIN_ROAD_WIDTH]} />
        <meshStandardMaterial color={ASPHALT} roughness={0.95} />
      </mesh>
      {/* side road slab heading north */}
      <mesh
        position={[CROSS_ROAD_CENTER_X, 0.08, -(MAIN_ROAD_WIDTH / 2 + 48)]}
        receiveShadow
      >
        <boxGeometry args={[CROSS_ROAD_WIDTH, 0.12, 96]} />
        <meshStandardMaterial color={ASPHALT} roughness={0.95} />
      </mesh>

      {/* center dashes, main road */}
      {laneDashes.map((x) => (
        <mesh key={`dash-${x}`} position={[x, 0.145, 0]}>
          <boxGeometry args={[3, 0.02, 0.3]} />
          <meshStandardMaterial color={MARKING} roughness={0.85} />
        </mesh>
      ))}
      {/* edge lines, main road */}
      {[-1, 1].map((side) => (
        <mesh
          key={`edge-${side}`}
          position={[0, 0.145, side * (MAIN_ROAD_WIDTH / 2 - 0.5)]}
        >
          <boxGeometry args={[ROAD_LENGTH, 0.02, 0.16]} />
          <meshStandardMaterial color={MARKING} roughness={0.85} />
        </mesh>
      ))}
      {/* center dashes, side road */}
      {crossRoadDashes.map((z) => (
        <mesh key={`xdash-${z}`} position={[CROSS_ROAD_CENTER_X, 0.145, z]}>
          <boxGeometry args={[0.3, 0.02, 3]} />
          <meshStandardMaterial color={MARKING} roughness={0.85} />
        </mesh>
      ))}

      <Crosswalk x={1} />
      <Crosswalk x={15} />

      {/* north sidewalk, split at the side road */}
      <Sidewalk
        x={(-ROAD_LENGTH / 2 + crossWest) / 2}
        z={northSidewalkZ}
        width={crossWest - -ROAD_LENGTH / 2}
        depth={SIDEWALK_WIDTH}
      />
      <Sidewalk
        x={(crossEast + ROAD_LENGTH / 2) / 2}
        z={northSidewalkZ}
        width={ROAD_LENGTH / 2 - crossEast}
        depth={SIDEWALK_WIDTH}
      />
      {/* south sidewalk, continuous */}
      <Sidewalk x={0} z={southSidewalkZ} width={ROAD_LENGTH} depth={SIDEWALK_WIDTH} />
      {/* side road sidewalks heading north */}
      <Sidewalk x={crossWest - 1.5} z={-(MAIN_ROAD_WIDTH / 2 + SIDEWALK_WIDTH + 45)} width={3} depth={90} />
      <Sidewalk x={crossEast + 1.5} z={-(MAIN_ROAD_WIDTH / 2 + SIDEWALK_WIDTH + 45)} width={3} depth={90} />
    </group>
  );
}
