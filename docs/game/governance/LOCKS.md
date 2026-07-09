# Nexo World — Hard Locks

## Player character lock
Treat as read-only:
```text
src/gta/Character.tsx
src/gta/characters/
public/assets/characters/
assets-source/characters/
```
Do not change player asset, appearance, scale, rig, animation implementation, materials, shadows, movement, or collision dimensions unless the user explicitly unlocks them.

## Stable systems
Do not casually modify GameInput, on-foot speed, sprint, jump, on-foot camera, collision-registry semantics, traffic-light timing, or the city coordinate system. A focused fix requires reproduction and regression coverage.

## Scope
Work only inside:
```text
/Volumes/Crucial X9/nexo-ai-clean/.claude/worktrees/bold-tu-ab067b/apps/nexo-world
```
No Git initialization, branch, commit, or push unless explicitly authorized.

## Role lock
Fable defines visual direction. Claude defines architecture and plans. Codex implements. Codex does not invent a new art style; Fable does not rewrite runtime architecture; Claude does not silently broaden scope.

## Prototype boundaries
No combat, weapons, wanted system, multiplayer, full economy, or large-world expansion during the vertical slice.
