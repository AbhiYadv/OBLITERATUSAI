# Performance And Memory Budget

## Runtime Memory Target

Target: about 300 MB in built macOS player tests.

The Unity Editor will use much more memory. Judge memory only from builds.

## Rough Budget

- Unity runtime and code: 80-120 MB
- scene geometry: 30-50 MB
- textures: 60-90 MB
- characters and animations: 30-50 MB
- audio, UI, networking: 20-40 MB
- safety margin: 30-50 MB

## Rules

- Use URP.
- Use baked lighting.
- Avoid HDRP.
- Avoid high-poly assets.
- Use texture atlases.
- Prefer 512 or 1024 textures.
- Use LODs for buildings, props, vehicles, and characters.
- Pool repeated objects.
- Stream or enable city blocks by distance.
- Do not load the whole future world at once.
- Keep animated NPC count low.
- Profile every milestone.

## First Performance Gate

The first playable build should prove:

- stable movement
- stable camera
- 2 players synced
- one small district loaded
- memory near target
- acceptable frame rate on the development Mac

