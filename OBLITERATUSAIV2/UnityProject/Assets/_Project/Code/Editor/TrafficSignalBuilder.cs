using ObliteratusAI.City;
using ObliteratusAI.Traffic;
using UnityEditor;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// Places four timed signal posts per intersection at all 25 grid
    /// crossings, matching the legacy TrafficLights.tsx corner pattern. The
    /// posts stay non-static because their lamp materials swap at runtime.
    /// </summary>
    internal static class TrafficSignalBuilder
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/City";
        private const string PrefabPath = PrefabFolder + "/TrafficSignalPost.prefab";
        private const float PoleHeight = 4.9f;
        private const float HeadY = 4.3f;

        /// <summary>Builds the signal network under <paramref name="parent"/> and returns its root.</summary>
        public static GameObject Build(Transform parent)
        {
            GameObject prefab = BuildPostPrefab();

            GameObject root = new GameObject("TrafficSignals");
            root.transform.SetParent(parent, false);
            root.AddComponent<TrafficSignalNetwork>();

            float off = CityLayout.RoadWidth * 0.5f + 0.9f;
            foreach (float x in CityLayout.Streets)
            {
                foreach (float z in CityLayout.Streets)
                {
                    PlacePost(prefab, root.transform, x + off, z + off, x, z, 0f, true);
                    PlacePost(prefab, root.transform, x - off, z - off, x, z, 180f, true);
                    PlacePost(prefab, root.transform, x + off, z - off, x, z, 90f, false);
                    PlacePost(prefab, root.transform, x - off, z + off, x, z, -90f, false);
                }
            }

            return root;
        }

        private static void PlacePost(
            GameObject prefab,
            Transform parent,
            float x,
            float z,
            float intersectionX,
            float intersectionZ,
            float yawDegrees,
            bool northSouthAxis)
        {
            GameObject post = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (post == null)
            {
                return;
            }

            post.transform.SetPositionAndRotation(
                new Vector3(x, CityLayout.SlabTop, z),
                Quaternion.Euler(0f, yawDegrees, 0f));
            TrafficSignalHead head = post.GetComponent<TrafficSignalHead>();
            head.NorthSouthAxis = northSouthAxis;
            head.ConfigureIntersection(intersectionX, intersectionZ);
        }

        private static GameObject BuildPostPrefab()
        {
            Material metal = EditorBuildUtility.GetOrCreateMaterial(
                "M_SignalMetal", new Color(0.165f, 0.176f, 0.192f), 0.4f, 0.5f);
            Material housing = EditorBuildUtility.GetOrCreateMaterial(
                "M_SignalHousing", new Color(0.11f, 0.118f, 0.125f), 0.25f);

            Material redLit = EditorBuildUtility.GetOrCreateEmissiveMaterial(
                "M_SignalRedLit", FromHex(0xff2a1a), 2.2f);
            Material redDark = EditorBuildUtility.GetOrCreateMaterial(
                "M_SignalRedDark", FromHex(0x3a1210));
            Material yellowLit = EditorBuildUtility.GetOrCreateEmissiveMaterial(
                "M_SignalYellowLit", FromHex(0xffb400), 2.2f);
            Material yellowDark = EditorBuildUtility.GetOrCreateMaterial(
                "M_SignalYellowDark", FromHex(0x392c08));
            Material greenLit = EditorBuildUtility.GetOrCreateEmissiveMaterial(
                "M_SignalGreenLit", FromHex(0x22d24a), 2.2f);
            Material greenDark = EditorBuildUtility.GetOrCreateMaterial(
                "M_SignalGreenDark", FromHex(0x0c2e14));

            GameObject postRoot = new GameObject("TrafficSignalPost");
            TrafficSignalHead head = postRoot.AddComponent<TrafficSignalHead>();

            BoxCollider collider = postRoot.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, PoleHeight * 0.5f, 0f);
            collider.size = new Vector3(0.48f, PoleHeight, 0.48f);

            EditorBuildUtility.CreateLocalCylinder(
                "Pole",
                new Vector3(0f, PoleHeight * 0.5f, 0f),
                new Vector3(0.18f, PoleHeight * 0.5f, 0.18f),
                metal,
                postRoot.transform);
            EditorBuildUtility.CreateLocalBox(
                "Head",
                new Vector3(0f, HeadY, 0.08f),
                new Vector3(0.34f, 1.0f, 0.24f),
                housing,
                postRoot.transform);

            MeshRenderer redLamp = CreateLamp(postRoot.transform, "LampRed", HeadY + 0.3f, redDark);
            MeshRenderer yellowLamp = CreateLamp(postRoot.transform, "LampYellow", HeadY, yellowDark);
            MeshRenderer greenLamp = CreateLamp(postRoot.transform, "LampGreen", HeadY - 0.3f, greenDark);

            head.ConfigureLamps(
                redLamp, yellowLamp, greenLamp,
                redLit, redDark, yellowLit, yellowDark, greenLit, greenDark);

            EditorBuildUtility.EnsureFolder(PrefabFolder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(postRoot, PrefabPath);
            Object.DestroyImmediate(postRoot);
            return prefab;
        }

        private static MeshRenderer CreateLamp(
            Transform parent,
            string name,
            float y,
            Material darkMaterial)
        {
            GameObject lamp = EditorBuildUtility.CreateLocalBox(
                name,
                new Vector3(0f, y, 0.205f),
                new Vector3(0.19f, 0.19f, 0.05f),
                darkMaterial,
                parent);
            return lamp.GetComponent<MeshRenderer>();
        }

        private static Color FromHex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xff) / 255f,
                ((rgb >> 8) & 0xff) / 255f,
                (rgb & 0xff) / 255f);
        }
    }
}
