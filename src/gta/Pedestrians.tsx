/**
 * Walking pedestrians: skinned clones of the rigged human, each with its
 * own mixer playing the walk clip, looping around a downtown block on the
 * sidewalk band. A third are tinted hi-vis like the construction crews in
 * the concept art.
 */
import { useEffect, useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import { useGLTF } from "@react-three/drei";
import { clone as cloneSkinned } from "three/examples/jsm/utils/SkeletonUtils.js";
import * as THREE from "three";
import { mulberry32 } from "../village/seed";
import { SLAB_TOP, type BlockRect, type CityPlan } from "./cityPlan";
import { worldCollisionRegistry } from "./collision/CollisionRegistry";
import {
  pedestrianBounds,
  pedestrianId,
  shouldStopForPedestrianObstacle,
} from "./characters/CharacterCollision";

const MODEL_URL = "/assets/characters/human-casual.glb";
const COUNT = 50;
const TARGET_HEIGHT = 1.75;
const CLIP_NATURAL_SPEED = 1.55;
const INSET = 1.9; // walking line inside the sidewalk band

interface Ped {
  group: THREE.Group;
  mixer: THREE.AnimationMixer;
  block: BlockRect;
  perimeter: number;
  dist: number;
  speed: number;
  currentSpeed: number;
  yaw: number;
  blocked: boolean;
}

function pointOnLoop(b: BlockRect, dist: number, out: THREE.Vector2): number {
  const w = b.maxX - b.minX - INSET * 2;
  const d = b.maxZ - b.minZ - INSET * 2;
  const x0 = b.minX + INSET;
  const z0 = b.minZ + INSET;
  let t = dist % (2 * (w + d));
  if (t < w) {
    out.set(x0 + t, z0);
    return Math.atan2(1, 0); // +x
  }
  t -= w;
  if (t < d) {
    out.set(x0 + w, z0 + t);
    return Math.atan2(0, 1); // +z
  }
  t -= d;
  if (t < w) {
    out.set(x0 + w - t, z0 + d);
    return Math.atan2(-1, 0); // -x
  }
  t -= w;
  out.set(x0, z0 + d - t);
  return Math.atan2(0, -1); // -z
}

export function Pedestrians({ plan, seed }: { plan: CityPlan; seed: number }) {
  const gltf = useGLTF(MODEL_URL);

  const built = useMemo(() => {
    const rand = mulberry32(seed ^ 0x9ed5);
    gltf.scene.updateMatrixWorld(true);
    const box = new THREE.Box3().setFromObject(gltf.scene);
    const size = box.getSize(new THREE.Vector3());
    const scale = size.y > 0 ? TARGET_HEIGHT / size.y : 1;
    const yOffset = -box.min.y * scale;

    // Downtown-ish blocks host the foot traffic.
    const core = plan.blocks.filter((b) => {
      const cx = (b.minX + b.maxX) / 2;
      const cz = (b.minZ + b.maxZ) / 2;
      return Math.hypot(cx + 350, cz / 2) < 260;
    });
    const clip = gltf.animations.find((c) => /mixamo|walk/i.test(c.name)) ?? gltf.animations[0];

    const peds: Ped[] = [];
    for (let i = 0; i < COUNT && core.length > 0; i++) {
      const block = core[Math.floor(rand() * core.length)];
      const model = cloneSkinned(gltf.scene);
      model.traverse((o) => {
        if ((o as THREE.Mesh).isMesh) {
          o.castShadow = true;
          (o as THREE.Mesh).frustumCulled = false;
          // Hi-vis tint for a third of the crowd (worker vibe).
          if (i % 3 === 0) {
            const mesh = o as THREE.Mesh;
            const src = (Array.isArray(mesh.material)
              ? mesh.material[0]
              : mesh.material) as THREE.MeshStandardMaterial;
            const tinted = src.clone();
            tinted.color = new THREE.Color("#ffd66e");
            mesh.material = tinted;
          }
        }
      });
      const inner = new THREE.Group();
      inner.position.y = yOffset;
      inner.scale.setScalar(scale);
      inner.add(model);
      const group = new THREE.Group();
      group.add(inner);

      const mixer = new THREE.AnimationMixer(model);
      const speed = 1.1 + rand() * 0.6;
      const action = mixer.clipAction(clip);
      action.play();
      action.timeScale = speed / CLIP_NATURAL_SPEED;

      const w = block.maxX - block.minX - INSET * 2;
      const d = block.maxZ - block.minZ - INSET * 2;
      peds.push({
        group,
        mixer,
        block,
        perimeter: 2 * (w + d),
        dist: rand() * 2 * (w + d),
        speed,
        currentSpeed: speed,
        yaw: 0,
        blocked: false,
      });
    }
    return { peds };
  }, [gltf, plan, seed]);

  useEffect(() => {
    built.peds.forEach((ped, i) => {
      const targetYaw = pointOnLoop(ped.block, ped.dist, tmp.current);
      ped.yaw = targetYaw;
      ped.group.position.set(tmp.current.x, SLAB_TOP, tmp.current.y);
      ped.group.rotation.y = targetYaw;
      worldCollisionRegistry.register({
        id: pedestrianId(i),
        kind: "pedestrian",
        solid: true,
        dynamic: true,
        bounds: pedestrianBounds(tmp.current.x, SLAB_TOP, tmp.current.y),
      });
    });
    return () => {
      built.peds.forEach((_, i) => worldCollisionRegistry.unregister(pedestrianId(i)));
    };
  }, [built]);

  const tmp = useRef(new THREE.Vector2());

  useFrame((_, rawDelta) => {
    const dt = Math.min(rawDelta, 0.05);
    for (let i = 0; i < built.peds.length; i++) {
      const ped = built.peds[i];
      const id = pedestrianId(i);
      ped.blocked = shouldStopForPedestrianObstacle(worldCollisionRegistry, {
        id,
        x: ped.group.position.x,
        y: SLAB_TOP,
        z: ped.group.position.z,
        yaw: ped.yaw,
        speed: ped.currentSpeed,
      });
      const targetSpeed = ped.blocked ? 0 : ped.speed;
      ped.currentSpeed += (targetSpeed - ped.currentSpeed) * Math.min(1, 8 * dt);
      ped.dist += ped.currentSpeed * dt;
      const targetYaw = pointOnLoop(ped.block, ped.dist, tmp.current);
      ped.group.position.set(tmp.current.x, SLAB_TOP, tmp.current.y);
      let dYaw = targetYaw - ped.yaw;
      while (dYaw > Math.PI) dYaw -= Math.PI * 2;
      while (dYaw < -Math.PI) dYaw += Math.PI * 2;
      ped.yaw += dYaw * Math.min(1, 10 * dt);
      ped.group.rotation.y = ped.yaw;
      ped.mixer.update(dt * Math.max(0.15, ped.currentSpeed / Math.max(0.1, ped.speed)));
      worldCollisionRegistry.update(id, pedestrianBounds(tmp.current.x, SLAB_TOP, tmp.current.y));
    }
  });

  useEffect(() => {
    return () => {
      for (const ped of built.peds) ped.mixer.stopAllAction();
    };
  }, [built]);

  return (
    <group>
      {built.peds.map((ped, i) => (
        <primitive key={i} object={ped.group} />
      ))}
    </group>
  );
}

useGLTF.preload(MODEL_URL);
