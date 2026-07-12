using System.Collections.Generic;
using ObliteratusAI.Core;
using ObliteratusAI.World;
using UnityEditor;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// Bakes the analytic terrain surround (hills, river, meadow), the water
    /// plane, and the seeded tree/rock scatter that replace the legacy
    /// OuterTerrain/WesternWater placeholder boxes. Chunk meshes get
    /// MeshColliders so the player can walk the hills. Everything is
    /// editor-baked; nothing here runs per-frame.
    /// </summary>
    internal static class LegacySurroundBuilder
    {
        public const int DefaultSeed = 20260702;

        private const int ChunksPerSide = 8;
        private const float ChunkSize = TerrainSampler.WorldSize / ChunksPerSide;
        private const string MeshFolder = "Assets/_Project/Art/City/Surround";
        private const string TextureFolder = "Assets/_Project/Art/Textures";
        private const int SplatSize = 512;
        private const int MaxTrees = 350;
        private const int MaxRocks = 80;

        public static void Build(Transform parent, int seed)
        {
            TerrainSampler.SetSeed(seed);
            EditorBuildUtility.EnsureFolder(MeshFolder);

            GameObject root = new GameObject("Surround");
            root.transform.SetParent(parent, false);

            Material terrain = CreateTerrainMaterial(seed);
            BuildChunks(root.transform, terrain);
            BuildWater(root.transform);
            ScatterVegetation(root.transform, seed);
        }

        private static void BuildChunks(Transform parent, Material terrainMaterial)
        {
            for (int cx = 0; cx < ChunksPerSide; cx++)
            {
                for (int cz = 0; cz < ChunksPerSide; cz++)
                {
                    float originX = -TerrainSampler.HalfWorld + cx * ChunkSize;
                    float originZ = -TerrainSampler.HalfWorld + cz * ChunkSize;

                    // Chunks fully under the city plateau are covered by the
                    // foundation slab; skip their geometry entirely.
                    if (IsInsideCity(originX, originZ)
                        && IsInsideCity(originX + ChunkSize, originZ + ChunkSize))
                    {
                        continue;
                    }

                    float centerX = originX + ChunkSize * 0.5f;
                    float centerZ = originZ + ChunkSize * 0.5f;
                    int resolution = Mathf.Max(Mathf.Abs(centerX), Mathf.Abs(centerZ)) < 320f ? 33 : 17;

                    Mesh mesh = BuildChunkMesh(originX, originZ, resolution);
                    mesh = SaveMesh(mesh, $"{MeshFolder}/Chunk_{cx}_{cz}.asset");

                    GameObject chunk = new GameObject(
                        $"TerrainChunk_{cx}_{cz}", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                    chunk.transform.SetParent(parent, false);
                    chunk.GetComponent<MeshFilter>().sharedMesh = mesh;
                    chunk.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
                    chunk.GetComponent<MeshCollider>().sharedMesh = mesh;
                }
            }
        }

        private static bool IsInsideCity(float x, float z)
        {
            return Mathf.Abs(x) <= TerrainSampler.CityEdge && Mathf.Abs(z) <= TerrainSampler.CityEdge;
        }

        private static Mesh BuildChunkMesh(float originX, float originZ, int resolution)
        {
            float step = ChunkSize / (resolution - 1);
            Vector3[] vertices = new Vector3[resolution * resolution];
            Vector3[] normals = new Vector3[resolution * resolution];
            Vector2[] uvs = new Vector2[resolution * resolution];
            const float e = 1.5f;

            for (int iz = 0; iz < resolution; iz++)
            {
                for (int ix = 0; ix < resolution; ix++)
                {
                    float x = originX + ix * step;
                    float z = originZ + iz * step;
                    TerrainSampler.TerrainSample sample = TerrainSampler.Sample(x, z);
                    // Dip the apron slightly under the foundation slab so the
                    // co-planar region cannot z-fight with it.
                    float height = sample.Height - sample.CityMask * 0.2f;
                    int index = iz * resolution + ix;
                    vertices[index] = new Vector3(x, height, z);
                    float hl = TerrainSampler.HeightAt(x - e, z);
                    float hr = TerrainSampler.HeightAt(x + e, z);
                    float hd = TerrainSampler.HeightAt(x, z - e);
                    float hu = TerrainSampler.HeightAt(x, z + e);
                    normals[index] = new Vector3(hl - hr, 2f * e, hd - hu).normalized;
                    uvs[index] = new Vector2(
                        x / TerrainSampler.WorldSize + 0.5f,
                        z / TerrainSampler.WorldSize + 0.5f);
                }
            }

            int quads = (resolution - 1) * (resolution - 1);
            int[] triangles = new int[quads * 6];
            int t = 0;
            for (int iz = 0; iz < resolution - 1; iz++)
            {
                for (int ix = 0; ix < resolution - 1; ix++)
                {
                    int i00 = iz * resolution + ix;
                    int i10 = i00 + 1;
                    int i01 = i00 + resolution;
                    int i11 = i01 + 1;
                    triangles[t++] = i00;
                    triangles[t++] = i01;
                    triangles[t++] = i11;
                    triangles[t++] = i00;
                    triangles[t++] = i11;
                    triangles[t++] = i10;
                }
            }

            Mesh mesh = new Mesh { name = "TerrainChunk" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// The legacy terrain used per-vertex colors; URP/Lit has no vertex
        /// color path, so the same color ramp is baked into one world-space
        /// splat texture (2 m/texel) shared by every chunk.
        /// </summary>
        private static Material CreateTerrainMaterial(int seed)
        {
            EditorBuildUtility.EnsureFolder(TextureFolder);
            string texturePath = $"{TextureFolder}/T_SurroundSplat.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            bool created = texture == null;
            if (created)
            {
                texture = new Texture2D(SplatSize, SplatSize, TextureFormat.RGBA32, true)
                {
                    name = "T_SurroundSplat",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            const float e = 1.5f;
            Color32[] pixels = new Color32[SplatSize * SplatSize];
            for (int pz = 0; pz < SplatSize; pz++)
            {
                for (int px = 0; px < SplatSize; px++)
                {
                    float x = (px / (float)(SplatSize - 1) - 0.5f) * TerrainSampler.WorldSize;
                    float z = (pz / (float)(SplatSize - 1) - 0.5f) * TerrainSampler.WorldSize;
                    TerrainSampler.TerrainSample sample = TerrainSampler.Sample(x, z);
                    float slope = Mathf.Max(
                        Mathf.Abs(TerrainSampler.HeightAt(x + e, z) - TerrainSampler.HeightAt(x - e, z)),
                        Mathf.Abs(TerrainSampler.HeightAt(x, z + e) - TerrainSampler.HeightAt(x, z - e)))
                        / (2f * e);
                    pixels[pz * SplatSize + px] = TerrainSampler.ColorAt(sample, x, z, slope);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            if (created)
            {
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            else
            {
                EditorUtility.SetDirty(texture);
            }

            Material material = EditorBuildUtility.GetOrCreateMaterial(
                "M_SurroundTerrain", Color.white, 0.05f);
            material.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildWater(Transform parent)
        {
            Material water = EditorBuildUtility.GetOrCreateMaterial(
                "M_LegacyWater", new Color(0.24f, 0.49f, 0.65f, 0.72f), 0.85f);
            water.SetFloat("_Surface", 1f);
            water.SetFloat("_ZWrite", 0f);
            water.renderQueue = 3000;
            EditorUtility.SetDirty(water);

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plane.name = "Water";
            plane.transform.SetParent(parent, false);
            plane.transform.localPosition = new Vector3(0f, TerrainSampler.WaterLevel - 0.04f, 0f);
            plane.transform.localScale = new Vector3(
                TerrainSampler.WorldSize, 0.08f, TerrainSampler.WorldSize);
            plane.GetComponent<Renderer>().sharedMaterial = water;
            Object.DestroyImmediate(plane.GetComponent<Collider>());
        }

        private static void ScatterVegetation(Transform parent, int seed)
        {
            GameObject[] treePrefabs = TreeMeshBaker.EnsureTreePrefabs(seed);
            GameObject rockPrefab = TreeMeshBaker.EnsureRockPrefab();

            Transform trees = new GameObject("Trees").transform;
            trees.SetParent(parent, false);
            List<Placement> treePlacements = ScatterTrees(seed);
            for (int i = 0; i < treePlacements.Count; i++)
            {
                PlaceInstance(treePrefabs[i % treePrefabs.Length], trees, treePlacements[i], $"Tree_{i}");
            }

            Transform rocks = new GameObject("Rocks").transform;
            rocks.SetParent(parent, false);
            List<Placement> rockPlacements = ScatterRocks(seed);
            for (int i = 0; i < rockPlacements.Count; i++)
            {
                PlaceInstance(rockPrefab, rocks, rockPlacements[i], $"Rock_{i}");
            }
        }

        private struct Placement
        {
            public Vector3 Position;
            public float Yaw;
            public float Scale;
        }

        private static void PlaceInstance(GameObject prefab, Transform parent, Placement p, string name)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                return;
            }
            instance.name = name;
            instance.transform.SetPositionAndRotation(
                p.Position, Quaternion.Euler(0f, p.Yaw * Mathf.Rad2Deg, 0f));
            instance.transform.localScale = Vector3.one * p.Scale;
        }

        private static List<Placement> ScatterTrees(int seed)
        {
            List<Placement> output = new List<Placement>();
            DeterministicRandom rand = new DeterministicRandom(unchecked((uint)seed ^ 0x51ab3eu));
            var zones = new (float cx, float cz, float r, int attempts)[]
            {
                (-60f, -370f, 200f, 900),
                (-370f, 300f, 180f, 700)
            };
            foreach (var zone in zones)
            {
                for (int i = 0; i < zone.attempts && output.Count < MaxTrees - 60; i++)
                {
                    float ang = rand.NextFloat() * Mathf.PI * 2f;
                    float rad = Mathf.Sqrt(rand.NextFloat()) * zone.r;
                    float x = zone.cx + Mathf.Cos(ang) * rad;
                    float z = zone.cz + Mathf.Sin(ang) * rad;
                    if (Mathf.Abs(x) > TerrainSampler.HalfWorld - 30f
                        || Mathf.Abs(z) > TerrainSampler.HalfWorld - 30f)
                    {
                        continue;
                    }
                    TerrainSampler.TerrainSample s = TerrainSampler.Sample(x, z);
                    if (s.ForestMask < 0.3f || s.CityMask > 0.02f)
                    {
                        continue;
                    }
                    if (s.Height < TerrainSampler.WaterLevel + 1.2f)
                    {
                        continue;
                    }
                    // Density falls off toward the zone edge for a treeline.
                    if (rand.NextFloat() > s.ForestMask * 0.75f + 0.15f)
                    {
                        continue;
                    }
                    output.Add(new Placement
                    {
                        Position = new Vector3(x, s.Height - 0.15f, z),
                        Yaw = rand.NextFloat() * Mathf.PI * 2f,
                        Scale = 0.85f + rand.NextFloat() * 0.5f
                    });
                }
            }
            // Sparse lone trees across the open surround.
            for (int i = 0; i < 400 && output.Count < MaxTrees; i++)
            {
                float x = (rand.NextFloat() * 2f - 1f) * (TerrainSampler.HalfWorld - 60f);
                float z = (rand.NextFloat() * 2f - 1f) * (TerrainSampler.HalfWorld - 60f);
                if (Noise.ValueNoise2(unchecked((uint)seed ^ 0x77f1u), x / 210f, z / 210f) < 0.45f)
                {
                    continue;
                }
                TerrainSampler.TerrainSample s = TerrainSampler.Sample(x, z);
                if (s.ForestMask > 0.3f || s.CityMask > 0.02f)
                {
                    continue;
                }
                if (s.Height < TerrainSampler.WaterLevel + 1.2f || s.Height > 50f)
                {
                    continue;
                }
                if (rand.NextFloat() > 0.35f)
                {
                    continue;
                }
                output.Add(new Placement
                {
                    Position = new Vector3(x, s.Height - 0.15f, z),
                    Yaw = rand.NextFloat() * Mathf.PI * 2f,
                    Scale = 0.95f + rand.NextFloat() * 0.55f
                });
            }
            return output;
        }

        private static List<Placement> ScatterRocks(int seed)
        {
            List<Placement> output = new List<Placement>();
            DeterministicRandom rand = new DeterministicRandom(unchecked((uint)seed ^ 0x0dd5c4u));
            for (int i = 0; i < 500 && output.Count < MaxRocks; i++)
            {
                float x = (rand.NextFloat() * 2f - 1f) * (TerrainSampler.HalfWorld - 40f);
                float z = (rand.NextFloat() * 2f - 1f) * (TerrainSampler.HalfWorld - 40f);
                TerrainSampler.TerrainSample s = TerrainSampler.Sample(x, z);
                bool nearRiver = s.RiverDist < 55f;
                if (s.HillMask < 0.25f && !nearRiver)
                {
                    continue;
                }
                if (s.CityMask > 0.02f || s.Height < TerrainSampler.WaterLevel - 1.5f)
                {
                    continue;
                }
                if (rand.NextFloat() > 0.5f)
                {
                    continue;
                }
                output.Add(new Placement
                {
                    Position = new Vector3(x, s.Height - 0.3f, z),
                    Yaw = rand.NextFloat() * Mathf.PI * 2f,
                    Scale = 0.5f + rand.NextFloat() * 1.7f
                });
            }
            return output;
        }

        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }
    }
}
