/**
 * GTA-style third-person controller: mouse-orbit camera, WASD movement
 * relative to the camera, character turns toward travel direction, sprint,
 * jump, gravity, terrain + city-slab ground collision, and AABB push-out
 * against buildings. F switches to a free-fly debug camera, R resets.
 */
import { useEffect, useRef } from "react";
import { useFrame, useThree } from "@react-three/fiber";
import * as THREE from "three";
import { PLAYER_BOUND, WATER_LEVEL, heightAt } from "../village/terrain";
import { villageStats } from "../village/stats";
import { cityGroundAt, type CityPlan } from "./cityPlan";
import type { MotionState } from "./Character";
import { makeBounds, makeObject, resolveHorizontalCapsule, worldCollisionRegistry } from "./collision/CollisionRegistry";
import { getGameInput } from "./input/GameInput";
import { driveableCar } from "./vehicle/DriveableCarState";

const WALK_SPEED = 4.2;
const SPRINT_SPEED = 8.4;
const ACCEL = 20;
const GRAVITY = 24;
const JUMP_VELOCITY = 7.4;
const TURN_RATE = 11;
const CAM_DIST = 5.6;
const CAM_HEIGHT = 1.75;
const CHAR_RADIUS = 0.42;
const CHAR_HALF_HEIGHT = 0.9;
const FLY_SPEED = 30;
const FLY_SPRINT = 95;
const PLAYER_COLLISION_ID = "player:main";
const PLAYER_IGNORE_KINDS = new Set(["player"]);
// The car collides with buildings and street props but drives past people
// and other cars (and never against its own box).
const CAR_IGNORE_KINDS = new Set(["player", "pedestrian", "moving-car", "parked-car"]);
const ENTER_RANGE = 3.8; // metres from the car to board it

// Arcade car handling, tuned for a grounded rather than twitchy feel.
const CAR_ENGINE = 10;
const CAR_BRAKE = 26;
const CAR_COAST = 6;
const CAR_MAX_FWD = 18;
const CAR_MAX_REV = 6;
const CAR_TURN = 1.5;
const CAR_CAM_DIST = 8.5;
const CAR_CAM_HEIGHT = 3.6;

const SPAWN = new THREE.Vector3(-345, 17.2, 22);

interface Props {
  charGroup: React.RefObject<THREE.Group | null>;
  playerPos: React.RefObject<THREE.Vector3>;
  motion: React.RefObject<MotionState>;
  plan: CityPlan;
}

export function ThirdPersonController({ charGroup, playerPos, motion, plan }: Props) {
  const { camera, gl } = useThree();
  const ctl = useRef({
    camYaw: 0, // camera south of spawn, looking north into the tower core
    camPitch: 0.24,
    tYaw: 0, // mouse writes these targets; actual angles ease toward them
    tPitch: 0.24,
    camCur: new THREE.Vector3(),
    camInit: false,
    charYaw: 0,
    prevCodes: new Set<string>(),
    horizVel: new THREE.Vector2(),
    velY: 0,
    grounded: true,
    fly: false,
    driving: false,
    locked: false,
    pos: SPAWN.clone(),
  });

  useEffect(() => {
    const c = ctl.current;
    const input = getGameInput();
    const releaseInput = input.acquire();
    input.setControllerMounted(true);
    camera.rotation.order = "YXZ";

    const onMouseMove = (e: MouseEvent) => {
      if (!c.locked) return;
      c.tYaw -= e.movementX * 0.0021;
      c.tPitch += e.movementY * 0.0021;
      c.tPitch = Math.max(-0.35, Math.min(1.1, c.tPitch));
    };
    const onClick = () => {
      if (!c.locked) gl.domElement.requestPointerLock();
    };
    const onLockChange = () => {
      c.locked = document.pointerLockElement === gl.domElement;
    };

    window.addEventListener("mousemove", onMouseMove);
    gl.domElement.addEventListener("click", onClick);
    document.addEventListener("pointerlockchange", onLockChange);

    if (window.__nexoVillage) {
      window.__nexoVillage.setPose = (x, y, z, yaw, pitch) => {
        c.fly = true;
        villageStats.mode = "fly";
        c.camYaw = yaw;
        c.camPitch = -pitch;
        c.tYaw = yaw;
        c.tPitch = -pitch;
        camera.position.set(x, y, z);
        camera.rotation.set(pitch, yaw, 0);
      };
    }
    return () => {
      window.removeEventListener("mousemove", onMouseMove);
      gl.domElement.removeEventListener("click", onClick);
      document.removeEventListener("pointerlockchange", onLockChange);
      c.prevCodes.clear();
      input.setControllerMounted(false);
      releaseInput();
    };
  }, [camera, gl]);

  useEffect(() => {
    worldCollisionRegistry.register(
      makeObject(
        PLAYER_COLLISION_ID,
        "player",
        { x: SPAWN.x, y: SPAWN.y + CHAR_HALF_HEIGHT, z: SPAWN.z },
        { x: CHAR_RADIUS, y: CHAR_HALF_HEIGHT, z: CHAR_RADIUS },
        0,
        true,
      ),
    );
    return () => worldCollisionRegistry.unregister(PLAYER_COLLISION_ID);
  }, []);

  const groundAt = (x: number, z: number): number => {
    return Math.max(heightAt(x, z), cityGroundAt(x, z, plan));
  };

  useFrame((_, rawDelta) => {
    const c = ctl.current;
    const dt = Math.min(rawDelta, 0.05);
    const input = getGameInput();
    const down = (code: string) => input.isPressed(code);
    const justPressed = (code: string) => down(code) && !c.prevCodes.has(code);
    const rememberPressedCodes = () => {
      c.prevCodes = new Set(input.pressedCodes());
    };

    // Ease look angles toward the mouse targets: smooth, jitter-free look.
    const look = Math.min(1, 22 * dt);
    c.camYaw += (c.tYaw - c.camYaw) * look;
    c.camPitch += (c.tPitch - c.camPitch) * look;

    if (justPressed("KeyF") && !c.driving) {
      c.fly = !c.fly;
      villageStats.mode = c.fly ? "fly" : "walk";
      if (c.fly) camera.position.y = Math.max(camera.position.y, c.pos.y + 4);
    }
    if (justPressed("KeyR")) {
      c.pos.copy(SPAWN);
      c.velY = 0;
      c.horizVel.set(0, 0);
      c.fly = false;
      c.driving = false;
      driveableCar.occupied = false;
      villageStats.mode = "walk";
    }

    // Enter / exit Mike's car with E.
    if (justPressed("KeyE") && !c.fly) {
      if (c.driving) {
        c.driving = false;
        driveableCar.occupied = false;
        driveableCar.speed = 0; // leave the car parked, not drifting
        // Step out onto the left of the car.
        const leftX = -Math.cos(driveableCar.yaw);
        const leftZ = Math.sin(driveableCar.yaw);
        const off = driveableCar.width / 2 + 1.1;
        c.pos.set(driveableCar.pos.x + leftX * off, driveableCar.pos.y, driveableCar.pos.z + leftZ * off);
        c.velY = 0;
        c.horizVel.set(0, 0);
        c.charYaw = driveableCar.yaw;
        villageStats.mode = "walk";
      } else if (driveableCar.ready) {
        const dx = c.pos.x - driveableCar.pos.x;
        const dz = c.pos.z - driveableCar.pos.z;
        if (Math.hypot(dx, dz) < ENTER_RANGE) {
          c.driving = true;
          driveableCar.occupied = true;
          driveableCar.speed = 0;
          villageStats.mode = "drive";
        }
      }
    }

    // Driving: arcade car handling with a chase camera.
    if (c.driving) {
      const car = driveableCar;
      const throttle =
        (down("KeyW") || down("ArrowUp") ? 1 : 0) - (down("KeyS") || down("ArrowDown") ? 1 : 0);
      const steerIn =
        (down("KeyA") || down("ArrowLeft") ? 1 : 0) - (down("KeyD") || down("ArrowRight") ? 1 : 0);
      const boost = down("ShiftLeft") || down("ShiftRight") ? 1.5 : 1;

      let sp = car.speed;
      if (throttle > 0) sp += CAR_ENGINE * boost * dt;
      else if (throttle < 0) sp -= (sp > 0.2 ? CAR_BRAKE : CAR_ENGINE * 0.6) * dt;
      else sp -= Math.sign(sp) * Math.min(Math.abs(sp), CAR_COAST * dt);
      sp = Math.max(-CAR_MAX_REV, Math.min(CAR_MAX_FWD * boost, sp));

      // Steering eases in from a standstill and sheds a little authority at
      // high speed, so the car feels planted instead of darty. Reverses in
      // reverse.
      const absSp = Math.abs(sp);
      const turnGain = Math.min(1, absSp / 4) * (1 - Math.min(0.4, Math.max(0, absSp - 9) * 0.03));
      car.yaw += steerIn * CAR_TURN * turnGain * Math.sign(sp || 1) * dt;
      car.steer += (steerIn * 0.5 - car.steer) * Math.min(1, 10 * dt);

      const fwdX = Math.sin(car.yaw);
      const fwdZ = Math.cos(car.yaw);
      const curr = { x: car.pos.x, y: car.pos.y + 0.7, z: car.pos.z };
      const des = { x: car.pos.x + fwdX * sp * dt, y: curr.y, z: car.pos.z + fwdZ * sp * dt };
      const res = resolveHorizontalCapsule(worldCollisionRegistry, {
        current: curr,
        desired: des,
        radius: Math.max(car.width / 2, 1.0),
        halfHeight: 0.7,
        ignoreKinds: CAR_IGNORE_KINDS,
      });
      car.pos.x = Math.max(-PLAYER_BOUND, Math.min(PLAYER_BOUND, res.position.x));
      car.pos.z = Math.max(-PLAYER_BOUND, Math.min(PLAYER_BOUND, res.position.z));
      if (res.collided) sp *= 0.35;
      car.pos.y = groundAt(car.pos.x, car.pos.z);
      car.speed = sp;
      car.wheelSpin += (sp / 0.36) * dt;

      // Player rides along; keep the collision box, light and terrain LOD
      // centred on the car, and hide the on-foot character.
      c.pos.copy(car.pos);
      worldCollisionRegistry.update(
        PLAYER_COLLISION_ID,
        makeBounds(car.pos.x, car.pos.y + CHAR_HALF_HEIGHT, car.pos.z, CHAR_RADIUS, CHAR_HALF_HEIGHT, CHAR_RADIUS, 0),
      );
      if (charGroup.current) charGroup.current.visible = false;

      const camX = car.pos.x - fwdX * CAR_CAM_DIST;
      const camZ = car.pos.z - fwdZ * CAR_CAM_DIST;
      let camY = car.pos.y + CAR_CAM_HEIGHT;
      camY = Math.max(camY, groundAt(camX, camZ) + 0.6);
      if (!c.camInit) {
        c.camCur.set(camX, camY, camZ);
        c.camInit = true;
      } else {
        const f = Math.min(1, 6 * dt);
        c.camCur.x += (camX - c.camCur.x) * f;
        c.camCur.y += (camY - c.camCur.y) * f;
        c.camCur.z += (camZ - c.camCur.z) * f;
      }
      camera.position.copy(c.camCur);
      camera.lookAt(car.pos.x, car.pos.y + 1.1, car.pos.z);

      playerPos.current?.copy(car.pos);
      villageStats.x = car.pos.x;
      villageStats.y = car.pos.y;
      villageStats.z = car.pos.z;
      if (motion.current) motion.current.speed = 0;
      rememberPressedCodes();
      return;
    }

    if (c.fly) {
      // Free-fly debug camera; the character stays put.
      const speed = down("ShiftLeft") || down("ShiftRight") ? FLY_SPRINT : FLY_SPEED;
      const dir = new THREE.Vector3();
      if (down("KeyW") || down("ArrowUp")) dir.z -= 1;
      if (down("KeyS") || down("ArrowDown")) dir.z += 1;
      if (down("KeyA") || down("ArrowLeft")) dir.x -= 1;
      if (down("KeyD") || down("ArrowRight")) dir.x += 1;
      dir.normalize().applyEuler(camera.rotation);
      if (down("Space")) dir.y += 1;
      if (down("KeyC")) dir.y -= 1;
      camera.position.addScaledVector(dir, speed * dt);
      camera.rotation.set(-c.camPitch, c.camYaw, 0);
      playerPos.current?.copy(camera.position);
      villageStats.x = camera.position.x;
      villageStats.y = camera.position.y;
      villageStats.z = camera.position.z;
      if (motion.current) motion.current.speed = 0;
      rememberPressedCodes();
      return;
    }

    // Camera-relative input.
    const fx = -Math.sin(c.camYaw);
    const fz = -Math.cos(c.camYaw);
    let inX = 0;
    let inZ = 0;
    if (down("KeyW") || down("ArrowUp")) {
      inX += fx;
      inZ += fz;
    }
    if (down("KeyS") || down("ArrowDown")) {
      inX -= fx;
      inZ -= fz;
    }
    if (down("KeyD") || down("ArrowRight")) {
      inX += -fz;
      inZ += fx;
    }
    if (down("KeyA") || down("ArrowLeft")) {
      inX -= -fz;
      inZ -= fx;
    }
    const inLen = Math.hypot(inX, inZ);
    if (inLen > 0) {
      inX /= inLen;
      inZ /= inLen;
    }
    const sprint = down("ShiftLeft") || down("ShiftRight");
    const targetSpeed = inLen > 0 ? (sprint ? SPRINT_SPEED : WALK_SPEED) : 0;
    c.horizVel.x += (inX * targetSpeed - c.horizVel.x) * Math.min(1, ACCEL * dt);
    c.horizVel.y += (inZ * targetSpeed - c.horizVel.y) * Math.min(1, ACCEL * dt);
    const currentPos = { x: c.pos.x, y: c.pos.y + CHAR_HALF_HEIGHT, z: c.pos.z };
    const desiredPos = {
      x: c.pos.x + c.horizVel.x * dt,
      y: c.pos.y + CHAR_HALF_HEIGHT,
      z: c.pos.z + c.horizVel.y * dt,
    };
    const resolved = resolveHorizontalCapsule(worldCollisionRegistry, {
      current: currentPos,
      desired: desiredPos,
      radius: CHAR_RADIUS,
      halfHeight: CHAR_HALF_HEIGHT,
      ignoreKinds: PLAYER_IGNORE_KINDS,
    });
    c.pos.x = resolved.position.x;
    c.pos.z = resolved.position.z;
    if (resolved.collided && dt > 0) {
      c.horizVel.x = (c.pos.x - currentPos.x) / dt;
      c.horizVel.y = (c.pos.z - currentPos.z) / dt;
    }

    c.pos.x = Math.max(-PLAYER_BOUND, Math.min(PLAYER_BOUND, c.pos.x));
    c.pos.z = Math.max(-PLAYER_BOUND, Math.min(PLAYER_BOUND, c.pos.z));

    // Vertical: gravity, jump, ground snap (wade in deep water).
    const ground = Math.max(groundAt(c.pos.x, c.pos.z), WATER_LEVEL - 1.1);
    c.velY -= GRAVITY * dt;
    c.pos.y += c.velY * dt;
    if (c.pos.y <= ground) {
      c.pos.y = ground;
      c.velY = 0;
      c.grounded = true;
    } else {
      c.grounded = false;
    }
    if (c.grounded && down("Space")) {
      c.velY = JUMP_VELOCITY;
      c.grounded = false;
    }

    // Turn the character toward its travel direction.
    const speed = Math.hypot(c.horizVel.x, c.horizVel.y);
    if (speed > 0.4 && inLen > 0) {
      const targetYaw = Math.atan2(c.horizVel.x, c.horizVel.y);
      let dYaw = targetYaw - c.charYaw;
      while (dYaw > Math.PI) dYaw -= Math.PI * 2;
      while (dYaw < -Math.PI) dYaw += Math.PI * 2;
      c.charYaw += dYaw * Math.min(1, TURN_RATE * dt);
    }

    const char = charGroup.current;
    if (char) {
      char.visible = true;
      char.position.copy(c.pos);
      char.rotation.y = c.charYaw;
    }
    worldCollisionRegistry.update(
      PLAYER_COLLISION_ID,
      makeBounds(c.pos.x, c.pos.y + CHAR_HALF_HEIGHT, c.pos.z, CHAR_RADIUS, CHAR_HALF_HEIGHT, CHAR_RADIUS, 0),
    );
    if (motion.current) {
      motion.current.speed = speed;
      motion.current.grounded = c.grounded;
    }

    // Orbit camera with ground clamp and positional damping, so the camera
    // glides behind the character instead of snapping rigidly.
    const cosP = Math.cos(c.camPitch);
    const camX = c.pos.x + Math.sin(c.camYaw) * cosP * CAM_DIST;
    const camZ = c.pos.z + Math.cos(c.camYaw) * cosP * CAM_DIST;
    let camY = c.pos.y + CAM_HEIGHT + Math.sin(c.camPitch) * CAM_DIST;
    camY = Math.max(camY, groundAt(camX, camZ) + 0.4);
    if (!c.camInit) {
      c.camCur.set(camX, camY, camZ);
      c.camInit = true;
    } else {
      const follow = Math.min(1, 14 * dt);
      c.camCur.x += (camX - c.camCur.x) * follow;
      c.camCur.y += (camY - c.camCur.y) * follow;
      c.camCur.z += (camZ - c.camCur.z) * follow;
    }
    camera.position.copy(c.camCur);
    camera.lookAt(c.pos.x, c.pos.y + 1.45, c.pos.z);

    playerPos.current?.copy(c.pos);
    villageStats.x = c.pos.x;
    villageStats.y = c.pos.y;
    villageStats.z = c.pos.z;
    villageStats.grounded = c.grounded;
    rememberPressedCodes();
  });

  return null;
}
