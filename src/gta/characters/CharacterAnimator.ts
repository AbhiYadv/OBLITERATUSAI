import * as THREE from "three";
import type { CharacterAnimationState, CharacterMotionSnapshot } from "./CharacterState";

export interface CharacterClipMap {
  idle: string | null;
  walk: string | null;
  run: string | null;
  turnLeft: string | null;
  turnRight: string | null;
  jumpStart: string | null;
  fall: string | null;
  landing: string | null;
  gesture: string | null;
}

export interface CharacterAnimationChoice {
  requestedState: CharacterAnimationState;
  availableState: CharacterAnimationState;
  clipName: string | null;
  timeScale: number;
}

const WALK_NATURAL_SPEED = 2.35;
const RUN_THRESHOLD = 6.2;
const MAX_WALK_FALLBACK_SCALE = 3.4;

export function mapCharacterClips(clipNames: readonly string[]): CharacterClipMap {
  const find = (patterns: RegExp[]) =>
    clipNames.find((name) => patterns.some((pattern) => pattern.test(name))) ?? null;
  return {
    idle: find([/idle/i, /stand/i]),
    walk: find([/walk/i, /locomotion/i]),
    run: find([/run/i, /sprint/i, /jog/i]),
    turnLeft: find([/turn.*left/i, /left.*turn/i]),
    turnRight: find([/turn.*right/i, /right.*turn/i]),
    jumpStart: find([/jump.*start/i, /^jump$/i]),
    fall: find([/fall/i, /air/i]),
    landing: find([/land/i]),
    gesture: find([/gesture/i, /wave/i, /phone/i, /talk/i]),
  };
}

export function chooseCharacterAnimation(
  clips: CharacterClipMap,
  motion: CharacterMotionSnapshot,
): CharacterAnimationChoice {
  const requestedState: CharacterAnimationState = !motion.grounded
    ? "fall"
    : motion.speed < 0.25
      ? "idle"
      : motion.speed >= RUN_THRESHOLD
        ? "run"
        : "walk";

  if (requestedState === "idle") {
    if (clips.idle) return { requestedState, availableState: "idle", clipName: clips.idle, timeScale: 1 };
    return { requestedState, availableState: "walk", clipName: clips.walk, timeScale: 0 };
  }
  if (requestedState === "run") {
    if (clips.run) {
      return {
        requestedState,
        availableState: "run",
        clipName: clips.run,
        timeScale: Math.max(0.7, motion.speed / RUN_THRESHOLD),
      };
    }
    return {
      requestedState,
      availableState: "walk",
      clipName: clips.walk,
      timeScale: Math.min(MAX_WALK_FALLBACK_SCALE, Math.max(1.2, motion.speed / WALK_NATURAL_SPEED)),
    };
  }
  if (requestedState === "fall") {
    if (clips.fall) return { requestedState, availableState: "fall", clipName: clips.fall, timeScale: 1 };
    if (clips.jumpStart) return { requestedState, availableState: "jumpStart", clipName: clips.jumpStart, timeScale: 0.8 };
    return { requestedState, availableState: "walk", clipName: clips.walk, timeScale: 0.25 };
  }
  return {
    requestedState,
    availableState: "walk",
    clipName: clips.walk,
    timeScale: Math.min(MAX_WALK_FALLBACK_SCALE, Math.max(0.35, motion.speed / WALK_NATURAL_SPEED)),
  };
}

export function fadeToAction(
  actions: Map<string, THREE.AnimationAction>,
  current: THREE.AnimationAction | null,
  clipName: string | null,
  fadeSeconds: number,
): THREE.AnimationAction | null {
  if (!clipName) return current;
  const next = actions.get(clipName);
  if (!next || next === current) return current;
  next.reset();
  next.enabled = true;
  next.play();
  if (current) current.crossFadeTo(next, fadeSeconds, false);
  return next;
}
