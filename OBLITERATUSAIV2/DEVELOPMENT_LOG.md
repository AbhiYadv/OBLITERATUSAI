# Development Log

This is the running implementation record for OBLITERATUS AI V2. Update it whenever code, scenes, packages, project settings, assets, or milestone status changes.

## Status Labels

- **Planned**: agreed work that has not been implemented.
- **Written**: files exist, but Unity has not verified them.
- **Editor verified**: Unity imported and compiled the work without relevant errors.
- **Play tested**: behavior was exercised in Play Mode.
- **Build tested**: behavior was exercised in a standalone player build.

## 2026-07-11 - Unity Project Baseline

Status: **Editor verified**

Completed:

- Created the Unity project under `OBLITERATUSAIV2/UnityProject`.
- Verified Unity `6000.0.79f1` on Apple Silicon.
- Verified Universal Render Pipeline package `17.0.4`.
- Verified the new Input System package `1.19.0` and active input handler.
- Created the canonical `_Project` folder scaffold.
- Confirmed the template `SampleScene` opens successfully.

Notes:

- Unity Editor memory is not the runtime memory target.
- The approximate 300 MB target must be measured using a standalone macOS build.

## 2026-07-11 - Milestone 1 Player Sandbox

Status: **Play tested**

Added runtime components:

- `PlayerMotor`: camera-relative walking, sprinting, turning, gravity, and jumping using `CharacterController`.
- `ThirdPersonCamera`: mouse orbit, follow smoothing, pitch limits, and cursor lock control.
- `IInteractable`: minimal gameplay interaction contract.
- `PlayerInteractor`: camera-center focus detection, interaction input, and contextual prompt.
- `PrototypeToggleInteractable`: test light that can be switched on and off without cloning its material.

Added editor tooling:

- `OBLITERATUS AI > Build Player Sandbox` creates the test environment.
- The generator saves `Assets/_Project/Scenes/Test/PlayerSandbox.unity`.
- The generator saves `Assets/_Project/Prefabs/Characters/PrototypePlayer.prefab`.
- The generator creates project-owned URP placeholder materials.
- The generated scene is added to Unity Build Settings.

Controls:

| Input | Action |
| --- | --- |
| `W`, `A`, `S`, `D` | Move |
| Mouse | Orbit camera |
| Left Shift | Sprint |
| Space | Jump |
| `E` | Use focused interaction |
| Escape | Release or recapture cursor |

Known limitations:

- City and interaction props still use placeholder primitives.
- Interaction UI uses prototype immediate-mode UI.
- Movement is local only and has no networking.
- No performance or runtime-memory claim has been made.

Verification checklist:

- [x] Unity imported all new scripts.
- [x] Unity compiled both runtime and editor assemblies without C# errors.
- [x] Ran the sandbox generator inside the active Unity Editor.
- [x] Confirmed the sandbox scene and player prefab were generated and imported.
- [x] Confirmed the sandbox scene was added to Unity Build Settings.
- [x] Entered Play Mode and verified the current controls and presentation with the user.
- [x] Confirmed the interaction light and player sandbox look acceptable for the prototype milestone.

Verification notes:

- The first generation attempt exposed an invalid `MaterialPropertyBlock` field initializer in `PrototypeToggleInteractable`.
- The property block is now allocated lazily from `ApplyState`, which Unity accepts during component creation.
- The generator explicitly saves project assets after updating Build Settings.
- A temporary editor auto-run hook was used to invoke the generator and then removed; future regeneration is manual through the documented menu command.

## 2026-07-11 - Old Project Character Migration

Status: **Play tested**

Selected asset:

- `human-casual.glb`, the 428 KB CesiumMan character already used by the old project.
- The model contains one skinned mesh, a 19-joint armature, and one animation clip.
- The old project's source and attribution record is preserved beside the imported model.

Implementation:

- Added Unity glTFast `6.19.0` to import GLB assets in Unity 6.
- Raw third-party content lives under `Assets/ThirdParty/Khronos/CesiumMan`.
- `PlayerSandboxBuilder` loads the model through `AssetDatabase` and places it inside the project-owned player prefab.
- The character is normalized to 1.8 m and aligned to the controller's feet.
- The capsule mesh is hidden when the character imports successfully; `CharacterController` remains responsible for collision and movement.
- The generator falls back to the visible capsule if the model cannot be loaded or has invalid bounds.
- A project-owned Animator Controller wraps the model's imported animation clip.
- `PlayerAnimationDriver` scales clip playback from the controller's horizontal movement speed and disables root motion.

Verification checklist:

- [x] Unity resolved glTFast `6.19.0` and its dependencies.
- [x] Unity imported `human-casual.glb` without relevant errors.
- [x] Regenerated the player sandbox and prefab.
- [x] Confirmed `CharacterVisual` exists under `PrototypePlayer`.
- [x] Confirmed the capsule renderer is disabled when the model is available.
- [x] Normalized the visual to a target height of 1.8 m and aligned its renderer bounds to the controller feet.
- [x] Confirmed imported clip `Clip_0` is assigned through `CesiumMan.controller`.
- [x] User confirmed the transferred character, movement, camera, and animation look acceptable for now.

## 2026-07-11 - Milestone 2 Vehicle Slice

Status: **Editor verified; Play Mode driving test pending**

Implementation scope:

- Actor-aware interactions so world objects know which player initiated an action.
- `VehicleDefinition` data asset separating vehicle tuning and visual prefab from gameplay code.
- `ArcadeVehicleController` with acceleration, reverse, speed limiting, steering, lateral grip, and braking.
- `VehicleSeat` for enter/exit, player control switching, renderer visibility, camera handoff, and safe exit checks.
- Contextual enter prompt and persistent in-vehicle exit prompt.
- Third-person camera sphere-cast collision for player and vehicle follow modes.
- Generated prototype sedan visual, wrapper prefab, and definition asset.

Vehicle controls:

| Input | Action |
| --- | --- |
| `E` | Enter or exit vehicle |
| `W`, `S` | Accelerate or reverse |
| `A`, `D` | Steer |
| Space | Brake |
| Mouse | Orbit vehicle camera |

Verification checklist:

- [x] Unity compiled all runtime and editor scripts without C# errors.
- [x] Regenerated the player sandbox in the active Unity Editor.
- [x] Confirmed `PrototypeSedan.asset`, `PrototypeSedanVisual.prefab`, and `PrototypeSedan.prefab` were generated and imported.
- [x] Confirmed the definition points to the visual prefab.
- [x] Confirmed the wrapper contains one rigidbody, box collider, arcade controller, and vehicle seat.
- [x] Confirmed the controller-definition and seat-controller references are serialized.
- [x] Confirmed the sandbox contains the vehicle wrapper prefab instance.
- [ ] Play-test driving, braking, camera collision, and safe exit behavior.

Interaction correction:

- The initial vehicle prompt required the camera-center ray to hit the car collider, which made proximity alone appear inactive.
- `PlayerInteractor` now prioritizes the aimed target and falls back to the nearest interactable within 3 m without per-frame managed allocations.
- The available seat prompt now reads `E  Sit in vehicle`.

## 2026-07-11 - Milestone 3 Legacy Visual Port

Status: **Editor and render verified; Play Mode exploration pending**

Port strategy:

- Recreated the old React/Three downtown composition in Unity rather than attempting an incompatible scene export.
- Compressed the old approximately 300 m by 700 m downtown into the agreed 300 m by 300 m prototype boundary.
- Preserved the recognizable visual language while retaining the Unity player, interaction, vehicle, and data architecture.

Static city content:

- 4 by 4 city blocks with five north-south and five east-west street lines.
- 11 m asphalt roads, 16 raised sidewalk slabs, lane dashes, and intersection crosswalks.
- 47 deterministic buildings using beige, precast, brick, and blue-glass facade families.
- Four generated repeating facade textures with dark and warm-lit window variation.
- Storefront bands, recessed dark entrances, roof caps, and rooftop AC units.
- One concrete-frame construction site with crane mast and jib.
- 60 streetlights, nine traffic-signal props, four billboards, and perimeter trees.
- Western water strip, green outer terrain, pale distance fog, and procedural daylight sky.

Legacy assets:

- Migrated `lowpoly-cars.glb`, `drivable-sedan.glb`, and `truck.glb` from the old project.
- Normalized `car9` from `lowpoly-cars.glb` into `LegacyCar9Visual.prefab`.
- Updated `PrototypeSedan.asset` to reference the legacy car visual without changing vehicle gameplay code.
- Added eight static parked-car instances using the same visual wrapper.

Performance-oriented implementation:

- Road dashes and crosswalks are one generated mesh rather than hundreds of GameObjects.
- Buildings share four facade materials and four small 128 px procedural textures.
- Static city objects are marked static for later batching and baking.
- Raw third-party files remain isolated under `Assets/ThirdParty/LegacyProject`.

Verification:

- [x] Unity compiled the port builder without C# errors.
- [x] Unity imported all three legacy vehicle GLBs through glTFast.
- [x] Regenerated and saved `PlayerSandbox.unity`.
- [x] Confirmed 47 buildings, 16 blocks, 60 streetlights, nine signals, eight parked cars, four billboards, and one construction site.
- [x] Confirmed the vehicle definition references `LegacyCar9Visual.prefab`.
- [x] Captured and visually inspected elevated and street-level Unity renders.
- [x] Corrected inherited transform scaling found in the first validation render.
- [ ] Explore the city in Play Mode and verify collisions, vehicle entry, and road navigation.
- [ ] Profile the static city in a standalone macOS build before adding crowds.

Deferred from the old project:

- Multiplayer, combat, missions, interiors, and production UI remain deferred.
- Standalone profiling remains required before increasing simulation counts or map density.

## 2026-07-11 - Dynamic City-Life Port

Status: **Automated Play Mode smoke tested; hands-on play test pending**

Runtime systems:

- Added deterministic road routes and 24 lightweight traffic vehicles using four legacy vehicle variants.
- Added a timed traffic-signal network with 100 signal heads across the city intersections.
- Added 28 animated pedestrians with sidewalk routes and project-owned definition data.
- Added moving cloud layers, surrounding terrain, river geometry, hills, rocks, and deterministic forest scattering.
- Added detailed street furniture, emissive billboards, planters, bollards, and seeded parked vehicles.
- Added shared simulation layers and deterministic random/noise helpers.

Editor pipeline:

- Added generators for traffic assets, pedestrian assets, signals, terrain, trees, clouds, and city-life detail.
- Connected `CityLifeDetailBuilder` to `LegacyCityPortBuilder`, which was the missing final handoff step.
- Removed superseded placeholder billboards, parked cars, perimeter trees, and static signals to avoid duplicate content.
- Regenerated `PlayerSandbox.unity` and both legacy-city validation previews.

Verification:

- [x] Unity imported and compiled the new runtime and editor scripts without relevant C# errors.
- [x] Automated Play Mode smoke test found exactly 24 traffic vehicles, 28 pedestrians, 100 signal heads, and one cloud system.
- [x] Captured and visually inspected updated elevated and street-level renders.
- [x] Confirmed generated character, vehicle, signal, tree, and rock assets are present.
- [ ] Play through traffic flow, signal stopping, pedestrian movement, player collision, vehicle handling, and terrain boundaries.
- [ ] Produce and profile a standalone macOS build against the 300 MB runtime-memory target.

Notes:

- Static validation renders confirm scene composition, but cannot prove movement quality or driving feel.
- Unity Editor memory is not representative of standalone player memory.

Addendum (same day, later session):

- Upgraded streetlights with a support arm and a slim pole collider (the legacy collision registry treated poles as solid).
- Added the remaining legacy construction-site detail: crane cable, hanging hook, counterweight, alternating orange/white barrier fence ring, and gate-corner cones.
- Compile-verified both script assemblies (runtime and editor) with Unity 6000.0.79f1's bundled Roslyn compiler: zero errors, zero code warnings. This validated all city-life scripts without closing the open editor.
- [x] Regenerated `PlayerSandbox.unity` with the streetlight and construction upgrades.

## 2026-07-11 - Traffic Consistency Fixes (play-test feedback)

Status: **Scene regenerated and automated Play Mode verified; hands-on feel test pending**

Play-test findings and fixes:

- **Vehicles drove backwards:** long-axis GLB normalization cannot tell nose from tail; the car2/car9/truck models face -Z after it. Their visual prefabs are now flipped 180 degrees at bake (the primitive pod already faced +Z).
- **Traffic clogged permanently:** vehicle sensing treated all static geometry as obstacles, so parked cars (whose legacy curb offset physically overlapped the traffic lane), signal posts, and bollards braked routes to a permanent stop. Sensing now brakes only for living obstacles — anything with a Rigidbody or the player's CharacterController — matching the legacy obstacle kinds; parked cars moved to a true curb-hugging offset clear of the lane.
- **Residual gridlock:** two vehicles blocked in each other's crossing corridors can never resolve; after 3 s at a full stop a vehicle now creeps for 2 s ignoring other traffic (the player and pedestrians are still respected). Route spawn positions are also spread into per-car loop segments so two cars can no longer spawn overlapping.
- **Weak physics feel:** vehicles now pitch under braking/throttle in addition to the existing corner body roll and wheel spin.
- **Pedestrian stalls (same class of bug):** benches/planters near the sidewalk walking line could stall the crowd forever; pedestrians use the same living-obstacle filter, and the furniture lines moved (trees/bins to 0.95 m inset, benches to 2.9 m) to clear the 1.9 m walking loop.
- **Stray objects on roads:** streetlights no longer spawn on cross-street asphalt at intersections; the red prototype interaction cube moved from the central intersection onto a sidewalk slab; the driveable sedan now spawns parked at the east curb instead of mid-road.

Verification:

- [x] Runtime and editor assemblies recompiled cleanly with the bundled Roslyn compiler (0 errors, 0 code warnings).
- [x] Regenerated the flipped vehicle prefabs, parked cars, furniture, streetlights, and scene.
- [ ] Hands-on play-test forward-facing traffic, free-flowing loops, and collision recovery.
- [ ] If any vehicle model still faces backwards, toggle its `flipForward` argument in `TrafficAssetBuilder.EnsureAssets` and rebuild.

Addendum (player-car facing):

- The driveable sedan's `LegacyCar9Visual` is built by a separate path in `LegacyCityPortBuilder` that was missed in the traffic flip; play testing confirmed W drove the sedan toward its visual tail. Its normalization now also flips 180 degrees, which additionally confirms the traffic flip direction was correct.
- Reminder: facing fixes are baked into prefab assets — they appear only after re-running `OBLITERATUS AI/Build Player Sandbox`, not on Play alone.

## 2026-07-11 - Home Garage, Car Respawn, Sandbox Cleanup

Status: **Scene regenerated and automated Play Mode verified**

Root cause of "still backwards": prefab timestamps show the vehicle visual prefabs were never regenerated after the facing fixes — all backwards reports so far were against the stale 11:14 assets. The facing flips only take effect after the build menu runs.

New features:

- **Home garage** at the east ring road (~x 160, z 14), opening west onto the street: driveway pad bridging the curb, three walls, door header, flat roof. The player's sedan spawns parked inside, nose out.
- **Abandoned-car respawn** (`VehicleGarageReturn` on the sedan): after the player exits away from home, a 30 s timer runs; on expiry the car disappears from where it was left and returns to its garage slot with velocity cleared. Re-entering restarts the timer, and incidental physics movement before the car has been driven cannot arm a return.
- `VehicleSeat` now exposes `IsOccupied` for the respawn logic.
- **Removed the red prototype interaction cube** (and its point light) entirely per feedback; `PrototypeToggleInteractable` stays available for future world objects.
- **Player spawn moved** from mid-road (0, -8) to the garage forecourt (157, 22).

Verification:

- [x] Fetched `origin/unitybuild`; local and remote branch tips were already identical.
- [x] Runtime and editor assemblies recompiled cleanly with no relevant C# errors.
- [x] Regenerated `PlayerSandbox.unity` and the player-vehicle prefab assets.
- [x] Confirmed the generated scene has a garage and no `PrototypeToggleInteractable` or red interaction-light object.
- [x] Automated Play Mode test occupied the sedan, exited it away from home, waited the full 30 s, and confirmed it returned to the garage.
- [x] Confirmed all 24 NPC traffic vehicles remained active during the return test.
- [ ] Hands-on test the garage exit, driving controls, manual vehicle exit, and return presentation.

## 2026-07-11 - Open-World Traffic Feel Pass

Status: **Automated Play Mode verified; hands-on feel test pending**

Design direction:

- Kept the fixed 24-car, route-distance simulation to protect the 300 MB goal; no per-car NavMesh agents or per-frame route searches were added.
- Used the node/link, road-state, and per-driver-characteristic ideas described in Take-Two's `US20200338450A1` navigation patent as design reference, without claiming this prototype reproduces proprietary GTA implementation details.
- Added four deterministic intersection phase offsets so the entire city no longer stops and launches in lockstep.
- Added seeded driver profiles with small differences in acceleration, braking, following distance, launch reaction, corner speed, and yellow-light commitment.
- Added look-ahead corner braking so vehicles do not take every bend at full cruise speed.
- Reserved a 24 m initial spawn-clear radius around the garage exit. Normal obstacle sensing still makes NPC traffic yield when the player's car enters a lane.

Verification:

- [x] Unity imported and compiled the traffic changes without relevant C# errors.
- [x] Regenerated the signal prefab and `PlayerSandbox.unity`.
- [x] Confirmed 100 signal heads use four serialized offsets: 0, 5.5, 11, and 16.5 seconds.
- [x] Confirmed 24 NPC vehicles spawned and zero spawned within 20 m of the garage exit.
- [x] After 14 seconds, all 24 vehicles had moved more than 8 m and 17 were actively driving; the remainder were queued at signal phases.
- [x] No gameplay exceptions were logged during the traffic test.
- [ ] Hands-on assess merging from the garage, queue spacing, corner speed, collisions, and whether the density feels appropriate.

## 2026-07-11 - Streetlight Direction And Tree Wind

Status: **Scene regenerated and automated render verified; hands-on feel test pending**

Streetlights:

- Corrected the builder so each lamp arm rotates from its authored local axis toward the nearest road centerline.
- Vertical-road lamps now face east or west as required; horizontal-road lamps face north or south.
- Preserved the existing sidewalk placement, slim pole collider, support arm, and static batching.

Vegetation wind:

- Added one directional `WorldWind` object with gentle main force, turbulence, and low-frequency pulses.
- Added `TreeWindController`, which publishes four shared wind values instead of updating every tree from C#.
- Added the project-owned URP `Tree Wind Lit` shader. It performs height-weighted canopy sway, faster small-amplitude flutter, world-position phase variation, and subtle per-tree color variation on the GPU.
- Kept bark static and excluded only foliage from static batching. The shared foliage material enables GPU instancing, avoiding hundreds of Animators or per-tree `Update` methods.
- The implementation follows Unity's Wind Zone model of main force, turbulence, and pulses, adapted for the project's procedural prefab meshes rather than SpeedTree/Terrain trees.

Verification:

- [x] Unity compiled the runtime, editor, and shader changes without relevant errors.
- [x] Regenerated `PlayerSandbox.unity` and all five procedural tree prefabs.
- [x] Confirmed all 40 streetlight arms face their nearest road; zero incorrect headings were found.
- [x] Confirmed 446 foliage renderers use the wind shader, remain non-static, and share the instanced material.
- [x] Confirmed the scene contains exactly one configured wind controller.
- [x] Controlled offscreen render comparison measured 887 foliage-only changed pixels between zero-wind and wind-deformed frames.
- [ ] Hands-on check that normal wind tuning reads as a small rustle rather than excessive whole-tree motion.

## 2026-07-11 - Ambient Pedestrians And Population Recycling

Status: **Automated Play Mode verified; hands-on feel test pending**

Pedestrian behavior:

- Replaced single-state rectangular walking with a deterministic four-state
  controller: walking, idling, waiting at a curb, and crossing.
- Added three inexpensive idle actions: pause, look around, and step aside.
  Step-aside movement shifts toward the block interior so an idle pedestrian
  is less likely to stop an entire sidewalk queue.
- Preserved the analytic block loops, but pedestrians can now continue through
  eligible corners on a marked crosswalk and adopt the adjacent block loop.
- Crosswalk choices are seeded per pooled pedestrian. Boundary blocks turn as
  before, so no path leaves the playable grid.
- Pedestrians start a crossing only on the compatible green signal with enough
  phase time remaining, and only when no moving vehicle is approaching the
  crossing corridor.
- A pedestrian already crossing never freezes for a vehicle. They increase to
  a configured hurry pace until the threat clears.

Vehicle response:

- NPC traffic still uses its staggered forward BoxCast, but now identifies a
  pedestrian explicitly and gives a crossing pedestrian an additional 2.6 m
  safety envelope with a more conservative target speed.
- The anti-gridlock creep rule still ignores only other NPC traffic. Player and
  pedestrian obstacles remain respected during creep recovery.
- The player-controlled car is not auto-braked. Pedestrians wait before entry
  or hurry while crossing; player collision consequences remain a later design
  decision.

Population lifecycle:

- The crowd remains a fixed pool of 28 instantiated objects. Normal play does
  not instantiate or destroy pedestrian GameObjects.
- Agents beyond 110 m are considered for recycling only outside an expanded
  camera viewport. A 145 m hard limit sits beyond the current 95 m fog range.
- Recycled agents reappear on clear sidewalk positions 28-92 m around the
  player and outside the camera view. Work is staggered to six checks and at
  most two moves every 0.4 simulation seconds.
- The population focus remains the player transform while walking or driving,
  because the player object is parented to the occupied car.

Performance and asset notes:

- Physics queries use shared non-alloc buffers and are staggered across agents.
- Routes remain scalar-distance calculations; no pedestrian NavMesh agents or
  runtime path allocations were introduced.
- The current CesiumMan asset exposes one usable locomotion clip. Idle actions
  use procedural root motion and a slow clip pace as a placeholder until real
  idle clips are added through the replaceable character wrapper.

Verification:

- [x] Unity 6000.0.79f1 imported and compiled runtime and editor assemblies
  without C# errors.
- [x] Regenerated `Pedestrian_Casual.asset`, `Pedestrian.prefab`, and
  `PlayerSandbox.unity`; the scene serializes its signal-network reference and
  all population ranges.
- [x] Added the Editor-only `PedestrianSimulationValidator`, available at
  `OBLITERATUS AI/Validation/Run Pedestrian Simulation Smoke Test`.
- [x] Automated Play Mode observed idle, curb-wait, and active-crossing states
  with all 28 pedestrians and all 24 NPC traffic vehicles present.
- [x] After a controlled player teleport, the validator observed 24 existing
  pedestrian instance IDs move to pooled off-screen spawn positions.
- [x] No gameplay exceptions were logged during the smoke test.
- [ ] Hands-on assess idle frequency, curb crowding, crossing pace, traffic
  braking distance, visible spawn popping, and behavior around the driven car.
- [ ] Profile a standalone macOS player before increasing the 28-agent pool.

## 2026-07-12 - Legacy Utility Parity Pass

Status: **Compiled, editor-tested, generated, and automated Play Mode smoke
tested; hands-on input and water-feel checks pending**

Restored development controls:

- Added `DebugTools` on one generated `DeveloperTools` object.
- `F` toggles a collision-free inspection camera. Follow movement and world
  interaction are disabled only while flying; returning adopts the current
  view rotation before resuming the third-person camera.
- `R` returns the on-foot player to the captured scene spawn with the
  CharacterController safely disabled during teleport and vertical movement
  reset.
- WASD and arrow keys now work for walking, driving, and free-fly. Either Shift
  key works for sprint/fast fly. Free-fly accepts E/Q and legacy Space/C for
  vertical motion.

Performance diagnostics:

- Added a hidden-by-default `PerformanceHud`; `F3` toggles it.
- The overlay reports smoothed FPS/frame time, main-thread time when available,
  draw calls, triangles, Unity allocated/reserved memory, active pedestrian and
  traffic counts, player mode, and position.
- `F4` starts or finishes a 10-second baseline. JSON output includes average
  FPS, one-percent-low FPS, p50/p95/p99/worst frame time, peak render counters,
  peak Unity memory, population counts, screen, Unity version, OS, CPU, GPU,
  and the measurement limitation.
- Baselines are written only on request under
  `Application.persistentDataPath/PerformanceBaselines`.

World behavior and rendering:

- Added terrain-aware river wading to `PlayerMotor`. It uses the same analytic
  sample that baked the terrain, so no 1024 m trigger collider was added.
- Wading limits foot depth to 1.1 m below the water surface and applies a 0.55
  movement multiplier. Existing ground, jump, camera, and vehicle behavior are
  unchanged outside the river.
- Added one box-projected downtown Reflection Probe with a 340 x 90 x 340 m
  influence volume, 128 px HDR resolution, and 0.75 intensity.
- The builder bakes the cubemap synchronously, switches the probe to Custom,
  and explicitly assigns the generated texture so no lighting-data asset is
  required at runtime.

Tests and verification:

- Added six edit-mode tests under `Assets/_Project/Code/Tests/Editor` for
  signal phase boundaries, stop-line geometry, legacy Mulberry32 golden values,
  deterministic closed traffic loops, value-noise behavior, and river carving.
- [x] Runtime and editor assemblies compile with zero C# errors.
- [x] Unity Test Runner: 6 tests discovered, 6 passed, 0 failed, 0 skipped.
- [x] Regenerated `PlayerSandbox.unity` and `PrototypePlayer.prefab` with the
  new systems and serialized wading values.
- [x] Baked `DowntownReflectionProbe.exr` at 768 x 128 (six 128 px faces), about
  250 KB; converted validation preview was nonblank and showed downtown in all
  directions.
- [x] The scene's Custom probe holds an explicit cubemap asset reference.
- [x] Existing automated Play Mode smoke test still passed with 28 pedestrians,
  24 traffic vehicles, idle/wait/cross behavior, and 24 pooled recycles.
- [x] No gameplay exceptions were logged during the smoke test.
- [ ] Hands-on verify F/R/F3/F4, fly-camera transition, river entry/exit, and
  whether 55 percent wading speed feels correct.
- [ ] Record the first standalone macOS baseline; Editor memory is not the
  300 MB target measurement.

Parity detail and intentionally deferred old content are recorded in
`LEGACY_FEATURE_PARITY.md`.
