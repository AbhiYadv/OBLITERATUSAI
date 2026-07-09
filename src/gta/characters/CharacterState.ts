export type CharacterAnimationState =
  | "idle"
  | "walk"
  | "run"
  | "turnLeft"
  | "turnRight"
  | "jumpStart"
  | "fall"
  | "landing"
  | "gesture";

export interface CharacterMotionSnapshot {
  speed: number;
  grounded: boolean;
}
