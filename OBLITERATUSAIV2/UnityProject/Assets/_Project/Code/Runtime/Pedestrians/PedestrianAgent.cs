using ObliteratusAI.City;
using ObliteratusAI.Core;
using ObliteratusAI.Traffic;
using UnityEngine;

namespace ObliteratusAI.Pedestrians
{
    /// <summary>
    /// Lightweight ambient pedestrian. Agents walk deterministic block loops,
    /// occasionally step aside for an idle, continue through eligible corners
    /// on signal-aware crosswalks, and can be recycled by PedestrianSystem.
    /// No NavMeshAgent or per-frame managed allocation is required.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PedestrianAgent : MonoBehaviour
    {
        public enum PedestrianState
        {
            Walking,
            Idling,
            WaitingToCross,
            Crossing
        }

        private enum IdleAction
        {
            Pause,
            LookAround,
            StepAside
        }

        private const float Inset = 1.9f;
        private const float Radius = 0.34f;
        private const float HalfHeight = 0.88f;
        private const float IdleAnimationPace = 0.08f;
        private const float IdleCornerClearance = 4f;
        private const float CurbWaitMin = 0.3f;
        private const float VehicleThreatSpeed = 0.65f;
        private const float VehicleThreatHalfLength = 18f;
        private const float SignalSafetySeconds = 0.45f;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[8];
        private static readonly Collider[] OverlapBuffer = new Collider[16];
        private static readonly int WalkingParameter = Animator.StringToHash("Walking");
        private static readonly int RunningParameter = Animator.StringToHash("Running");
        // Above walking range (max 1.7 m/s) but below crossing speed (2.35),
        // so crossings and vehicle-hurry read as a jog.
        private const float RunAnimationThreshold = 2.05f;
        private const float RunClipNaturalSpeed = 3.1f;

        private PedestrianDefinition _definition;
        private CityLayout.BlockRect _block;
        private int _blockIndex;
        private Rigidbody _body;
        private Collider _collider;
        private Animator _animator;
        private bool _hasWalkingParameter;
        private bool _hasRunningParameter;
        private int _senseMask;
        private DeterministicRandom _random;

        private float _perimeter;
        private float _dist;
        private float _speed;
        private float _currentSpeed;
        private float _pathYaw;
        private float _yaw;
        private float _clipNaturalSpeed = 1.55f;
        private float _nextIdleTimer;
        private float _stateTimer;
        private float _stateDuration;
        private float _idleDirection;
        private float _waitLookClock;
        private bool _blocked;
        private bool _vehicleThreat;
        private IdleAction _idleAction;
        private PedestrianState _state;

        private Vector3 _crossStart;
        private Vector3 _crossEnd;
        private Vector3 _crossDirection;
        private float _crossLength;
        private float _crossProgress;
        private int _destinationBlockIndex;
        private float _destinationDistance;
        private float _intersectionX;
        private float _intersectionZ;
        private bool _eastWestCrossing;

        public PedestrianState CurrentState => _state;
        public bool IsCrossing => _state == PedestrianState.Crossing;
        public bool IsWaitingToCross => _state == PedestrianState.WaitingToCross;
        public bool CanRecycle => !IsCrossing && !IsWaitingToCross;
        public Vector3 Position => transform.position;

        public static Vector3 PreviewSpawnPosition(int blockIndex, float startFraction)
        {
            int clampedIndex = Mathf.Clamp(blockIndex, 0, CityLayout.Blocks.Length - 1);
            CityLayout.BlockRect block = CityLayout.Blocks[clampedIndex];
            float distance = Mathf.Repeat(startFraction, 1f) * Perimeter(block);
            PointOnLoop(block, distance, out float x, out float z);
            return new Vector3(x, CityLayout.SlabTop, z);
        }

        public void Configure(
            int blockIndex,
            float startFraction,
            float speed,
            PedestrianDefinition definition,
            int senseMask,
            uint behaviorSeed)
        {
            _definition = definition;
            _senseMask = senseMask;
            _body = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _animator = GetComponentInChildren<Animator>();
            _hasWalkingParameter = false;
            _hasRunningParameter = false;
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                foreach (AnimatorControllerParameter parameter in _animator.parameters)
                {
                    if (parameter.nameHash == WalkingParameter)
                    {
                        _hasWalkingParameter = true;
                    }
                    else if (parameter.nameHash == RunningParameter)
                    {
                        _hasRunningParameter = true;
                    }
                }
            }
            _clipNaturalSpeed = Mathf.Max(0.1f, definition.ClipNaturalSpeed);
            Recycle(blockIndex, startFraction, speed, behaviorSeed);
        }

        /// <summary>
        /// Repositions an existing pooled agent on a sidewalk and resets all
        /// transient behavior. PedestrianSystem calls this only while both the
        /// old and new positions are outside the camera view.
        /// </summary>
        public void Recycle(
            int blockIndex,
            float startFraction,
            float speed,
            uint behaviorSeed)
        {
            _blockIndex = Mathf.Clamp(blockIndex, 0, CityLayout.Blocks.Length - 1);
            _block = CityLayout.Blocks[_blockIndex];
            _perimeter = Perimeter(_block);
            _dist = Mathf.Repeat(startFraction, 1f) * _perimeter;
            _speed = Mathf.Max(0.1f, speed);
            _currentSpeed = _speed;
            _random = new DeterministicRandom(behaviorSeed);
            _state = PedestrianState.Walking;
            _blocked = false;
            _vehicleThreat = false;
            _stateTimer = 0f;
            _stateDuration = 0f;
            _crossProgress = 0f;
            _waitLookClock = _random.Range(0f, 4f);
            ScheduleNextIdle();

            _pathYaw = PointOnLoop(_block, _dist, out float x, out float z);
            _yaw = _pathYaw;
            Vector3 position = new Vector3(x, CityLayout.SlabTop, z);
            Quaternion rotation = Quaternion.AngleAxis(_yaw * Mathf.Rad2Deg, Vector3.up);
            if (_body != null)
            {
                _body.position = position;
                _body.rotation = rotation;
            }
            transform.SetPositionAndRotation(position, rotation);
            SetAnimationPace(_speed);
        }

        /// <summary>Advance the pedestrian from PedestrianSystem.FixedUpdate.</summary>
        public void Tick(
            float dt,
            TrafficSignalNetwork signalNetwork,
            bool senseThisStep)
        {
            switch (_state)
            {
                case PedestrianState.Idling:
                    TickIdle(dt);
                    break;
                case PedestrianState.WaitingToCross:
                    TickWaiting(dt, signalNetwork, senseThisStep);
                    break;
                case PedestrianState.Crossing:
                    TickCrossing(dt, senseThisStep);
                    break;
                default:
                    TickWalking(dt, senseThisStep);
                    break;
            }
        }

        private void TickWalking(float dt, bool senseThisStep)
        {
            if (senseThisStep)
            {
                _blocked = SenseBlocked();
            }

            _nextIdleTimer -= dt;
            if (!_blocked
                && _nextIdleTimer <= 0f
                && DistanceToNearestCorner(_dist) > IdleCornerClearance)
            {
                ScheduleNextIdle();
                if (_random.NextFloat() < _definition.IdleChance)
                {
                    BeginIdle();
                    TickIdle(dt);
                    return;
                }
            }

            float targetSpeed = _blocked ? 0f : _speed;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, 8f * dt);

            float previousDistance = _dist;
            float rawDistance = previousDistance + _currentSpeed * dt;
            if (!_blocked
                && _currentSpeed > 0.05f
                && TryGetPassedCorner(previousDistance, rawDistance, out int corner, out float cornerDistance)
                && _random.NextFloat() < _definition.CrosswalkChance
                && TryBeginCrosswalk(corner, cornerDistance))
            {
                TickWaiting(dt, null, senseThisStep);
                return;
            }

            _dist = Mathf.Repeat(rawDistance, _perimeter);
            _pathYaw = PointOnLoop(_block, _dist, out float x, out float z);
            MovePose(new Vector3(x, CityLayout.SlabTop, z), _pathYaw, 10f, dt);
            SetAnimationPace(_currentSpeed);
        }

        private void BeginIdle()
        {
            _state = PedestrianState.Idling;
            _stateDuration = _random.Range(
                _definition.MinIdleDuration,
                _definition.MaxIdleDuration);
            _stateTimer = _stateDuration;
            _idleAction = (IdleAction)_random.NextInt(3);
            _idleDirection = _random.NextFloat() < 0.5f ? -1f : 1f;
        }

        private void TickIdle(float dt)
        {
            _stateTimer -= dt;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, 9f * dt);

            float progress = _stateDuration > 0f
                ? Mathf.Clamp01(1f - _stateTimer / _stateDuration)
                : 1f;
            float ease = Mathf.Sin(progress * Mathf.PI);
            float lateralOffset = 0f;
            float lookOffset = 0f;
            switch (_idleAction)
            {
                case IdleAction.LookAround:
                    lateralOffset = 0.24f * ease;
                    lookOffset = Mathf.Sin(progress * Mathf.PI * 2f) * 0.58f;
                    break;
                case IdleAction.StepAside:
                    lateralOffset = 0.58f * ease;
                    lookOffset = _idleDirection * 0.28f * ease;
                    break;
                default:
                    lookOffset = Mathf.Sin(progress * Mathf.PI) * _idleDirection * 0.12f;
                    break;
            }

            _pathYaw = PointOnLoop(_block, _dist, out float x, out float z);
            Vector3 left = new Vector3(-Mathf.Cos(_pathYaw), 0f, Mathf.Sin(_pathYaw));
            Vector3 position = new Vector3(x, CityLayout.SlabTop, z) + left * lateralOffset;
            MovePose(position, _pathYaw + lookOffset, 6f, dt);
            SetAnimationPace(0f);

            if (_stateTimer <= 0f)
            {
                _state = PedestrianState.Walking;
            }
        }

        private bool TryBeginCrosswalk(int corner, float cornerDistance)
        {
            int side = CityLayout.Streets.Length - 1;
            int blockX = _blockIndex / side;
            int blockZ = _blockIndex % side;
            int destinationX = blockX;
            int destinationZ = blockZ;
            float destinationDistance;

            switch (corner)
            {
                case 0: // Continue east from the bottom edge.
                    destinationX++;
                    destinationDistance = 0f;
                    _intersectionX = CityLayout.Streets[blockX + 1];
                    _intersectionZ = CityLayout.Streets[blockZ];
                    _eastWestCrossing = true;
                    break;
                case 1: // Continue north from the right edge.
                    destinationZ++;
                    destinationDistance = _block.Width - Inset * 2f;
                    _intersectionX = CityLayout.Streets[blockX + 1];
                    _intersectionZ = CityLayout.Streets[blockZ + 1];
                    _eastWestCrossing = false;
                    break;
                case 2: // Continue west from the top edge.
                    destinationX--;
                    destinationDistance =
                        (_block.Width - Inset * 2f) + (_block.Depth - Inset * 2f);
                    _intersectionX = CityLayout.Streets[blockX];
                    _intersectionZ = CityLayout.Streets[blockZ + 1];
                    _eastWestCrossing = true;
                    break;
                default: // Continue south from the left edge.
                    destinationZ--;
                    destinationDistance =
                        2f * (_block.Width - Inset * 2f) + (_block.Depth - Inset * 2f);
                    _intersectionX = CityLayout.Streets[blockX];
                    _intersectionZ = CityLayout.Streets[blockZ];
                    _eastWestCrossing = false;
                    break;
            }

            if (destinationX < 0 || destinationX >= side
                || destinationZ < 0 || destinationZ >= side)
            {
                return false;
            }

            _destinationBlockIndex = destinationX * side + destinationZ;
            CityLayout.BlockRect destination = CityLayout.Blocks[_destinationBlockIndex];
            PointOnLoop(_block, cornerDistance, out float startX, out float startZ);
            PointOnLoop(destination, destinationDistance, out float endX, out float endZ);
            _crossStart = new Vector3(startX, CityLayout.SlabTop, startZ);
            _crossEnd = new Vector3(endX, CityLayout.SlabTop, endZ);
            Vector3 crossing = _crossEnd - _crossStart;
            _crossLength = crossing.magnitude;
            if (_crossLength <= 0.01f)
            {
                return false;
            }

            _crossDirection = crossing / _crossLength;
            _destinationDistance = destinationDistance;
            _crossProgress = 0f;
            _dist = Mathf.Repeat(cornerDistance, _perimeter);
            _currentSpeed = 0f;
            _state = PedestrianState.WaitingToCross;
            _stateTimer = CurbWaitMin + _random.Range(0f, 0.45f);
            _waitLookClock = _random.Range(0f, 4f);
            _vehicleThreat = true;
            return true;
        }

        private void TickWaiting(
            float dt,
            TrafficSignalNetwork signalNetwork,
            bool senseThisStep)
        {
            _stateTimer -= dt;
            _waitLookClock += dt;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, 10f * dt);
            if (senseThisStep)
            {
                _vehicleThreat = SenseCrosswalkVehicleThreat();
            }

            float crossingYaw = Mathf.Atan2(_crossDirection.x, _crossDirection.z);
            float trafficCheck = Mathf.Sin(_waitLookClock * 2.25f) * 0.82f;
            MovePose(_crossStart, crossingYaw + trafficCheck, 7f, dt);
            SetAnimationPace(0f);

            if (_stateTimer <= 0f
                && HasWalkSignal(signalNetwork)
                && !_vehicleThreat)
            {
                _state = PedestrianState.Crossing;
                _yaw = crossingYaw;
            }
        }

        private void TickCrossing(float dt, bool senseThisStep)
        {
            if (senseThisStep)
            {
                _vehicleThreat = SenseCrosswalkVehicleThreat();
            }

            float targetSpeed = _definition.CrossingSpeed;
            if (_vehicleThreat)
            {
                targetSpeed *= _definition.VehicleHurryMultiplier;
            }
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, 12f * dt);
            _crossProgress += _currentSpeed * dt;

            if (_crossProgress >= _crossLength)
            {
                CompleteCrossing();
                return;
            }

            float t = _crossProgress / _crossLength;
            Vector3 position = Vector3.LerpUnclamped(_crossStart, _crossEnd, t);
            float crossingYaw = Mathf.Atan2(_crossDirection.x, _crossDirection.z);
            MovePose(position, crossingYaw, 12f, dt);
            SetAnimationPace(_currentSpeed);
        }

        private void CompleteCrossing()
        {
            _blockIndex = _destinationBlockIndex;
            _block = CityLayout.Blocks[_blockIndex];
            _perimeter = Perimeter(_block);
            _dist = Mathf.Repeat(_destinationDistance, _perimeter);
            _state = PedestrianState.Walking;
            _blocked = false;
            _vehicleThreat = false;
            _currentSpeed = _speed;
            ScheduleNextIdle();

            _pathYaw = PointOnLoop(_block, _dist, out float x, out float z);
            _yaw = _pathYaw;
            Vector3 position = new Vector3(x, CityLayout.SlabTop, z);
            _body.MovePosition(position);
            _body.MoveRotation(Quaternion.AngleAxis(_yaw * Mathf.Rad2Deg, Vector3.up));
            SetAnimationPace(_currentSpeed);
        }

        private bool HasWalkSignal(TrafficSignalNetwork signalNetwork)
        {
            if (signalNetwork == null)
            {
                return true;
            }

            SignalState signal = signalNetwork.StateAt(_intersectionX, _intersectionZ);
            bool hasGreen = _eastWestCrossing ? signal.EwGo : signal.NsGo;
            float approachAndRoadTime =
                (Inset + CityLayout.RoadWidth) / _definition.CrossingSpeed;
            return hasGreen
                && signal.PhaseRemaining >= approachAndRoadTime + SignalSafetySeconds;
        }

        private bool SenseCrosswalkVehicleThreat()
        {
            Vector3 crossingCenter = (_crossStart + _crossEnd) * 0.5f;
            Vector3 halfExtents = _eastWestCrossing
                ? new Vector3(CityLayout.RoadWidth * 0.42f, 1.2f, VehicleThreatHalfLength)
                : new Vector3(VehicleThreatHalfLength, 1.2f, CityLayout.RoadWidth * 0.42f);

            int hits = Physics.OverlapBoxNonAlloc(
                crossingCenter + Vector3.up * 0.9f,
                halfExtents,
                OverlapBuffer,
                Quaternion.identity,
                _senseMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
            {
                Collider hit = OverlapBuffer[i];
                if (hit == null || hit == _collider || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                Rigidbody hitBody = hit.attachedRigidbody;
                if (hitBody == null || hitBody.TryGetComponent(out PedestrianAgent _))
                {
                    continue;
                }

                Vector3 velocity;
                TrafficVehicle trafficVehicle = hitBody.GetComponent<TrafficVehicle>();
                if (trafficVehicle != null)
                {
                    velocity = hitBody.transform.forward * trafficVehicle.CurrentSpeed;
                }
                else if (!hitBody.isKinematic)
                {
                    velocity = hitBody.linearVelocity;
                }
                else
                {
                    continue;
                }

                velocity.y = 0f;
                float speed = velocity.magnitude;
                if (speed < VehicleThreatSpeed)
                {
                    continue;
                }

                Vector3 toCrossing = crossingCenter - hitBody.position;
                toCrossing.y = 0f;
                if (toCrossing.sqrMagnitude < 5f * 5f
                    || Vector3.Dot(velocity, toCrossing) > 0f)
                {
                    return true;
                }
            }
            return false;
        }

        private bool SenseBlocked()
        {
            Vector3 forward = new Vector3(Mathf.Sin(_pathYaw), 0f, Mathf.Cos(_pathYaw));
            Vector3 position = transform.position;
            Vector3 origin = new Vector3(
                position.x + forward.x * (Radius + 0.08f),
                position.y + HalfHeight,
                position.z + forward.z * (Radius + 0.08f));
            float length = Mathf.Max(1.25f, _currentSpeed * 1.25f + Radius);
            Vector3 halfExtents = new Vector3(Radius + 0.18f, 0.7f, 0.12f);
            Quaternion orientation = Quaternion.AngleAxis(_pathYaw * Mathf.Rad2Deg, Vector3.up);

            int hits = Physics.BoxCastNonAlloc(
                origin,
                halfExtents,
                forward,
                HitBuffer,
                orientation,
                length,
                _senseMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
            {
                Collider hit = HitBuffer[i].collider;
                if (hit == _collider || hit.transform.IsChildOf(transform))
                {
                    continue;
                }
                if (hit.attachedRigidbody == null && !(hit is CharacterController))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        private void MovePose(Vector3 position, float targetYaw, float turnRate, float dt)
        {
            float delta = WrapAngle(targetYaw - _yaw);
            _yaw += delta * Mathf.Min(1f, turnRate * dt);
            _body.MovePosition(position);
            _body.MoveRotation(Quaternion.AngleAxis(_yaw * Mathf.Rad2Deg, Vector3.up));
        }

        private void SetAnimationPace(float movementSpeed)
        {
            if (_animator == null)
            {
                return;
            }

            bool moving = movementSpeed > 0.05f;
            if (_hasWalkingParameter)
            {
                // Idle/Walk/Run controller: the real idle clip plays at its
                // authored pace while stopped; moving still scales foot
                // speed to movement speed against the active gait's pace.
                bool running = _hasRunningParameter && movementSpeed > RunAnimationThreshold;
                _animator.SetBool(WalkingParameter, moving);
                if (_hasRunningParameter)
                {
                    _animator.SetBool(RunningParameter, running);
                }
                _animator.speed = moving
                    ? movementSpeed / (running ? RunClipNaturalSpeed : _clipNaturalSpeed)
                    : 1f;
                return;
            }

            // Legacy single-clip controller: slow-motion walk stands in for
            // an idle animation.
            _animator.speed = moving
                ? movementSpeed / _clipNaturalSpeed
                : IdleAnimationPace;
        }

        private void ScheduleNextIdle()
        {
            _nextIdleTimer = _random.Range(
                _definition.MinIdleInterval,
                _definition.MaxIdleInterval);
        }

        private bool TryGetPassedCorner(
            float previousDistance,
            float rawDistance,
            out int corner,
            out float cornerDistance)
        {
            float width = _block.Width - Inset * 2f;
            float depth = _block.Depth - Inset * 2f;
            float first = width;
            float second = width + depth;
            float third = width * 2f + depth;

            if (previousDistance < first && rawDistance >= first)
            {
                corner = 0;
                cornerDistance = first;
                return true;
            }
            if (previousDistance < second && rawDistance >= second)
            {
                corner = 1;
                cornerDistance = second;
                return true;
            }
            if (previousDistance < third && rawDistance >= third)
            {
                corner = 2;
                cornerDistance = third;
                return true;
            }
            if (rawDistance >= _perimeter)
            {
                corner = 3;
                cornerDistance = _perimeter;
                return true;
            }

            corner = -1;
            cornerDistance = 0f;
            return false;
        }

        private float DistanceToNearestCorner(float distance)
        {
            float width = _block.Width - Inset * 2f;
            float depth = _block.Depth - Inset * 2f;
            float nearest = Mathf.Min(distance, _perimeter - distance);
            nearest = Mathf.Min(nearest, Mathf.Abs(distance - width));
            nearest = Mathf.Min(nearest, Mathf.Abs(distance - width - depth));
            nearest = Mathf.Min(nearest, Mathf.Abs(distance - width * 2f - depth));
            return nearest;
        }

        private static float Perimeter(in CityLayout.BlockRect block)
        {
            float width = block.Width - Inset * 2f;
            float depth = block.Depth - Inset * 2f;
            return 2f * (width + depth);
        }

        private static float PointOnLoop(
            in CityLayout.BlockRect block,
            float distance,
            out float x,
            out float z)
        {
            float width = block.Width - Inset * 2f;
            float depth = block.Depth - Inset * 2f;
            float x0 = block.MinX + Inset;
            float z0 = block.MinZ + Inset;
            float perimeter = 2f * (width + depth);
            float t = Mathf.Repeat(distance, perimeter);

            if (t < width)
            {
                x = x0 + t;
                z = z0;
                return Mathf.PI * 0.5f;
            }
            t -= width;
            if (t < depth)
            {
                x = x0 + width;
                z = z0 + t;
                return 0f;
            }
            t -= depth;
            if (t < width)
            {
                x = x0 + width - t;
                z = z0 + depth;
                return -Mathf.PI * 0.5f;
            }
            t -= width;
            x = x0;
            z = z0 + depth - t;
            return Mathf.PI;
        }

        private static float WrapAngle(float angle)
        {
            while (angle > Mathf.PI)
            {
                angle -= Mathf.PI * 2f;
            }
            while (angle < -Mathf.PI)
            {
                angle += Mathf.PI * 2f;
            }
            return angle;
        }
    }
}
