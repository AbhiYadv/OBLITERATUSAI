using System.Collections.Generic;
using ObliteratusAI.City;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// Assembles whole buildings from the Quaternius Downtown MegaKit's
    /// modular wall system (2 m x 3 m facade tiles, facade plane at local
    /// z = 0 facing +Z, base at y = 0) and bakes every design into one
    /// multi-submesh mesh asset plus prefab. A baked building costs a
    /// handful of draw calls instead of hundreds of module GameObjects,
    /// so the whole map can use kit art.
    /// </summary>
    internal static class QuaterniusModularBuildingBaker
    {
        private const string SourceRoot = QuaterniusCityImportProcessor.SourceRoot;
        private const string ModelFolder = SourceRoot + "/Models";
        private const string MeshFolder = "Assets/_Project/Art/City/Quaternius";
        private const string PrefabFolder = "Assets/_Project/Prefabs/City/Quaternius";

        private const float Tile = 2f;
        private const float RowHeight = 3f;
        private const float CorniceHeight = 1f;

        private enum Family
        {
            Brick,
            Metal,
            Trim
        }

        private readonly struct Design
        {
            public readonly string Id;
            public readonly Family Family;
            public readonly int WidthTiles;   // full side length in 2 m tiles
            public readonly int DepthTiles;
            public readonly int Rows;         // 3 m floor rows including ground
            public readonly bool PaleBrick;

            public Design(string id, Family family, int widthTiles, int depthTiles, int rows, bool paleBrick = false)
            {
                Id = id;
                Family = family;
                WidthTiles = widthTiles;
                DepthTiles = depthTiles;
                Rows = rows;
                PaleBrick = paleBrick;
            }
        }

        // Footprints target the 20-26 m mid-rise lots and the ~38 m full-block
        // tower lots of the canonical grid (heights are measured after bake).
        private static readonly Design[] Designs =
        {
            new Design("Brick_S", Family.Brick, 11, 11, 4),
            new Design("Brick_M", Family.Brick, 11, 13, 6, true),
            new Design("Brick_L", Family.Brick, 13, 13, 8),
            new Design("Brick_T", Family.Brick, 13, 13, 10, true),
            new Design("Trim_S", Family.Trim, 11, 11, 5),
            new Design("Trim_M", Family.Trim, 13, 11, 7),
            new Design("Metal_M", Family.Metal, 11, 11, 9),
            new Design("Metal_L", Family.Metal, 13, 13, 12),
            new Design("Metal_T1", Family.Metal, 15, 15, 18),
            new Design("Metal_T2", Family.Metal, 17, 17, 24)
        };

        private sealed class ModulePlacement
        {
            public GameObject Asset;
            public Vector3 Position;
            public float Yaw;
            public float Scale = 1f;
            public Vector3? InteriorCubeSize; // when set, bake a builtin cube instead of a module
        }

        public static List<CityVisualSet.BuildingVariant> BakeVariants(
            IReadOnlyDictionary<string, Material> materials,
            System.Func<string, IReadOnlyDictionary<string, Material>, Material> resolveMaterial)
        {
            EditorBuildUtility.EnsureFolder(MeshFolder);
            EditorBuildUtility.EnsureFolder(PrefabFolder);
            EnsureReadableModules();

            var variants = new List<CityVisualSet.BuildingVariant>(Designs.Length);
            foreach (Design design in Designs)
            {
                GameObject prefab = BakeDesign(design, materials, resolveMaterial, out float height);
                if (prefab == null)
                {
                    continue;
                }

                variants.Add(new CityVisualSet.BuildingVariant(
                    design.Id,
                    prefab,
                    height,
                    design.WidthTiles * Tile,
                    design.DepthTiles * Tile,
                    true));
            }

            return variants;
        }

        private static GameObject BakeDesign(
            Design design,
            IReadOnlyDictionary<string, Material> materials,
            System.Func<string, IReadOnlyDictionary<string, Material>, Material> resolveMaterial,
            out float height)
        {
            height = design.Rows * RowHeight + CorniceHeight;
            List<ModulePlacement> placements = ComposeBuilding(design);
            if (placements == null || placements.Count == 0)
            {
                return null;
            }

            // Palette variation: pale-brick designs remap the red-brick
            // sources to the pale material so the same modules read as a
            // second facade family across the map.
            System.Func<string, IReadOnlyDictionary<string, Material>, Material> resolve = resolveMaterial;
            if (design.PaleBrick)
            {
                resolve = (sourceName, map) =>
                    sourceName != null
                    && sourceName.StartsWith("MI_RedBrick", System.StringComparison.OrdinalIgnoreCase)
                    && map.TryGetValue("MI_RedBrick_Pale", out Material pale)
                        ? pale
                        : resolveMaterial(sourceName, map);
            }

            // Group every module submesh by its resolved project material.
            var perMaterial = new Dictionary<Material, List<CombineInstance>>();
            foreach (ModulePlacement placement in placements)
            {
                Matrix4x4 world = Matrix4x4.TRS(
                    placement.Position,
                    Quaternion.Euler(0f, placement.Yaw, 0f),
                    Vector3.one * placement.Scale);
                AppendModule(placement, world, materials, resolve, perMaterial);
            }

            if (perMaterial.Count == 0)
            {
                Debug.LogWarning($"Quaternius modular design {design.Id} produced no geometry.");
                return null;
            }

            // First pass: one mesh per material; second pass: one mesh with
            // one submesh per material.
            var sortedMaterials = new List<Material>(perMaterial.Keys);
            sortedMaterials.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var secondPass = new CombineInstance[sortedMaterials.Count];
            for (int i = 0; i < sortedMaterials.Count; i++)
            {
                Mesh grouped = new Mesh { indexFormat = IndexFormat.UInt32 };
                grouped.CombineMeshes(perMaterial[sortedMaterials[i]].ToArray(), true, true);
                secondPass[i] = new CombineInstance { mesh = grouped, transform = Matrix4x4.identity };
            }

            Mesh combined = new Mesh
            {
                name = $"QModular_{design.Id}",
                indexFormat = IndexFormat.UInt32
            };
            combined.CombineMeshes(secondPass, false, true);
            combined.RecalculateBounds();
            foreach (CombineInstance instance in secondPass)
            {
                Object.DestroyImmediate(instance.mesh);
            }

            string meshPath = $"{MeshFolder}/QModular_{design.Id}.asset";
            Mesh meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (meshAsset == null)
            {
                AssetDatabase.CreateAsset(combined, meshPath);
                meshAsset = combined;
            }
            else
            {
                EditorUtility.CopySerialized(combined, meshAsset);
                Object.DestroyImmediate(combined);
            }

            GameObject wrapper = new GameObject($"QuaterniusModular_{design.Id}");
            GameObject visual = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            visual.transform.SetParent(wrapper.transform, false);
            visual.GetComponent<MeshFilter>().sharedMesh = meshAsset;
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = sortedMaterials.ToArray();
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            // Exact wall-plane collider baked into the prefab. The scene
            // placement code must not replace it with a render-bounds box:
            // cornices and corner lips pad the bounds, which lets the player
            // walk into the facade before the collider reacts.
            BoxCollider collider = wrapper.AddComponent<BoxCollider>();
            float wallTop = design.Rows * RowHeight + CorniceHeight;
            collider.center = new Vector3(0f, wallTop * 0.5f, 0f);
            collider.size = new Vector3(
                design.WidthTiles * Tile,
                wallTop,
                design.DepthTiles * Tile);

            height = meshAsset.bounds.size.y;

            string prefabPath = $"{PrefabFolder}/QuaterniusModular_{design.Id}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
            Object.DestroyImmediate(wrapper);
            return prefab;
        }

        // ------------------------------------------------------------------
        // Composition
        // ------------------------------------------------------------------

        private static List<ModulePlacement> ComposeBuilding(Design design)
        {
            var placements = new List<ModulePlacement>(256);
            System.Random random = new System.Random(StableSeed(design.Id));

            float halfW = design.WidthTiles * Tile * 0.5f;
            float halfD = design.DepthTiles * Tile * 0.5f;
            float wallTop = design.Rows * RowHeight;

            // Brick corners are grid pieces that consume 1 m of facade at
            // each end; Metal/Trim corners are freestanding columns centered
            // on the corner point, so their walls run the full side.
            float cornerInset = design.Family == Family.Brick ? 1f : 0f;

            // The four facade runs: outward normal, yaw, run axis.
            var sides = new (Vector3 outward, float yaw)[]
            {
                (Vector3.forward, 0f),
                (Vector3.right, 90f),
                (Vector3.back, 180f),
                (Vector3.left, 270f)
            };

            for (int sideIndex = 0; sideIndex < sides.Length; sideIndex++)
            {
                (Vector3 outward, float yaw) = sides[sideIndex];
                bool alongX = Mathf.Abs(outward.z) > 0.5f;
                float sideLength = alongX ? halfW * 2f : halfD * 2f;
                float runStart = -sideLength * 0.5f + cornerInset;
                int slotCount = Mathf.FloorToInt((sideLength - 2f * cornerInset) / Tile);
                Vector3 runDirection = alongX
                    ? new Vector3(1f, 0f, 0f)
                    : new Vector3(0f, 0f, 1f);
                Vector3 facadeOrigin = outward * (alongX ? halfD : halfW);

                for (int row = 0; row < design.Rows; row++)
                {
                    ComposeFacadeRow(
                        placements, design, random, row, sideIndex, slotCount,
                        slot => facadeOrigin
                                + runDirection * (runStart + slot * Tile + Tile * 0.5f)
                                + Vector3.up * (row * RowHeight),
                        yaw);
                }

                // Cornice band above the top row, full side length so the
                // perpendicular runs simply overlap at the corners.
                int corniceCount = Mathf.FloorToInt(sideLength / Tile);
                float corniceStart = -sideLength * 0.5f;
                for (int i = 0; i < corniceCount; i++)
                {
                    placements.Add(new ModulePlacement
                    {
                        Asset = LoadModule(CorniceModel(design.Family)),
                        Position = facadeOrigin
                                   + runDirection * (corniceStart + i * Tile + Tile * 0.5f)
                                   + Vector3.up * wallTop,
                        Yaw = yaw
                    });
                }
            }

            ComposeCorners(placements, design, halfW, halfD, wallTop);
            ComposeRoof(placements, design, random, halfW, halfD, wallTop);

            // Dark interior occluder so windows read as rooms instead of a
            // hollow shell with visible far-wall backfaces.
            placements.Add(new ModulePlacement
            {
                Position = new Vector3(0f, wallTop * 0.5f, 0f),
                InteriorCubeSize = new Vector3(halfW * 2f - 0.7f, wallTop, halfD * 2f - 0.7f)
            });
            return placements;
        }

        private static void ComposeFacadeRow(
            List<ModulePlacement> placements,
            Design design,
            System.Random random,
            int row,
            int sideIndex,
            int slotCount,
            System.Func<int, Vector3> slotPosition,
            float yaw)
        {
            bool ground = row == 0;
            int doorSlot = ground ? slotCount / 2 : -1;

            int slot = 0;
            while (slot < slotCount)
            {
                Vector3 position = slotPosition(slot);
                if (slot == doorSlot)
                {
                    placements.Add(new ModulePlacement
                    {
                        Asset = LoadModule(DoorFrameModel(design.Family)),
                        Position = position,
                        Yaw = yaw
                    });
                    placements.Add(new ModulePlacement
                    {
                        Asset = LoadModule("Door_1.fbx"),
                        Position = position,
                        Yaw = yaw
                    });
                    slot++;
                    continue;
                }

                // Occasionally use a 4 m piece when two slots are free and
                // the door does not fall inside the span.
                bool wideFits = slot + 1 < slotCount
                                && (doorSlot < slot || doorSlot > slot + 1)
                                && random.NextDouble() < 0.30;
                string model = SelectWallModel(design.Family, ground, wideFits, random);
                Vector3 placeAt = position;
                if (wideFits)
                {
                    placeAt = (slotPosition(slot) + slotPosition(slot + 1)) * 0.5f;
                }

                placements.Add(new ModulePlacement
                {
                    Asset = LoadModule(model),
                    Position = placeAt,
                    Yaw = yaw
                });
                slot += wideFits ? 2 : 1;
            }
        }

        private static string SelectWallModel(Family family, bool ground, bool wide, System.Random random)
        {
            double roll = random.NextDouble();
            switch (family)
            {
                case Family.Brick when ground && wide:
                    return "Brick_Inset_Window.fbx";
                case Family.Brick when ground:
                    return roll < 0.7 ? "Brick_Window_Square_Single.fbx" : "Brick_BottomTrim.fbx";
                case Family.Brick when wide:
                    return roll < 0.6 ? "Brick_Window_CurvedDouble.fbx" : "Brick_RedWhite_DoubleWindow.fbx";
                case Family.Brick:
                    return roll < 0.72
                        ? "Brick_Window_Trim.fbx"
                        : roll < 0.9 ? "Brick_Plain_3.fbx" : "Brick_TopTrim.fbx";

                case Family.Metal when ground && wide:
                case Family.Metal when ground:
                    return roll < 0.8 ? "Metal_FirstFloor_Window.fbx" : "Metal_FirstFloor_Wall.fbx";
                case Family.Metal when wide:
                    return "Metal_Window.fbx";
                case Family.Metal:
                    return roll < 0.85 ? "Metal_FullWindow.fbx" : "Metal_Plain_3.fbx";

                case Family.Trim when ground:
                    return roll < 0.75 ? "Trim_FirstFloor_Window_001.fbx" : "Trim_FirstFloor_Wall.fbx";
                default:
                    return roll < 0.78 ? "Trim_Window.fbx" : "Trim_Plain_3.fbx";
            }
        }

        private static string CorniceModel(Family family)
        {
            switch (family)
            {
                case Family.Brick: return "Cornice_Brick_Center.fbx";
                case Family.Metal: return "Cornice_Metal_Center.fbx";
                default: return "Cornice_Trim_Center.fbx";
            }
        }

        private static string DoorFrameModel(Family family)
        {
            switch (family)
            {
                case Family.Brick: return "DoorFrame_Wooden.fbx";
                case Family.Metal: return "DoorFrame_Metal_Single.fbx";
                default: return "DoorFrame_Trim.fbx";
            }
        }

        private static void ComposeCorners(
            List<ModulePlacement> placements,
            Design design,
            float halfW,
            float halfD,
            float wallTop)
        {
            var corners = new Vector3[]
            {
                new Vector3(halfW, 0f, halfD),
                new Vector3(halfW, 0f, -halfD),
                new Vector3(-halfW, 0f, -halfD),
                new Vector3(-halfW, 0f, halfD)
            };

            foreach (Vector3 corner in corners)
            {
                Vector2 outward = new Vector2(Mathf.Sign(corner.x), Mathf.Sign(corner.z));
                for (int row = 0; row < design.Rows; row++)
                {
                    string model = CornerModel(design.Family, row, design.Rows);
                    GameObject asset = LoadModule(model);
                    float yaw = design.Family == Family.Brick
                        ? BrickCornerYaw(asset, outward)
                        : 0f;
                    placements.Add(new ModulePlacement
                    {
                        Asset = asset,
                        Position = new Vector3(corner.x, row * RowHeight, corner.z),
                        Yaw = yaw
                    });
                }
            }
        }

        private static string CornerModel(Family family, int row, int rows)
        {
            bool bottom = row == 0;
            bool top = row == rows - 1;
            switch (family)
            {
                case Family.Brick:
                    return bottom
                        ? "Brick_CornerColumn_Bottom.fbx"
                        : top ? "Brick_CornerColumn_Top.fbx" : "Brick_CornerColumn_Center.fbx";
                case Family.Metal:
                    return bottom
                        ? "Metal_Column_Bottom.fbx"
                        : top ? "Metal_Column_Top.fbx" : "Metal_Column_Center.fbx";
                default:
                    return bottom
                        ? "Trim_Column_Bottom.fbx"
                        : top ? "Trim_Column_Top.fbx" : "Trim_Column_Center.fbx";
            }
        }

        /// <summary>
        /// Brick corner columns are asymmetric: the bulk sits inside the
        /// building and a small lip protrudes past the two facade planes.
        /// Detect the authored protrusion directions from the mesh bounds
        /// and pick the yaw that points them at this corner's outward pair,
        /// so the result is import-orientation proof.
        /// </summary>
        private static float BrickCornerYaw(GameObject asset, Vector2 outward)
        {
            if (asset == null || !TryGetAssetBounds(asset, out Bounds bounds))
            {
                return 0f;
            }

            float protrudeX = bounds.max.x < -bounds.min.x ? 1f : -1f;
            float protrudeZ = bounds.max.z < -bounds.min.z ? 1f : -1f;
            for (int step = 0; step < 4; step++)
            {
                float yaw = step * 90f;
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
                Vector3 px = rotation * new Vector3(protrudeX, 0f, 0f);
                Vector3 pz = rotation * new Vector3(0f, 0f, protrudeZ);
                Vector2 a = new Vector2(Mathf.Round(px.x + pz.x), Mathf.Round(px.z + pz.z));
                if (Mathf.Approximately(a.x, outward.x) && Mathf.Approximately(a.y, outward.y))
                {
                    return yaw;
                }
            }
            return 0f;
        }

        private static void ComposeRoof(
            List<ModulePlacement> placements,
            Design design,
            System.Random random,
            float halfW,
            float halfD,
            float wallTop)
        {
            // Roof_2x2's surface sits 0.2 below its pivot; lifting the pivot
            // 0.3 above the wall top puts the slab 0.1 above the last row,
            // with the 1 m cornice ring reading as a parapet.
            float roofPivotY = wallTop + 0.3f;
            GameObject roofTile = LoadModule("Roof_2x2.fbx");
            for (float x = -halfW + 1f; x < halfW; x += 2f)
            {
                for (float z = -halfD + 1f; z < halfD; z += 2f)
                {
                    placements.Add(new ModulePlacement
                    {
                        Asset = roofTile,
                        Position = new Vector3(x, roofPivotY, z),
                        Yaw = 0f
                    });
                }
            }

            GameObject acUnit = LoadModule("Prop_ACUnit.fbx");
            int unitCount = 2 + random.Next(3);
            for (int i = 0; i < unitCount; i++)
            {
                float x = ((float)random.NextDouble() - 0.5f) * (halfW * 2f - 6f);
                float z = ((float)random.NextDouble() - 0.5f) * (halfD * 2f - 6f);
                placements.Add(new ModulePlacement
                {
                    Asset = acUnit,
                    Position = new Vector3(x, wallTop + 0.1f, z),
                    Yaw = random.Next(4) * 90f,
                    Scale = 1.8f
                });
            }
        }

        // ------------------------------------------------------------------
        // Mesh extraction
        // ------------------------------------------------------------------

        private static void AppendModule(
            ModulePlacement placement,
            Matrix4x4 world,
            IReadOnlyDictionary<string, Material> materials,
            System.Func<string, IReadOnlyDictionary<string, Material>, Material> resolveMaterial,
            Dictionary<Material, List<CombineInstance>> perMaterial)
        {
            if (placement.InteriorCubeSize.HasValue)
            {
                Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                Material interior = materials.TryGetValue("MI_FakeInterior_3", out Material dark)
                    ? dark
                    : resolveMaterial(string.Empty, materials);
                Matrix4x4 cubeMatrix = Matrix4x4.TRS(
                    placement.Position, Quaternion.identity, placement.InteriorCubeSize.Value);
                Add(perMaterial, interior, cube, 0, cubeMatrix);
                return;
            }

            if (placement.Asset == null)
            {
                return;
            }

            foreach (MeshFilter filter in placement.Asset.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Renderer renderer = filter.GetComponent<Renderer>();
                Material[] sourceMaterials = renderer != null
                    ? renderer.sharedMaterials
                    : System.Array.Empty<Material>();
                Matrix4x4 localToRoot = placement.Asset.transform.worldToLocalMatrix
                                        * filter.transform.localToWorldMatrix;
                Matrix4x4 matrix = world * localToRoot;
                for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    string sourceName = subMesh < sourceMaterials.Length && sourceMaterials[subMesh] != null
                        ? sourceMaterials[subMesh].name
                        : string.Empty;
                    Material resolved = resolveMaterial(sourceName, materials);
                    Add(perMaterial, resolved, mesh, subMesh, matrix);
                }
            }
        }

        private static void Add(
            Dictionary<Material, List<CombineInstance>> perMaterial,
            Material material,
            Mesh mesh,
            int subMesh,
            Matrix4x4 matrix)
        {
            if (!perMaterial.TryGetValue(material, out List<CombineInstance> list))
            {
                list = new List<CombineInstance>();
                perMaterial[material] = list;
            }
            list.Add(new CombineInstance { mesh = mesh, subMeshIndex = subMesh, transform = matrix });
        }

        /// <summary>
        /// Mesh combining requires readable meshes. If a probe module still
        /// carries the old non-readable import (its file content did not
        /// change, so only the postprocessor version bump reimports it),
        /// force one synchronous reimport of the model folder.
        /// </summary>
        private static void EnsureReadableModules()
        {
            ModuleCache.Clear();
            GameObject probe = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{ModelFolder}/Brick_Plain_3.fbx");
            MeshFilter filter = probe != null ? probe.GetComponentInChildren<MeshFilter>(true) : null;
            if (filter != null && filter.sharedMesh != null && !filter.sharedMesh.isReadable)
            {
                AssetDatabase.ImportAsset(
                    ModelFolder,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            }
        }

        private static readonly Dictionary<string, GameObject> ModuleCache =
            new Dictionary<string, GameObject>();

        private static GameObject LoadModule(string fileName)
        {
            if (ModuleCache.TryGetValue(fileName, out GameObject cached) && cached != null)
            {
                return cached;
            }

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{fileName}");
            if (asset == null)
            {
                Debug.LogWarning($"Quaternius module is not imported: {fileName}");
            }
            ModuleCache[fileName] = asset;
            return asset;
        }

        private static bool TryGetAssetBounds(GameObject asset, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (MeshFilter filter in asset.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }
                Matrix4x4 localToRoot = asset.transform.worldToLocalMatrix
                                        * filter.transform.localToWorldMatrix;
                Bounds meshBounds = filter.sharedMesh.bounds;
                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = new Vector3(
                        (i & 1) == 0 ? min.x : max.x,
                        (i & 2) == 0 ? min.y : max.y,
                        (i & 4) == 0 ? min.z : max.z);
                    Vector3 transformed = localToRoot.MultiplyPoint3x4(corner);
                    if (!found)
                    {
                        bounds = new Bounds(transformed, Vector3.zero);
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(transformed);
                    }
                }
            }
            return found;
        }

        private static int StableSeed(string id)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in id)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }
    }
}
