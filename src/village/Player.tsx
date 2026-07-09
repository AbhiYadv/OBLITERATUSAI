/**
 * First-person player controller, adapted from the LAAS FlyCamera
 * walk/fly pattern (reduced: no bob, no pointer-lock retry loop).
 *
 * Walk mode: WASD + Shift sprint + Space jump, gravity, terrain height
 * collision from the analytic heightfield, wading near water.
 * Fly mode (F): free camera for debugging, Space/C for up/down.
 * R resets to the village spawn.
 */
import { useEffect, useRef } from "react";
import { useFrame, useThree } from "@react-three/fiber";
import * as THREE from "three";
import { PLAYER_BOUND, VILLAGE_HEIGHT, WATER_LEVEL, heightAt } from "./terrain";
import { villageStats } from "./stats";

const EYE_HEIGHT = 1.65;
const WALK_SPEED = 4.4;
const SPRINT_SPEED = 9.0;
const FLY_SPEED = 30;
const FLY_SPRINT_SPEED = 95;
const GRAVITY = 24;
const JUMP_VELOCITY = 7.6;
const ACCEL = 24;

const SPAWN = new THREE.Vector3(0, VILLAGE_HEIGHT + EYE_HEIGHT, 26);
const SPAWN_YAW = 0; // spawn south of the plaza; yaw 0 faces -z, toward the well

interface PlayerControllerProps {
  playerPos: React.RefObject<THREE.Vector3>;
}

export function PlayerController({ playerPos }: PlayerControllerProps) {
  const { camera, gl } = useThree();
  const ctl = useRef({
    keys: new Set<string>(),
    yaw: SPAWN_YAW,
    pitch: -0.06,
    velY: 0,
    horizVel: new THREE.Vector2(0, 0),
    grounded: true,
    fly: false,
    locked: false,
  });

  useEffect(() => {
    camera.position.copy(SPAWN);
    camera.rotation.order = "YXZ";
    const c = ctl.current;

    const onKeyDown = (e: KeyboardEvent) => {
      if (e.repeat) return;
      c.keys.add(e.code);
      if (e.code === "KeyF") {
        c.fly = !c.fly;
        c.velY = 0;
        villageStats.mode = c.fly ? "fly" : "walk";
      }
      if (e.code === "KeyR") {
        camera.position.copy(SPAWN);
        c.yaw = SPAWN_YAW;
        c.pitch = -0.06;
        c.velY = 0;
        c.horizVel.set(0, 0);
      }
      if (e.code === "Space") e.preventDefault();
    };
    const onKeyUp = (e: KeyboardEvent) => c.keys.delete(e.code);
    const onMouseMove = (e: MouseEvent) => {
      if (!c.locked) return;
      c.yaw -= e.movementX * 0.0023;
      c.pitch -= e.movementY * 0.0023;
      c.pitch = Math.max(-1.45, Math.min(1.45, c.pitch));
    };
    const onClick = () => {
      if (!c.locked) gl.domElement.requestPointerLock();
    };
    const onLockChange = () => {
      c.locked = document.pointerLockElement === gl.domElement;
      if (!c.locked) c.keys.clear();
    };

    window.addEventListener("keydown", onKeyDown);
    window.addEventListener("keyup", onKeyUp);
    window.addEventListener("mousemove", onMouseMove);
    gl.domElement.addEventListener("click", onClick);
    document.addEventListener("pointerlockchange", onLockChange);
    // Deterministic pose hook for screenshot tooling and tests.
    if (window.__nexoVillage) {
      window.__nexoVillage.setPose = (x, y, z, yaw, pitch) => {
        c.fly = true;
        villageStats.mode = "fly";
        camera.position.set(x, y, z);
        c.yaw = yaw;
        c.pitch = Math.max(-1.45, Math.min(1.45, pitch));
        c.velY = 0;
        c.horizVel.set(0, 0);
      };
    }
    return () => {
      window.removeEventListener("keydown", onKeyDown);
      window.removeEventListener("keyup", onKeyUp);
      window.removeEventListener("mousemove", onMouseMove);
      gl.domElement.removeEventListener("click", onClick);
      document.removeEventListener("pointerlockchange", onLockChange);
    };
  }, [camera, gl]);

  useFrame((_, rawDelta) => {
    const c = ctl.current;
    const dt = Math.min(rawDelta, 0.05);
    const pos = camera.position;

    const forwardX = -Math.sin(c.yaw);
    const forwardZ = -Math.cos(c.yaw);
    const rightX = -forwardZ;
    const rightZ = forwardX;

    let inX = 0;
    let inZ = 0;
    if (c.keys.has("KeyW")) {
      inX += forwardX;
      inZ += forwardZ;
    }
    if (c.keys.has("KeyS")) {
      inX -= forwardX;
      inZ -= forwardZ;
    }
    if (c.keys.has("KeyD")) {
      inX += rightX;
      inZ += rightZ;
    }
    if (c.keys.has("KeyA")) {
      inX -= rightX;
      inZ -= rightZ;
    }
    const inLen = Math.hypot(inX, inZ);
    if (inLen > 0) {
      inX /= inLen;
      inZ /= inLen;
    }
    const sprint = c.keys.has("ShiftLeft") || c.keys.has("ShiftRight");

    if (c.fly) {
      const speed = sprint ? FLY_SPRINT_SPEED : FLY_SPEED;
      // Pitch applies to forward motion in fly mode.
      const cosP = Math.cos(c.pitch);
      let vx = 0;
      let vy = 0;
      let vz = 0;
      if (c.keys.has("KeyW") || c.keys.has("KeyS")) {
        const dir = c.keys.has("KeyW") ? 1 : -1;
        vx += dir * forwardX * cosP;
        vy += dir * Math.sin(c.pitch);
        vz += dir * forwardZ * cosP;
      }
      if (c.keys.has("KeyD")) {
        vx += rightX;
        vz += rightZ;
      }
      if (c.keys.has("KeyA")) {
        vx -= rightX;
        vz -= rightZ;
      }
      if (c.keys.has("Space")) vy += 1;
      if (c.keys.has("KeyC")) vy -= 1;
      const len = Math.hypot(vx, vy, vz);
      if (len > 0) {
        pos.x += (vx / len) * speed * dt;
        pos.y += (vy / len) * speed * dt;
        pos.z += (vz / len) * speed * dt;
      }
      pos.y = Math.max(pos.y, heightAt(pos.x, pos.z) + 0.5);
      c.grounded = false;
    } else {
      const targetSpeed = sprint ? SPRINT_SPEED : WALK_SPEED;
      const target = new THREE.Vector2(inX * targetSpeed, inZ * targetSpeed);
      c.horizVel.lerp(target, Math.min(1, ACCEL * dt));
      pos.x += c.horizVel.x * dt;
      pos.z += c.horizVel.y * dt;

      const ground = heightAt(pos.x, pos.z);
      // Wade instead of sinking in deep water.
      const standY = Math.max(ground, WATER_LEVEL - 1.1) + EYE_HEIGHT;

      c.velY -= GRAVITY * dt;
      pos.y += c.velY * dt;
      if (pos.y <= standY) {
        pos.y = standY;
        c.velY = 0;
        c.grounded = true;
      } else {
        c.grounded = false;
      }
      if (c.grounded && c.keys.has("Space")) {
        c.velY = JUMP_VELOCITY;
        c.grounded = false;
      }
    }

    pos.x = Math.max(-PLAYER_BOUND, Math.min(PLAYER_BOUND, pos.x));
    pos.z = Math.max(-PLAYER_BOUND, Math.min(PLAYER_BOUND, pos.z));

    camera.rotation.set(c.pitch, c.yaw, 0);
    playerPos.current?.copy(pos);

    villageStats.x = pos.x;
    villageStats.y = pos.y;
    villageStats.z = pos.z;
    villageStats.grounded = c.grounded;
  });

  return null;
}
