using UnityEngine;

namespace ObliteratusAI.Vehicles
{
    [CreateAssetMenu(menuName = "OBLITERATUS AI/Vehicle Definition", fileName = "VehicleDefinition")]
    public sealed class VehicleDefinition : ScriptableObject
    {
        [SerializeField] private string vehicleId = "prototype-sedan";
        [SerializeField] private string displayName = "Prototype Sedan";
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private float mass = 1200f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float reverseAcceleration = 10f;
        [SerializeField] private float maximumSpeed = 16f;
        [SerializeField] private float steeringSpeed = 85f;
        [SerializeField] private float braking = 28f;
        [SerializeField] private float lateralGrip = 7f;

        public string VehicleId => vehicleId;
        public string DisplayName => displayName;
        public GameObject VisualPrefab => visualPrefab;
        public float Mass => mass;
        public float Acceleration => acceleration;
        public float ReverseAcceleration => reverseAcceleration;
        public float MaximumSpeed => maximumSpeed;
        public float SteeringSpeed => steeringSpeed;
        public float Braking => braking;
        public float LateralGrip => lateralGrip;

        public void ConfigurePrototype(GameObject newVisualPrefab)
        {
            visualPrefab = newVisualPrefab;
        }
    }
}
