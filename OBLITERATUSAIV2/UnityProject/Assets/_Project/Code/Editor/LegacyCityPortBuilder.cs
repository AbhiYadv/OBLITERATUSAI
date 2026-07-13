using System;
using System.Collections.Generic;
using ObliteratusAI.City;
using UnityEditor;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    internal static class LegacyCityPortBuilder
    {
        private const float RoadWidth = CityLayout.RoadWidth;
        private const float SidewalkHeight = CityLayout.SidewalkHeight;
        private const string MaterialFolder = "Assets/_Project/Art/Materials";
        private const string TextureFolder = "Assets/_Project/Art/Textures";
        private const string CityArtFolder = "Assets/_Project/Art/City";
        private const string LegacyCarSource = "Assets/ThirdParty/LegacyProject/Vehicles/lowpoly-cars.glb";
        private const string LegacyCarVisualPrefab = "Assets/_Project/Prefabs/Vehicles/LegacyCar9Visual.prefab";
        private const string RoadPaintMeshPath = "Assets/_Project/Art/City/LegacyRoadPaint.asset";

        private static readonly float[] Streets = CityLayout.Streets;

        private struct PaintRect
        {
            public Vector3 Center;
            public Vector2 Size;
            public float Yaw;
        }

        public static GameObject Build()
        {
            Material concrete = GetOrCreateMaterial("M_LegacyConcrete", new Color(0.59f, 0.58f, 0.55f));
            Material asphalt = GetOrCreateMaterial("M_LegacyAsphalt", new Color(0.085f, 0.095f, 0.105f));
            Material paint = GetOrCreateMaterial("M_LegacyRoadPaint", new Color(0.85f, 0.85f, 0.81f));
            Material roof = GetOrCreateMaterial("M_LegacyRoof", new Color(0.25f, 0.25f, 0.27f));
            Material dark = GetOrCreateMaterial("M_LegacyDarkMetal", new Color(0.08f, 0.10f, 0.12f));
            Material[] facades = CreateFacadeMaterials();
            Material[] storefronts =
            {
                GetOrCreateMaterial("M_StorefrontGreen", new Color(0.08f, 0.38f, 0.25f)),
                GetOrCreateMaterial("M_StorefrontBlue", new Color(0.08f, 0.28f, 0.48f)),
                GetOrCreateMaterial("M_StorefrontCyan", new Color(0.04f, 0.42f, 0.55f)),
                GetOrCreateMaterial("M_StorefrontViolet", new Color(0.25f, 0.18f, 0.42f))
            };
            CityVisualSet importedCityVisuals = QuaterniusCityAssetBuilder.EnsureAssets();

            // Kit ground surfaces. World-scale UVs are baked into the slab
            // meshes, so these materials keep 1:1 tiling and stay independent
            // of the building modules' own UVs. If the kit textures are not
            // imported the flat legacy materials remain the fallback.
            Material kitAsphalt = QuaterniusCityAssetBuilder.EnsureLit(
                "M_QC_RoadAsphalt", "T_Concrete_Asphalt_BaseColor.png", null, Color.white, 0.06f, 0f);
            Material kitSidewalk = QuaterniusCityAssetBuilder.EnsureLit(
                "M_QC_SidewalkConcrete", "T_Concrete_BaseColor.png", "T_Concrete_Normal.png",
                Color.white, 0.10f, 0f);
            Material roadSurface = kitAsphalt.GetTexture("_BaseMap") != null ? kitAsphalt : asphalt;
            Material walkSurface = kitSidewalk.GetTexture("_BaseMap") != null ? kitSidewalk : concrete;

            GameObject root = new GameObject("LegacyCityPort");
            CreateBase(root.transform, walkSurface, roadSurface);
            LegacySurroundBuilder.Build(root.transform, LegacySurroundBuilder.DefaultSeed);
            List<Vector4> blocks = CreateSidewalkBlocks(root.transform, walkSurface);
            CreateRoadPaint(root.transform, paint);
            CreateManholes(root.transform, importedCityVisuals);
            CreateBuildings(root.transform, blocks, facades, storefronts, roof, dark, importedCityVisuals);
            CreateStreetLights(root.transform, dark, paint);
            CreateConstructionSite(root.transform, concrete, dark);
            CityLifeDetailBuilder.Build(root.transform, LegacySurroundBuilder.DefaultSeed, importedCityVisuals);

            GameObject carVisual = CreateLegacyCarVisualPrefab();

            SetStaticRecursively(root);

            // Built after the static pass on purpose: lamp materials swap at
            // runtime, so the signal posts must not be batched static.
            TrafficSignalBuilder.Build(root.transform);

            ConfigureEnvironmentLighting();
            return carVisual;
        }

        private static void CreateBase(
            Transform root,
            Material concrete,
            Material asphalt)
        {
            // The grass/water placeholder boxes are replaced by the baked
            // terrain surround (LegacySurroundBuilder).
            GameObject foundation = CreateTexturedSlab(
                "CityFoundation",
                "Assets/_Project/Art/City/LegacyFoundationSlab.asset",
                new Vector3(0f, 0f, 0f),
                new Vector2(310f, 310f),
                0f,
                0.15f,
                concrete,
                root);
            BoxCollider foundationCollider = foundation.AddComponent<BoxCollider>();
            foundationCollider.center = new Vector3(0f, -0.08f, 0f);
            foundationCollider.size = new Vector3(310f, 0.16f, 310f);

            // Roads keep their exact legacy footprints and heights (the NS/EW
            // 5 mm offset avoids z-fighting at intersections); only the flat
            // color becomes the kit asphalt texture.
            foreach (float street in Streets)
            {
                CreateTexturedSlab($"Road_NS_{street}",
                    "Assets/_Project/Art/City/LegacyRoadSlabNS.asset",
                    new Vector3(street, 0.03f, 0f),
                    new Vector2(RoadWidth, 300f),
                    0f,
                    0.12f,
                    asphalt,
                    root);
                CreateTexturedSlab($"Road_EW_{street}",
                    "Assets/_Project/Art/City/LegacyRoadSlabEW.asset",
                    new Vector3(0f, 0.035f, street),
                    new Vector2(300f, RoadWidth),
                    0f,
                    0.12f,
                    asphalt,
                    root);
            }
        }

        private static List<Vector4> CreateSidewalkBlocks(Transform root, Material concrete)
        {
            List<Vector4> blocks = new List<Vector4>();
            Transform blockRoot = new GameObject("SidewalkBlocks").transform;
            blockRoot.SetParent(root);
            for (int x = 0; x < Streets.Length - 1; x++)
            {
                for (int z = 0; z < Streets.Length - 1; z++)
                {
                    float minX = Streets[x] + RoadWidth * 0.5f;
                    float maxX = Streets[x + 1] - RoadWidth * 0.5f;
                    float minZ = Streets[z] + RoadWidth * 0.5f;
                    float maxZ = Streets[z + 1] - RoadWidth * 0.5f;
                    blocks.Add(new Vector4(minX, maxX, minZ, maxZ));
                    GameObject slab = CreateTexturedSlab($"Block_{x}_{z}",
                        "Assets/_Project/Art/City/LegacyBlockSlab.asset",
                        new Vector3((minX + maxX) * 0.5f, SidewalkHeight, (minZ + maxZ) * 0.5f),
                        new Vector2(maxX - minX, maxZ - minZ),
                        SidewalkHeight,
                        0.25f,
                        concrete,
                        blockRoot);
                    BoxCollider collider = slab.AddComponent<BoxCollider>();
                    collider.center = new Vector3(0f, -SidewalkHeight * 0.5f, 0f);
                    collider.size = new Vector3(maxX - minX, SidewalkHeight, maxZ - minZ);
                }
            }

            return blocks;
        }

        /// <summary>
        /// Flat slab with its top face at the object's y position, optional
        /// curb sides hanging down, and UVs in meters so tiled surface
        /// textures keep a constant density regardless of slab size. The
        /// mesh is stored as a reusable asset; identical slabs share it.
        /// </summary>
        private static GameObject CreateTexturedSlab(
            string name,
            string meshAssetPath,
            Vector3 topCenter,
            Vector2 size,
            float sideHeight,
            float uvPerMeter,
            Material material,
            Transform parent)
        {
            Mesh mesh = BuildSlabMesh(size, sideHeight, uvPerMeter);
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, meshAssetPath);
                existing = mesh;
            }
            else
            {
                EditorUtility.CopySerialized(mesh, existing);
                UnityEngine.Object.DestroyImmediate(mesh);
            }

            GameObject slab = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            slab.transform.SetParent(parent);
            slab.transform.position = topCenter;
            slab.GetComponent<MeshFilter>().sharedMesh = existing;
            slab.GetComponent<MeshRenderer>().sharedMaterial = material;
            return slab;
        }

        private static Mesh BuildSlabMesh(Vector2 size, float sideHeight, float uvPerMeter)
        {
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal,
                Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
            {
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                for (int i = 0; i < 4; i++)
                {
                    normals.Add(normal);
                }
                uvs.Add(uvA); uvs.Add(uvB); uvs.Add(uvC); uvs.Add(uvD);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
            }

            AddQuad(
                new Vector3(-halfX, 0f, -halfZ),
                new Vector3(halfX, 0f, -halfZ),
                new Vector3(halfX, 0f, halfZ),
                new Vector3(-halfX, 0f, halfZ),
                Vector3.up,
                new Vector2(-halfX, -halfZ) * uvPerMeter,
                new Vector2(halfX, -halfZ) * uvPerMeter,
                new Vector2(halfX, halfZ) * uvPerMeter,
                new Vector2(-halfX, halfZ) * uvPerMeter);

            if (sideHeight > 0.001f)
            {
                float bottom = -sideHeight;
                // South, North, West, East curb faces.
                AddQuad(
                    new Vector3(-halfX, bottom, -halfZ), new Vector3(halfX, bottom, -halfZ),
                    new Vector3(halfX, 0f, -halfZ), new Vector3(-halfX, 0f, -halfZ),
                    Vector3.back,
                    new Vector2(-halfX, 0f) * uvPerMeter, new Vector2(halfX, 0f) * uvPerMeter,
                    new Vector2(halfX, sideHeight) * uvPerMeter, new Vector2(-halfX, sideHeight) * uvPerMeter);
                AddQuad(
                    new Vector3(halfX, bottom, halfZ), new Vector3(-halfX, bottom, halfZ),
                    new Vector3(-halfX, 0f, halfZ), new Vector3(halfX, 0f, halfZ),
                    Vector3.forward,
                    new Vector2(-halfX, 0f) * uvPerMeter, new Vector2(halfX, 0f) * uvPerMeter,
                    new Vector2(halfX, sideHeight) * uvPerMeter, new Vector2(-halfX, sideHeight) * uvPerMeter);
                AddQuad(
                    new Vector3(-halfX, bottom, halfZ), new Vector3(-halfX, bottom, -halfZ),
                    new Vector3(-halfX, 0f, -halfZ), new Vector3(-halfX, 0f, halfZ),
                    Vector3.left,
                    new Vector2(-halfZ, 0f) * uvPerMeter, new Vector2(halfZ, 0f) * uvPerMeter,
                    new Vector2(halfZ, sideHeight) * uvPerMeter, new Vector2(-halfZ, sideHeight) * uvPerMeter);
                AddQuad(
                    new Vector3(halfX, bottom, -halfZ), new Vector3(halfX, bottom, halfZ),
                    new Vector3(halfX, 0f, halfZ), new Vector3(halfX, 0f, -halfZ),
                    Vector3.right,
                    new Vector2(-halfZ, 0f) * uvPerMeter, new Vector2(halfZ, 0f) * uvPerMeter,
                    new Vector2(halfZ, sideHeight) * uvPerMeter, new Vector2(-halfZ, sideHeight) * uvPerMeter);
            }

            Mesh mesh = new Mesh { name = "LegacySlab" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateManholes(Transform root, CityVisualSet visualSet)
        {
            GameObject prefab = visualSet != null ? visualSet.ManholePrefab : null;
            if (prefab == null)
            {
                return;
            }

            Transform parent = new GameObject("Manholes").transform;
            parent.SetParent(root);
            int index = 0;
            foreach (float street in Streets)
            {
                for (float along = -138f; along <= 138f; along += 31f)
                {
                    if (NearStreet(along, 12f))
                    {
                        continue;
                    }

                    float lane = index++ % 2 == 0 ? 1.5f : -1.5f;
                    // NS road tops sit at 0.03, EW at 0.035; the cover base is
                    // authored at -0.014, so these heights rest it on asphalt.
                    GameObject onVertical = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                    if (onVertical != null)
                    {
                        onVertical.name = "Manhole";
                        onVertical.transform.position = new Vector3(street + lane, 0.045f, along);
                    }

                    GameObject onHorizontal = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                    if (onHorizontal != null)
                    {
                        onHorizontal.name = "Manhole";
                        onHorizontal.transform.position = new Vector3(along, 0.05f, street + lane);
                    }
                }
            }
        }

        private static void CreateRoadPaint(Transform root, Material paint)
        {
            List<PaintRect> markings = new List<PaintRect>();
            foreach (float x in Streets)
            {
                for (float z = -140f; z <= 140f; z += 8f)
                {
                    if (!NearStreet(z, 10f))
                    {
                        markings.Add(new PaintRect { Center = new Vector3(x, 0.055f, z), Size = new Vector2(0.18f, 3f) });
                    }
                }
            }

            foreach (float z in Streets)
            {
                for (float x = -140f; x <= 140f; x += 8f)
                {
                    if (!NearStreet(x, 10f))
                    {
                        markings.Add(new PaintRect
                        {
                            Center = new Vector3(x, 0.057f, z),
                            Size = new Vector2(0.18f, 3f),
                            Yaw = 90f
                        });
                    }
                }
            }

            foreach (float x in Streets)
            {
                foreach (float z in Streets)
                {
                    for (int stripe = -2; stripe <= 2; stripe++)
                    {
                        float offset = stripe * 0.95f;
                        markings.Add(new PaintRect
                        {
                            Center = new Vector3(x + offset, 0.06f, z - RoadWidth * 0.5f - 1.4f),
                            Size = new Vector2(0.62f, 2.2f)
                        });
                        markings.Add(new PaintRect
                        {
                            Center = new Vector3(x + offset, 0.06f, z + RoadWidth * 0.5f + 1.4f),
                            Size = new Vector2(0.62f, 2.2f)
                        });
                        markings.Add(new PaintRect
                        {
                            Center = new Vector3(x - RoadWidth * 0.5f - 1.4f, 0.061f, z + offset),
                            Size = new Vector2(0.62f, 2.2f),
                            Yaw = 90f
                        });
                        markings.Add(new PaintRect
                        {
                            Center = new Vector3(x + RoadWidth * 0.5f + 1.4f, 0.061f, z + offset),
                            Size = new Vector2(0.62f, 2.2f),
                            Yaw = 90f
                        });
                    }
                }
            }

            Mesh paintMesh = BuildPaintMesh(markings);
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(RoadPaintMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(paintMesh, RoadPaintMeshPath);
                existing = paintMesh;
            }
            else
            {
                EditorUtility.CopySerialized(paintMesh, existing);
                UnityEngine.Object.DestroyImmediate(paintMesh);
            }

            GameObject paintObject = new GameObject("RoadPaint", typeof(MeshFilter), typeof(MeshRenderer));
            paintObject.transform.SetParent(root);
            paintObject.GetComponent<MeshFilter>().sharedMesh = existing;
            paintObject.GetComponent<MeshRenderer>().sharedMaterial = paint;
        }

        private static void CreateBuildings(
            Transform root,
            List<Vector4> blocks,
            Material[] facades,
            Material[] storefronts,
            Material roof,
            Material dark,
            CityVisualSet importedCityVisuals)
        {
            Transform buildingRoot = new GameObject("Buildings").transform;
            buildingRoot.SetParent(root);
            System.Random random = new System.Random(0xC17F0A);
            int buildingIndex = 0;

            for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
            {
                if (blockIndex == 10)
                {
                    continue;
                }

                Vector4 block = blocks[blockIndex];
                float centerX = (block.x + block.y) * 0.5f;
                float centerZ = (block.z + block.w) * 0.5f;
                float downtown = 1f - Mathf.Clamp01(new Vector2(centerX, centerZ).magnitude / 190f);
                bool tower = downtown > 0.55f && random.NextDouble() < 0.45;
                int lotCount = tower ? 1 : 4;

                for (int lot = 0; lot < lotCount; lot++)
                {
                    if (!tower && random.NextDouble() < 0.14)
                    {
                        continue;
                    }

                    int lotX = lot % 2;
                    int lotZ = lot / 2;
                    float innerWidth = block.y - block.x - 7f;
                    float innerDepth = block.w - block.z - 7f;
                    float lotWidth = tower ? innerWidth : innerWidth * 0.5f;
                    float lotDepth = tower ? innerDepth : innerDepth * 0.5f;
                    float x = tower
                        ? centerX
                        : block.x + 3.5f + lotWidth * (lotX + 0.5f);
                    float z = tower
                        ? centerZ
                        : block.z + 3.5f + lotDepth * (lotZ + 0.5f);
                    float width = lotWidth * (tower ? 0.67f : Mathf.Lerp(0.72f, 0.91f, NextFloat(random)));
                    float depth = lotDepth * (tower ? 0.62f : Mathf.Lerp(0.72f, 0.91f, NextFloat(random)));
                    float height = tower
                        ? Mathf.Lerp(50f, 82f, NextFloat(random))
                        : 10f + NextFloat(random) * 14f + downtown * (18f + NextFloat(random) * 22f);
                    int variant = height > 45f && random.NextDouble() < 0.7
                        ? 3
                        : random.Next(0, 3);

                    Material storefront = storefronts[random.Next(storefronts.Length)];
                    // Every lot, towers included, now uses kit art. The
                    // procedural facade boxes remain only as the fallback
                    // when the kit is not imported.
                    bool imported = TryCreateImportedBuilding(
                        buildingRoot,
                        buildingIndex,
                        x,
                        z,
                        width,
                        depth,
                        height,
                        importedCityVisuals);

                    if (imported)
                    {
                        ConsumeLegacyRooftopRandom(height, random);
                    }
                    else
                    {
                        CreateBuilding(buildingRoot, buildingIndex, x, z, width, depth, height,
                            facades[variant], storefront, roof, dark, random);
                    }
                    buildingIndex++;
                }
            }
        }

        private static bool TryCreateImportedBuilding(
            Transform parent,
            int index,
            float x,
            float z,
            float width,
            float depth,
            float requestedHeight,
            CityVisualSet visualSet)
        {
            if (visualSet == null
                || !visualSet.TryGetClosestBuilding(requestedHeight, width, depth, index, out GameObject prefab)
                || prefab == null)
            {
                return false;
            }

            GameObject building = new GameObject($"Building_{index}_Quaternius");
            building.transform.SetParent(parent);
            building.transform.position = new Vector3(x, SidewalkHeight, z);

            GameObject visual = PrefabUtility.InstantiatePrefab(prefab, building.transform) as GameObject;
            if (visual == null || !EditorBuildUtility.TryGetCombinedBounds(visual, out Bounds bounds))
            {
                UnityEngine.Object.DestroyImmediate(building);
                return false;
            }

            visual.name = "Visual";
            // These complete models have street facades on local +X and +Z,
            // with party walls on -X and -Z. Rotate each lot so both detailed
            // sides face its two nearest streets and the blank sides face the
            // block interior.
            float xDirection = NearestStreetDirection(x);
            float zDirection = NearestStreetDirection(z);
            float yaw;
            if (xDirection > 0f && zDirection > 0f)
            {
                yaw = 0f;
            }
            else if (xDirection > 0f)
            {
                yaw = 90f;
            }
            else if (zDirection > 0f)
            {
                yaw = -90f;
            }
            else
            {
                yaw = 180f;
            }
            visual.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            if (!EditorBuildUtility.TryGetCombinedBounds(visual, out bounds)
                || bounds.size.x < 0.01f
                || bounds.size.y < 0.01f
                || bounds.size.z < 0.01f)
            {
                UnityEngine.Object.DestroyImmediate(building);
                return false;
            }

            float footprintScale = Mathf.Min(
                width * 0.94f / bounds.size.x,
                depth * 0.94f / bounds.size.z);
            // Preserve the authored proportions and form a dense street wall.
            // Shrinking to the old box height exposes the pack's party walls
            // across large gaps and defeats the purpose of the detailed mesh.
            float scale = footprintScale;
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0.01f)
            {
                UnityEngine.Object.DestroyImmediate(building);
                return false;
            }

            visual.transform.localScale = Vector3.one * scale;
            if (!EditorBuildUtility.TryGetCombinedBounds(visual, out bounds))
            {
                UnityEngine.Object.DestroyImmediate(building);
                return false;
            }

            visual.transform.position += new Vector3(
                x - bounds.center.x,
                SidewalkHeight - bounds.min.y,
                z - bounds.center.z);

            // Modular prefabs carry an exact wall-plane collider (scaled with
            // the visual); only the complete kit models still need the
            // approximate render-bounds box.
            if (visual.GetComponentInChildren<Collider>(true) == null)
            {
                EditorBuildUtility.TryGetCombinedBounds(visual, out bounds);
                BoxCollider collider = building.AddComponent<BoxCollider>();
                collider.center = building.transform.InverseTransformPoint(bounds.center);
                collider.size = new Vector3(
                    bounds.size.x * 0.90f,
                    bounds.size.y,
                    bounds.size.z * 0.90f);
            }
            return true;
        }

        private static void ConsumeLegacyRooftopRandom(float height, System.Random random)
        {
            if (height <= 20f)
            {
                return;
            }

            int unitCount = 1 + random.Next(0, 2);
            for (int i = 0; i < unitCount; i++)
            {
                NextFloat(random);
                NextFloat(random);
            }
        }

        private static void CreateBuilding(
            Transform parent,
            int index,
            float x,
            float z,
            float width,
            float depth,
            float height,
            Material facade,
            Material storefront,
            Material roof,
            Material dark,
            System.Random random)
        {
            GameObject building = new GameObject($"Building_{index}");
            building.transform.SetParent(parent);
            building.transform.position = new Vector3(x, SidewalkHeight, z);
            CreateLocalBox("Facade", new Vector3(0f, height * 0.5f, 0f),
                new Vector3(width, height, depth), facade, building.transform, false);
            CreateLocalBox("RoofCap", new Vector3(0f, height + 0.35f, 0f),
                new Vector3(width + 0.3f, 0.7f, depth + 0.3f), roof, building.transform);

            bool facesX = NearestStreetDistance(x) <= NearestStreetDistance(z);
            float direction = NearestStreetDirection(facesX ? x : z);
            Vector3 panelPosition = facesX
                ? new Vector3(direction * (width * 0.5f + 0.04f), 2f, 0f)
                : new Vector3(0f, 2f, direction * (depth * 0.5f + 0.04f));
            Vector3 panelScale = facesX
                ? new Vector3(0.12f, 3.8f, depth * 0.78f)
                : new Vector3(width * 0.78f, 3.8f, 0.12f);
            CreateLocalBox("Storefront", panelPosition, panelScale, storefront, building.transform);

            Vector3 doorPosition = panelPosition;
            doorPosition.y = 1.5f;
            Vector3 doorScale = facesX ? new Vector3(0.16f, 3f, 2.2f) : new Vector3(2.2f, 3f, 0.16f);
            CreateLocalBox("Entrance", doorPosition, doorScale, dark, building.transform);

            if (height > 20f)
            {
                int unitCount = 1 + random.Next(0, 2);
                for (int i = 0; i < unitCount; i++)
                {
                    float unitX = (NextFloat(random) - 0.5f) * width * 0.45f;
                    float unitZ = (NextFloat(random) - 0.5f) * depth * 0.45f;
                    CreateLocalBox($"AC_{i}", new Vector3(unitX, height + 1.1f, unitZ),
                        new Vector3(1.6f, 1.1f, 1.6f), roof, building.transform);
                }
            }
        }

        private static void CreateStreetLights(Transform root, Material dark, Material paint)
        {
            Transform furniture = new GameObject("StreetLights").transform;
            furniture.SetParent(root);

            int lightIndex = 0;
            foreach (float street in Streets)
            {
                for (float along = -125f; along <= 125f; along += 50f)
                {
                    // Positions on a cross street would put the pole in the
                    // middle of the road at an intersection.
                    if (NearStreet(along, RoadWidth * 0.5f + 1.5f))
                    {
                        continue;
                    }
                    float side = lightIndex++ % 2 == 0 ? 1f : -1f;
                    CreateStreetLight(
                        furniture,
                        new Vector3(street + side * 6.4f, SidewalkHeight, along),
                        new Vector3(-side, 0f, 0f),
                        dark,
                        paint);
                    CreateStreetLight(
                        furniture,
                        new Vector3(along, SidewalkHeight, street + side * 6.4f),
                        new Vector3(0f, 0f, -side),
                        dark,
                        paint);
                }
            }

        }

        private static void CreateStreetLight(
            Transform parent,
            Vector3 basePosition,
            Vector3 directionToRoad,
            Material pole,
            Material lamp)
        {
            GameObject root = new GameObject("StreetLight");
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(
                basePosition,
                Quaternion.FromToRotation(Vector3.right, directionToRoad));
            CreateCylinder("Pole", new Vector3(0f, 2.4f, 0f), new Vector3(0.09f, 2.4f, 0.09f), pole, root.transform);
            GameObject arm = new GameObject("Arm");
            arm.transform.SetParent(root.transform, false);
            arm.transform.localPosition = new Vector3(0.35f, 4.72f, 0f);
            arm.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CreateCylinder("ArmTube", Vector3.zero, new Vector3(0.06f, 0.38f, 0.06f), pole, arm.transform);
            CreateLocalBox("Lamp", new Vector3(0.35f, 4.75f, 0f), new Vector3(0.8f, 0.16f, 0.28f), lamp, root.transform);

            // The legacy collision registry treated poles as solid.
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 2.4f, 0f);
            collider.size = new Vector3(0.2f, 4.8f, 0.2f);
        }

        private static void CreateConstructionSite(Transform root, Material concrete, Material dark)
        {
            Transform site = new GameObject("ConstructionSite").transform;
            site.SetParent(root);
            site.position = new Vector3(37.5f, SidewalkHeight, 37.5f);
            for (int floor = 0; floor < 5; floor++)
            {
                CreateLocalBox($"Floor_{floor}", new Vector3(0f, floor * 4.2f + 0.2f, 0f),
                    new Vector3(48f, 0.4f, 48f), concrete, site);
            }
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    CreateLocalBox("Column", new Vector3(x * 18f, 10.5f, z * 18f),
                        new Vector3(0.7f, 21f, 0.7f), concrete, site);
                }
            }
            CreateLocalBox("CraneMast", new Vector3(-18f, 17f, -18f), new Vector3(0.8f, 34f, 0.8f), dark, site);
            CreateLocalBox("CraneJib", new Vector3(2f, 33.5f, -18f), new Vector3(40f, 0.6f, 0.6f), dark, site);

            // Legacy Construction.tsx detail: hanging hook, counterweight,
            // alternating barrier fence ring, and cones at the gate corner.
            CreateLocalBox("CraneCable", new Vector3(18f, 29.5f, -18f), new Vector3(0.06f, 8f, 0.06f), dark, site);
            CreateLocalBox("CraneHook", new Vector3(18f, 25.15f, -18f), new Vector3(0.7f, 0.7f, 0.7f), dark, site);
            CreateLocalBox("Counterweight", new Vector3(-18f, 31.3f, -18f), new Vector3(1.8f, 1.8f, 1.8f), dark, site);

            Material orange = GetOrCreateMaterial("M_ConstructionOrange", new Color(0.886f, 0.376f, 0.102f));
            Material white = GetOrCreateMaterial("M_ConstructionWhite", new Color(0.910f, 0.902f, 0.878f));
            int segment = 0;
            for (float t = -24f; t <= 24f; t += 2f, segment++)
            {
                Material barrier = segment % 2 == 0 ? orange : white;
                CreateLocalBox($"FenceN_{segment}", new Vector3(t, 0.5f, -26f), new Vector3(1.9f, 1f, 0.09f), barrier, site);
                CreateLocalBox($"FenceS_{segment}", new Vector3(t, 0.5f, 26f), new Vector3(1.9f, 1f, 0.09f), white == barrier ? orange : white, site);
                CreateLocalBox($"FenceW_{segment}", new Vector3(-26f, 0.5f, t), new Vector3(0.09f, 1f, 1.9f), barrier, site);
                CreateLocalBox($"FenceE_{segment}", new Vector3(26f, 0.5f, t), new Vector3(0.09f, 1f, 1.9f), white == barrier ? orange : white, site);
            }

            Vector2[] coneSpots =
            {
                new Vector2(21f, -21f), new Vector2(22.6f, -19.4f), new Vector2(19.4f, -22.6f),
                new Vector2(24f, -17.8f), new Vector2(17.8f, -24f), new Vector2(22f, -22f)
            };
            foreach (Vector2 spot in coneSpots)
            {
                CreateLocalBox($"ConeBase_{spot.x}_{spot.y}", new Vector3(spot.x, 0.03f, spot.y),
                    new Vector3(0.34f, 0.06f, 0.34f), orange, site);
                CreateLocalBox($"ConeBody_{spot.x}_{spot.y}", new Vector3(spot.x, 0.31f, spot.y),
                    new Vector3(0.16f, 0.5f, 0.16f), orange, site);
            }
        }

        private static GameObject CreateLegacyCarVisualPrefab()
        {
            // Preferred player car: the Kenney Car Kit sedan (faces +Z, so no
            // flip). Normalized to the sedan collider's 1.95 m width — the
            // kit's toy proportions would exceed the lane width if the
            // legacy 4.3 m length normalization were applied.
            GameObject kenneySedan = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{TrafficAssetBuilder.KenneyModelFolder}/sedan.glb");
            if (kenneySedan != null)
            {
                GameObject sedanInstance = PrefabUtility.InstantiatePrefab(kenneySedan) as GameObject;
                if (sedanInstance != null)
                {
                    GameObject sedanWrapper = new GameObject("LegacyCar9Visual");
                    sedanInstance.transform.SetParent(sedanWrapper.transform, false);
                    if (TryGetBounds(sedanInstance, out Bounds sedanBounds)
                        && sedanBounds.size.x > 0.01f
                        && sedanBounds.size.z > 0.01f)
                    {
                        float sedanScale = 1.95f / Mathf.Min(sedanBounds.size.x, sedanBounds.size.z);
                        sedanInstance.transform.localScale = Vector3.one * sedanScale;
                        if (TryGetBounds(sedanInstance, out sedanBounds))
                        {
                            sedanInstance.transform.position += new Vector3(
                                -sedanBounds.center.x, -sedanBounds.min.y, -sedanBounds.center.z);
                        }
                        GameObject sedanPrefab =
                            PrefabUtility.SaveAsPrefabAsset(sedanWrapper, LegacyCarVisualPrefab);
                        UnityEngine.Object.DestroyImmediate(sedanWrapper);
                        return sedanPrefab;
                    }
                    UnityEngine.Object.DestroyImmediate(sedanWrapper);
                }
            }

            GameObject sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(LegacyCarSource);
            if (sourceAsset == null)
            {
                Debug.LogWarning($"Legacy car source is not imported: {LegacyCarSource}");
                return null;
            }

            GameObject importedRoot = PrefabUtility.InstantiatePrefab(sourceAsset) as GameObject;
            if (importedRoot == null)
            {
                return null;
            }

            GameObject wrapper = new GameObject("LegacyCar9Visual");
            importedRoot.transform.SetParent(wrapper.transform, false);
            Transform car9 = FindDescendant(importedRoot.transform, "car9");
            Transform car2 = FindDescendant(importedRoot.transform, "car2");
            if (car2 != null)
            {
                car2.gameObject.SetActive(false);
            }

            if (car9 == null || !TryGetBounds(car9.gameObject, out Bounds bounds))
            {
                UnityEngine.Object.DestroyImmediate(wrapper);
                Debug.LogWarning("Legacy car9 model or renderer bounds were not found.");
                return null;
            }

            // car9 faces -Z after long-axis normalization (confirmed in play
            // testing: W drove the sedan toward its visual tail), so flip it
            // to face +Z like every other vehicle visual.
            float rotation = (bounds.size.x > bounds.size.z ? 90f : 0f) + 180f;
            importedRoot.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
            float scale = 4.3f / Mathf.Max(bounds.size.x, bounds.size.z);
            importedRoot.transform.localScale = Vector3.one * scale;
            if (TryGetBounds(car9.gameObject, out bounds))
            {
                Vector3 center = bounds.center;
                importedRoot.transform.position += new Vector3(-center.x, -bounds.min.y, -center.z);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, LegacyCarVisualPrefab);
            UnityEngine.Object.DestroyImmediate(wrapper);
            return prefab;
        }

        private static Material[] CreateFacadeMaterials()
        {
            Color[] wallColors =
            {
                new Color(0.72f, 0.68f, 0.60f),
                new Color(0.56f, 0.59f, 0.60f),
                new Color(0.54f, 0.29f, 0.22f),
                new Color(0.29f, 0.44f, 0.56f)
            };
            Material[] materials = new Material[wallColors.Length];
            for (int i = 0; i < wallColors.Length; i++)
            {
                Texture2D texture = GetOrCreateFacadeTexture(i, wallColors[i]);
                materials[i] = GetOrCreateMaterial($"M_LegacyFacade_{i}", Color.white);
                materials[i].SetTexture("_BaseMap", texture);
                materials[i].SetTextureScale("_BaseMap", new Vector2(1f, 3f));
                materials[i].SetFloat("_Smoothness", i == 3 ? 0.62f : 0.12f);
                EditorUtility.SetDirty(materials[i]);
            }
            return materials;
        }

        private static Texture2D GetOrCreateFacadeTexture(int variant, Color wallColor)
        {
            string path = $"{TextureFolder}/T_LegacyFacade_{variant}.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
            {
                return existing;
            }

            const int size = 128;
            const int cells = 4;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = $"T_LegacyFacade_{variant}",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            Color32[] pixels = new Color32[size * size];
            Color32 wall = wallColor;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = wall;
            }

            System.Random random = new System.Random(9000 + variant);
            int cellSize = size / cells;
            for (int cy = 0; cy < cells; cy++)
            {
                for (int cx = 0; cx < cells; cx++)
                {
                    int minX = cx * cellSize + 6;
                    int maxX = (cx + 1) * cellSize - 6;
                    int minY = cy * cellSize + 8;
                    int maxY = (cy + 1) * cellSize - 7;
                    Color32 window = random.NextDouble() < 0.16
                        ? new Color32(232, 195, 122, 255)
                        : new Color32(43, 58, 72, 255);
                    for (int y = minY; y < maxY; y++)
                    {
                        for (int x = minX; x < maxX; x++)
                        {
                            bool frame = x == minX || y == minY || x == maxX - 1 || y == maxY - 1;
                            pixels[y * size + x] = frame ? new Color32(20, 25, 30, 255) : window;
                        }
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static Mesh BuildPaintMesh(List<PaintRect> markings)
        {
            List<Vector3> vertices = new List<Vector3>(markings.Count * 4);
            List<Vector2> uvs = new List<Vector2>(markings.Count * 4);
            List<int> triangles = new List<int>(markings.Count * 6);
            foreach (PaintRect marking in markings)
            {
                Quaternion rotation = Quaternion.Euler(0f, marking.Yaw, 0f);
                Vector3 right = rotation * Vector3.right * marking.Size.x * 0.5f;
                Vector3 forward = rotation * Vector3.forward * marking.Size.y * 0.5f;
                int start = vertices.Count;
                vertices.Add(marking.Center - right - forward);
                vertices.Add(marking.Center + right - forward);
                vertices.Add(marking.Center + right + forward);
                vertices.Add(marking.Center - right + forward);
                uvs.Add(Vector2.zero);
                uvs.Add(Vector2.right);
                uvs.Add(Vector2.one);
                uvs.Add(Vector2.up);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start);
                triangles.Add(start + 3);
                triangles.Add(start + 2);
            }

            Mesh mesh = new Mesh { name = "LegacyRoadPaint" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material GetOrCreateMaterial(string name, Color color, float alpha = 1f)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            color.a = alpha;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.12f);
            if (alpha < 1f)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = 3000;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateBox(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent,
            bool removeCollider = false)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            if (removeCollider)
            {
                UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
            }
            return gameObject;
        }

        private static GameObject CreateLocalBox(
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Transform parent,
            bool removeCollider = true)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            if (removeCollider)
            {
                UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
            }
            return gameObject;
        }

        private static void CreateCylinder(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent);
            cylinder.transform.localPosition = position;
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        }

        private static bool NearStreet(float value, float radius)
        {
            foreach (float street in Streets)
            {
                if (Mathf.Abs(value - street) < radius)
                {
                    return true;
                }
            }
            return false;
        }

        private static float NearestStreetDistance(float value)
        {
            float distance = float.PositiveInfinity;
            foreach (float street in Streets)
            {
                distance = Mathf.Min(distance, Mathf.Abs(value - street));
            }
            return distance;
        }

        private static float NearestStreetDirection(float value)
        {
            float nearest = Streets[0];
            float distance = Mathf.Abs(value - nearest);
            foreach (float street in Streets)
            {
                float candidate = Mathf.Abs(value - street);
                if (candidate < distance)
                {
                    distance = candidate;
                    nearest = street;
                }
            }
            return Mathf.Sign(nearest - value) == 0f ? 1f : Mathf.Sign(nearest - value);
        }

        private static float NextFloat(System.Random random)
        {
            return (float)random.NextDouble();
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return found;
        }

        private static void SetStaticRecursively(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                // Wind foliage deforms in object space and uses GPU instancing;
                // static batching would bake away the per-tree transform.
                if (child.name == "Foliage")
                {
                    child.gameObject.isStatic = false;
                    continue;
                }

                // The baked kit buildings are already one combined mesh each.
                // Feeding them to static batching duplicates their large
                // vertex buffers into batch storage and tanks the frame rate,
                // so they keep every static flag except batching.
                MeshFilter filter = child.GetComponent<MeshFilter>();
                if (filter != null
                    && filter.sharedMesh != null
                    && filter.sharedMesh.vertexCount > 32000)
                {
                    GameObjectUtility.SetStaticEditorFlags(
                        child.gameObject,
                        (StaticEditorFlags)~0 & ~StaticEditorFlags.BatchingStatic);
                    continue;
                }

                child.gameObject.isStatic = true;
            }
        }

        private static void ConfigureEnvironmentLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.87f, 0.92f, 0.96f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.61f, 0.59f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.35f, 0.31f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.81f, 0.87f, 0.91f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 220f;
            RenderSettings.fogEndDistance = 600f;

            const string skyPath = MaterialFolder + "/M_LegacySky.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural")) { name = "M_LegacySky" };
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            sky.SetColor("_SkyTint", new Color(0.56f, 0.73f, 0.88f));
            sky.SetColor("_GroundColor", new Color(0.45f, 0.48f, 0.46f));
            sky.SetFloat("_AtmosphereThickness", 0.75f);
            sky.SetFloat("_Exposure", 1.15f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
        }
    }
}
