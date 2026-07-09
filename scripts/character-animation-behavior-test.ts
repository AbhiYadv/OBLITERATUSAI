import {
  chooseCharacterAnimation,
  mapCharacterClips,
} from "../src/gta/characters/CharacterAnimator.ts";

function expect(condition: boolean, label: string): void {
  if (!condition) throw new Error(label);
}

const mapped = mapCharacterClips(["walk"]);
expect(mapped.walk === "walk", "walk clip should map by name");
expect(mapped.idle === null, "idle must not be invented when absent");
expect(mapped.run === null, "run must not be invented when absent");
expect(mapped.jumpStart === null, "jump start must not be invented when absent");

const idleChoice = chooseCharacterAnimation(mapped, { speed: 0, grounded: true });
expect(idleChoice.requestedState === "idle", "stationary grounded character requests idle");
expect(idleChoice.clipName === "walk", "stationary fallback may hold the available walk pose");
expect(idleChoice.availableState === "walk", "fallback clip should be reported honestly");
expect(idleChoice.timeScale === 0, "stationary fallback should hold a pose, not play walking");

const walkChoice = chooseCharacterAnimation(mapped, { speed: 4.2, grounded: true });
expect(walkChoice.requestedState === "walk", "normal movement requests walk");
expect(walkChoice.clipName === "walk", "normal movement uses walk clip");
expect(walkChoice.timeScale > 0.9 && walkChoice.timeScale < 3, "walk playback should scale with movement speed");

const runChoice = chooseCharacterAnimation(mapped, { speed: 8.4, grounded: true });
expect(runChoice.requestedState === "run", "sprint speed requests run");
expect(runChoice.clipName === "walk", "run falls back to walk when no run clip exists");
expect(runChoice.availableState === "walk", "run fallback should be reported as walk");

const airChoice = chooseCharacterAnimation(mapped, { speed: 3, grounded: false });
expect(airChoice.requestedState === "fall", "airborne character requests fall");
expect(airChoice.clipName === "walk", "airborne fallback uses available walk pose when jump/fall clips are absent");
expect(airChoice.timeScale < walkChoice.timeScale, "airborne fallback should not run full walking speed");

console.log("character animation behavior checks passed");
