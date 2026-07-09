/**
 * Skinned third-person player character. Uses the rigged human GLB in the
 * asset folder; the walk clip's playback speed follows actual movement
 * speed, easing to a neutral stand at rest. The controller owns the outer
 * group transform.
 */
import { forwardRef, useEffect, useMemo } from "react";
import { useFrame } from "@react-three/fiber";
import { useGLTF } from "@react-three/drei";
import * as THREE from "three";

const MODEL_URL = "/assets/characters/human-casual.glb";
const TARGET_HEIGHT = 1.8;
/** Walk-cycle stride speed the clip was authored at (m/s), used to sync
 * foot movement with ground speed. */
const CLIP_NATURAL_SPEED = 1.55;
/** Cap so sprinting reads as a fast run, not a cartoon blur. */
const MAX_TIME_SCALE = 3.4;

export interface MotionState {
  speed: number;
  grounded: boolean;
}

interface PlayerCharacterProps {
  motion: React.RefObject<MotionState>;
  /** Extra yaw if the model's rest forward axis differs from +z. */
  modelYawOffset?: number;
}

export const PlayerCharacter = forwardRef<THREE.Group, PlayerCharacterProps>(
  function PlayerCharacter({ motion, modelYawOffset = 0 }, ref) {
    const gltf = useGLTF(MODEL_URL);

    const { scale, yOffset } = useMemo(() => {
      gltf.scene.updateMatrixWorld(true);
      const box = new THREE.Box3().setFromObject(gltf.scene);
      const size = box.getSize(new THREE.Vector3());
      const s = size.y > 0 ? TARGET_HEIGHT / size.y : 1;
      gltf.scene.traverse((o) => {
        if ((o as THREE.Mesh).isMesh) {
          o.castShadow = true;
          (o as THREE.Mesh).frustumCulled = false; // skinned bounds are unreliable
        }
      });
      return { scale: s, yOffset: -box.min.y * s };
    }, [gltf]);

    const mixer = useMemo(() => new THREE.AnimationMixer(gltf.scene), [gltf]);
    const action = useMemo(() => {
      const clip =
        gltf.animations.find((c) => /mixamo|walk/i.test(c.name)) ?? gltf.animations[0];
      const a = mixer.clipAction(clip);
      a.play();
      return a;
    }, [mixer, gltf]);

    useFrame((_, rawDelta) => {
      const dt = Math.min(rawDelta, 0.05);
      const m = motion.current;
      const speed = m ? m.speed : 0;
      const airborne = m ? !m.grounded : false;
      // Scale the walk cycle to ground speed; slow-strobe it in the air.
      const target = airborne
        ? 0.35
        : speed > 0.25
          ? Math.min(speed / CLIP_NATURAL_SPEED, MAX_TIME_SCALE)
          : 0;
      action.timeScale += (target - action.timeScale) * Math.min(1, 14 * dt);
      // When stopping, settle into the nearest neutral stance instead of
      // freezing mid-stride.
      if (target === 0 && action.timeScale < 0.2) {
        const duration = action.getClip().duration;
        const settle = action.time > duration / 2 ? duration : 0;
        action.time += (settle - action.time) * Math.min(1, 8 * dt);
      }
      mixer.update(dt);
    });

    useEffect(() => {
      return () => {
        mixer.stopAllAction();
      };
    }, [mixer]);

    return (
      <group ref={ref}>
        <group rotation-y={modelYawOffset} position-y={yOffset} scale={scale}>
          <primitive object={gltf.scene} />
        </group>
      </group>
    );
  },
);

useGLTF.preload(MODEL_URL);
