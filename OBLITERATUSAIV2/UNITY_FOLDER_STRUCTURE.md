# Unity Folder Structure

This structure is for the Unity project that will live at:

```text
OBLITERATUSAIV2/UnityProject
```

Do not create the Unity project manually by making folders. Create the project from Unity Hub first, then run `scripts/scaffold-unity-folders.sh`.

## Principles

- Keep all game-owned content under `Assets/_Project`.
- Keep downloaded or vendor content under `Assets/ThirdParty`.
- Do not edit third-party assets directly; wrap them with project prefabs, materials, or definitions.
- Keep scenes thin. Gameplay logic lives in scripts and data assets, not in one giant scene.
- Every replaceable model should be referenced through a definition asset, not hardcoded in code.
- Prototype with primitives first, then swap prefabs through data.
- Separate runtime code, editor tools, and tests.
- Avoid `Resources` except for tiny bootstrap-only assets.

## Structure

```text
UnityProject/
  Assets/
    _Project/
      Art/
        Characters/
        Vehicles/
        City/
        Props/
        Materials/
        Textures/
        VFX/
        UI/
      Audio/
        Ambience/
        Music/
        SFX/
      Code/
        Runtime/
          Core/
          Input/
          Player/
          Camera/
          World/
          City/
          Vehicles/
          Traffic/
          Pedestrians/
          Interactions/
          Multiplayer/
          UI/
          Save/
          Debugging/
        Editor/
        Tests/
      Data/
        AssetCatalog/
        Characters/
        Vehicles/
        Buildings/
        Props/
        Missions/
        Settings/
      Prefabs/
        Systems/
        Characters/
        Vehicles/
        City/
        Props/
        UI/
      Scenes/
        Boot/
        Gameplay/
        Test/
        Lighting/
      Settings/
        Input/
        RenderPipeline/
        Addressables/
      Tools/
        Profiling/
        Validation/
    ThirdParty/
      Kenney/
      Quaternius/
      Mixamo/
      Other/
    Plugins/
    StreamingAssets/
    Gizmos/
  Packages/
  ProjectSettings/
```

## Runtime Code Ownership

`Core`
: game mode, service registry, bootstrap, time/session state.

`Input`
: player input mapping and control ownership.

`Player`
: on-foot movement, animation state, player collision.

`Camera`
: third-person follow camera and vehicle camera.

`World`
: world loading, chunk activation, global environment.

`City`
: city block generation, roads, sidewalks, buildings, props.

`Vehicles`
: vehicle definitions, driving, enter/exit, collision, seats.

`Traffic`
: non-player traffic loops and simple obstacle awareness.

`Pedestrians`
: ambient pedestrian loops, animation, simple avoidance.

`Interactions`
: focus detection, prompts, interactable interface.

`Multiplayer`
: network ownership, player replication, room state.

`UI`
: prompts, debug HUD, menus.

`Save`
: local prototype save data only.

`Debugging`
: FPS/RAM overlay, build diagnostics, test helpers.

## Asset Replacement Pattern

Use ScriptableObject definitions for assets that may change.

Examples:

```text
Data/Vehicles/Sedan_Prototype.asset
Data/Characters/Player_Prototype.asset
Data/Buildings/Office_LowRise_A.asset
Data/Props/StreetLight_A.asset
```

Each definition should eventually track:

- id
- display name
- prefab
- expected scale
- collision dimensions
- triangle budget
- texture budget
- license
- source URL or local provenance
- approval status

Game code should reference definitions, not raw model files.

## Old Project Mapping

```text
src/gta/GtaWorld.tsx              -> Scenes + Core/GameBootstrap
src/gta/cityPlan.ts               -> City/CityPlanGenerator
src/gta/ThirdPersonController.tsx -> Player + Camera + Vehicles split
src/gta/Traffic.tsx               -> Traffic/TrafficSystem
src/gta/Pedestrians.tsx           -> Pedestrians/PedestrianSystem
src/gta/vehicle/*                 -> Vehicles
src/gta/collision/*               -> use Unity physics first, custom helpers only if needed
src/world/assets/AssetManifest.ts -> Data/AssetCatalog ScriptableObjects
public/assets/*                   -> imported under Art or ThirdParty, then wrapped by Prefabs
```

## Rules For Imported Models

- Import raw files under `Assets/ThirdParty/<Source>/...` when downloaded externally.
- Create project-owned prefabs under `Assets/_Project/Prefabs/...`.
- Create project-owned materials under `Assets/_Project/Art/Materials/...`.
- Do not build gameplay directly on imported FBX/GLB roots.
- Do not use copyrighted GTA/Vice City assets, maps, names, music, UI, or missions.
- Prefer CC0, CC-BY with attribution, owned, or properly licensed assets.
