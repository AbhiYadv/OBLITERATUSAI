using System.Collections.Generic;
using System.Text.RegularExpressions;
using ObliteratusAI.Traffic;
using UnityEditor;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// Builds the traffic fleet assets: normalized visual wrapper prefabs from
    /// the migrated legacy GLBs (forward +Z, ground at y=0, target length),
    /// the primitive shuttle pod from the legacy concept, and the
    /// TrafficVehicleDefinition data assets. Wheel meshes are found by the
    /// legacy name pattern (the GLB spells it "whell") and re-hung on
    /// vehicle-aligned pivots so they can spin.
    /// </summary>
    internal static class TrafficAssetBuilder
    {
        private const string CarSourcePath = "Assets/ThirdParty/LegacyProject/Vehicles/lowpoly-cars.glb";
        private const string TruckSourcePath = "Assets/ThirdParty/LegacyProject/Vehicles/truck.glb";
        internal const string KenneyModelFolder = "Assets/ThirdParty/Kenney/CarKit/Models";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Vehicles";
        private const string DataFolder = "Assets/_Project/Data/Vehicles";
        private const float CarLength = 4.3f;
        // Spacing length for trucks; the Kenney delivery truck is stubbier
        // than the legacy articulated truck, so 8.8 m gaps read as a bug.
        private const float TruckLength = 6.5f;
        private const float PodLength = 4.6f;
        // The Kenney kit is toy-proportioned (cars nearly as wide as long),
        // so its visuals are normalized to the definitions' collision width
        // instead of the legacy target length. Length-normalizing would
        // produce 3.4 m wide cars that visually clip oncoming traffic.
        private const float CarWidth = 2.04f;
        private const float TruckWidth = 2.5f;

        private static readonly Regex WheelPattern =
            new Regex("whe+l|tyre|tire", RegexOptions.IgnoreCase);

        public struct Result
        {
            public TrafficVehicleDefinition Car2;
            public TrafficVehicleDefinition Car9;
            public TrafficVehicleDefinition Truck;
            public TrafficVehicleDefinition Pod;
        }

        [MenuItem("OBLITERATUS AI/Build Traffic Assets")]
        public static void BuildMenu()
        {
            EnsureAssets();
            AssetDatabase.SaveAssets();
        }

        public static Result EnsureAssets()
        {
            EditorBuildUtility.EnsureFolder(PrefabFolder);
            EditorBuildUtility.EnsureFolder(DataFolder);

            // Preferred visuals are Kenney Car Kit models (authored facing
            // +Z, named wheel-* nodes). The legacy GLBs remain the fallback
            // when the kit is not imported; those face -Z after long-axis
            // normalization (observed in play testing: cars drove backwards),
            // so only the fallback path flips forward 180 degrees.
            GameObject car2Visual = BuildCarVisual(
                "suv.glb", CarSourcePath, "car2",
                $"{PrefabFolder}/TrafficCar2Visual.prefab", CarLength, CarWidth, true);
            GameObject car9Visual = BuildCarVisual(
                "hatchback-sports.glb", CarSourcePath, "car9",
                $"{PrefabFolder}/TrafficCar9Visual.prefab", CarLength, CarWidth, true);
            GameObject truckVisual = BuildCarVisual(
                "delivery.glb", TruckSourcePath, null,
                $"{PrefabFolder}/TrafficTruckVisual.prefab", TruckLength, TruckWidth, false);
            GameObject podVisual = BuildPodVisual($"{PrefabFolder}/ShuttlePodVisual.prefab");

            Result result;
            result.Car2 = EnsureDefinition(
                $"{DataFolder}/Traffic_Car2.asset", car2Visual, CarLength, 1.02f, 0.95f, 8.5f, 13.5f, true);
            result.Car9 = EnsureDefinition(
                $"{DataFolder}/Traffic_Car9.asset", car9Visual, CarLength, 1.02f, 0.95f, 8.5f, 13.5f, true);
            result.Truck = EnsureDefinition(
                $"{DataFolder}/Traffic_Truck.asset", truckVisual, TruckLength, 1.25f, 1.45f, 6.5f, 9f, false);
            result.Pod = EnsureDefinition(
                $"{DataFolder}/Traffic_ShuttlePod.asset", podVisual, PodLength, 1.08f, 1.05f, 7f, 9f, false);
            return result;
        }

        private static TrafficVehicleDefinition EnsureDefinition(
            string path,
            GameObject visual,
            float length,
            float halfWidth,
            float halfHeight,
            float minCruise,
            float maxCruise,
            bool tintable)
        {
            TrafficVehicleDefinition definition =
                AssetDatabase.LoadAssetAtPath<TrafficVehicleDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<TrafficVehicleDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }
            definition.Configure(visual, length, halfWidth, halfHeight, minCruise, maxCruise, tintable);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        /// <summary>
        /// Kenney model when imported, otherwise the legacy GLB fallback.
        /// `legacySpinWheels` mirrors the old per-source choice (the legacy
        /// truck has no usable wheel nodes).
        /// </summary>
        private static GameObject BuildCarVisual(
            string kenneyFile,
            string legacySourcePath,
            string legacyNodeName,
            string prefabPath,
            float targetLength,
            float targetWidth,
            bool legacySpinWheels)
        {
            string kenneyPath = $"{KenneyModelFolder}/{kenneyFile}";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(kenneyPath) != null)
            {
                return BuildGlbVisual(
                    kenneyPath, null, prefabPath, targetLength, true, false, targetWidth);
            }
            return BuildGlbVisual(
                prefabPath: prefabPath,
                sourcePath: legacySourcePath,
                nodeName: legacyNodeName,
                targetLength: targetLength,
                spinWheels: legacySpinWheels,
                flipForward: true);
        }

        /// <summary>
        /// Bake a GLB (or one named node of it) into a normalized wrapper
        /// prefab: long axis along +Z, ground at y=0. Scaled to targetWidth
        /// (short horizontal axis) when given, otherwise to targetLength.
        /// </summary>
        private static GameObject BuildGlbVisual(
            string sourcePath,
            string nodeName,
            string prefabPath,
            float targetLength,
            bool spinWheels,
            bool flipForward,
            float targetWidth = 0f)
        {
            GameObject sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourceAsset == null)
            {
                Debug.LogWarning($"Traffic vehicle source is not imported: {sourcePath}");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(sourceAsset) as GameObject;
            if (instance == null)
            {
                return null;
            }
            PrefabUtility.UnpackPrefabInstance(
                instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            string wrapperName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
            GameObject wrapper = new GameObject(wrapperName);
            Transform holder = new GameObject("Model").transform;
            holder.SetParent(wrapper.transform, false);

            Transform target = nodeName == null
                ? instance.transform
                : FindDescendant(instance.transform, nodeName);
            if (target == null)
            {
                Debug.LogWarning($"Node '{nodeName}' was not found in {sourcePath}.");
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(wrapper);
                return null;
            }

            target.SetParent(holder, true);
            if (target.gameObject != instance)
            {
                Object.DestroyImmediate(instance);
            }

            if (!EditorBuildUtility.TryGetCombinedBounds(wrapper, out Bounds bounds)
                || bounds.size.sqrMagnitude < 1e-6f)
            {
                Debug.LogWarning($"No renderer bounds for {sourcePath} ({nodeName}).");
                Object.DestroyImmediate(wrapper);
                return null;
            }

            float rotY = bounds.size.x > bounds.size.z ? 90f : 0f;
            if (flipForward)
            {
                rotY += 180f;
            }
            holder.localRotation = Quaternion.Euler(0f, rotY, 0f);
            float scale = targetWidth > 0f
                ? targetWidth / Mathf.Min(bounds.size.x, bounds.size.z)
                : targetLength / Mathf.Max(bounds.size.x, bounds.size.z);
            holder.localScale = Vector3.one * scale;
            if (EditorBuildUtility.TryGetCombinedBounds(wrapper, out bounds))
            {
                holder.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            }

            Transform[] pivots = spinWheels
                ? CreateWheelPivots(wrapper.transform, holder)
                : System.Array.Empty<Transform>();
            wrapper.AddComponent<TrafficVehicleVisual>().SetWheelPivots(pivots);
            Debug.Log($"{wrapperName}: {pivots.Length} wheel pivot(s) found.");

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
            Object.DestroyImmediate(wrapper);
            return prefab;
        }

        /// <summary>
        /// Insert a vehicle-aligned pivot at each wheel mesh's center and
        /// re-hang the wheel under it, so spinning is a local +X rotation.
        /// </summary>
        private static Transform[] CreateWheelPivots(Transform wrapper, Transform holder)
        {
            List<Transform> wheels = new List<Transform>();
            foreach (Transform child in holder.GetComponentsInChildren<Transform>(true))
            {
                if (!WheelPattern.IsMatch(child.name))
                {
                    continue;
                }
                if (child.GetComponentInChildren<Renderer>(true) == null)
                {
                    continue;
                }
                bool nestedInMatch = false;
                foreach (Transform picked in wheels)
                {
                    if (child.IsChildOf(picked))
                    {
                        nestedInMatch = true;
                        break;
                    }
                }
                if (!nestedInMatch)
                {
                    wheels.Add(child);
                }
            }

            List<Transform> pivots = new List<Transform>(wheels.Count);
            for (int i = 0; i < wheels.Count; i++)
            {
                if (!EditorBuildUtility.TryGetCombinedBounds(wheels[i].gameObject, out Bounds wheelBounds))
                {
                    continue;
                }
                Transform pivot = new GameObject($"WheelPivot_{i}").transform;
                pivot.SetParent(wrapper, false);
                pivot.position = wheelBounds.center;
                wheels[i].SetParent(pivot, true);
                pivots.Add(pivot);
            }
            return pivots.ToArray();
        }

        /// <summary>Primitive port of the legacy makePodModel autonomous shuttle.</summary>
        private static GameObject BuildPodVisual(string prefabPath)
        {
            Material white = EditorBuildUtility.GetOrCreateMaterial(
                "M_PodWhite", new Color(0.933f, 0.941f, 0.949f), 0.72f, 0.25f);
            Material glass = EditorBuildUtility.GetOrCreateMaterial(
                "M_PodGlass", new Color(0.071f, 0.094f, 0.122f), 0.92f, 0.6f);
            Material dark = EditorBuildUtility.GetOrCreateMaterial(
                "M_PodDark", new Color(0.141f, 0.153f, 0.169f), 0.45f, 0.35f);
            Material trim = EditorBuildUtility.GetOrCreateMaterial(
                "M_PodTrim", new Color(0.227f, 0.239f, 0.259f), 0.6f, 0.6f);
            Material head = EditorBuildUtility.GetOrCreateEmissiveMaterial(
                "M_PodHeadlight", new Color(0.918f, 0.949f, 1f), 0.9f);
            Material tail = EditorBuildUtility.GetOrCreateEmissiveMaterial(
                "M_PodTaillight", new Color(1f, 0.165f, 0.125f), 0.8f);
            Material wheelMat = EditorBuildUtility.GetOrCreateMaterial(
                "M_PodWheel", new Color(0.075f, 0.075f, 0.082f), 0.15f);
            Material hub = EditorBuildUtility.GetOrCreateMaterial(
                "M_PodHub", new Color(0.604f, 0.627f, 0.651f), 0.65f, 0.8f);

            GameObject wrapper = new GameObject("ShuttlePodVisual");
            Transform root = wrapper.transform;

            EditorBuildUtility.CreateLocalBox("Skirt", new Vector3(0f, 0.62f, 0f),
                new Vector3(2.02f, 0.62f, 4.5f), dark, root);
            EditorBuildUtility.CreateLocalBox("Cabin", new Vector3(0f, 1.34f, 0f),
                new Vector3(1.98f, 1.02f, 4.34f), white, root);
            EditorBuildUtility.CreateLocalBox("Roof", new Vector3(0f, 1.96f, 0f),
                new Vector3(1.78f, 0.22f, 4.0f), white, root);
            GameObject ridge = EditorBuildUtility.CreateLocalCylinder(
                "RoofRidge", new Vector3(0f, 1.98f, 0f), new Vector3(1f, 1.95f, 0.28f), white, root);
            ridge.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            EditorBuildUtility.CreateLocalBox("GlassBand", new Vector3(0f, 1.5f, 0f),
                new Vector3(2.04f, 0.86f, 4.12f), glass, root);
            GameObject windshieldFront = EditorBuildUtility.CreateLocalBox(
                "WindshieldFront", new Vector3(0f, 1.52f, 2.14f),
                new Vector3(1.9f, 0.9f, 0.14f), glass, root);
            windshieldFront.transform.localRotation = Quaternion.Euler(-12.6f, 0f, 0f);
            GameObject windshieldRear = EditorBuildUtility.CreateLocalBox(
                "WindshieldRear", new Vector3(0f, 1.52f, -2.14f),
                new Vector3(1.9f, 0.9f, 0.14f), glass, root);
            windshieldRear.transform.localRotation = Quaternion.Euler(12.6f, 0f, 0f);
            EditorBuildUtility.CreateLocalBox("BeltTrim", new Vector3(0f, 0.98f, 0f),
                new Vector3(2.06f, 0.1f, 4.4f), trim, root);
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1.02f : 1.02f;
                foreach (float sz in new[] { 0f, 1.3f, -1.3f })
                {
                    EditorBuildUtility.CreateLocalBox($"DoorSeam_{side}_{sz}",
                        new Vector3(sx, 1.4f, sz), new Vector3(0.04f, 0.9f, 0.05f), trim, root);
                }
            }
            EditorBuildUtility.CreateLocalBox("BumperFront", new Vector3(0f, 0.55f, 2.28f),
                new Vector3(1.96f, 0.34f, 0.22f), trim, root);
            EditorBuildUtility.CreateLocalBox("BumperRear", new Vector3(0f, 0.55f, -2.28f),
                new Vector3(1.96f, 0.34f, 0.22f), trim, root);
            EditorBuildUtility.CreateLocalBox("HeadlightStrip", new Vector3(0f, 0.95f, 2.27f),
                new Vector3(1.5f, 0.14f, 0.06f), head, root);
            EditorBuildUtility.CreateLocalBox("TaillightStrip", new Vector3(0f, 0.95f, -2.27f),
                new Vector3(1.5f, 0.14f, 0.06f), tail, root);

            Vector2[] wheelSpots =
            {
                new Vector2(-0.86f, 1.5f),
                new Vector2(0.86f, 1.5f),
                new Vector2(-0.86f, -1.5f),
                new Vector2(0.86f, -1.5f)
            };
            Transform[] pivots = new Transform[wheelSpots.Length];
            for (int i = 0; i < wheelSpots.Length; i++)
            {
                float x = wheelSpots[i].x;
                float z = wheelSpots[i].y;
                EditorBuildUtility.CreateLocalBox($"Fender_{i}", new Vector3(x, 0.72f, z),
                    new Vector3(0.5f, 0.5f, 1.0f), dark, root);

                Transform pivot = new GameObject($"WheelPivot_{i}").transform;
                pivot.SetParent(root, false);
                pivot.localPosition = new Vector3(x, 0.36f, z);
                GameObject wheel = EditorBuildUtility.CreateLocalCylinder(
                    "Wheel", Vector3.zero, new Vector3(0.72f, 0.13f, 0.72f), wheelMat, pivot);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                GameObject hubCap = EditorBuildUtility.CreateLocalCylinder(
                    "Hub", Vector3.zero, new Vector3(0.3f, 0.14f, 0.3f), hub, pivot);
                hubCap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                pivots[i] = pivot;
            }

            wrapper.AddComponent<TrafficVehicleVisual>().SetWheelPivots(pivots);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
            Object.DestroyImmediate(wrapper);
            return prefab;
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
    }
}
