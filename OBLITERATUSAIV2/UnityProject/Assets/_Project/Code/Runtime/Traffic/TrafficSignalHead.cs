using UnityEngine;

namespace ObliteratusAI.Traffic
{
    /// <summary>
    /// One corner post's three-lamp signal head. Lamp state is applied by
    /// swapping shared lit/dark materials so no material instances are ever
    /// created at runtime.
    /// </summary>
    public sealed class TrafficSignalHead : MonoBehaviour
    {
        [SerializeField] private bool northSouthAxis;
        [SerializeField] private MeshRenderer redLamp;
        [SerializeField] private MeshRenderer yellowLamp;
        [SerializeField] private MeshRenderer greenLamp;
        [SerializeField] private Material redLit;
        [SerializeField] private Material redDark;
        [SerializeField] private Material yellowLit;
        [SerializeField] private Material yellowDark;
        [SerializeField] private Material greenLit;
        [SerializeField] private Material greenDark;
        [SerializeField] private double phaseOffsetSeconds;

        private int _lastPhase = -1;

        public bool NorthSouthAxis
        {
            get => northSouthAxis;
            set => northSouthAxis = value;
        }

        /// <summary>Editor-time wiring used by the city builder.</summary>
        public void ConfigureLamps(
            MeshRenderer red,
            MeshRenderer yellow,
            MeshRenderer green,
            Material redLitMaterial,
            Material redDarkMaterial,
            Material yellowLitMaterial,
            Material yellowDarkMaterial,
            Material greenLitMaterial,
            Material greenDarkMaterial)
        {
            redLamp = red;
            yellowLamp = yellow;
            greenLamp = green;
            redLit = redLitMaterial;
            redDark = redDarkMaterial;
            yellowLit = yellowLitMaterial;
            yellowDark = yellowDarkMaterial;
            greenLit = greenLitMaterial;
            greenDark = greenDarkMaterial;
        }

        public void ConfigureIntersection(float x, float z)
        {
            phaseOffsetSeconds = TrafficSignalTimeline.OffsetAt(x, z);
        }

        public void ApplyAt(double elapsedTime)
        {
            Apply(TrafficSignalTimeline.StateAt(elapsedTime + phaseOffsetSeconds));
        }

        public void Apply(in SignalState state)
        {
            if (state.Phase == _lastPhase)
            {
                return;
            }
            _lastPhase = state.Phase;

            bool go = northSouthAxis ? state.NsGo : state.EwGo;
            bool caution = northSouthAxis ? state.NsYellow : state.EwYellow;
            redLamp.sharedMaterial = !go && !caution ? redLit : redDark;
            yellowLamp.sharedMaterial = caution ? yellowLit : yellowDark;
            greenLamp.sharedMaterial = go ? greenLit : greenDark;
        }
    }
}
