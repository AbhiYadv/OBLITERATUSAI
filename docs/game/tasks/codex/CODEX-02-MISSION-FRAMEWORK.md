# CODEX-02 — Implement the Minimal Mission Framework

## Prerequisite

Claude’s mission architecture plan is approved.

## Canonical inputs

- `docs/02-GAME-DESIGN-DOCUMENT.md`
- `docs/03-VERTICAL-SLICE-MERIDIAN-SIGNAL-FAILURE.md`
- `docs/06-TECHNICAL-ARCHITECTURE.md`
- approved Claude implementation plan

## Goal

Implement only the content-driven framework required by Meridian Signal Failure.

## Systems

- game-mode ownership
- world event bus
- interaction registry
- mission director
- evidence store
- dialogue director
- decision prerequisites
- world-state reactions
- AAR builder

## Rules

- no per-frame React mission state
- no backend
- no multiplayer
- no player asset changes
- no city rewrite
- one mission only
- behavior tests before broad scene integration

## Acceptance

Mission JSON loads; objectives advance deterministically; evidence gates decisions; events are ordered; save/load restores mission state; AAR is reproducible.
