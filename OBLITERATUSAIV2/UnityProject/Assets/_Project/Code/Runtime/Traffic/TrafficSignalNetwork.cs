using UnityEngine;

namespace ObliteratusAI.Traffic
{
    /// <summary>
    /// Owns the shared signal clock. Vehicles poll <see cref="CurrentState"/>;
    /// lamp heads are refreshed only when the city-wide phase changes.
    /// </summary>
    public sealed class TrafficSignalNetwork : MonoBehaviour
    {
        private TrafficSignalHead[] _heads;
        private double _startTime;

        public SignalState CurrentState =>
            TrafficSignalTimeline.StateAt(Time.timeAsDouble - _startTime);

        public SignalState StateAt(float intersectionX, float intersectionZ)
        {
            double elapsed = Time.timeAsDouble - _startTime;
            return TrafficSignalTimeline.StateAt(
                elapsed + TrafficSignalTimeline.OffsetAt(intersectionX, intersectionZ));
        }

        private void Awake()
        {
            _startTime = Time.timeAsDouble;
            _heads = GetComponentsInChildren<TrafficSignalHead>(true);
        }

        private void Update()
        {
            double elapsed = Time.timeAsDouble - _startTime;
            for (int i = 0; i < _heads.Length; i++)
            {
                _heads[i].ApplyAt(elapsed);
            }
        }
    }
}
