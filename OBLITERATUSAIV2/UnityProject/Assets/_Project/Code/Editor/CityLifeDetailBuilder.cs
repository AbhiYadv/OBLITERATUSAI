using System.Collections.Generic;
using ObliteratusAI.City;
using ObliteratusAI.Core;
using UnityEditor;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// City-life detail passes ported from the legacy StreetFurniture.tsx,
    /// Billboards.tsx, and cityPlan.ts parked-car algorithm: benches, bins,
    /// bollards, street trees in planters, cafe umbrellas, emissive LED
    /// billboards, and seeded curb-parked cars with colliders.
    /// </summary>
    internal static class CityLifeDetailBuilder
    {
        private const float SlabTop = CityLayout.SlabTop;

        public static void Build(Transform root, int seed)
        {
            BuildStreetFurniture(root, seed);
            BuildBillboards(root);
            BuildParkedCars(root, seed);
        }

        // ------------------------------------------------------------------
        // Street furniture: walk each block edge, every 15 m place a bench,
        // bin, or street tree on the sidewalk band 1.1 m in from the curb,
        // with crossing bollards near the corners. (The legacy per-blade
        // grass verge is cut — thousands of blades don't suit baked objects.)
        // ------------------------------------------------------------------
        private static void BuildStreetFurniture(Transform root, int seed)
        {
            Transform parent = new GameObject("SidewalkFurniture").transform;
            parent.SetParent(root, false);

            Material planter = EditorBuildUtility.GetOrCreateMaterial(
                "M_Planter", new Color(0.42f, 0.384f, 0.353f), 0.1f);
            Material benchWood = EditorBuildUtility.GetOrCreateMaterial(
                "M_BenchWood", new Color(0.478f, 0.361f, 0.243f), 0.15f);
            Material benchLeg = EditorBuildUtility.GetOrCreateMaterial(
                "M_BenchLeg", new Color(0.227f, 0.239f, 0.251f), 0.5f, 0.5f);
            Material bin = EditorBuildUtility.GetOrCreateMaterial(
                "M_Bin", new Color(0.184f, 0.357f, 0.271f), 0.3f, 0.3f);
            Material bollard = EditorBuildUtility.GetOrCreateMaterial(
                "M_Bollard", new Color(0.604f, 0.635f, 0.659f), 0.65f, 0.7f);
            Material umbrellaPole = EditorBuildUtility.GetOrCreateMaterial(
                "M_UmbrellaPole", new Color(0.29f, 0.306f, 0.322f), 0.5f, 0.4f);
            Material umbrella = EditorBuildUtility.GetOrCreateMaterial(
                "M_Umbrella", new Color(0.69f, 0.204f, 0.173f), 0.25f);

            GameObject[] treePrefabs = TreeMeshBaker.EnsureTreePrefabs(seed ^ 0x5721);
            DeterministicRandom rand = new DeterministicRandom(unchecked((uint)seed ^ 0xf0a71au));
            int treeIndex = 0;

            foreach (CityLayout.BlockRect block in CityLayout.Blocks)
            {
                var edges = new (float ax, float az, float bx, float bz)[]
                {
                    (block.MinX, block.MinZ, block.MaxX, block.MinZ),
                    (block.MaxX, block.MinZ, block.MaxX, block.MaxZ),
                    (block.MaxX, block.MaxZ, block.MinX, block.MaxZ),
                    (block.MinX, block.MaxZ, block.MinX, block.MinZ)
                };
                foreach (var edge in edges)
                {
                    FillEdge(parent, ref rand, edge.ax, edge.az, edge.bx, edge.bz,
                        planter, benchWood, benchLeg, bin, bollard, umbrellaPole, umbrella,
                        treePrefabs, ref treeIndex);
                }
            }
        }

        private static void FillEdge(
            Transform parent,
            ref DeterministicRandom rand,
            float ax, float az, float bx, float bz,
            Material planter, Material benchWood, Material benchLeg, Material bin,
            Material bollard, Material umbrellaPole, Material umbrella,
            GameObject[] treePrefabs, ref int treeIndex)
        {
            float len = Mathf.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az));
            float dx = (bx - ax) / len;
            float dz = (bz - az) / len;
            // Inward normal (block corners are traversed counter-clockwise).
            float inX = -dz;
            float inZ = dx;
            float rot = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;

            // The pedestrian loop walks 1.9 m inside the slab edge; the tree/
            // bin line sits at 0.95 m and benches at 2.9 m so neither side of
            // the walking line is obstructed.
            int slot = rand.NextInt(3);
            for (float t = 9f; t < len - 9f; t += 15f)
            {
                float px = ax + dx * t + inX * 0.95f;
                float pz = az + dz * t + inZ * 0.95f;
                int kind = slot++ % 5;
                if (kind == 2)
                {
                    CreateBench(parent,
                        new Vector3(px + inX * 1.95f, SlabTop, pz + inZ * 1.95f),
                        rot + 180f, benchWood, benchLeg);
                }
                else if (kind == 4)
                {
                    CreateBin(parent, new Vector3(px, SlabTop, pz), bin);
                }
                else
                {
                    // Street-tree scale: base tree is ~7.5 m, 0.55-0.75 gives 4-5.5 m.
                    float treeRotation = rand.NextFloat() * 360f;
                    float scale = 0.55f + rand.NextFloat() * 0.2f;
                    CreateStreetTree(parent, new Vector3(px, SlabTop, pz), treeRotation, scale,
                        treePrefabs[treeIndex++ % treePrefabs.Length], planter);
                }
            }

            // Crossing bollards near both corners.
            foreach (float s in new[] { 2f, 3.6f, 5.2f })
            {
                CreateBollard(parent,
                    new Vector3(ax + dx * s + inX * 0.55f, SlabTop, az + dz * s + inZ * 0.55f), bollard);
                CreateBollard(parent,
                    new Vector3(bx - dx * s + inX * 0.55f, SlabTop, bz - dz * s + inZ * 0.55f), bollard);
            }

            // Occasional cafe umbrella.
            if (rand.NextFloat() < 0.18f)
            {
                float t = 12f + rand.NextFloat() * (len - 24f);
                CreateUmbrella(parent,
                    new Vector3(ax + dx * t + inX * 2.6f, SlabTop, az + dz * t + inZ * 2.6f),
                    umbrellaPole, umbrella);
            }
        }

        private static void CreateStreetTree(
            Transform parent, Vector3 position, float rotation, float scale,
            GameObject treePrefab, Material planterMaterial)
        {
            GameObject item = new GameObject("StreetTree");
            item.transform.SetParent(parent, false);
            item.transform.position = position;

            GameObject tree = PrefabUtility.InstantiatePrefab(treePrefab, item.transform) as GameObject;
            if (tree != null)
            {
                tree.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
                tree.transform.localScale = Vector3.one * scale;
            }
            EditorBuildUtility.CreateLocalBox("Planter", new Vector3(0f, 0.16f, 0f),
                new Vector3(1.3f, 0.32f, 1.3f), planterMaterial, item.transform, false);
        }

        private static void CreateBench(
            Transform parent, Vector3 position, float rotation, Material wood, Material leg)
        {
            GameObject bench = new GameObject("Bench");
            bench.transform.SetParent(parent, false);
            bench.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation, 0f));
            EditorBuildUtility.CreateLocalBox("Seat", new Vector3(0f, 0.46f, 0f),
                new Vector3(1.75f, 0.09f, 0.55f), wood, bench.transform);
            EditorBuildUtility.CreateLocalBox("Back", new Vector3(0f, 0.75f, -0.26f),
                new Vector3(1.75f, 0.5f, 0.07f), wood, bench.transform);
            EditorBuildUtility.CreateLocalBox("Leg", new Vector3(0f, 0.23f, 0f),
                new Vector3(0.08f, 0.46f, 0.5f), leg, bench.transform);
            BoxCollider collider = bench.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.42f, 0f);
            collider.size = new Vector3(1.92f, 0.84f, 0.84f);
        }

        private static void CreateBin(Transform parent, Vector3 position, Material material)
        {
            GameObject item = new GameObject("Bin");
            item.transform.SetParent(parent, false);
            item.transform.position = position;
            EditorBuildUtility.CreateLocalCylinder("Body", new Vector3(0f, 0.39f, 0f),
                new Vector3(0.52f, 0.39f, 0.52f), material, item.transform);
            BoxCollider collider = item.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.39f, 0f);
            collider.size = new Vector3(0.52f, 0.78f, 0.52f);
        }

        private static void CreateBollard(Transform parent, Vector3 position, Material material)
        {
            GameObject item = new GameObject("Bollard");
            item.transform.SetParent(parent, false);
            item.transform.position = position;
            EditorBuildUtility.CreateLocalCylinder("Post", new Vector3(0f, 0.4f, 0f),
                new Vector3(0.17f, 0.4f, 0.17f), material, item.transform);
            BoxCollider collider = item.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.4f, 0f);
            collider.size = new Vector3(0.32f, 0.8f, 0.32f);
        }

        private static void CreateUmbrella(
            Transform parent, Vector3 position, Material poleMaterial, Material canopyMaterial)
        {
            GameObject item = new GameObject("CafeUmbrella");
            item.transform.SetParent(parent, false);
            item.transform.position = position;
            EditorBuildUtility.CreateLocalCylinder("Pole", new Vector3(0f, 1.17f, 0f),
                new Vector3(0.07f, 1.17f, 0.07f), poleMaterial, item.transform);
            // Flattened cylinder standing in for the legacy cone canopy.
            EditorBuildUtility.CreateLocalCylinder("Canopy", new Vector3(0f, 2.4f, 0f),
                new Vector3(3.0f, 0.27f, 3.0f), canopyMaterial, item.transform);
            BoxCollider collider = item.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.18f, 0f);
            collider.size = new Vector3(0.32f, 2.36f, 0.32f);
        }

        // ------------------------------------------------------------------
        // Billboards: the four legacy positions get emissive baked LED
        // designs (block graphics; canvas text does not port) with mounting
        // frames instead of flat color boxes.
        // ------------------------------------------------------------------
        private static void BuildBillboards(Transform root)
        {
            Transform parent = new GameObject("Billboards").transform;
            parent.SetParent(root, false);
            Material frame = EditorBuildUtility.GetOrCreateMaterial(
                "M_BillboardFrame", new Color(0.11f, 0.12f, 0.13f), 0.4f, 0.5f);

            var boards = new (Vector3 position, float yaw)[]
            {
                (new Vector3(-110f, 10f, -42f), 0f),
                (new Vector3(38f, 12f, -108f), 90f),
                (new Vector3(112f, 11f, 42f), 180f),
                (new Vector3(-38f, 14f, 108f), -90f)
            };
            for (int i = 0; i < boards.Length; i++)
            {
                Material face = CreateBillboardMaterial(i);
                GameObject board = new GameObject($"Billboard_{i}");
                board.transform.SetParent(parent, false);
                board.transform.SetPositionAndRotation(
                    boards[i].position, Quaternion.Euler(0f, boards[i].yaw, 0f));
                EditorBuildUtility.CreateLocalBox("Frame", Vector3.zero,
                    new Vector3(10.4f, 4.6f, 0.3f), frame, board.transform, false);
                EditorBuildUtility.CreateLocalBox("Face", new Vector3(0f, 0f, -0.18f),
                    new Vector3(10f, 4.2f, 0.06f), face, board.transform);
                float dropHeight = boards[i].position.y - 2.1f;
                foreach (float side in new[] { -3.4f, 3.4f })
                {
                    EditorBuildUtility.CreateLocalCylinder("Support",
                        new Vector3(side, -2.1f - dropHeight * 0.5f, 0.1f),
                        new Vector3(0.16f, dropHeight * 0.5f, 0.16f), frame, board.transform);
                }
            }
        }

        private static Material CreateBillboardMaterial(int design)
        {
            Texture2D texture = GetOrCreateBillboardTexture(design);
            Material material = EditorBuildUtility.GetOrCreateEmissiveMaterial(
                $"M_BillboardFace_{design}", Color.white, 1.1f);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_EmissionColor", Color.white * 1.1f);
            material.SetTexture("_EmissionMap", texture);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D GetOrCreateBillboardTexture(int design)
        {
            string path = $"Assets/_Project/Art/Textures/T_Billboard_{design}.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
            {
                return existing;
            }

            const int width = 256;
            const int height = 112;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = $"T_Billboard_{design}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color32[] pixels = new Color32[width * height];

            void Fill(Color32 color, int x0, int y0, int w, int h)
            {
                int x1 = Mathf.Min(width, x0 + w);
                int y1 = Mathf.Min(height, y0 + h);
                for (int y = Mathf.Max(0, y0); y < y1; y++)
                {
                    for (int x = Mathf.Max(0, x0); x < x1; x++)
                    {
                        pixels[y * width + x] = color;
                    }
                }
            }

            switch (design % 4)
            {
                case 0:
                    // Split panel: white field with a magenta accent column.
                    Fill(new Color32(16, 35, 63, 255), 0, 0, width, height);
                    Fill(new Color32(242, 245, 248, 255), 0, 0, (int)(width * 0.68f), height);
                    Fill(new Color32(224, 52, 140, 255), (int)(width * 0.68f), 0, width, height);
                    Fill(new Color32(18, 50, 92, 255), 20, height - 40, 96, 16);
                    Fill(new Color32(91, 108, 128, 255), 20, height - 62, 70, 10);
                    break;
                case 1:
                    // LED bar chart on a dark field.
                    Fill(new Color32(16, 24, 32, 255), 0, 0, width, height);
                    for (int i = 0; i < 5; i++)
                    {
                        int barHeight = 24 + ((i * 37) % 44);
                        Color32 bar = i % 3 == 0
                            ? new Color32(87, 200, 255, 255)
                            : new Color32(43, 127, 184, 255);
                        Fill(bar, 40 + i * 44, (int)(height * 0.2f), 26, barHeight);
                    }
                    break;
                case 2:
                    // Pixel-block cluster on deep blue.
                    Fill(new Color32(13, 58, 95, 255), 0, 0, width, height);
                    for (int i = 0; i < 3; i++)
                    {
                        Fill(new Color32(90, 209, 240, 255), (int)(width * 0.42f) + i * 30, (int)(height * 0.55f), 24, 18);
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        Fill(new Color32(90, 209, 240, 255), (int)(width * 0.455f) + i * 30, (int)(height * 0.55f) + 24, 24, 18);
                    }
                    Fill(new Color32(230, 240, 248, 255), 24, 24, 90, 12);
                    break;
                default:
                    // Light panel with brand stripes.
                    Fill(new Color32(238, 242, 245, 255), 0, 0, width, height);
                    Fill(new Color32(47, 95, 143, 255), 0, 0, width, 12);
                    Fill(new Color32(47, 95, 143, 255), 0, height - 12, width, 12);
                    Fill(new Color32(47, 95, 143, 255), 28, 40, 120, 14);
                    Fill(new Color32(224, 52, 140, 255), 28, 62, 64, 10);
                    break;
            }

            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        // ------------------------------------------------------------------
        // Parked cars: the legacy seeded curb algorithm, with box colliders
        // so vehicles can no longer be driven through. Editor-baked, so tints
        // are skipped (MaterialPropertyBlocks do not serialize).
        // ------------------------------------------------------------------
        private static void BuildParkedCars(Transform root, int seed)
        {
            TrafficAssetBuilder.Result assets = TrafficAssetBuilder.EnsureAssets();
            GameObject[] visuals =
            {
                assets.Car2 != null ? assets.Car2.VisualPrefab : null,
                assets.Car9 != null ? assets.Car9.VisualPrefab : null
            };
            if (visuals[0] == null && visuals[1] == null)
            {
                Debug.LogWarning("No traffic car visuals available; parked cars skipped.");
                return;
            }

            Transform parent = new GameObject("ParkedCars").transform;
            parent.SetParent(root, false);
            DeterministicRandom rand = new DeterministicRandom(unchecked((uint)seed ^ 0x9a7cedu));
            float[] streets = CityLayout.Streets;
            float min = streets[0];
            float max = streets[streets.Length - 1];
            // Legacy curb offset was RoadWidth/2 - 1.15, which physically
            // overlaps the 2.75 m traffic lane on the 11 m roads and reads
            // as random cars stopped in the street. Parked cars now hug the
            // curb (slightly overhanging it) so the lane stays clear.
            float curb = CityLayout.RoadWidth * 0.5f - 0.55f;

            int placed = 0;
            for (int k = 0; k < 26 && placed < 14; k++)
            {
                float x;
                float z;
                float yaw;
                if (rand.NextFloat() < 0.5f)
                {
                    x = streets[rand.NextInt(streets.Length)];
                    z = min + 25f + rand.NextFloat() * (max - min - 50f);
                    if (NearAnyStreet(z))
                    {
                        continue;
                    }
                    float side = rand.NextFloat() < 0.5f ? 1f : -1f;
                    x += side * curb;
                    yaw = side > 0f ? 180f : 0f;
                }
                else
                {
                    z = streets[rand.NextInt(streets.Length)];
                    x = min + 25f + rand.NextFloat() * (max - min - 50f);
                    if (NearAnyStreet(x))
                    {
                        continue;
                    }
                    float side = rand.NextFloat() < 0.5f ? 1f : -1f;
                    z += side * curb;
                    yaw = side > 0f ? 90f : -90f;
                }

                GameObject visual = visuals[placed % 2] != null ? visuals[placed % 2] : visuals[(placed + 1) % 2];
                GameObject car = new GameObject($"ParkedCar_{placed}");
                car.transform.SetParent(parent, false);
                car.transform.SetPositionAndRotation(
                    new Vector3(x, CityLayout.RoadY, z), Quaternion.Euler(0f, yaw, 0f));
                BoxCollider collider = car.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.95f, 0f);
                collider.size = new Vector3(2.04f, 1.9f, 4.3f);
                PrefabUtility.InstantiatePrefab(visual, car.transform);
                placed++;
            }
        }

        private static bool NearAnyStreet(float value)
        {
            foreach (float street in CityLayout.Streets)
            {
                if (Mathf.Abs(value - street) < 12f)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
