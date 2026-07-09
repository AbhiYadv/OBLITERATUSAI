# Nexo World runtime 3D assets

Runtime models are `.glb`/`.gltf` only, served from this directory:

```text
public/assets/city/        hero buildings
public/assets/characters/  humanoid characters
public/assets/props/       street & construction props
```

The single source of truth is `src/world/assets/AssetManifest.ts`. Each entry
records path, placement, scale normalization, license, and a sourcing spec.
Entries marked `missing` render their procedural fallback; drop the file in
place and flip the status to `installed` to swap the real asset in.

## Installed

| id | file | license |
|---|---|---|
| human-casual | characters/human-casual.glb | CC-BY 4.0 — CesiumMan © Cesium (Analytical Graphics, Inc.), via [KhronosGroup/glTF-Sample-Assets](https://github.com/KhronosGroup/glTF-Sample-Assets) |

## Required (status: missing)

| id | target file | spec |
|---|---|---|
| nexo-labs-hq | city/nexo-labs-hq.glb | glass+concrete office tower, ~18×16 m, ~36 m tall, ≤15k tris |
| worker-bureau | city/worker-bureau.glb | mid-rise office, ~10×12 m, ~18 m tall, ≤10k tris |
| mcp-marketplace | city/mcp-marketplace-row.glb | 2-story storefront row, ~28 m, four glazed bays, ≤20k tris |
| preview-theater | city/preview-theater.glb | theater with marquee, ~16×12 m, ≤12k tris |
| street-tree | props/street-tree.glb | single urban tree, no ground tile, ≤2k tris |
| car-sedan | props/car-sedan.glb | modern sedan ~4.5 m, ≤8k tris |
| street-light | props/street-light.glb | single-arm pole ~5 m, ≤1.5k tris |
| tower-crane | props/tower-crane.glb | lattice crane 20–24 m, ≤12k tris |
| scaffolding-kit | props/scaffolding-kit.glb | modular scaffold bay, ≤4k tris |

Constraints for every asset: licensed CC0/CC-BY or licensed to this project,
≤1K textures, Y-up, meters, browser-friendly poly counts.

Suggested sources: [Kenney City Kit Commercial](https://kenney.nl/assets/city-kit-commercial)
(CC0 — download zip manually, export GLB), [Quaternius packs](https://quaternius.com) (CC0),
Sketchfab filtered to CC0/CC-BY. The Kenney *Starter Kit City Builder* GitHub models were
evaluated and rejected: toy/cartoon tile style, wrong fit for the modern-city target.
