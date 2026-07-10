# Technical Decisions

## Engine

Use Unity 6 LTS.

Reason: stable long-term Unity version, good macOS support, good enough tooling for a free prototype, and practical path to Windows later.

## Render Pipeline

Use URP, the Universal Render Pipeline.

Reason: URP is lighter than HDRP, works well on macOS and Windows, and is appropriate for stylized open-world graphics.

Avoid HDRP for this prototype.

## Scripting Backend

Start with Mono.

Reason: Mono builds faster and is easier during early iteration.

Use IL2CPP later only when preparing release-style builds or doing serious performance testing.

## First Platform

Target macOS first.

Reason: local development and testing are on Mac, so the core loop can be tested quickly without cross-platform build friction.

## Later Platforms

Windows can come later after the macOS prototype works.

For Windows from Mac:

- Mono builds can be created from macOS with Windows Build Support.
- Final Windows IL2CPP builds should be made and tested on a real Windows machine.

## Multiplayer

Start simple.

Recommended first path:

- Unity Netcode for GameObjects
- small room size
- 4 to 8 players
- server-authoritative positions where practical
- no MMO systems
- no economy
- no persistence at first

