# Legacy Feature Parity

This record compares the old React/Three projects with the active Unity
prototype. It distinguishes missing behavior from deliberate scope changes so
future work does not blindly port superseded code.

Last reviewed: 2026-07-12.

## Restored Or Replaced

| Legacy capability | Unity status |
| --- | --- |
| `F` free-fly inspection camera | Restored with `DebugTools`; movement and interaction pause only while flying. |
| `R` reset to spawn | Restored on foot with CharacterController-safe teleport and vertical-motion reset. |
| WASD, arrow keys, either Shift key | Restored as additive aliases for walking, driving, and free-fly. |
| FPS/render statistics HUD | Restored as an `F3` runtime overlay using Unity profiler counters. |
| Structured performance baseline | Replaced by a fixed 10-second `F4` JSON capture with frame percentiles, memory, render peaks, population counts, and environment metadata. |
| River wading floor | Restored from the analytic terrain sample; speed drops to 55 percent and the player cannot sink below 1.1 m water depth. |
| Room-environment reflections | Replaced by one 128 px baked downtown cubemap assigned to a box-projected custom Reflection Probe. |
| Script behavior tests | Replaced by six Unity edit-mode tests for signal phases, stop lines, legacy RNG parity, route closure, noise, and river carving. |
| Basic traffic and sidewalk loops | Ported and extended with signal offsets, driver variation, pedestrian crossings, and population recycling. |
| Driveable-car toggle | Replaced by the safer seat, camera, exit, garage, and abandoned-car return systems. |

## Still Deferred

| Legacy capability/content | Decision |
| --- | --- |
| Runtime URL seed selection | Deferred. The Unity city, terrain meshes, textures, props, and reflection probe are editor-baked, so changing one runtime integer cannot currently rebuild the complete world safely. |
| `NexoLabsHQ`, `WorkerBureau`, `MCPMarketplace`, and `PreviewTheater` | These belonged to the alternate world prototype and were not in the active GTA scene composition, but equivalent project/tool/marketplace landmarks are highly relevant to the vibe-coding product direction. Add them as new Unity-owned landmarks rather than copying the old JSX literally. |
| Canvas-rendered billboard text | Deferred until the first landmark/content pass. Use TextMeshPro or baked sign textures, not hundreds of live text canvases. |
| Village paths, pond, farm, and social zones | Deliberately omitted when the world was compressed to the focused city prototype. Reconsider only as authored destinations. |
| Hundreds of instanced grass tufts | Deliberately omitted for clarity and budget. Add bounded verge clusters only after standalone profiling. |
| Player-following directional-light rig | Deferred. The 300 m city currently uses a fixed sun and bounded shadow distance; a moving sun anchor is unnecessary until hands-on shadow coverage proves otherwise. |
| Permanent controls-help overlay and crosshair | Not ported. Unity already presents contextual interaction prompts; controls remain in project documentation to keep the game view clean. |

## Priority

1. Hands-on validate the newly restored debug, wading, reflection, and
   performance tools.
2. Build four readable concept landmarks for projects, tools, agents, and the
   marketplace, with replaceable labels and interiors deferred.
3. Replace placeholder billboard graphics with a small pooled sign system.
4. Complete a standalone macOS memory/FPS baseline before adding vegetation or
   expanding the terrain content.
