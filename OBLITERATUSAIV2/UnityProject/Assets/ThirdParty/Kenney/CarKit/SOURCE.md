# Kenney Car Kit

- Source: https://kenney.nl/assets/car-kit
- Version: 3.1 (kit creation date 02-04-2026)
- Creator: Kenney (www.kenney.nl)
- License: CC0 1.0 Universal (crediting Kenney is appreciated, not required)
- Retrieved: 2026-07-12

## Project Import

This project retains the 50 GLB models (vehicles, wheels, and debris pieces)
plus `Textures/colormap.png` — the GLBs do not embed the palette; they
reference that file by relative path, so it must stay beside the models. The
duplicate FBX and OBJ exports are omitted. Models are imported through Unity glTFast. The authored convention
is forward = +Z with named `wheel-*` nodes, which the traffic asset builder
re-hangs on spin pivots.

Project-owned wrapper prefabs, vehicle definitions, and integration code live
under `Assets/_Project`; do not edit the imported files in place.
