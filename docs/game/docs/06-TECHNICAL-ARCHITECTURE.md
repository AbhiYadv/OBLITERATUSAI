# Nexo World — Technical Architecture

## Architecture decision

Preserve the current stack:

- TypeScript
- React
- React Three Fiber
- Three.js
- Vite
- Blender-generated GLB assets
- existing collision registry and spatial hash
- custom kinematic player and vehicle controllers

Do not migrate engines during the vertical slice.

## Renderer decision

Use the existing WebGL renderer as the shipping baseline. WebGPU may be evaluated later behind capability detection, but it is not a vertical-slice dependency.

## Ownership

### React layer

Owns menus, objective UI, dialogue UI, evidence UI, AAR UI, loading, and errors.

Avoid per-frame gameplay transforms in React state.

### Frame/runtime layer

Owns player, vehicle, pedestrian, and traffic transforms; collision queries; camera smoothing; animation mixers; and world reactions.

Use refs, plain TypeScript objects, and focused state stores.

## System boundaries

### PlayerController
- on-foot movement only
- sprint, jump, facing
- exposes transform
- suspends while vehicle mode owns input

### VehicleSystem
- interaction state
- door sequence
- driving
- collision
- wheel/light animation
- vehicle camera
- safe exit

### PedestrianSystem
- waypoint routes
- role schedules
- collision registration
- personal spacing
- distance-based animation

### TrafficSystem
- routes
- lights
- obstacle awareness
- driven-vehicle awareness

### InteractionSystem
- candidate registration
- proximity and facing selection
- prompt
- action dispatch

### MissionDirector
- reads mission definition
- advances objectives
- emits world events
- persists progress

### EvidenceSystem
- collected evidence
- visibility
- decision prerequisites
- AAR projection

### DialogueSystem
- role dialogue
- mission conditions
- choices and consequences

### WorldStateSystem
- signals
- signs
- gates
- blocked routes
- agent reactions
- restored district visuals

### AARSystem
- scoring
- evidence use
- safety assessment
- explanation

## Event envelope

```ts
interface WorldEvent<T = unknown> {
  id: string;
  type: string;
  source: string;
  sessionId: string;
  sequence: number;
  occurredAt: number;
  payload: T;
}
```

Representative events:

- `mission.started`
- `objective.activated`
- `interaction.completed`
- `evidence.collected`
- `dialogue.choice_selected`
- `vehicle.entered`
- `vehicle.exited`
- `intervention.applied`
- `validation.passed`
- `world.signal_restored`
- `mission.completed`

## Suggested files

```text
src/gta/gameplay/
  GameMode.ts
  GameSession.ts
  WorldEventBus.ts

src/gta/interactions/
  InteractionRegistry.ts
  InteractionPrompt.tsx

src/gta/missions/
  MissionDirector.ts
  MissionTypes.ts
  MissionLoader.ts

src/gta/evidence/
  EvidenceStore.ts
  EvidenceTypes.ts

src/gta/dialogue/
  DialogueDirector.ts
  DialogueTypes.ts

src/gta/world-state/
  WorldStateStore.ts
  WorldReactionSystem.ts

src/gta/aar/
  AarBuilder.ts
  AarPanel.tsx

public/content/missions/
  meridian-signal-failure.json
```

## Asset pipeline

1. author source in Blender
2. validate names, pivots, dimensions, and materials
3. export GLB
4. inspect and reload
5. prune and deduplicate
6. apply compatible mesh compression
7. compress textures
8. capture QA renders
9. register asset manifest

## Testing

### Unit/behavior
Mission transitions, evidence prerequisites, intervention outcomes, scoring, collision helpers, and vehicle state machine.

### Browser E2E
Focus/input, mission start, prompts, evidence collection, vehicle entry/exit, validation, and AAR.

### Visual
Deterministic screenshots for landmarks, vehicle states, incident state, and recovery state.

## Save model

For the vertical slice: local, versioned JSON stored in IndexedDB or local storage. Save mission state, evidence, settings, and world-state flags. No backend is required.

## Anti-rewrite rule

New systems integrate through explicit interfaces. They do not justify rebuilding movement, the entire city, or the scene graph.
