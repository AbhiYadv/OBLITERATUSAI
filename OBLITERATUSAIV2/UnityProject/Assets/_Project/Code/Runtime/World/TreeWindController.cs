using UnityEngine;

namespace ObliteratusAI.World
{
    /// <summary>
    /// Bridges one directional WindZone into the shared procedural-tree
    /// shader. Tree variation and vertex movement stay on the GPU.
    /// </summary>
    [RequireComponent(typeof(WindZone))]
    public sealed class TreeWindController : MonoBehaviour
    {
        private static readonly int TreeWindId = Shader.PropertyToID("_ObliteratusTreeWind");

        private WindZone _windZone;

        public void Configure(
            float main,
            float turbulence,
            float pulseMagnitude,
            float pulseFrequency)
        {
            WindZone zone = GetComponent<WindZone>();
            zone.mode = WindZoneMode.Directional;
            zone.windMain = main;
            zone.windTurbulence = turbulence;
            zone.windPulseMagnitude = pulseMagnitude;
            zone.windPulseFrequency = pulseFrequency;
        }

        private void Awake()
        {
            _windZone = GetComponent<WindZone>();
            ApplyWind();
        }

        private void Update()
        {
            ApplyWind();
        }

        private void ApplyWind()
        {
            Vector3 direction = transform.forward;
            float pulse = 1f + Mathf.Sin(
                Time.time * Mathf.PI * 2f * _windZone.windPulseFrequency)
                * _windZone.windPulseMagnitude;
            float strength = Mathf.Max(0f, _windZone.windMain * pulse);
            Shader.SetGlobalVector(
                TreeWindId,
                new Vector4(direction.x, direction.z, strength, _windZone.windTurbulence));
        }
    }
}
