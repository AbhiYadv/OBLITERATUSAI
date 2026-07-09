# Nexo World — Game Design and Production Package

**Prepared:** 2026-07-08

This package converts the existing Nexo World browser prototype into a coherent, optimized, playable vertical slice without rebuilding working systems.

## Product decision

Nexo World is not a GTA clone. It borrows readable third-person controls, urban traversal, traffic, pedestrians, vehicles, and a living-city feel. Its unique identity is an urban systems simulation where software projects, AI agents, tools, incidents, evidence, and operational work become physical places, people, and events.

## Existing prototype to preserve

- third-person movement, sprinting, jumping, and camera
- the approved current player character
- city roads, blocks, buildings, traffic lights, traffic, and pedestrians
- shared collision registry and spatial queries
- pedestrian collision and basic vehicle awareness
- a partial drivable-vehicle spike
- Blender-to-GLB automation and validation scripts
- TypeScript/build behavior checks

## Recommended reading order

1. `docs/01-GAME-NORTH-STAR.md`
2. `docs/02-GAME-DESIGN-DOCUMENT.md`
3. `docs/03-VERTICAL-SLICE-MERIDIAN-SIGNAL-FAILURE.md`
4. `docs/04-WORLD-AND-DISTRICT-DESIGN.md`
5. `docs/05-VISUAL-BIBLE.md`
6. `docs/06-TECHNICAL-ARCHITECTURE.md`
7. `docs/07-PERFORMANCE-BUDGET.md`
8. `docs/08-PRODUCTION-ROADMAP.md`
9. `governance/LOCKS.md`
10. `governance/ACCEPTANCE-GATES.md`

## Agent role split

- **Fable:** visual direction, map composition, world mood, assets, materials, Blender-ready art briefs.
- **Claude:** deep audits, architecture, dependency mapping, risk analysis, implementation plans.
- **Codex:** code and Blender implementation, tests, profiling, asset validation, browser verification.

## Immediate execution order

1. Freeze the approved player and stable movement/input systems.
2. Establish a measurable performance baseline.
3. Approve the visual bible and Meridian District map.
4. Complete the redesigned sedan asset.
5. Implement proper vehicle entry, driving, collision, and exit.
6. Build the first mission framework and one end-to-end mission.
7. Polish only the first 400 m × 400 m district.
8. Optimize and validate before expanding the world.
