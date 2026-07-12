# OBLITERATUS AI V2

Unity prototype for a small open-world multiplayer city experience.

## Direction

- Build in Unity, not Three.js, for V2.
- Target macOS first because development and testing are on Mac.
- Use Unity Personal while the project qualifies for the free tier.
- Keep the prototype small, playable, and measurable before expanding.

## First Prototype Goal

Create a compact city district where a few players can walk around, see each other, enter a simple vehicle, and interact with world objects.

## Style Target

Vice City-era inspired readability and density, not copied assets, map, UI, names, music, or missions.

The art direction should be:

- low-poly but clean
- colorful but restrained
- modular city blocks
- baked lighting
- simple materials
- strong silhouettes
- fogged distance

## Hard Constraint

Aim for about 300 MB runtime memory in built player tests.

## Unity Structure

After creating the Unity project in Unity Hub, use the canonical folder structure in `UNITY_FOLDER_STRUCTURE.md`.

Run:

```bash
./scripts/scaffold-unity-folders.sh ./UnityProject
```

## Project Guardrail

Before major design, code, asset, or architecture decisions, read `GUARDRAIL.md`.

## Project Records

- `DEVELOPMENT_LOG.md` records completed work, controls, verification state, and known limitations.
- `BUILD_PLAN.md` defines milestone order and current progress.
- `TECHNICAL_DECISIONS.md` records agreed technology choices.
- `PERFORMANCE_BUDGET.md` defines measurable runtime limits.
- `INITIAL_GAMEPLAY_REVIEW.md` compares the current city with GTA-style open-world design and ranks the next gameplay improvements.
- `LEGACY_FEATURE_PARITY.md` tracks which old-project capabilities were restored, replaced, or deliberately deferred.
