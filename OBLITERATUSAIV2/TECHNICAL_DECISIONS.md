# Technical Decisions

## Engine

Use Unity 6 LTS.

Reason: stable long-term Unity version, good macOS support, good enough tooling for a free prototype, and practical path to Windows later.

## Development Tooling

Use the free starting workflow:

- Unity Hub
- Unity 6 LTS
- Visual Studio Code
- Codex

Codex handles code, project files, docs, and debugging support from the filesystem. Unity Editor remains the place for pressing Play, inspecting scenes, placing objects visually, and assigning scene references when needed.

Do not add paid IDEs, Unity AI subscription tools, or community Unity MCP plugins until the basic prototype is working.

## Render Pipeline

Use URP, the Universal Render Pipeline.

Reason: URP is lighter than HDRP, works well on macOS and Windows, and is appropriate for stylized open-world graphics.

Avoid HDRP for this prototype.

## Scripting Backend

Start with Mono.

Reason: Mono builds faster and is easier during early iteration.

Use IL2CPP later only when preparing release-style builds or doing serious performance testing.

## First Platform

Target macOS first.

Reason: local development and testing are on Mac, so the core loop can be tested quickly without cross-platform build friction.

## Later Platforms

Windows can come later after the macOS prototype works.

For Windows from Mac:

- Mono builds can be created from macOS with Windows Build Support.
- Final Windows IL2CPP builds should be made and tested on a real Windows machine.

## Multiplayer

Start simple.

Recommended first path:

- Unity Netcode for GameObjects
- small room size
- 4 to 8 players
- server-authoritative positions where practical
- no MMO systems
- no economy
- no persistence at first

## Traffic Simulation

Use a small deterministic fleet on pre-sampled road loops for the prototype.

Reason: 24 kinematic traffic vehicles with staggered sensing are predictable, multiplayer-friendly, and substantially cheaper than one NavMesh/pathfinding agent per vehicle. Add perceived variety through driver parameters, signal offsets, vehicle visuals, and animation before increasing simulation count.

Traffic design principles are informed by Take-Two's [virtual navigation patent](https://patents.google.com/patent/US20200338450A1/en): represent roads as nodes and links, retain higher-level road knowledge, and vary traversal characteristics by vehicle or driver. This is a public design reference, not evidence of GTA V's exact implementation.

Do not add dynamic lane changing, city-wide rerouting, or population streaming until hands-on play tests and a standalone memory profile show they are needed and affordable.

## Pedestrian Simulation

Use a fixed pool of 28 kinematic pedestrians with analytic sidewalk paths,
small deterministic behavior state machines, and staggered physics sensing.

Reason: the compact grid does not need one NavMesh agent and path search per
pedestrian. A fixed pool keeps memory stable and avoids runtime instantiate and
destroy spikes, while idle actions, curb waits, crosswalk decisions, and block
migration create more perceived variety than simply increasing crowd count.

Pedestrians normally follow the inset loop around a city block. At eligible
corners, a seeded subset can continue straight across the marked crossing into
an adjacent block. They enter only during the parallel green phase with enough
time remaining, also check for a physically approaching vehicle, and hurry to
finish rather than freezing if a moving vehicle enters the crossing.

Recycle only agents beyond 110 m that are outside an expanded camera viewport,
with a 145 m hard limit behind the 95 m fog range. Place the same pooled object
on an unoccupied sidewalk 28-92 m around the player. Do not create or destroy
pedestrians during normal play.

The current CesiumMan wrapper has one usable locomotion clip. Pause,
look-around, and step-aside actions therefore use procedural root facing,
lateral movement, and a very slow animation pace. Replace these placeholders
with real idle clips through the character wrapper later; do not hard-code
model-specific bone animation into pedestrian logic.

## Development Diagnostics

Keep free-fly, spawn reset, the performance HUD, and baseline capture in two
small runtime components on one `DeveloperTools` scene object.

Reason: these are useful in Editor and standalone prototype builds without a
new package or production UI dependency. The HUD is hidden until `F3`; profiler
counters degrade to `n/a` when unavailable. `F4` records into one fixed frame
buffer and writes only after an explicit request.

Do not treat HUD memory as process RSS. It reports Unity allocated and reserved
memory; the standalone macOS process must still be measured separately for the
300 MB gate.

## Water Wading

Derive water presence from `TerrainSampler` and the known water level rather
than adding a collider to the 1024 m water plane.

Reason: only the carved river lies below the plane. The same deterministic
sample that generated the terrain can identify river water without a broad
physics trigger. Clamp the player's feet to at most 1.1 m below the surface and
reduce horizontal speed while wading; leave vehicle physics unchanged.

## Reflections

Use one 128 px HDR cubemap baked at editor time and assigned explicitly to a
box-projected custom Reflection Probe covering downtown.

Reason: a single roughly 250 KB source cubemap gives vehicles, windows, and
water local environmental response with no realtime probe rendering. Do not
add per-block or realtime probes before profiling and visual evidence justify
their cost.

## Vegetation Wind

Use one directional `WindZone`, one global controller, and a shared URP vertex-wind material for procedural trees.

Reason: Unity's [Wind Zone documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/class-WindZone.html) models natural wind with main force, turbulence, and pulses. The project's trees are ordinary procedural prefab meshes, not Terrain or SpeedTree assets, so the controller bridges those values into a project-owned shader.

Animate foliage vertices on the GPU with world-position phase variation. Keep trunks and branches static, keep sway subtle, and enable material instancing. Do not add an Animator or per-tree behavior component to hundreds of trees.
