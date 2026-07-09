# CODEX-01 — Capture the Existing Performance Baseline

## Mode

Measurement only. Do not optimize yet.

## Goal

Add development-only measurement hooks and produce a repeatable baseline report.

## Measure

Average FPS, 1% low FPS, draw calls, triangles, geometries, textures, active mixers, active pedestrians, active traffic, collision queries per frame, transferred bytes, and time to controllable frame.

## Benchmark route

1. stand at spawn for 15 seconds
2. walk through plaza for 15 seconds
3. sprint along main street for 15 seconds
4. approach traffic and pedestrians for 15 seconds
5. repeat after hard refresh

## Deliverables

```text
reports/performance/baseline.json
reports/performance/baseline.md
src/gta/debug/PerformanceProbe.ts
```

## Locks

No player, visual, gameplay, or Git changes. Do not optimize in this task.
