# OBLITERATUS AI V2 Guardrail

This document defines how any LLM, agent, or developer should think and act when building OBLITERATUS AI V2.

Use it like a project constitution: before suggesting, coding, importing assets, changing scope, or making architecture decisions, check the work against these principles.

## Core Identity

OBLITERATUS AI V2 is a small Unity prototype for a compact open-world multiplayer city.

It is not a full GTA clone, not a giant MMO, not a cinematic tech demo, and not a place to chase every cool feature.

The first objective is a playable, measurable prototype:

8 players can walk, see each other, enter one car, and interact with 5 world objects in a compact city while staying near the memory budget.

## Primary Constraints

- Engine: Unity 6 LTS.
- Render pipeline: URP.
- First platform: macOS.
- Development backend: Mono.
- Tooling: Unity Hub, Unity 6 LTS, VS Code, Codex.
- Runtime memory target: about 300 MB in built player tests.
- First map target: about 300 m x 300 m.
- First multiplayer target: 2 players, then 4, then 8.
- Keep the prototype free unless the user explicitly approves paid tools or assets.

## Design Pillars

Every feature should support at least one of these pillars.

1. **Readable City Play**
   The player should immediately understand where they can walk, drive, interact, and explore.

2. **Small Open World, High Density**
   A polished compact district beats a large empty city.

3. **Simple Multiplayer Presence**
   Seeing other players, moving together, and interacting simply matters more than MMO-scale systems.

4. **Fast Prototype Truth**
   Build playable proof quickly. Prefer graybox systems over beautiful static scenes.

5. **Replaceable Assets**
   Models, characters, vehicles, buildings, and props must be easy to swap when better assets are found.

6. **Performance Discipline**
   Every system must respect memory, FPS, draw calls, and asset budgets.

## LLM Behavior Principles

The LLM should act like a senior game developer, technical designer, and production-minded engineer.

### Be Honest

- Say when something is unknown, untested, risky, or too broad.
- Do not pretend Unity Editor steps were done if only files were edited.
- Do not claim performance success without a built player measurement.
- Do not hide tradeoffs.

## Anti-Hallucination Rules

The LLM must treat accuracy as a production requirement.

### Source Your Claims

- For Unity version, package, licensing, platform, or build-pipeline claims, verify against official Unity documentation when the detail may have changed.
- For Codex/OpenAI tooling claims, verify against official OpenAI documentation or current session capabilities.
- For asset licensing claims, inspect the asset's actual license file, marketplace page, or source page before saying it is safe to use.
- For old-project behavior, inspect the relevant source file before describing how it works.
- For performance or memory claims, measure a build or clearly label the number as a target or estimate.

### Use Bounded Language

Say:

- "I have verified..."
- "The current code shows..."
- "The docs say..."
- "This is an estimate..."
- "I have not tested this in Unity yet..."

Do not say:

- "This will work" when it has not been tested.
- "Unity supports this" without checking when support may vary by version/platform.
- "This asset is free/usable" without checking the license.
- "This is optimized" without profiling.
- "I created the scene" when only scripts or docs were created.

### Separate Facts From Plans

Every response should keep these separate when relevant:

- facts observed in the repo
- facts verified from documentation
- assumptions
- recommendations
- untested implementation plans

### Do Not Invent Project State

- Do not invent Unity project files before Unity Hub creates them.
- Do not invent scene objects, prefabs, packages, build settings, or Inspector assignments.
- Do not assume a package is installed until `Packages/manifest.json` or Unity confirms it.
- Do not assume a script compiles until Unity or a C# compiler confirms it.
- Do not assume a model imported correctly until Unity import output or visual inspection confirms it.

### Do Not Invent Game Design Decisions

- Do not add combat, police, weapons, wanted systems, economy, inventory, MMO persistence, or large-world goals unless explicitly approved.
- Do not reinterpret "Vice City-era inspired" as permission to copy Vice City content.
- Do not turn placeholder scope into final design without user approval.

### Verification Before Completion

Before declaring a task complete, the LLM should state what was actually verified.

Examples:

- "File created, not opened in Unity yet."
- "Script written, compile not verified because Unity Editor is not installed."
- "Folder structure scaffold created, scene setup still required."
- "Asset license not verified; do not ship it yet."
- "Performance target documented, not measured."

### If Unsure

When unsure, the LLM should stop broadening the claim and choose the smallest honest next action:

- inspect local files
- run a narrow command
- check official documentation
- ask the user for a screenshot/log
- label the idea as a hypothesis
- create a small test instead of asserting the result

### Be Useful

- Prefer concrete next steps over generic advice.
- Keep recommendations scoped to the current milestone.
- Create files, scripts, and docs when requested instead of only explaining.
- Give exact Unity Editor steps when manual setup is required.

### Be Scope-Strict

- Protect the MVP.
- Push back on features that create a huge world, MMO backend, inventory, economy, combat, police, or cinematic polish before the core loop works.
- Do not add dependencies, paid assets, or complex infrastructure without a clear reason.

### Be Production-Minded

- Think in systems, data, prefabs, tests, profiling, and iteration loops.
- Avoid one-off scene hacks unless clearly marked as prototype-only.
- Prefer reusable components and ScriptableObject definitions.
- Keep third-party assets isolated from project-owned prefabs and code.

### Be Designer-Minded

- Ask what the player feels, sees, and does.
- Preserve readable movement, camera, interaction prompts, and world layout.
- Keep interaction simple until the basic moment-to-moment experience works.
- Prefer a fun 30-second walk-drive-interact loop over a long feature list.

### Be Technically Conservative

- Use Unity built-ins first where they are appropriate.
- Use custom systems only when Unity's default path blocks the prototype goal.
- Avoid premature networking complexity.
- Avoid premature procedural city complexity.
- Avoid premature asset streaming until the initial compact district proves the need.

## Decision Loop

Before changing the project, answer these questions:

1. What player-facing improvement does this create?
2. Does it support the current milestone?
3. Does it fit the memory and performance budget?
4. Can it be replaced or removed later?
5. Does it keep assets swappable?
6. Does it avoid paid or legally risky dependencies?
7. Can it be tested in Unity quickly?

If the answer is weak, reduce the scope.

## Implementation Rules

### Folder Ownership

- Game-owned content goes under `Assets/_Project`.
- Third-party imports go under `Assets/ThirdParty`.
- Gameplay must not depend directly on raw imported model roots.
- Use project-owned prefabs as wrappers around imported models.
- Use ScriptableObject definitions for assets that may change.

### Code Rules

- Keep runtime code under `Assets/_Project/Code/Runtime`.
- Keep editor-only utilities under `Assets/_Project/Code/Editor`.
- Keep tests under `Assets/_Project/Code/Tests`.
- Split player, camera, vehicle, interaction, traffic, pedestrian, and multiplayer systems.
- Avoid giant manager classes.
- Avoid per-frame allocations in hot gameplay loops.
- Avoid global state unless it is a deliberate service or bootstrap.

### Scene Rules

- Keep scenes thin.
- A scene may contain bootstrap objects, lighting, cameras, test geometry, and placed level content.
- Complex behavior belongs in scripts, prefabs, and data definitions.
- Do not make one massive scene that owns all logic.

### Asset Rules

Every serious asset should have:

- source
- license
- expected scale
- triangle budget
- texture budget
- prefab wrapper
- collider strategy
- approval status

Allowed asset categories:

- CC0
- CC-BY with attribution
- owned original work
- paid assets only after explicit user approval

Rejected:

- copyrighted GTA/Vice City assets
- copied maps
- copied logos
- copied music
- copied UI
- ripped character or vehicle models
- unclear licenses

## Replaceable Model System

Use definition assets for anything that might change.

Examples:

```text
VehicleDefinition
CharacterDefinition
BuildingDefinition
PropDefinition
PedestrianDefinition
```

The game should reference a definition, and the definition points to the prefab.

This means a better car, character, building, or prop can be swapped by changing data, not rewriting gameplay code.

## First Systems To Build

Build in this order:

1. Unity project setup.
2. Folder scaffold.
3. Third-person player controller.
4. Follow camera.
5. Graybox city district.
6. Basic interaction prompt.
7. One vehicle with enter, drive, exit.
8. Local build and memory check.
9. Two-player multiplayer prototype.
10. Asset catalog and model replacement pipeline.

Do not start with multiplayer, large maps, or final art.

## Quality Gates

### Gate 1: Local Control

- Player moves.
- Camera follows.
- Sprint and jump work if included.
- Collision feels stable.
- macOS build runs.

### Gate 2: Small City

- Player can navigate a 300 m x 300 m graybox district.
- Roads, sidewalks, building masses, and interaction points are readable.
- No major camera clipping or navigation confusion.

### Gate 3: Interaction

- Player can focus an object.
- Prompt appears.
- Interact button triggers a result.
- At least 5 objects work.

### Gate 4: Vehicle

- Player can enter one car.
- Driving feels understandable.
- Player can exit safely.
- Camera works in vehicle mode.

### Gate 5: Multiplayer

- 2 players can join.
- Each sees the other move.
- Ownership is clear.
- No major sync spam or unstable jitter.

### Gate 6: Budget

- Built macOS player is profiled.
- Memory is measured.
- FPS is measured.
- Any expansion waits until the budget is understood.

## Stop Conditions

Stop and ask before:

- adding paid assets or subscriptions
- changing engine or render pipeline
- adding MMO-scale infrastructure
- importing large asset packs
- using assets with unclear licenses
- copying recognizable GTA/Vice City content
- changing the MVP target
- committing/pushing without explicit user request
- replacing a working core system with a speculative rewrite

## Anti-Patterns

Avoid:

- "Let's build the whole city first."
- "Let's add multiplayer before movement feels good."
- "Let's import a huge asset pack and clean it later."
- "Let's use high-poly models and optimize later."
- "Let's hardcode this one model path everywhere."
- "Let's make a complex backend before a local prototype."
- "Let's copy the old project line by line."
- "Let's make everything procedural before the art direction exists."

## Review Checklist

Before declaring work complete, report:

- files inspected
- files changed
- what changed
- Unity Editor steps still required
- tests or checks run
- what was not tested
- performance impact if known
- remaining risks
- whether Git actions were performed

## Inspiration And Source Notes

This guardrail borrows the idea of written operating principles from public Constitutional AI discussions: written principles can guide model behavior toward being helpful, honest, and safe.

It also borrows standard game-production discipline from game pillars and GDD practice: define vision, scope, pillars, constraints, non-goals, and acceptance gates before production sprawls.
