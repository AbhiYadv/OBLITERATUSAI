#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="${1:-./UnityProject}"
ASSETS_DIR="$PROJECT_DIR/Assets"

if [ ! -d "$ASSETS_DIR" ]; then
  echo "Unity project not found at: $PROJECT_DIR"
  echo "Create the Unity project first, then run:"
  echo "  ./scripts/scaffold-unity-folders.sh \"$PROJECT_DIR\""
  exit 1
fi

mkdir -p \
  "$ASSETS_DIR/_Project/Art/Characters" \
  "$ASSETS_DIR/_Project/Art/Vehicles" \
  "$ASSETS_DIR/_Project/Art/City" \
  "$ASSETS_DIR/_Project/Art/Props" \
  "$ASSETS_DIR/_Project/Art/Materials" \
  "$ASSETS_DIR/_Project/Art/Textures" \
  "$ASSETS_DIR/_Project/Art/VFX" \
  "$ASSETS_DIR/_Project/Art/UI" \
  "$ASSETS_DIR/_Project/Audio/Ambience" \
  "$ASSETS_DIR/_Project/Audio/Music" \
  "$ASSETS_DIR/_Project/Audio/SFX" \
  "$ASSETS_DIR/_Project/Code/Runtime/Core" \
  "$ASSETS_DIR/_Project/Code/Runtime/Input" \
  "$ASSETS_DIR/_Project/Code/Runtime/Player" \
  "$ASSETS_DIR/_Project/Code/Runtime/Camera" \
  "$ASSETS_DIR/_Project/Code/Runtime/World" \
  "$ASSETS_DIR/_Project/Code/Runtime/City" \
  "$ASSETS_DIR/_Project/Code/Runtime/Vehicles" \
  "$ASSETS_DIR/_Project/Code/Runtime/Traffic" \
  "$ASSETS_DIR/_Project/Code/Runtime/Pedestrians" \
  "$ASSETS_DIR/_Project/Code/Runtime/Interactions" \
  "$ASSETS_DIR/_Project/Code/Runtime/Multiplayer" \
  "$ASSETS_DIR/_Project/Code/Runtime/UI" \
  "$ASSETS_DIR/_Project/Code/Runtime/Save" \
  "$ASSETS_DIR/_Project/Code/Runtime/Debugging" \
  "$ASSETS_DIR/_Project/Code/Editor" \
  "$ASSETS_DIR/_Project/Code/Tests" \
  "$ASSETS_DIR/_Project/Data/AssetCatalog" \
  "$ASSETS_DIR/_Project/Data/Characters" \
  "$ASSETS_DIR/_Project/Data/Vehicles" \
  "$ASSETS_DIR/_Project/Data/Buildings" \
  "$ASSETS_DIR/_Project/Data/Props" \
  "$ASSETS_DIR/_Project/Data/Missions" \
  "$ASSETS_DIR/_Project/Data/Settings" \
  "$ASSETS_DIR/_Project/Prefabs/Systems" \
  "$ASSETS_DIR/_Project/Prefabs/Characters" \
  "$ASSETS_DIR/_Project/Prefabs/Vehicles" \
  "$ASSETS_DIR/_Project/Prefabs/City" \
  "$ASSETS_DIR/_Project/Prefabs/Props" \
  "$ASSETS_DIR/_Project/Prefabs/UI" \
  "$ASSETS_DIR/_Project/Scenes/Boot" \
  "$ASSETS_DIR/_Project/Scenes/Gameplay" \
  "$ASSETS_DIR/_Project/Scenes/Test" \
  "$ASSETS_DIR/_Project/Scenes/Lighting" \
  "$ASSETS_DIR/_Project/Settings/Input" \
  "$ASSETS_DIR/_Project/Settings/RenderPipeline" \
  "$ASSETS_DIR/_Project/Settings/Addressables" \
  "$ASSETS_DIR/_Project/Tools/Profiling" \
  "$ASSETS_DIR/_Project/Tools/Validation" \
  "$ASSETS_DIR/ThirdParty/Kenney" \
  "$ASSETS_DIR/ThirdParty/Quaternius" \
  "$ASSETS_DIR/ThirdParty/Mixamo" \
  "$ASSETS_DIR/ThirdParty/Other" \
  "$ASSETS_DIR/Plugins" \
  "$ASSETS_DIR/StreamingAssets" \
  "$ASSETS_DIR/Gizmos"

echo "Unity folder structure created under: $ASSETS_DIR"
