using System.Collections.Generic;
using ObliteratusAI.City;
using UnityEditor;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    internal static class QuaterniusCityAssetBuilder
    {
        private const string SourceRoot = QuaterniusCityImportProcessor.SourceRoot;
        private const string ModelFolder = SourceRoot + "/Models";
        private const string TextureFolder = SourceRoot + "/Textures";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Quaternius";
        private const string PrefabFolder = "Assets/_Project/Prefabs/City/Quaternius";
        private const string CatalogPath =
            "Assets/_Project/Data/Buildings/QuaterniusDowntownVisualSet.asset";

        private readonly struct BuildingSpec
        {
            public readonly string Id;
            public readonly string SourceFile;
            public readonly bool SupportsCornerPlacement;

            public BuildingSpec(string id, string sourceFile, bool supportsCornerPlacement)
            {
                Id = id;
                SourceFile = sourceFile;
                SupportsCornerPlacement = supportsCornerPlacement;
            }
        }

        private static readonly BuildingSpec[] BuildingSpecs =
        {
            new BuildingSpec("Small", "Building_Small_1.fbx", false),
            new BuildingSpec("Medium", "Building_Medium_2_001.fbx", false),
            new BuildingSpec("Large", "Building_Large_2.fbx", true)
        };

        [MenuItem("OBLITERATUS AI/Assets/Rebuild Quaternius Downtown Visuals")]
        public static void RebuildFromMenu()
        {
            CityVisualSet catalog = EnsureAssets();
            AssetDatabase.SaveAssets();
            Debug.Log(catalog != null
                ? $"Rebuilt Quaternius city visuals with {catalog.BuildingVariantCount} building variants."
                : "Quaternius city visuals could not be built; procedural fallbacks remain active.");
        }

        public static CityVisualSet EnsureAssets()
        {
            EditorBuildUtility.EnsureFolder(MaterialFolder);
            EditorBuildUtility.EnsureFolder(PrefabFolder);
            EditorBuildUtility.EnsureFolder("Assets/_Project/Data/Buildings");

            Dictionary<string, Material> materials = EnsureMaterials();
            List<CityVisualSet.BuildingVariant> variants =
                new List<CityVisualSet.BuildingVariant>(BuildingSpecs.Length);

            foreach (BuildingSpec spec in BuildingSpecs)
            {
                GameObject prefab = EnsureBuildingPrefab(spec, materials);
                if (prefab == null)
                {
                    continue;
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                Vector3 size = EditorBuildUtility.TryGetCombinedBounds(instance, out Bounds bounds)
                    ? bounds.size
                    : new Vector3(20f, 20f, 20f);
                Object.DestroyImmediate(instance);
                variants.Add(new CityVisualSet.BuildingVariant(
                    spec.Id,
                    prefab,
                    size.y,
                    size.x,
                    size.z,
                    spec.SupportsCornerPlacement));
            }

            // Buildings assembled from the kit's modular wall system: every
            // variant has four detailed facades, so all of them are corner
            // capable and the whole map can drop the procedural boxes.
            variants.AddRange(QuaterniusModularBuildingBaker.BakeVariants(materials, ResolveMaterial));

            if (variants.Count == 0)
            {
                Debug.LogWarning("No complete Quaternius buildings were imported. Using procedural city visuals.");
                return null;
            }

            CityVisualSet catalog = AssetDatabase.LoadAssetAtPath<CityVisualSet>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CityVisualSet>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Configure("Quaternius Downtown City MegaKit Standard (CC0)", variants.ToArray());
            catalog.ConfigureProps(
                EnsurePropPrefab("Bollard", "Prop_Bollard.fbx", materials),
                EnsurePropPrefab("Planter", "Prop_Planter_Single.fbx", materials),
                EnsurePropPrefab("Manhole", "Prop_ManholeCover.fbx", materials),
                EnsurePropPrefab("ACUnit", "Prop_ACUnit.fbx", materials));
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static GameObject EnsurePropPrefab(
            string id,
            string sourceFile,
            IReadOnlyDictionary<string, Material> materials)
        {
            string sourcePath = $"{ModelFolder}/{sourceFile}";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogWarning($"Quaternius prop is not imported: {sourcePath}");
                return null;
            }

            GameObject imported = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (imported == null)
            {
                return null;
            }

            GameObject wrapper = new GameObject($"QuaterniusProp_{id}");
            imported.name = "Model";
            imported.transform.SetParent(wrapper.transform, false);
            foreach (Renderer renderer in wrapper.GetComponentsInChildren<Renderer>(true))
            {
                Material[] remapped = renderer.sharedMaterials;
                for (int i = 0; i < remapped.Length; i++)
                {
                    string sourceName = remapped[i] != null ? remapped[i].name : string.Empty;
                    remapped[i] = ResolveMaterial(sourceName, materials);
                }
                renderer.sharedMaterials = remapped;
            }

            string prefabPath = $"{PrefabFolder}/QuaterniusProp_{id}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
            Object.DestroyImmediate(wrapper);
            return prefab;
        }

        private static Dictionary<string, Material> EnsureMaterials()
        {
            Material asphalt = EnsureLit("M_QC_Asphalt", "T_Concrete_Asphalt_BaseColor.png", null,
                Color.white, 0.08f, 0f);
            Material concrete = EnsureLit("M_QC_Concrete", "T_Concrete_BaseColor.png", "T_Concrete_Normal.png",
                Color.white, 0.12f, 0f);
            Material marble = EnsureLit("M_QC_Marble", "T_MarbleFloor_BaseColor.png", "T_MarbleFloor_Normal.png",
                Color.white, 0.35f, 0f);
            Material metalConcrete = EnsureLit("M_QC_MetalConcrete", "T_MetalConcrete_BaseColor.png",
                "T_MetalConcrete_Normal.png", Color.white, 0.22f, 0.08f);
            Material brick = EnsureLit("M_QC_RedBrick", "T_RedBrick_BaseColor.png", "T_RedBrick_Normal.png",
                Color.white, 0.10f, 0f);
            Material brickPale = EnsureLit("M_QC_RedBrickPale", "T_RedBrick_BaseColor.png", "T_RedBrick_Normal.png",
                new Color(0.88f, 0.82f, 0.72f), 0.10f, 0f);
            Material trim = EnsureLit("M_QC_Trim", "T_Trim_BaseColor.png", "T_Trim_Normal.png",
                Color.white, 0.20f, 0f);
            Material trimDark = EnsureLit("M_QC_TrimDark", "T_Trim_BaseColor.png", "T_Trim_Normal.png",
                new Color(0.34f, 0.36f, 0.38f), 0.20f, 0f);
            Material trimGreen = EnsureLit("M_QC_TrimGreen", "T_Trim_BaseColor.png", "T_Trim_Normal.png",
                new Color(0.36f, 0.62f, 0.47f), 0.20f, 0f);
            Material glass = EnsureLit("M_QC_Glass", null, null,
                new Color(0.07f, 0.14f, 0.18f), 0.92f, 0.08f);
            Material interiorDark = EnsureLit("M_QC_InteriorDark", "T_dark_interior.png", null,
                new Color(0.32f, 0.34f, 0.36f), 0.10f, 0f);
            Material interiorLit1 = EnsureEmissive("M_QC_InteriorLit1", "T_lit_interior_1.png", 0.65f);
            Material interiorLit2 = EnsureEmissive("M_QC_InteriorLit2", "T_lit_interior_2.png", 0.55f);
            Material fallback = EnsureLit("M_QC_Fallback", null, null,
                new Color(0.58f, 0.56f, 0.52f), 0.12f, 0f);

            return new Dictionary<string, Material>
            {
                ["MI_Asphalt"] = asphalt,
                ["MI_Concrete"] = concrete,
                ["MI_FakeInterior_1"] = interiorLit1,
                ["MI_FakeInterior_2"] = interiorLit2,
                ["MI_FakeInterior_3"] = interiorDark,
                ["MI_FakeInterior_4"] = interiorLit1,
                ["MI_Glass"] = glass,
                ["MI_InteriorFloor"] = marble,
                ["MI_InteriorWall"] = concrete,
                ["MI_RedBrick"] = brick,
                ["MI_RedBrick_Pale"] = brickPale,
                ["MI_Trim"] = trim,
                ["MI_Trim_Dark"] = trimDark,
                ["MI_Trim_Green"] = trimGreen,
                ["MI_Trim_MetalConcrete"] = metalConcrete,
                ["__fallback"] = fallback
            };
        }

        private static GameObject EnsureBuildingPrefab(
            BuildingSpec spec,
            IReadOnlyDictionary<string, Material> materials)
        {
            string sourcePath = $"{ModelFolder}/{spec.SourceFile}";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogWarning($"Quaternius model is not imported: {sourcePath}");
                return null;
            }

            GameObject imported = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (imported == null)
            {
                return null;
            }

            GameObject wrapper = new GameObject($"QuaterniusBuilding_{spec.Id}");
            imported.name = "Model";
            imported.transform.SetParent(wrapper.transform, false);

            foreach (Collider collider in wrapper.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }
            foreach (Camera camera in wrapper.GetComponentsInChildren<Camera>(true))
            {
                Object.DestroyImmediate(camera.gameObject);
            }
            foreach (Light light in wrapper.GetComponentsInChildren<Light>(true))
            {
                Object.DestroyImmediate(light.gameObject);
            }

            foreach (Renderer renderer in wrapper.GetComponentsInChildren<Renderer>(true))
            {
                Material[] remapped = renderer.sharedMaterials;
                for (int i = 0; i < remapped.Length; i++)
                {
                    string sourceName = remapped[i] != null ? remapped[i].name : string.Empty;
                    remapped[i] = ResolveMaterial(sourceName, materials);
                }
                renderer.sharedMaterials = remapped;
                renderer.receiveShadows = true;
            }

            if (!EditorBuildUtility.TryGetCombinedBounds(wrapper, out Bounds bounds))
            {
                Object.DestroyImmediate(wrapper);
                Debug.LogWarning($"Quaternius model has no renderable bounds: {sourcePath}");
                return null;
            }

            imported.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            string prefabPath = $"{PrefabFolder}/QuaterniusBuilding_{spec.Id}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
            Object.DestroyImmediate(wrapper);
            return prefab;
        }

        private static Material ResolveMaterial(
            string sourceName,
            IReadOnlyDictionary<string, Material> materials)
        {
            foreach (KeyValuePair<string, Material> pair in materials)
            {
                if (pair.Key == "__fallback")
                {
                    continue;
                }
                if (sourceName.StartsWith(pair.Key, System.StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }
            return materials["__fallback"];
        }

        internal static Material EnsureLit(
            string name,
            string baseTextureName,
            string normalTextureName,
            Color tint,
            float smoothness,
            float metallic)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.renderQueue = -1;

            Texture2D baseTexture = LoadTexture(baseTextureName);
            material.SetTexture("_BaseMap", baseTexture);
            Texture2D normalTexture = LoadTexture(normalTextureName);
            material.SetTexture("_BumpMap", normalTexture);
            if (normalTexture != null)
            {
                material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_BumpScale", 0.65f);
            }
            else
            {
                material.DisableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureEmissive(string name, string textureName, float intensity)
        {
            Material material = EnsureLit(name, textureName, null, Color.white, 0.05f, 0f);
            Texture2D texture = LoadTexture(textureName);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_EmissionColor", Color.white * intensity);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D LoadTexture(string fileName)
        {
            return string.IsNullOrEmpty(fileName)
                ? null
                : AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{fileName}");
        }
    }
}
