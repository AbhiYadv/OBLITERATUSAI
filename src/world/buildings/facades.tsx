import { useMemo } from "react";
import { Instance, Instances } from "@react-three/drei";

/** Shared curtain-wall glass material props (used by procedural fallbacks). */
export const GLASS = {
  color: "#2e4a63",
  metalness: 0.45,
  roughness: 0.2,
  emissive: "#223950",
  emissiveIntensity: 0.45,
};

const GLASS_LIT = "#f4c98a";

interface WindowRowsProps {
  width: number;
  floors: number;
  cols: number;
  floorHeight?: number;
  windowHeight?: number;
  baseY?: number;
}

/** Instanced punched-window grid for one facade plane (local +z facing). */
export function WindowRows({
  width,
  floors,
  cols,
  floorHeight = 3.4,
  windowHeight = 1.7,
  baseY = 2.2,
}: WindowRowsProps) {
  const { dark, lit, winW } = useMemo(() => {
    const cellW = width / cols;
    const darkPositions: Array<[number, number, number]> = [];
    const litPositions: Array<[number, number, number]> = [];
    for (let f = 0; f < floors; f++) {
      for (let c = 0; c < cols; c++) {
        const p: [number, number, number] = [
          -width / 2 + cellW * (c + 0.5),
          baseY + f * floorHeight,
          0,
        ];
        // deterministic sprinkle of warm lit windows
        if ((f * 7 + c * 13) % 11 === 0) {
          litPositions.push(p);
        } else {
          darkPositions.push(p);
        }
      }
    }
    return { dark: darkPositions, lit: litPositions, winW: cellW * 0.58 };
  }, [width, floors, cols, floorHeight, baseY]);

  return (
    <>
      <Instances limit={dark.length}>
        <planeGeometry args={[winW, windowHeight]} />
        <meshStandardMaterial
          color="#2a3d4f"
          metalness={0.85}
          roughness={0.18}
          emissive="#2b3d4f"
          emissiveIntensity={0.35}
        />
        {dark.map((p, i) => (
          <Instance key={i} position={p} />
        ))}
      </Instances>
      {lit.length > 0 && (
        <Instances limit={lit.length}>
          <planeGeometry args={[winW, windowHeight]} />
          <meshStandardMaterial
            color={GLASS_LIT}
            emissive={GLASS_LIT}
            emissiveIntensity={0.55}
            roughness={0.4}
          />
          {lit.map((p, i) => (
            <Instance key={i} position={p} />
          ))}
        </Instances>
      )}
    </>
  );
}

interface PunchedFacadesProps {
  w: number;
  d: number;
  floors: number;
  colsX: number;
  colsZ: number;
  floorHeight?: number;
  baseY?: number;
}

/** Window grids wrapped around all four sides of a box body. */
export function PunchedFacades({
  w,
  d,
  floors,
  colsX,
  colsZ,
  floorHeight,
  baseY,
}: PunchedFacadesProps) {
  const inset = 1.2;
  return (
    <>
      <group position={[0, 0, d / 2 + 0.04]}>
        <WindowRows width={w - inset} floors={floors} cols={colsX} floorHeight={floorHeight} baseY={baseY} />
      </group>
      <group position={[0, 0, -d / 2 - 0.04]} rotation-y={Math.PI}>
        <WindowRows width={w - inset} floors={floors} cols={colsX} floorHeight={floorHeight} baseY={baseY} />
      </group>
      <group position={[w / 2 + 0.04, 0, 0]} rotation-y={Math.PI / 2}>
        <WindowRows width={d - inset} floors={floors} cols={colsZ} floorHeight={floorHeight} baseY={baseY} />
      </group>
      <group position={[-w / 2 - 0.04, 0, 0]} rotation-y={-Math.PI / 2}>
        <WindowRows width={d - inset} floors={floors} cols={colsZ} floorHeight={floorHeight} baseY={baseY} />
      </group>
    </>
  );
}
