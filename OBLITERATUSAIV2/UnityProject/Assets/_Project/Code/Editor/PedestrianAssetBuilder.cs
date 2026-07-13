using System.Collections.Generic;
using System.Linq;
using ObliteratusAI.Core;
using ObliteratusAI.Pedestrians;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// Builds the pedestrian prefabs and PedestrianDefinition. Preferred
    /// visuals are twelve Quaternius Ultimate Animated Character civilians
    /// (each with a real Idle/Walk controller); the CesiumMan single-clip
    /// prefab remains the fallback when the pack is not imported. Suit_Male
    /// is intentionally not in the roster — it is the player's exclusive
    /// skin.
    /// </summary>
    internal static class PedestrianAssetBuilder
    {
        private const string CharacterPackFolder =
            "Assets/ThirdParty/Quaternius/UltimateAnimatedCharacters/Models";
        private const string CharacterModelPath = "Assets/ThirdParty/Khronos/CesiumMan/human-casual.glb";
        private const string CharacterControllerPath = "Assets/_Project/Art/Characters/CesiumMan.controller";
        private const string ControllerFolder = "Assets/_Project/Art/Characters/Pedestrians";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Characters";
        private const string VariantPrefabFolder = PrefabFolder + "/Pedestrians";
        private const string PrefabPath = PrefabFolder + "/Pedestrian.prefab";
        private const string DataFolder = "Assets/_Project/Data/Characters";
        private const string DefinitionPath = DataFolder + "/Pedestrian_Casual.asset";
        private const float TargetHeight = 1.75f;
        private const float CapsuleRadius = 0.34f;
        private const float CapsuleHeight = 1.76f;

        private static readonly string[] CivilianCharacters =
        {
            "Casual_Male",
            "Casual_Female",
            "Casual2_Male",
            "Casual2_Female",
            "Casual3_Male",
            "Casual3_Female",
            "Casual_Bald",
            "Worker_Male",
            "Worker_Female",
            "OldClassy_Male",
            "OldClassy_Female",
            "Suit_Female"
        };

        [MenuItem("OBLITERATUS AI/Build Pedestrian Assets")]
        public static void BuildMenu()
        {
            EnsureAssets();
            AssetDatabase.SaveAssets();
        }

        public static PedestrianDefinition EnsureAssets()
        {
            EditorBuildUtility.EnsureFolder(PrefabFolder);
            EditorBuildUtility.EnsureFolder(DataFolder);

            GameObject fallbackPrefab = BuildPedestrianPrefab(
                CharacterModelPath, PrefabPath, useLegacyController: true);

            List<GameObject> variants = new List<GameObject>(CivilianCharacters.Length);
            foreach (string character in CivilianCharacters)
            {
                string modelPath = $"{CharacterPackFolder}/{character}.gltf";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) == null)
                {
                    continue;
                }

                EditorBuildUtility.EnsureFolder(VariantPrefabFolder);
                GameObject variant = BuildPedestrianPrefab(
                    modelPath,
                    $"{VariantPrefabFolder}/Pedestrian_{character}.prefab",
                    useLegacyController: false);
                if (variant != null)
                {
                    variants.Add(variant);
                }
            }

            PedestrianDefinition definition =
                AssetDatabase.LoadAssetAtPath<PedestrianDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PedestrianDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            if (variants.Count > 0)
            {
                definition.ConfigureVariants(
                    fallbackPrefab != null ? fallbackPrefab : variants[0],
                    variants.ToArray());
                Debug.Log($"Pedestrian crowd uses {variants.Count} Quaternius character variants.");
            }
            else
            {
                definition.Configure(fallbackPrefab);
            }
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static GameObject BuildPedestrianPrefab(
            string modelPath,
            string prefabPath,
            bool useLegacyController)
        {
            GameObject root = new GameObject("Pedestrian");
            int pedestrianLayer = LayerMask.NameToLayer(SimulationLayers.Pedestrian);
            if (pedestrianLayer >= 0)
            {
                root.layer = pedestrianLayer;
            }

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, CapsuleHeight * 0.5f, 0f);
            capsule.radius = CapsuleRadius;
            capsule.height = CapsuleHeight;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            root.AddComponent<PedestrianAgent>();
            AttachVisual(root, modelPath, useLegacyController);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AttachVisual(GameObject root, string modelPath, bool useLegacyController)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"Pedestrian model is not imported: {modelPath}");
                return;
            }

            GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset, root.transform) as GameObject;
            if (visual == null)
            {
                return;
            }
            visual.name = "Visual";

            if (!EditorBuildUtility.TryGetCombinedBounds(visual, out Bounds bounds)
                || bounds.size.y <= 0.001f)
            {
                Debug.LogWarning("Pedestrian model has no usable renderer bounds.");
                Object.DestroyImmediate(visual);
                return;
            }

            float scale = TargetHeight / bounds.size.y;
            visual.transform.localScale = Vector3.one * scale;
            if (EditorBuildUtility.TryGetCombinedBounds(visual, out bounds))
            {
                visual.transform.position += new Vector3(
                    -bounds.center.x,
                    root.transform.position.y - bounds.min.y,
                    -bounds.center.z);
            }

            ConfigureAnimation(visual, modelPath, useLegacyController);
        }

        private static void ConfigureAnimation(
            GameObject visual,
            string modelPath,
            bool useLegacyController)
        {
            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning($"Pedestrian rig missing on {modelPath}; crowd will not animate.");
                return;
            }

            AnimatorController controller;
            if (useLegacyController)
            {
                AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                if (clip == null)
                {
                    Debug.LogWarning("Pedestrian walk clip missing; crowd will not animate.");
                    return;
                }
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CharacterControllerPath);
                if (controller == null)
                {
                    EditorBuildUtility.EnsureFolder("Assets/_Project/Art/Characters");
                    controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(
                        CharacterControllerPath, clip);
                }
            }
            else
            {
                EditorBuildUtility.EnsureFolder(ControllerFolder);
                string characterName = System.IO.Path.GetFileNameWithoutExtension(modelPath);
                controller = CharacterAnimationBuilder.EnsureController(
                    $"{ControllerFolder}/Pedestrian_{characterName}.controller",
                    modelPath);
                if (controller == null)
                {
                    Debug.LogWarning($"No usable clips in {modelPath}; crowd will not animate.");
                    return;
                }
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
        }
    }
}
