using System.Collections.Generic;
using ObliteratusAI.Core;
using UnityEngine;

namespace ObliteratusAI.Traffic
{
    /// <summary>
    /// Spawns the seeded traffic fleet at Start and ticks every vehicle from
    /// one FixedUpdate loop with a single shared signal state. Fleet mix and
    /// tint palette ported from the legacy Traffic.tsx: every 5th vehicle is
    /// the truck, every 3rd the shuttle pod, the rest alternate the two cars.
    /// </summary>
    public sealed class TrafficSystem : MonoBehaviour
    {
        private static readonly Color[] Tints =
        {
            new Color(1f, 1f, 1f),
            new Color(1f, 0.616f, 0.580f),
            new Color(0.616f, 0.722f, 1f),
            new Color(1f, 0.878f, 0.541f),
            new Color(0.624f, 0.902f, 0.675f),
            new Color(0.851f, 0.851f, 0.886f),
            new Color(0.788f, 0.635f, 0.910f)
        };
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseColorFactorId = Shader.PropertyToID("baseColorFactor");
        private static readonly Vector2 GarageExit = new Vector2(150f, 14f);

        [SerializeField] private TrafficSignalNetwork signalNetwork;
        [SerializeField] private TrafficVehicleDefinition carPrimary;
        [SerializeField] private TrafficVehicleDefinition carSecondary;
        [SerializeField] private TrafficVehicleDefinition truck;
        [SerializeField] private TrafficVehicleDefinition shuttlePod;
        [SerializeField] private int seed = 20260702;
        [SerializeField] private int routeCount = 12;
        [SerializeField] private int carsPerRoute = 2;

        private TrafficVehicle[] _vehicles = System.Array.Empty<TrafficVehicle>();
        private MaterialPropertyBlock _propertyBlock;
        private int _senseOffset;

        public int ActiveCount => _vehicles.Length;

        /// <summary>Editor-time wiring used by the sandbox builder.</summary>
        public void Configure(
            TrafficSignalNetwork network,
            TrafficVehicleDefinition primary,
            TrafficVehicleDefinition secondary,
            TrafficVehicleDefinition truckDefinition,
            TrafficVehicleDefinition podDefinition)
        {
            signalNetwork = network;
            carPrimary = primary;
            carSecondary = secondary;
            truck = truckDefinition;
            shuttlePod = podDefinition;
        }

        private void Start()
        {
            if (carPrimary == null || carSecondary == null)
            {
                Debug.LogWarning("TrafficSystem is missing car definitions; no traffic spawned.", this);
                return;
            }
            if (signalNetwork == null)
            {
                Debug.LogWarning("TrafficSystem has no signal network; no traffic spawned.", this);
                return;
            }

            _propertyBlock = new MaterialPropertyBlock();
            DeterministicRandom rand = new DeterministicRandom(unchecked((uint)seed ^ 0x7ea4c0deu));
            TrafficRoute[] routes = TrafficRouteBuilder.BuildLoops(routeCount, ref rand);
            int senseMask = SimulationLayers.SenseMask();
            int vehicleLayer = SimulationLayers.TrafficVehicleLayer();

            List<TrafficVehicle> vehicles = new List<TrafficVehicle>(routeCount * carsPerRoute);
            int g = 0;
            foreach (TrafficRoute route in routes)
            {
                for (int k = 0; k < carsPerRoute; k++, g++)
                {
                    TrafficVehicleDefinition definition = PickDefinition(g);
                    float cruise = rand.Range(definition.MinCruiseSpeed, definition.MaxCruiseSpeed);
                    // Spread the route's cars into separate loop segments so
                    // two can never spawn overlapping (an unresolvable
                    // mutual-blocking standstill).
                    float slot = (k + 0.6f * rand.NextFloat()) / carsPerRoute;
                    float startDist = KeepGarageExitClear(route, slot * route.TotalLength);
                    float startSpin = rand.NextFloat() * 6f;
                    TrafficDriverProfile driver = new TrafficDriverProfile(
                        rand.Range(0.85f, 1.15f),
                        rand.Range(0.9f, 1.12f),
                        rand.Range(2.8f, 4.4f),
                        rand.Range(0.12f, 0.75f),
                        rand.Range(5.8f, 8.4f),
                        rand.Range(6f, 10f));
                    Color? tint = definition.Tintable
                        ? Tints[rand.NextInt(Tints.Length)]
                        : (Color?)null;
                    vehicles.Add(Spawn(
                        vehicles.Count, route, definition, cruise, startDist, startSpin,
                        driver, tint, senseMask, vehicleLayer));
                }
            }
            _vehicles = vehicles.ToArray();
        }

        private void FixedUpdate()
        {
            if (_vehicles.Length == 0)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < _vehicles.Length; i++)
            {
                bool sense = ((i + _senseOffset) & 1) == 0;
                _vehicles[i].Tick(dt, signalNetwork, sense);
            }
            _senseOffset ^= 1;
        }

        private TrafficVehicleDefinition PickDefinition(int g)
        {
            if (g % 5 == 4 && truck != null)
            {
                return truck;
            }
            if (g % 3 == 1 && shuttlePod != null)
            {
                return shuttlePod;
            }
            return g % 2 == 0 ? carPrimary : carSecondary;
        }

        private TrafficVehicle Spawn(
            int index,
            TrafficRoute route,
            TrafficVehicleDefinition definition,
            float cruise,
            float startDist,
            float startSpin,
            TrafficDriverProfile driver,
            Color? tint,
            int senseMask,
            int vehicleLayer)
        {
            GameObject vehicleObject = new GameObject($"TrafficVehicle_{index}")
            {
                layer = vehicleLayer
            };
            vehicleObject.transform.SetParent(transform, false);

            BoxCollider collider = vehicleObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, definition.HalfHeight, 0f);
            collider.size = new Vector3(
                definition.HalfWidth * 2f,
                definition.HalfHeight * 2f,
                definition.Length);

            Rigidbody body = vehicleObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            if (definition.VisualPrefab != null)
            {
                GameObject visual = Instantiate(definition.VisualPrefab, vehicleObject.transform);
                visual.name = "Visual";
                if (tint.HasValue)
                {
                    ApplyTint(visual, tint.Value);
                }
            }

            TrafficVehicle vehicle = vehicleObject.AddComponent<TrafficVehicle>();
            vehicle.Configure(route, definition, cruise, startDist, startSpin, driver, senseMask);
            return vehicle;
        }

        private static float KeepGarageExitClear(TrafficRoute route, float startDistance)
        {
            int pointer = 0;
            float distance = startDistance;
            for (int i = 0; i < 24; i++)
            {
                route.Sample(distance, ref pointer, out float x, out float z);
                if ((new Vector2(x, z) - GarageExit).sqrMagnitude >= 24f * 24f)
                {
                    return distance;
                }
                distance = (distance + 4f) % route.TotalLength;
            }
            return distance;
        }

        private void ApplyTint(GameObject visual, Color tint)
        {
            TrafficVehicleVisual marker = visual.GetComponentInChildren<TrafficVehicleVisual>();
            Transform[] wheelPivots = marker != null && marker.WheelPivots != null
                ? marker.WheelPivots
                : System.Array.Empty<Transform>();

            _propertyBlock.Clear();
            _propertyBlock.SetColor(BaseColorId, tint);
            _propertyBlock.SetColor(BaseColorFactorId, tint);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
            {
                bool isWheel = false;
                foreach (Transform pivot in wheelPivots)
                {
                    if (pivot != null && renderer.transform.IsChildOf(pivot))
                    {
                        isWheel = true;
                        break;
                    }
                }
                if (!isWheel)
                {
                    renderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }
    }
}
