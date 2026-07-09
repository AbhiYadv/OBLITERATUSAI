export const MAN_PLAYER_MODEL_URL = "/assets/characters/man-player.glb";
export const MAN_PLAYER_TARGET_HEIGHT = 1.8;

export const HUMANO_ANIMATION_INVENTORY = {
  source: "assets-source/characters/humano/Humano_Anim_045-3745-W5",
  selectedLod: "LOD1",
  textureSet: "2K",
  availableRuntimeClips: ["walk"],
  missingRuntimeClips: [
    "idle",
    "run",
    "turn left",
    "turn right",
    "jump start",
    "airborne/fall",
    "landing",
    "neutral gesture",
  ],
} as const;
