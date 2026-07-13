using ObliteratusAI.City;
using ObliteratusAI.Core;
using ObliteratusAI.Player;
using ObliteratusAI.Traffic;
using UnityEngine;

namespace ObliteratusAI.Pedestrians
{
    /// <summary>
    /// Owns a fixed pedestrian pool, ticks agents from one FixedUpdate, and
    /// recycles distant off-screen agents onto sidewalks around the player.
    /// Runtime population changes never instantiate or destroy GameObjects.
    /// </summary>
    public sealed class PedestrianSystem : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseColorFactorId = Shader.PropertyToID("baseColorFactor");

        [Header("Population")]
        [SerializeField] private PedestrianDefinition definition;
        [SerializeField] private TrafficSignalNetwork signalNetwork;
        [SerializeField] private int seed = 20260702;
        [SerializeField, Min(1)] private int count = 28;

        [Header("Off-screen recycling")]
        [SerializeField, Min(5f)] private float spawnMinDistance = 28f;
        [SerializeField, Min(10f)] private float spawnMaxDistance = 92f;
        [SerializeField, Min(20f)] private float recycleDistance = 110f;
        [SerializeField, Min(30f)] private float hardRecycleDistance = 145f;
        [SerializeField, Min(0.1f)] private float populationRefreshInterval = 0.4f;
        [SerializeField, Range(1, 12)] private int checksPerRefresh = 6;
        [SerializeField, Range(1, 4)] private int maxRecyclesPerRefresh = 2;
        [SerializeField, Range(0f, 0.35f)] private float offscreenViewportMargin = 0.12f;

        private PedestrianAgent[] _agents = System.Array.Empty<PedestrianAgent>();
        private int[] _generations = System.Array.Empty<int>();
        private DeterministicRandom _populationRandom;
        private Transform _focus;
        private Camera _camera;
        private float _populationTimer;
        private int _populationCursor;
        private int _senseOffset;

        public int ActiveCount => _agents.Length;

        /// <summary>Editor-time wiring used by the sandbox builder.</summary>
        public void Configure(
            PedestrianDefinition pedestrianDefinition,
            TrafficSignalNetwork network = null)
        {
            definition = pedestrianDefinition;
            signalNetwork = network;
        }

        private void Start()
        {
            if (definition == null || definition.PedestrianPrefab == null)
            {
                Debug.LogWarning("PedestrianSystem is missing its definition; no crowd spawned.", this);
                return;
            }

            if (signalNetwork == null)
            {
                signalNetwork = FindFirstObjectByType<TrafficSignalNetwork>();
            }
            ResolvePopulationFocus();

            _populationRandom = new DeterministicRandom(unchecked((uint)seed ^ 0x9ed5u));
            int senseMask = SimulationLayers.SenseMask();
            int pedestrianLayer = SimulationLayers.PedestrianLayer();
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor(BaseColorId, definition.HiVisTint);
            propertyBlock.SetColor(BaseColorFactorId, definition.HiVisTint);

            _agents = new PedestrianAgent[count];
            _generations = new int[count];
            Vector3 focusPosition = _focus != null ? _focus.position : Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                if (!TryChooseSpawn(
                        focusPosition,
                        requireOffscreen: false,
                        ignoreAgent: null,
                        out int blockIndex,
                        out float startFraction,
                        out _))
                {
                    blockIndex = _populationRandom.NextInt(CityLayout.Blocks.Length);
                    startFraction = _populationRandom.NextFloat();
                }

                float speed = _populationRandom.Range(definition.MinSpeed, definition.MaxSpeed);
                GameObject pedestrian = Instantiate(definition.GetPrefab(i), transform);
                pedestrian.name = $"Pedestrian_{i}";
                pedestrian.layer = pedestrianLayer;

                if (definition.HiVisEvery > 0 && i % definition.HiVisEvery == 0)
                {
                    foreach (Renderer renderer in pedestrian.GetComponentsInChildren<Renderer>())
                    {
                        renderer.SetPropertyBlock(propertyBlock);
                    }
                }

                PedestrianAgent agent = pedestrian.GetComponent<PedestrianAgent>();
                if (agent == null)
                {
                    agent = pedestrian.AddComponent<PedestrianAgent>();
                }
                agent.Configure(
                    blockIndex,
                    startFraction,
                    speed,
                    definition,
                    senseMask,
                    BehaviorSeed(i, 0));
                _agents[i] = agent;
            }
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < _agents.Length; i++)
            {
                bool sense = ((i + _senseOffset) & 1) == 0;
                _agents[i].Tick(dt, signalNetwork, sense);
            }
            _senseOffset ^= 1;

            _populationTimer -= dt;
            if (_populationTimer <= 0f)
            {
                _populationTimer = populationRefreshInterval;
                RefreshPopulation();
            }
        }

        private void RefreshPopulation()
        {
            if (_agents.Length == 0)
            {
                return;
            }

            if (_focus == null || _camera == null)
            {
                ResolvePopulationFocus();
            }
            if (_focus == null)
            {
                return;
            }

            Vector3 focusPosition = _focus.position;
            int recycled = 0;
            for (int checkedCount = 0; checkedCount < checksPerRefresh; checkedCount++)
            {
                int index = _populationCursor;
                _populationCursor = (_populationCursor + 1) % _agents.Length;
                PedestrianAgent agent = _agents[index];
                Vector3 delta = agent.Position - focusPosition;
                delta.y = 0f;
                float distanceSquared = delta.sqrMagnitude;
                bool beyondHardLimit = distanceSquared > hardRecycleDistance * hardRecycleDistance;
                if (distanceSquared <= recycleDistance * recycleDistance)
                {
                    continue;
                }
                if (!agent.CanRecycle && !beyondHardLimit)
                {
                    continue;
                }
                if (!beyondHardLimit && IsVisible(agent.Position))
                {
                    continue;
                }

                if (!TryChooseSpawn(
                        focusPosition,
                        requireOffscreen: true,
                        ignoreAgent: agent,
                        out int blockIndex,
                        out float startFraction,
                        out _))
                {
                    continue;
                }

                _generations[index]++;
                float speed = _populationRandom.Range(definition.MinSpeed, definition.MaxSpeed);
                agent.Recycle(
                    blockIndex,
                    startFraction,
                    speed,
                    BehaviorSeed(index, _generations[index]));
                recycled++;
                if (recycled >= maxRecyclesPerRefresh)
                {
                    break;
                }
            }
        }

        private bool TryChooseSpawn(
            Vector3 focusPosition,
            bool requireOffscreen,
            PedestrianAgent ignoreAgent,
            out int blockIndex,
            out float startFraction,
            out Vector3 position)
        {
            float minDistanceSquared = spawnMinDistance * spawnMinDistance;
            float maxDistance = Mathf.Max(spawnMinDistance + 1f, spawnMaxDistance);
            float maxDistanceSquared = maxDistance * maxDistance;
            for (int attempt = 0; attempt < 36; attempt++)
            {
                int candidateBlock = _populationRandom.NextInt(CityLayout.Blocks.Length);
                float candidateFraction = _populationRandom.NextFloat();
                Vector3 candidate = PedestrianAgent.PreviewSpawnPosition(
                    candidateBlock,
                    candidateFraction);
                Vector3 delta = candidate - focusPosition;
                delta.y = 0f;
                float distanceSquared = delta.sqrMagnitude;
                if (distanceSquared < minDistanceSquared || distanceSquared > maxDistanceSquared)
                {
                    continue;
                }
                if (requireOffscreen && IsVisible(candidate))
                {
                    continue;
                }
                if (!IsSpawnClear(candidate, ignoreAgent))
                {
                    continue;
                }

                blockIndex = candidateBlock;
                startFraction = candidateFraction;
                position = candidate;
                return true;
            }

            blockIndex = 0;
            startFraction = 0f;
            position = default;
            return false;
        }

        private bool IsSpawnClear(Vector3 candidate, PedestrianAgent ignoreAgent)
        {
            const float separationSquared = 1.45f * 1.45f;
            for (int i = 0; i < _agents.Length; i++)
            {
                PedestrianAgent other = _agents[i];
                if (other == null || other == ignoreAgent)
                {
                    continue;
                }

                Vector3 delta = other.Position - candidate;
                delta.y = 0f;
                if (delta.sqrMagnitude < separationSquared)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsVisible(Vector3 worldPosition)
        {
            if (_camera == null)
            {
                return false;
            }

            Vector3 viewport = _camera.WorldToViewportPoint(worldPosition + Vector3.up * 0.9f);
            return viewport.z > 0f
                && viewport.x >= -offscreenViewportMargin
                && viewport.x <= 1f + offscreenViewportMargin
                && viewport.y >= -offscreenViewportMargin
                && viewport.y <= 1f + offscreenViewportMargin;
        }

        private void ResolvePopulationFocus()
        {
            PlayerMotor player = FindFirstObjectByType<PlayerMotor>();
            _focus = player != null ? player.transform : null;
            _camera = Camera.main;
        }

        private uint BehaviorSeed(int index, int generation)
        {
            unchecked
            {
                return (uint)seed
                    ^ ((uint)(index + 1) * 0x9e3779b9u)
                    ^ ((uint)(generation + 1) * 0x85ebca6bu);
            }
        }
    }
}
