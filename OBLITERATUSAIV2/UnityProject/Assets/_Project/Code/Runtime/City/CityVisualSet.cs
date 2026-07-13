using System;
using UnityEngine;

namespace ObliteratusAI.City
{
    [CreateAssetMenu(menuName = "OBLITERATUS AI/City Visual Set", fileName = "CityVisualSet")]
    public sealed class CityVisualSet : ScriptableObject
    {
        [Serializable]
        public struct BuildingVariant
        {
            [SerializeField] private string id;
            [SerializeField] private GameObject prefab;
            [SerializeField] private float preferredHeight;
            [SerializeField] private float preferredWidth;
            [SerializeField] private float preferredDepth;
            [SerializeField] private bool supportsCornerPlacement;

            public string Id => id;
            public GameObject Prefab => prefab;
            public float PreferredHeight => preferredHeight;
            public float PreferredWidth => preferredWidth;
            public float PreferredDepth => preferredDepth;
            public bool SupportsCornerPlacement => supportsCornerPlacement;

            public BuildingVariant(
                string id,
                GameObject prefab,
                float preferredHeight,
                bool supportsCornerPlacement)
                : this(id, prefab, preferredHeight, 0f, 0f, supportsCornerPlacement)
            {
            }

            public BuildingVariant(
                string id,
                GameObject prefab,
                float preferredHeight,
                float preferredWidth,
                float preferredDepth,
                bool supportsCornerPlacement)
            {
                this.id = id;
                this.prefab = prefab;
                this.preferredHeight = preferredHeight;
                this.preferredWidth = preferredWidth;
                this.preferredDepth = preferredDepth;
                this.supportsCornerPlacement = supportsCornerPlacement;
            }
        }

        [SerializeField] private string sourceName;
        [SerializeField] private BuildingVariant[] buildingVariants = Array.Empty<BuildingVariant>();
        [SerializeField] private GameObject bollardPrefab;
        [SerializeField] private GameObject planterPrefab;
        [SerializeField] private GameObject manholePrefab;
        [SerializeField] private GameObject acUnitPrefab;

        public string SourceName => sourceName;
        public int BuildingVariantCount => buildingVariants?.Length ?? 0;
        public GameObject BollardPrefab => bollardPrefab;
        public GameObject PlanterPrefab => planterPrefab;
        public GameObject ManholePrefab => manholePrefab;
        public GameObject AcUnitPrefab => acUnitPrefab;

        public bool TryGetClosestBuilding(float requestedHeight, int variation, out GameObject prefab)
        {
            return TryGetClosestBuilding(requestedHeight, 0f, 0f, variation, out prefab);
        }

        /// <summary>
        /// Pick the corner-capable variant closest to the requested lot size.
        /// Height dominates the score; the footprint term breaks ties between
        /// variants authored at different widths. A zero width/depth request
        /// scores by height alone. `variation` rotates the starting index so
        /// equal-scoring variants alternate deterministically between lots.
        /// </summary>
        public bool TryGetClosestBuilding(
            float requestedHeight,
            float requestedWidth,
            float requestedDepth,
            int variation,
            out GameObject prefab)
        {
            prefab = null;
            if (buildingVariants == null || buildingVariants.Length == 0)
            {
                return false;
            }

            float bestScore = float.PositiveInfinity;
            int bestIndex = -1;
            int offset = Mathf.Abs(variation) % buildingVariants.Length;
            for (int i = 0; i < buildingVariants.Length; i++)
            {
                int index = (i + offset) % buildingVariants.Length;
                BuildingVariant variant = buildingVariants[index];
                if (variant.Prefab == null || !variant.SupportsCornerPlacement)
                {
                    continue;
                }

                float score = Mathf.Abs(variant.PreferredHeight - requestedHeight);
                if (requestedWidth > 0f && variant.PreferredWidth > 0f)
                {
                    score += 0.5f * Mathf.Abs(variant.PreferredWidth - requestedWidth);
                }
                if (requestedDepth > 0f && variant.PreferredDepth > 0f)
                {
                    score += 0.5f * Mathf.Abs(variant.PreferredDepth - requestedDepth);
                }
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = index;
                }
            }

            if (bestIndex < 0)
            {
                return false;
            }

            prefab = buildingVariants[bestIndex].Prefab;
            return true;
        }

        public void Configure(string visualSource, BuildingVariant[] variants)
        {
            sourceName = visualSource;
            buildingVariants = variants ?? Array.Empty<BuildingVariant>();
        }

        public void ConfigureProps(
            GameObject bollard,
            GameObject planter,
            GameObject manhole,
            GameObject acUnit)
        {
            bollardPrefab = bollard;
            planterPrefab = planter;
            manholePrefab = manhole;
            acUnitPrefab = acUnit;
        }
    }
}
