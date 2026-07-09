/**
 * Mike's driveable car: one real GLB car (from the low-poly car pack),
 * normalized to face +z with its wheels on the ground, following the shared
 * driveableCar state each frame. It keeps a collision box registered so the
 * car is solid to people on foot; the controller's car physics ignores that
 * box while driving. Wheels spin with travel.
 */
import { useEffect, useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import { useGLTF } from "@react-three/drei";
import * as THREE from "three";
import { makeBounds, worldCollisionRegistry } from "../collision/CollisionRegistry";
import { driveableCar } from "./DriveableCarState";

const CAR_URL = "/assets/vehicles/lowpoly-cars.glb";
const CAR_LEN = 4.3;
const CAR_ID = "mike-car";
/** Rest-forward correction. 0 matches the proven convention in Traffic.tsx:
 * the same GLB + long-axis->+z normalization there drives nose-first with no
 * offset, so this car (same asset, same normalization) does too. */
const MODEL_YAW_OFFSET = 0;

export function DriveableCar() {
  const gltf = useGLTF(CAR_URL);
  const groupRef = useRef<THREE.Group>(null);

  const built = useMemo(() => {
    const src = (gltf.scene.getObjectByName("car9") ??
      gltf.scene.getObjectByName("car2") ??
      gltf.scene) as THREE.Object3D;
    const clone = src.clone(true);
    clone.updateMatrixWorld(true);

    const box = new THREE.Box3().setFromObject(clone);
    const size = box.getSize(new THREE.Vector3());
    const center = box.getCenter(new THREE.Vector3());
    const rotY = size.x > size.z ? Math.PI / 2 : 0; // long axis -> +z
    const scale = CAR_LEN / Math.max(size.x, size.z);

    clone.position.set(-center.x, -center.y, -center.z);
    const norm = new THREE.Group();
    norm.add(clone);
    norm.rotation.y = rotY;
    norm.scale.setScalar(scale);
    norm.position.y = (size.y / 2) * scale; // lift base to y=0

    clone.traverse((o) => {
      const mesh = o as THREE.Mesh;
      if (mesh.isMesh) {
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        mesh.frustumCulled = false;
      }
    });

    // Collect wheels for spin.
    const wheels: THREE.Object3D[] = [];
    clone.traverse((o) => {
      if (/whe+l|tyre|tire/i.test(o.name)) wheels.push(o);
    });
    const wheelBase = wheels.map((w) => w.rotation.x);

    // Publish real footprint to the shared state for collision / exit offset.
    const widthAxis = rotY === 0 ? size.x : size.z;
    driveableCar.length = CAR_LEN;
    driveableCar.width = widthAxis * scale;

    return { norm, wheels, wheelBase };
  }, [gltf]);

  // Register the solid box on mount; keep updated each frame.
  useEffect(() => {
    const s = driveableCar;
    worldCollisionRegistry.register({
      id: CAR_ID,
      kind: "parked-car",
      solid: true,
      dynamic: true,
      bounds: makeBounds(s.pos.x, s.pos.y + 0.7, s.pos.z, s.width / 2 + 0.15, 0.8, s.length / 2 + 0.15, s.yaw),
    });
    driveableCar.ready = true;
    return () => {
      worldCollisionRegistry.unregister(CAR_ID);
      driveableCar.ready = false;
    };
  }, []);

  useFrame(() => {
    const s = driveableCar;
    const g = groupRef.current;
    if (g) {
      g.position.copy(s.pos);
      g.rotation.y = s.yaw + MODEL_YAW_OFFSET;
    }
    for (let i = 0; i < built.wheels.length; i++) {
      built.wheels[i].rotation.x = built.wheelBase[i] + s.wheelSpin;
    }
    worldCollisionRegistry.update(
      CAR_ID,
      makeBounds(s.pos.x, s.pos.y + 0.7, s.pos.z, s.width / 2 + 0.15, 0.8, s.length / 2 + 0.15, s.yaw),
    );
  });

  return (
    <group ref={groupRef}>
      <primitive object={built.norm} />
    </group>
  );
}

useGLTF.preload(CAR_URL);
