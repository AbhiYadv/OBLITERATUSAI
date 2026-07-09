# Modern Tools and Pipeline

## Keep the current engine stack
React Three Fiber + Three.js + Vite is adequate for the vertical slice. Do not migrate engines without measured evidence.

## R3F practices
Use shared geometry/materials, loader caching, instancing, LOD, adaptive DPR, progressive loading, and distance-limited updates.

## GLB optimization
Use glTF Transform after export:
```sh
gltf-transform inspect input.glb
gltf-transform optimize input.glb output.glb --texture-compress webp
gltf-transform meshopt input.glb output.glb --level medium
```
Evaluate KTX2/Basis Universal after validating loader configuration and image quality.

## Browser testing
Use Playwright for focus/input, mission flow, vehicle entry/exit, screenshot regression, and Chromium/WebKit/Firefox smoke tests.

## WebGPU
Keep WebGL as the shipping baseline. Evaluate WebGPU later behind feature detection.

## Blender
Continue deterministic inspect → prepare → validate → export → reload → render scripts. Generated files must be reproducible from scripts.

## Profiling
Use `renderer.info`, Chrome Performance, the network panel, fixed benchmark routes, and deterministic scene positions.

## Avoid
Engine migration, full ECS rewrite, heavy rigid-body physics without evidence, procedural art that ignores the visual bible, and assets with uncertain licenses.
