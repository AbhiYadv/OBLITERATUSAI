using UnityEngine;

namespace ObliteratusAI.Traffic
{
    /// <summary>
    /// Swappable data for one traffic fleet vehicle type: visual prefab,
    /// collision dimensions, and cruise speed range.
    /// </summary>
    [CreateAssetMenu(fileName = "TrafficVehicle", menuName = "OBLITERATUS AI/Traffic Vehicle Definition")]
    public sealed class TrafficVehicleDefinition : ScriptableObject
    {
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private float length = 4.3f;
        [SerializeField] private float halfWidth = 1.02f;
        [SerializeField] private float halfHeight = 0.95f;
        [SerializeField] private float minCruiseSpeed = 8.5f;
        [SerializeField] private float maxCruiseSpeed = 13.5f;
        [SerializeField] private bool tintable = true;

        public GameObject VisualPrefab => visualPrefab;
        public float Length => length;
        public float HalfLength => length * 0.5f;
        public float HalfWidth => halfWidth;
        public float HalfHeight => halfHeight;
        public float MinCruiseSpeed => minCruiseSpeed;
        public float MaxCruiseSpeed => maxCruiseSpeed;
        public bool Tintable => tintable;

        /// <summary>Editor-time wiring used by the traffic asset builder.</summary>
        public void Configure(
            GameObject visual,
            float bodyLength,
            float bodyHalfWidth,
            float bodyHalfHeight,
            float minCruise,
            float maxCruise,
            bool canTint)
        {
            visualPrefab = visual;
            length = bodyLength;
            halfWidth = bodyHalfWidth;
            halfHeight = bodyHalfHeight;
            minCruiseSpeed = minCruise;
            maxCruiseSpeed = maxCruise;
            tintable = canTint;
        }
    }
}
