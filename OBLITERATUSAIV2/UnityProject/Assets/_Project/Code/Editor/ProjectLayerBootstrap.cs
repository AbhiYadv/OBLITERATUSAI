using ObliteratusAI.Core;
using UnityEditor;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// Idempotently registers the physics layers used by the city-life
    /// simulation (traffic vehicles, pedestrians) in TagManager.asset.
    /// Called by the sandbox builder and available as a menu item.
    /// </summary>
    internal static class ProjectLayerBootstrap
    {
        [MenuItem("OBLITERATUS AI/Setup/Ensure Project Layers")]
        public static void EnsureLayers()
        {
            EnsureLayer(SimulationLayers.TrafficVehicle);
            EnsureLayer(SimulationLayers.Pedestrian);
        }

        private static void EnsureLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0)
            {
                return;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets.Length == 0)
            {
                Debug.LogError("TagManager.asset could not be loaded; add layers manually.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"Added project layer '{layerName}' at index {i}.");
                    return;
                }
            }

            Debug.LogError($"No free user layer slot for '{layerName}'; free one in Tags and Layers.");
        }
    }
}
