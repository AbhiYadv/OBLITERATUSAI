# Current Project State

## Classification
Playable prototype with good technical foundations and incomplete product/gameplay integration.

## Working systems
- third-person walking, sprinting, jumping, and camera
- stable shared input lifecycle
- city roads, blocks, buildings, traffic lights, traffic, and pedestrians
- spatial-hash collision registry
- player and pedestrian collision
- traffic obstacle checks
- Blender inspect/prepare/export/validate pipeline
- TypeScript/build behavior tests

## Player
The currently rendered player is approved and frozen. The uploaded Humano W5 package was found to be female and was correctly not shipped as the male player. Do not switch the player asset.

## Pedestrians
Sidewalk-loop navigation and stop-for-obstacle behavior exist. Known limitation: stop-only movement can deadlock and rerouting remains incomplete.

## Vehicle
Low-poly cars are suitable only for traffic/reference. The truck has a richer hierarchy but is not the desired city car. A generated sedan pipeline exists, but the early asset looked boxy. Fable produced a modern compact-sedan redesign brief; implement that before final runtime integration.

## Vehicle runtime gap
The direct E-toggle spike is not acceptable: entry can occur from the wrong position, the player can disappear instantly, door sequencing and safe exit are absent, collision coverage is incomplete, and driving ownership is mixed into the player controller.

## First vertical slice
`Meridian Signal Failure`: meet agents, gather evidence, use the sedan, choose an intervention, validate recovery, watch the district react, and receive an AAR.

## Tooling note
`Stop hook failed: invalid stop hook JSON output` is a Codex workflow-hook issue, not an app failure.
