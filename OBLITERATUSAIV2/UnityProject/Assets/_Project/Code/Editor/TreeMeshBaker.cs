using System.Collections.Generic;
using ObliteratusAI.Core;
using UnityEditor;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// Ports the legacy treeBuilder.ts: believable procedural trees (tapered
    /// trunk, primary branches, overlapping irregular leaf clusters — never a
    /// single sphere canopy) baked to mesh assets and wrapped in prefabs, plus
    /// a squashed-icosahedron rock. ~1 in 3 variants is a conifer.
    /// </summary>
    internal static class TreeMeshBaker
    {
        private const string MeshFolder = "Assets/_Project/Art/City/Trees";
        private const string PrefabFolder = "Assets/_Project/Prefabs/City";
        private const string WindShaderName = "OBLITERATUS AI/Tree Wind Lit";
        public const int VariantCount = 5;

        public static GameObject[] EnsureTreePrefabs(int seed)
        {
            EditorBuildUtility.EnsureFolder(MeshFolder);
            EditorBuildUtility.EnsureFolder(PrefabFolder);
            Material bark = EditorBuildUtility.GetOrCreateMaterial(
                "M_TreeBark", new Color(0.416f, 0.290f, 0.188f), 0.05f);
            Material foliage = GetOrCreateWindFoliageMaterial();

            DeterministicRandom rand = new DeterministicRandom(unchecked((uint)seed ^ 0x7ee5u));
            GameObject[] prefabs = new GameObject[VariantCount];
            for (int i = 0; i < VariantCount; i++)
            {
                MeshBuilder barkMesh = new MeshBuilder();
                MeshBuilder foliageMesh = new MeshBuilder();
                BuildTree(ref rand, i % 3 != 0, barkMesh, foliageMesh);
                Mesh barkAsset = SaveMesh(barkMesh.ToMesh($"Tree{i}_Bark"), $"{MeshFolder}/Tree{i}_Bark.asset");
                Mesh foliageAsset = SaveMesh(foliageMesh.ToMesh($"Tree{i}_Foliage"), $"{MeshFolder}/Tree{i}_Foliage.asset");
                prefabs[i] = BuildPrefab(
                    $"TreeVariant_{i}", barkAsset, bark, foliageAsset, foliage,
                    $"{PrefabFolder}/TreeVariant_{i}.prefab");
            }
            return prefabs;
        }

        private static Material GetOrCreateWindFoliageMaterial()
        {
            const string materialPath = "Assets/_Project/Art/Materials/M_TreeFoliage.mat";
            Shader shader = Shader.Find(WindShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"Tree wind shader was not found: {WindShaderName}");
                return EditorBuildUtility.GetOrCreateMaterial(
                    "M_TreeFoliage", new Color(0.290f, 0.478f, 0.204f), 0.15f);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "M_TreeFoliage" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", new Color(0.290f, 0.478f, 0.204f));
            material.SetFloat("_WindHeight", 7.5f);
            material.SetFloat("_SwayStrength", 0.34f);
            material.SetFloat("_FlutterStrength", 0.07f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static GameObject EnsureRockPrefab()
        {
            EditorBuildUtility.EnsureFolder(MeshFolder);
            EditorBuildUtility.EnsureFolder(PrefabFolder);
            Material rockMaterial = EditorBuildUtility.GetOrCreateMaterial(
                "M_Rock", new Color(0.490f, 0.490f, 0.510f), 0.05f);

            MeshBuilder builder = new MeshBuilder();
            Vector3[] vertices = IcosphereVertices(0);
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(vertices[i].x, vertices[i].y * 0.72f, vertices[i].z);
            }
            int[] triangles = IcosphereTriangles(0);
            for (int t = 0; t < triangles.Length; t += 3)
            {
                builder.AddTriangle(
                    vertices[triangles[t]], vertices[triangles[t + 1]], vertices[triangles[t + 2]]);
            }
            Mesh rockMesh = SaveMesh(builder.ToMesh("Rock"), $"{MeshFolder}/Rock.asset");

            GameObject root = new GameObject("Rock");
            GameObject body = new GameObject("Body", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(root.transform, false);
            body.GetComponent<MeshFilter>().sharedMesh = rockMesh;
            body.GetComponent<MeshRenderer>().sharedMaterial = rockMaterial;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/Rock.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildPrefab(
            string name,
            Mesh barkMesh,
            Material barkMaterial,
            Mesh foliageMesh,
            Material foliageMaterial,
            string prefabPath)
        {
            GameObject root = new GameObject(name);
            GameObject bark = new GameObject("Bark", typeof(MeshFilter), typeof(MeshRenderer));
            bark.transform.SetParent(root.transform, false);
            bark.GetComponent<MeshFilter>().sharedMesh = barkMesh;
            bark.GetComponent<MeshRenderer>().sharedMaterial = barkMaterial;
            GameObject foliage = new GameObject("Foliage", typeof(MeshFilter), typeof(MeshRenderer));
            foliage.transform.SetParent(root.transform, false);
            foliage.GetComponent<MeshFilter>().sharedMesh = foliageMesh;
            foliage.GetComponent<MeshRenderer>().sharedMaterial = foliageMaterial;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
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

        private static void BuildTree(
            ref DeterministicRandom rand,
            bool broadleaf,
            MeshBuilder bark,
            MeshBuilder foliage)
        {
            float trunkHeight = broadleaf ? 2.4f + rand.NextFloat() * 0.8f : 3.0f + rand.NextFloat() * 0.9f;
            float rBase = broadleaf ? 0.26f : 0.22f;
            float rTop = rBase * 0.55f;
            float leanX = (rand.NextFloat() - 0.5f) * 0.4f;
            float leanZ = (rand.NextFloat() - 0.5f) * 0.4f;
            AddLimb(bark, Vector3.zero, new Vector3(leanX, trunkHeight, leanZ), rBase, rTop, 7);

            if (broadleaf)
            {
                // 4-6 primary branches fanning from the upper trunk.
                int branches = 4 + rand.NextInt(3);
                List<Vector4> tips = new List<Vector4>(branches);
                for (int i = 0; i < branches; i++)
                {
                    float ang = i / (float)branches * Mathf.PI * 2f + rand.NextFloat() * 0.6f;
                    float startY = trunkHeight * (0.6f + rand.NextFloat() * 0.3f);
                    float spread = 1.4f + rand.NextFloat() * 1.1f;
                    float rise = 1.6f + rand.NextFloat() * 1.3f;
                    float bx = leanX + Mathf.Cos(ang) * spread;
                    float bz = leanZ + Mathf.Sin(ang) * spread;
                    float by = startY + rise;
                    AddLimb(bark,
                        new Vector3(leanX, startY, leanZ),
                        new Vector3(bx, by, bz),
                        rTop * 0.8f, rTop * 0.4f, 5);
                    tips.Add(new Vector4(bx, by, bz, 1.0f + rand.NextFloat() * 0.4f));
                }
                // Foliage: a clump at each branch tip plus a big crown centre.
                float crownY = trunkHeight + 1.9f + rand.NextFloat() * 0.6f;
                AddCluster(foliage, new Vector3(leanX, crownY, leanZ),
                    1.7f + rand.NextFloat() * 0.4f, ref rand);
                foreach (Vector4 tip in tips)
                {
                    AddCluster(foliage, new Vector3(tip.x, tip.y + 0.3f, tip.z), 1.1f * tip.w, ref rand);
                    if (rand.NextFloat() < 0.6f)
                    {
                        AddCluster(foliage,
                            new Vector3(tip.x * 0.6f + leanX * 0.4f, tip.y + 0.9f, tip.z * 0.6f + leanZ * 0.4f),
                            1.0f * tip.w, ref rand);
                    }
                }
            }
            else
            {
                // Conifer: whorls of clusters forming a tapered irregular column.
                int layers = 4 + rand.NextInt(2);
                for (int l = 0; l < layers; l++)
                {
                    float t = l / (float)(layers - 1);
                    float y = trunkHeight * 0.5f + t * (trunkHeight * 1.9f);
                    float r = (1.5f - t) * (1.1f + rand.NextFloat() * 0.3f);
                    int perLayer = 3 + rand.NextInt(2);
                    for (int i = 0; i < perLayer; i++)
                    {
                        float ang = i / (float)perLayer * Mathf.PI * 2f + l * 0.7f;
                        float cx = leanX + Mathf.Cos(ang) * r * 0.5f;
                        float cz = leanZ + Mathf.Sin(ang) * r * 0.5f;
                        AddCluster(foliage, new Vector3(cx, y, cz), Mathf.Max(0.5f, r * 0.9f), ref rand);
                    }
                }
            }
        }

        /// <summary>A tapered open cylinder spanning a-&gt;b.</summary>
        private static void AddLimb(
            MeshBuilder builder,
            Vector3 a,
            Vector3 b,
            float rBottom,
            float rTop,
            int radial)
        {
            Vector3 dir = (b - a).normalized;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, dir);
            for (int i = 0; i < radial; i++)
            {
                float a0 = i / (float)radial * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)radial * Mathf.PI * 2f;
                Vector3 r0 = rotation * new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 r1 = rotation * new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 b0 = a + r0 * rBottom;
                Vector3 b1 = a + r1 * rBottom;
                Vector3 t0 = b + r0 * rTop;
                Vector3 t1 = b + r1 * rTop;
                builder.AddTriangle(b0, t0, t1);
                builder.AddTriangle(b0, t1, b1);
            }
        }

        /// <summary>
        /// An irregular leaf cluster: subdivided icosahedron pushed around by
        /// noise and squashed vertically so it reads as a foliage clump.
        /// </summary>
        private static void AddCluster(
            MeshBuilder builder,
            Vector3 center,
            float radius,
            ref DeterministicRandom rand)
        {
            Vector3[] vertices = IcosphereVertices(1);
            for (int i = 0; i < vertices.Length; i++)
            {
                float jitter = 0.62f + rand.NextFloat() * 0.5f;
                Vector3 v = vertices[i] * radius * jitter;
                v.y *= 0.82f;
                vertices[i] = v + center;
            }
            int[] triangles = IcosphereTriangles(1);
            for (int t = 0; t < triangles.Length; t += 3)
            {
                builder.AddTriangle(
                    vertices[triangles[t]], vertices[triangles[t + 1]], vertices[triangles[t + 2]]);
            }
        }

        private static Vector3[] _icoVertices0;
        private static int[] _icoTriangles0;
        private static Vector3[] _icoVertices1;
        private static int[] _icoTriangles1;

        private static Vector3[] IcosphereVertices(int subdivisions)
        {
            EnsureIcosphere();
            Vector3[] source = subdivisions == 0 ? _icoVertices0 : _icoVertices1;
            return (Vector3[])source.Clone();
        }

        private static int[] IcosphereTriangles(int subdivisions)
        {
            EnsureIcosphere();
            return subdivisions == 0 ? _icoTriangles0 : _icoTriangles1;
        }

        private static void EnsureIcosphere()
        {
            if (_icoVertices0 != null)
            {
                return;
            }

            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            List<Vector3> vertices = new List<Vector3>
            {
                new Vector3(-1f, t, 0f), new Vector3(1f, t, 0f),
                new Vector3(-1f, -t, 0f), new Vector3(1f, -t, 0f),
                new Vector3(0f, -1f, t), new Vector3(0f, 1f, t),
                new Vector3(0f, -1f, -t), new Vector3(0f, 1f, -t),
                new Vector3(t, 0f, -1f), new Vector3(t, 0f, 1f),
                new Vector3(-t, 0f, -1f), new Vector3(-t, 0f, 1f)
            };
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i].normalized;
            }
            int[] faces =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };
            _icoVertices0 = vertices.ToArray();
            _icoTriangles0 = faces;

            // One subdivision: split each face into 4, midpoints normalized.
            Dictionary<long, int> midpointCache = new Dictionary<long, int>();
            List<Vector3> subVertices = new List<Vector3>(vertices);
            List<int> subFaces = new List<int>(faces.Length * 4);
            int Midpoint(int i0, int i1)
            {
                long key = i0 < i1 ? ((long)i0 << 32) | (uint)i1 : ((long)i1 << 32) | (uint)i0;
                if (midpointCache.TryGetValue(key, out int index))
                {
                    return index;
                }
                Vector3 mid = ((subVertices[i0] + subVertices[i1]) * 0.5f).normalized;
                subVertices.Add(mid);
                index = subVertices.Count - 1;
                midpointCache[key] = index;
                return index;
            }
            for (int f = 0; f < faces.Length; f += 3)
            {
                int v0 = faces[f];
                int v1 = faces[f + 1];
                int v2 = faces[f + 2];
                int m01 = Midpoint(v0, v1);
                int m12 = Midpoint(v1, v2);
                int m20 = Midpoint(v2, v0);
                subFaces.AddRange(new[] { v0, m01, m20, v1, m12, m01, v2, m20, m12, m01, m12, m20 });
            }
            _icoVertices1 = subVertices.ToArray();
            _icoTriangles1 = subFaces.ToArray();
        }

        /// <summary>Accumulates flat-shaded (non-indexed) triangles.</summary>
        private sealed class MeshBuilder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<int> _triangles = new List<int>();

            public void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
            {
                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                int start = _vertices.Count;
                _vertices.Add(v0);
                _vertices.Add(v1);
                _vertices.Add(v2);
                _normals.Add(normal);
                _normals.Add(normal);
                _normals.Add(normal);
                _triangles.Add(start);
                _triangles.Add(start + 1);
                _triangles.Add(start + 2);
            }

            public Mesh ToMesh(string name)
            {
                Mesh mesh = new Mesh { name = name };
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
