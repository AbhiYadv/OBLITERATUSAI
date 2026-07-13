using UnityEngine;

namespace ObliteratusAI.Pedestrians
{
    /// <summary>
    /// Swappable data for the sidewalk crowd: which prefab to spawn, walk
    /// speed range, animation pacing, and the hi-vis tint pattern from the
    /// legacy concept art (every 3rd pedestrian reads as a worker).
    /// </summary>
    [CreateAssetMenu(fileName = "Pedestrian", menuName = "OBLITERATUS AI/Pedestrian Definition")]
    public sealed class PedestrianDefinition : ScriptableObject
    {
        [Header("Visual and movement")]
        [SerializeField] private GameObject pedestrianPrefab;
        [SerializeField] private GameObject[] pedestrianPrefabVariants = System.Array.Empty<GameObject>();
        [SerializeField] private float minSpeed = 1.1f;
        [SerializeField] private float maxSpeed = 1.7f;
        [SerializeField] private float clipNaturalSpeed = 1.55f;
        [SerializeField] private Color hiVisTint = new Color(1f, 0.839f, 0.431f);
        [SerializeField] private int hiVisEvery = 3;

        [Header("Ambient behavior")]
        [SerializeField, Min(1f)] private float minIdleInterval = 7f;
        [SerializeField, Min(1f)] private float maxIdleInterval = 16f;
        [SerializeField, Min(0.25f)] private float minIdleDuration = 1.4f;
        [SerializeField, Min(0.25f)] private float maxIdleDuration = 3.8f;
        [SerializeField, Range(0f, 1f)] private float idleChance = 0.72f;
        [SerializeField, Range(0f, 1f)] private float crosswalkChance = 0.48f;
        [SerializeField, Min(1.5f)] private float crossingSpeed = 2.35f;
        [SerializeField, Min(1f)] private float vehicleHurryMultiplier = 1.35f;

        public GameObject PedestrianPrefab => pedestrianPrefab;

        /// <summary>
        /// Prefab for pool slot `index`: cycles the variant list when one is
        /// configured, otherwise the single legacy prefab.
        /// </summary>
        public GameObject GetPrefab(int index)
        {
            if (pedestrianPrefabVariants != null && pedestrianPrefabVariants.Length > 0)
            {
                GameObject variant =
                    pedestrianPrefabVariants[Mathf.Abs(index) % pedestrianPrefabVariants.Length];
                if (variant != null)
                {
                    return variant;
                }
            }
            return pedestrianPrefab;
        }
        public float MinSpeed => minSpeed;
        public float MaxSpeed => maxSpeed;
        public float ClipNaturalSpeed => clipNaturalSpeed;
        public Color HiVisTint => hiVisTint;
        public int HiVisEvery => hiVisEvery;
        public float MinIdleInterval => minIdleInterval;
        public float MaxIdleInterval => Mathf.Max(minIdleInterval, maxIdleInterval);
        public float MinIdleDuration => minIdleDuration;
        public float MaxIdleDuration => Mathf.Max(minIdleDuration, maxIdleDuration);
        public float IdleChance => idleChance;
        public float CrosswalkChance => crosswalkChance;
        public float CrossingSpeed => crossingSpeed;
        public float VehicleHurryMultiplier => vehicleHurryMultiplier;

        /// <summary>Editor-time wiring used by the pedestrian asset builder.</summary>
        public void Configure(GameObject prefab)
        {
            pedestrianPrefab = prefab;
        }

        /// <summary>
        /// Variant wiring: authored character models replace the tint-based
        /// hi-vis pattern (the Worker variants carry real vests).
        /// </summary>
        public void ConfigureVariants(GameObject fallbackPrefab, GameObject[] variants)
        {
            pedestrianPrefab = fallbackPrefab;
            pedestrianPrefabVariants = variants ?? System.Array.Empty<GameObject>();
            if (pedestrianPrefabVariants.Length > 0)
            {
                hiVisEvery = 0;
            }
        }
    }
}
