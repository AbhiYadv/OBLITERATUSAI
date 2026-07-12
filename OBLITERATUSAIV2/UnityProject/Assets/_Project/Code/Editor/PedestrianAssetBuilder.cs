using System.Linq;
using ObliteratusAI.Core;
using ObliteratusAI.Pedestrians;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// Builds the pedestrian prefab (CesiumMan visual normalized to 1.75 m,
    /// kinematic capsule on the Pedestrian layer, looping walk clip) and its
    /// PedestrianDefinition data asset.
    /// </summary>
    internal static class PedestrianAssetBuilder
    {
        private const string CharacterModelPath = "Assets/ThirdParty/Khronos/CesiumMan/human-casual.glb";
        private const string CharacterControllerPath = "Assets/_Project/Art/Characters/CesiumMan.controller";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Characters";
        private const string PrefabPath = PrefabFolder + "/Pedestrian.prefab";
        private const string DataFolder = "Assets/_Project/Data/Characters";
        private const string DefinitionPath = DataFolder + "/Pedestrian_Casual.asset";
        private const float TargetHeight = 1.75f;
        private const float CapsuleRadius = 0.34f;
        private const float CapsuleHeight = 1.76f;

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

            GameObject prefab = BuildPedestrianPrefab();
            PedestrianDefinition definition =
                AssetDatabase.LoadAssetAtPath<PedestrianDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PedestrianDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }
            definition.Configure(prefab);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static GameObject BuildPedestrianPrefab()
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
            AttachVisual(root);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AttachVisual(GameObject root)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModelPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"Pedestrian model is not imported: {CharacterModelPath}");
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

            ConfigureAnimation(visual);
        }

        private static void ConfigureAnimation(GameObject visual)
        {
            Animator animator = visual.GetComponentInChildren<Animator>(true);
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(CharacterModelPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (animator == null || clip == null)
            {
                Debug.LogWarning("Pedestrian rig or walk clip missing; crowd will not animate.");
                return;
            }

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(CharacterControllerPath);
            if (controller == null)
            {
                EditorBuildUtility.EnsureFolder("Assets/_Project/Art/Characters");
                controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(
                    CharacterControllerPath, clip);
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
        }
    }
}
