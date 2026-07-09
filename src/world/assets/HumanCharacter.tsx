import { CityAsset } from "./CityAsset";
import { HumanScaleCharacter } from "../HumanScaleCharacter";

interface HumanCharacterProps {
  position?: [number, number, number];
  rotationY?: number;
}

/**
 * Street-scale human. Renders the installed humanoid GLB (manifest id
 * "human-casual", normalized to 1.8 m); the procedural figure remains only as
 * the explicit fallback when the file is absent or fails to load.
 * Static for now — animation support arrives with a later task.
 */
export function HumanCharacter({
  position = [0, 0, 0],
  rotationY = 0,
}: HumanCharacterProps) {
  return (
    <group position={position} rotation-y={rotationY}>
      <CityAsset assetId="human-casual" fallback={<HumanScaleCharacter rotationY={0} />} />
    </group>
  );
}
