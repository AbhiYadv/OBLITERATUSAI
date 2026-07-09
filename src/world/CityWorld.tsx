import { Sky } from "@react-three/drei";
import { CityTerrain } from "./CityTerrain";
import { Roads } from "./Roads";
import { Skyline } from "./Buildings";
import { NexoLabsHQ } from "./buildings/NexoLabsHQ";
import { WorkerBureau } from "./buildings/WorkerBureau";
import { MCPMarketplace } from "./buildings/MCPMarketplace";
import { PreviewTheater } from "./buildings/PreviewTheater";
import { ConstructionSite } from "./ConstructionSite";
import { StreetProps } from "./StreetProps";
import { HumanCharacter } from "./assets/HumanCharacter";
import { Labels } from "./Labels";

export function CityWorld() {
  return (
    <>
      <Sky
        distance={4500}
        sunPosition={[90, 60, 35]}
        turbidity={5.5}
        rayleigh={1}
        mieCoefficient={0.004}
        mieDirectionalG={0.85}
      />
      <fog attach="fog" args={["#d9e4ec", 90, 380]} />

      <hemisphereLight args={["#e6eff7", "#8f8c85", 0.55]} />
      <directionalLight
        castShadow
        position={[70, 95, 45]}
        intensity={1.5}
        shadow-mapSize={[2048, 2048]}
        shadow-camera-left={-100}
        shadow-camera-right={100}
        shadow-camera-top={100}
        shadow-camera-bottom={-100}
        shadow-camera-far={320}
        shadow-bias={-0.0004}
      />
      <directionalLight position={[-50, 40, -60]} intensity={0.3} />

      <CityTerrain />
      <Roads />
      <NexoLabsHQ />
      <WorkerBureau />
      <MCPMarketplace />
      <PreviewTheater />
      <Skyline />
      <ConstructionSite />
      <StreetProps />
      <HumanCharacter position={[1, 0.14, 1.5]} rotationY={Math.PI} />
      <Labels />
    </>
  );
}
