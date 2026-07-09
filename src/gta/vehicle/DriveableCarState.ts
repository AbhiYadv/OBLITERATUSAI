/**
 * Shared runtime state for Mike's driveable car. The visual component
 * (DriveableCar) reads this to place the car each frame; the third-person
 * controller writes it while driving. Kept as a module singleton so the two
 * stay in lock-step without prop threading, mirroring villageStats.
 */
import * as THREE from "three";

export interface DriveableCarState {
  /** Ground origin of the car (wheels touch here). */
  pos: THREE.Vector3;
  /** Heading; 0 faces +z. */
  yaw: number;
  /** Signed forward speed, m/s. */
  speed: number;
  /** Accumulated wheel rotation for spin. */
  wheelSpin: number;
  /** Front-wheel visual steer angle, radians. */
  steer: number;
  /** True while the player is driving. */
  occupied: boolean;
  /** True once the model has loaded and been placed. */
  ready: boolean;
  /** Normalized body length / width (metres), for collision + exit offset. */
  length: number;
  width: number;
}

export const driveableCar: DriveableCarState = {
  pos: new THREE.Vector3(-347, 17.02, 30),
  yaw: 0,
  speed: 0,
  wheelSpin: 0,
  steer: 0,
  occupied: false,
  ready: false,
  length: 4.3,
  width: 1.9,
};
