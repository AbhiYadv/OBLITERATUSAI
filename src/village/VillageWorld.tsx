/**
 * Scene composition root for the 2 km Nexo Village world foundation:
 * sky, fog, lights (shadow frustum follows the player), chunked terrain,
 * water, paths, forest, and the first-person controller.
 */
import { useMemo, useRef } from "react";
import { useFrame, useThree } from "@react-three/fiber";
import { Sky } from "@react-three/drei";
import * as THREE from "three";
import { parseSeed } from "./seed";
import { VILLAGE_HEIGHT, setCityEnabled, setTerrainSeed } from "./terrain";
import { TerrainChunks } from "./TerrainChunks";
import { Water } from "./Water";
import { Forest } from "./Forest";
import { Paths } from "./Paths";
import { PlayerController } from "./Player";
import { installVillageHooks, villageStats } from "./stats";

const SHADOW_RANGE = 95;

export function VillageWorld() {
  // Seed must be set before any child samples the terrain.
  const seed = useMemo(() => {
    const s = parseSeed();
    setTerrainSeed(s);
    setCityEnabled(false);
    villageStats.seed = s;
    installVillageHooks();
    return s;
  }, []);

  const playerPos = useRef(new THREE.Vector3(0, VILLAGE_HEIGHT + 1.65, 26));
  const lightRef = useRef<THREE.DirectionalLight>(null);
  const lightTarget = useMemo(() => new THREE.Object3D(), []);

  const { gl } = useThree();
  const fpsState = useRef({ ema: 60 });

  useFrame((_, delta) => {
    // Perf stats for the HUD.
    const fps = 1 / Math.max(delta, 1e-4);
    fpsState.current.ema += (fps - fpsState.current.ema) * 0.08;
    villageStats.fps = fpsState.current.ema;
    villageStats.frameMs = delta * 1000;
    villageStats.drawCalls = gl.info.render.calls;
    villageStats.triangles = gl.info.render.triangles;
    villageStats.geometries = gl.info.memory.geometries;

    // Keep the shadow frustum centered on the player.
    const light = lightRef.current;
    if (light) {
      const p = playerPos.current;
      light.position.set(p.x + 120, p.y + 150, p.z + 70);
      lightTarget.position.copy(p);
      lightTarget.updateMatrixWorld();
    }
  });

  return (
    <>
      <Sky
        distance={45000}
        sunPosition={[120, 150, 70]}
        turbidity={5}
        rayleigh={1.1}
        mieCoefficient={0.004}
        mieDirectionalG={0.85}
      />
      <fog attach="fog" args={["#cfdde8", 380, 1700]} />

      <hemisphereLight args={["#dfeaf4", "#8a8a78", 0.55]} />
      <directionalLight
        ref={lightRef}
        castShadow
        target={lightTarget}
        intensity={1.6}
        shadow-mapSize={[2048, 2048]}
        shadow-camera-left={-SHADOW_RANGE}
        shadow-camera-right={SHADOW_RANGE}
        shadow-camera-top={SHADOW_RANGE}
        shadow-camera-bottom={-SHADOW_RANGE}
        shadow-camera-near={1}
        shadow-camera-far={520}
        shadow-bias={-0.0004}
      />
      <primitive object={lightTarget} />

      <TerrainChunks playerPos={playerPos} />
      <Water />
      <Paths />
      <Forest seed={seed} />
      <PlayerController playerPos={playerPos} />
    </>
  );
}
