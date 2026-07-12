using UnityEngine;

namespace ObliteratusAI.Traffic
{
    /// <summary>
    /// Marks a traffic vehicle visual prefab. Wheel pivots are inserted by the
    /// asset builder at each wheel's center, axis-aligned with the vehicle, so
    /// spinning is a plain local rotation around +X regardless of how the
    /// source GLB oriented its wheel nodes.
    /// </summary>
    public sealed class TrafficVehicleVisual : MonoBehaviour
    {
        [SerializeField] private Transform[] wheelPivots;

        public Transform[] WheelPivots => wheelPivots;

        public void SetWheelPivots(Transform[] pivots)
        {
            wheelPivots = pivots;
        }
    }
}
