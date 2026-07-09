# Nexo World — Performance and Asset Budget

These are target budgets. Claude/Codex must measure the current baseline before optimization.

## Frame targets

| Mode | Target |
|---|---:|
| Standard quality | stable 60 FPS |
| Reduced quality | stable 30 FPS minimum |
| 60 FPS frame time | under 16.7 ms |
| 30 FPS frame time | under 33.3 ms |

## Scene budgets

| Metric | Target | Hard ceiling |
|---|---:|---:|
| Draw calls | 250 | 400 |
| Visible triangles | 800k | 1.2M |
| Active skinned characters | 12 | 16 |
| Distant simplified people | 20 | 30 |
| Dynamic shadow-casting characters | 8 | 12 |
| Real-time shadow lights | 1 directional | 2 total |

## Asset budgets

### Hero NPC
- 10k–25k triangles
- 1–2K textures
- LOD1 and LOD2 before population expansion

### Pedestrian
- near: 8k–15k triangles
- far: 1k–4k triangles
- shared materials and textures

### Hero vehicle
- 12k target, 25k hard ceiling
- 1–2K texture set if used
- separate collision proxy

### Buildings
- reusable facade kits
- static batching by block where safe
- simplified distant silhouettes

## Load budgets

| Metric | Target |
|---|---:|
| Initial playable transfer | under 25 MB |
| Full vertical-slice transfer | under 80 MB |
| Initial JS gzip | under 1.5 MB |
| Time to controllable frame on broadband | under 5 seconds |
| Slow-connection fallback | under 10 seconds |

## Optimization order

1. Measure.
2. Remove unnecessary work.
3. Reuse geometry/materials.
4. Instance repeated objects.
5. Limit update radius.
6. Add LOD.
7. Compress textures and meshes.
8. Adapt resolution/effects.
9. Consider renderer migration only after evidence.

## Runtime rules

- no per-frame React state for transforms
- no per-frame allocation in hot loops
- cap delta time
- update distant NPCs less frequently
- spatially filter collision
- reuse materials
- limit shadow maps
- use deterministic population caps
- disable distant simulation detail

## Quality tiers

### High
Higher DPR, full population, contact shadows, wider LOD range.

### Medium
Reduced DPR, fewer distant pedestrians, shorter shadow range, reduced grass.

### Low
DPR 1, no decorative contact shadows, low vegetation, simplified distance, 30 FPS target.

## Baseline report

Capture browser/device, average FPS, 1% low, draw calls, triangles, geometries, textures, heap, transfer size, largest assets, active mixers, collision queries, and time to controllable frame.
