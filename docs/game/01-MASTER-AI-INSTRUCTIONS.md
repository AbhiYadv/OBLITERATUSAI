# Master Instructions for Any AI

## Product
Nexo World is a third-person urban systems simulation. It borrows readable urban traversal, pedestrians, traffic, and vehicle interaction from open-world games, but not crime, combat, police, copied content, or giant empty scope.

## Scope
Work only inside:
```text
/Volumes/Crucial X9/nexo-ai-clean/.claude/worktrees/bold-tu-ab067b/apps/nexo-world
```
No Git init, branch, commit, push, or parent-repository edits unless explicitly authorized.

## Player lock
Treat these as read-only:
```text
src/gta/Character.tsx
src/gta/characters/
public/assets/characters/
assets-source/characters/
```
Do not alter the player model, face, body, clothing, scale, rig, animation behavior, movement, shadow behavior, or collision dimensions. The vehicle and world adapt to the player.

## Stable systems
Do not casually rewrite shared input, on-foot movement/camera, collision registry, pedestrian collision, traffic lights, traffic awareness, city coordinates, or deterministic Blender automation.

## Agent roles
- Fable: visual direction and Blender-ready art briefs.
- Claude: audits, architecture, dependencies, risks, and plans.
- Codex: implementation, Blender scripts, tests, profiling, and browser verification.

## Work method
Inspect current files → read canonical docs → state the gap → execute the smallest bounded task → run relevant checks → report changed files, tests, manual validation, limitations, and next gate.

## Stop when
A required asset is missing, visual direction is unapproved, the player would change, an unapproved rewrite is needed, no acceptance test exists, or the task conflicts with canonical direction.
