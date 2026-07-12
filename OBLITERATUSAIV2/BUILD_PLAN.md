# Build Plan

Current milestone: **Dynamic city-life hands-on validation**

Progress is tracked with four states: planned, written, editor verified, and play tested. Detailed evidence lives in `DEVELOPMENT_LOG.md`.

## Phase 1: Unity Project Setup

Status: **Editor verified**

- Create Unity 6 LTS project.
- Choose 3D URP template.
- Set target platform to macOS.
- Use Mono scripting backend.
- Add Git-friendly Unity settings.
- Add initial empty scene.

## Phase 2: Controller And World Blockout

Status: **Editor verified; player and character play-tested; full-world hands-on test pending**

- Add third-person camera.
- Add capsule player controller.
- Build a 300 m x 300 m graybox city district.
- Add basic collision.
- Add simple lighting and fog.

## Phase 3: Multiplayer Prototype

Status: **Planned after the performance gate**

- Add Netcode for GameObjects.
- Spawn two players.
- Sync position, rotation, and simple animation state.
- Test host and client locally.

## Phase 4: Interactions

Status: **Core interaction play-tested; content interactions planned**

- Add interactable interface.
- Add prompt UI.
- Add 5 test objects.
- Add one simple interaction animation or emote.

## Phase 5: Vehicle Prototype

Status: **Editor verified; hands-on driving test pending**

- Add one low-poly car.
- Add enter and exit.
- Add simple arcade driving.
- Add chase camera.
- Prevent unsafe exit inside walls.

## Phase 6: Art Pass

Status: **First legacy-inspired pass editor verified**

- Replace graybox with modular low-poly city kit.
- Add roads, sidewalks, buildings, props, signs, and trees.
- Keep all art within memory budget.

## Phase 7: Budget Check

Status: **Pending**

- Build macOS player.
- Measure memory.
- Measure frame rate.
- Reduce assets or systems before adding scope.
