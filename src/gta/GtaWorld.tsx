/**
 * GTA scene composition: the 2 km terrain world (with the downtown city
 * district enabled) plus city streets, textured buildings, traffic, and a
 * third-person animated character. Reuses the village engine systems:
 * chunked LOD terrain, water, paths, forest, stats HUD.
 */
import { useEffect, useMemo, useRef } from "react";
import { useFrame, useThree } from "@react-three/fiber";
import { Sky } from "@react-three/drei";
import * as THREE from "three";
import { RoomEnvironment } from "three/examples/jsm/environments/RoomEnvironment.js";
import { parseSeed } from "../village/seed";
import { setCityEnabled, setTerrainSeed } from "../village/terrain";
import { TerrainChunks } from "../village/TerrainChunks";
import { Water } from "../village/Water";
import { Forest } from "../village/Forest";
import { Paths } from "../village/Paths";
import { installVillageHooks, villageStats } from "../village/stats";
import { buildCityPlan } from "./cityPlan";
import { CloudLayer } from "./Clouds";
import { CityRoads } from "./CityRoads";
import { TrafficLights } from "./TrafficLights";
import { CityBlocks } from "./CityBlocks";
import { Traffic } from "./Traffic";
import { Billboards } from "./Billboards";
import { StreetFurniture } from "./StreetFurniture";
import { Construction } from "./Construction";
import { Pedestrians } from "./Pedestrians";
import { DriveableCar } from "./vehicle/DriveableCar";
import { PlayerCharacter, type MotionState } from "./Character";
import { ThirdPersonController } from "./ThirdPersonController";
import { worldCollisionRegistry } from "./collision/CollisionRegistry";
import { getNexoBaseline } from "./debug/PerformanceProbe";

const SHADOW_RANGE = 85; // 4k map over ±85 m: crisp texels, distant cutoff

export function GtaWorld() {
  // Seed + city flag must be set before any child samples the terrain.
  const seed = useMemo(() => {
    const s = parseSeed();
    setTerrainSeed(s);
    setCityEnabled(true);
    villageStats.seed = s;
    installVillageHooks();
    return s;
  }, []);

  const plan = useMemo(() => buildCityPlan(seed), [seed]);

  const playerPos = useRef(new THREE.Vector3(-345, 18, 22));
  const charGroup = useRef<THREE.Group>(null);
  const motion = useRef<MotionState>({ speed: 0, grounded: true });
  const lightRef = useRef<THREE.DirectionalLight>(null);
  const lightTarget = useMemo(() => new THREE.Object3D(), []);

  const { gl, scene } = useThree();
  const fpsState = useRef({ ema: 60 });

  // Image-based lighting from three's built-in RoomEnvironment: gives cars,
  // glass, and water real reflections so objects read as solid, no fetches.
  useEffect(() => {
    const pmrem = new THREE.PMREMGenerator(gl);
    const envTex = pmrem.fromScene(new RoomEnvironment(), 0.04).texture;
    scene.environment = envTex;
    scene.environmentIntensity = 0.4;
    pmrem.dispose();
    return () => {
      scene.environment = null;
      envTex.dispose();
    };
  }, [gl, scene]);

  useFrame((_, delta) => {
    const fps = 1 / Math.max(delta, 1e-4);
    fpsState.current.ema += (fps - fpsState.current.ema) * 0.08;
    villageStats.fps = fpsState.current.ema;
    villageStats.frameMs = delta * 1000;
    villageStats.drawCalls = gl.info.render.calls;
    villageStats.triangles = gl.info.render.triangles;
    villageStats.geometries = gl.info.memory.geometries;

    const baseline = getNexoBaseline();
    if (baseline?.isRecording()) {
      const activePedestrians = worldCollisionRegistry.countByPrefix("pedestrian:");
      baseline.recordFrame({
        deltaMs: delta * 1000,
        drawCalls: gl.info.render.calls,
        triangles: gl.info.render.triangles,
        geometries: gl.info.memory.geometries,
        textures: gl.info.memory.textures,
        activePedestrians,
        activeTrafficVehicles: worldCollisionRegistry.countByPrefix("traffic:"),
        confirmedActiveMixers: 0,
        inferredActiveMixers: activePedestrians + (charGroup.current ? 1 : 0),
      });
    }

    const light = lightRef.current;
    if (light) {
      const p = playerPos.current;
      light.position.set(p.x + 70, p.y + 185, p.z + 45);
      lightTarget.position.copy(p);
      lightTarget.updateMatrixWorld();
    }
  });

  return (
    <>
      <Sky
        distance={45000}
        sunPosition={[70, 185, 45]}
        turbidity={3}
        rayleigh={2.6}
        mieCoefficient={0.003}
        mieDirectionalG={0.85}
      />
      <fog attach="fog" args={["#cfdde8", 380, 1700]} />
      <CloudLayer seed={seed} />

      <hemisphereLight args={["#dfeaf4", "#8a8a78", 0.85]} />
      <directionalLight
        ref={lightRef}
        castShadow
        target={lightTarget}
        intensity={1.6}
        shadow-mapSize={[4096, 4096]}
        shadow-camera-left={-SHADOW_RANGE}
        shadow-camera-right={SHADOW_RANGE}
        shadow-camera-top={SHADOW_RANGE}
        shadow-camera-bottom={-SHADOW_RANGE}
        shadow-camera-near={1}
        shadow-camera-far={420}
        shadow-bias={-0.00015}
        shadow-normalBias={0.4}
      />
      <primitive object={lightTarget} />

      <TerrainChunks playerPos={playerPos} />
      <Water />
      <Paths />
      <Forest seed={seed} />

      <CityRoads plan={plan} />
      <TrafficLights />
      <CityBlocks plan={plan} seed={seed} />
      <Billboards plan={plan} seed={seed} />
      <StreetFurniture plan={plan} seed={seed} />
      <Construction plan={plan} />
      <Traffic plan={plan} seed={seed} />
      <Pedestrians plan={plan} seed={seed} />
      <DriveableCar />

      <PlayerCharacter ref={charGroup} motion={motion} />
      <ThirdPersonController
        charGroup={charGroup}
        playerPos={playerPos}
        motion={motion}
        plan={plan}
      />
    </>
  );
}
