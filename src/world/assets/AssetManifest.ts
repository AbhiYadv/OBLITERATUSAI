/**
 * Runtime 3D asset registry. Files are served from public/, so every path
 * starts with /assets/. Entries with status "missing" render their procedural
 * fallback; drop the .glb in place and flip the status to "installed" to swap
 * the real asset in — no other code changes needed.
 *
 * Licensing policy: CC0, CC-BY (attribution recorded here), or assets licensed
 * to this project. Nothing else ships at runtime.
 */
export type AssetStatus = "installed" | "missing";

export interface CityAssetDefinition {
  id: string;
  path: string;
  position: [number, number, number];
  rotation: [number, number, number];
  scale: number;
  /** When set, the model is uniformly rescaled so its bounding-box height matches this (meters). */
  normalizeHeight?: number;
  status: AssetStatus;
  license: string;
  /** Sourcing spec used to procure the file while status is "missing". */
  requirement: string;
}

export const CITY_ASSETS: Record<string, CityAssetDefinition> = {
  "nexo-labs-hq": {
    id: "nexo-labs-hq",
    path: "/assets/city/nexo-labs-hq.glb",
    position: [-26, 0, -16.5],
    rotation: [0, 0, 0],
    scale: 1,
    normalizeHeight: 36,
    status: "missing",
    license: "TBD — CC0/CC-BY required",
    requirement:
      "Modern glass+concrete office tower, ~18×16 m footprint, 8–12 floors (~36 m), entrance on +z, ≤15k tris, ≤1K textures. Candidates: Kenney City Kit Commercial (CC0, kenney.nl/assets/city-kit-commercial), Quaternius Buildings packs (CC0).",
  },
  "worker-bureau": {
    id: "worker-bureau",
    path: "/assets/city/worker-bureau.glb",
    position: [-6, 0, -14.5],
    rotation: [0, 0, 0],
    scale: 1,
    normalizeHeight: 18,
    status: "missing",
    license: "TBD — CC0/CC-BY required",
    requirement:
      "Mid-rise office, ~10×12 m footprint, ~5 floors (18 m), entrance on +z, ≤10k tris, ≤1K textures.",
  },
  "mcp-marketplace": {
    id: "mcp-marketplace",
    path: "/assets/city/mcp-marketplace-row.glb",
    position: [28, 0, -15.5],
    rotation: [0, 0, 0],
    scale: 1,
    normalizeHeight: 9,
    status: "missing",
    license: "TBD — CC0/CC-BY required",
    requirement:
      "Two-story commercial storefront row, ~28 m long, four distinct glazed bays facing +z (signage bands kept clear for GitHub/Database/Docker/Browser Research), ≤20k tris, ≤1K textures.",
  },
  "preview-theater": {
    id: "preview-theater",
    path: "/assets/city/preview-theater.glb",
    position: [52, 0, -14.5],
    rotation: [0, 0, 0],
    scale: 1,
    normalizeHeight: 12,
    status: "missing",
    license: "TBD — CC0/CC-BY required",
    requirement:
      "Theater/showroom, ~16×12 m, marquee entrance on +z, ≤12k tris, ≤1K textures.",
  },
  "human-casual": {
    id: "human-casual",
    path: "/assets/characters/human-casual.glb",
    position: [0, 0, 0],
    rotation: [0, 0, 0],
    scale: 1,
    normalizeHeight: 1.8,
    status: "installed",
    license:
      "CC-BY 4.0 — CesiumMan © Cesium (Analytical Graphics, Inc.), via KhronosGroup/glTF-Sample-Assets",
    requirement:
      "Humanoid in modern casual clothing, human proportions, ≤10k tris. Upgrade candidate: Quaternius Universal Base Characters (CC0).",
  },
  "street-tree": {
    id: "street-tree",
    path: "/assets/props/street-tree.glb",
    position: [0, 0, 0],
    rotation: [0, 0, 0],
    scale: 1,
    status: "missing",
    license: "TBD — CC0/CC-BY required",
    requirement:
      "Single urban street tree (no ground tile), ≤2k tris; instanced along sidewalks by StreetProps.",
  },
  "car-sedan": {
    id: "car-sedan",
    path: "/assets/props/car-sedan.glb",
    position: [0, 0, 0],
    rotation: [0, 0, 0],
    scale: 1,
    status: "missing",
    license: "TBD — CC0/CC-BY required",
    requirement: "Low-poly modern sedan, ~4.5 m long, ≤8k tris; used for parked/street vehicles.",
  },
  "street-light": {
    id: "street-light",
    path: "/assets/props/street-light.glb",
    position: [0, 0, 0],
    rotation: [0, 0, 0],
    scale: 1,
    status: "missing",
    license: "TBD — CC0/CC-BY required",
    requirement: "Modern single-arm street light, ~5 m tall, ≤1.5k tris.",
  },
  "tower-crane": {
    id: "tower-crane",
    path: "/assets/props/tower-crane.glb",
    position: [44, 0, -32],
    rotation: [0, 0, 0],
    scale: 1,
    normalizeHeight: 22,
    status: "missing",
    license: "TBD — CC0/CC-BY required",
    requirement: "Lattice tower crane, ~20–24 m, ≤12k tris; used by ConstructionSite.",
  },
  "scaffolding-kit": {
    id: "scaffolding-kit",
    path: "/assets/props/scaffolding-kit.glb",
    position: [36, 0, -28.4],
    rotation: [0, 0, 0],
    scale: 1,
    status: "missing",
    license: "TBD — CC0/CC-BY required",
    requirement: "Modular scaffolding bay (posts, ledgers, planks), ≤4k tris; tiled by ConstructionSite.",
  },
};

export function getAsset(id: string): CityAssetDefinition {
  const def = CITY_ASSETS[id];
  if (!def) {
    throw new Error(`Unknown asset id: ${id}`);
  }
  return def;
}

export function missingAssets(): CityAssetDefinition[] {
  return Object.values(CITY_ASSETS).filter((asset) => asset.status === "missing");
}
