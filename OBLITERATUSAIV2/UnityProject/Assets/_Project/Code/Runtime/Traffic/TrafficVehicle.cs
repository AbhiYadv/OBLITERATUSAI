using ObliteratusAI.City;
using ObliteratusAI.Pedestrians;
using UnityEngine;

namespace ObliteratusAI.Traffic
{
    /// <summary>
    /// One autonomous traffic vehicle following a loop route. Movement is
    /// analytic (a single distance along the route, multiplayer-friendly); the
    /// kinematic rigidbody only carries the pose so the player can collide
    /// with it. Braking rules and constants ported from the legacy Traffic.tsx.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TrafficVehicle : MonoBehaviour
    {
        private const float DriveAccel = 4.5f;
        private const float Brake = 9f;
        private const float StunSeconds = 2.5f;
        private const float StunImpactSpeed = 2f;
        private const float SignalLookahead = 24f;
        private const float CreepAfterBlockedSeconds = 3f;
        private const float CreepSeconds = 2f;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[8];

        private TrafficRoute _route;
        private TrafficVehicleDefinition _definition;
        private Rigidbody _body;
        private Collider _collider;
        private Transform[] _wheelPivots;
        private int _senseMask;
        private TrafficDriverProfile _driver;

        private float _dist;
        private float _speed;
        private float _cruise;
        private int _pointer;
        private float _roll;
        private float _pitch;
        private float _prevYaw;
        private float _spin;
        private float _stunTimer;
        private float _blockedTime;
        private float _creepTimer;
        private float _sensedTarget = float.PositiveInfinity;
        private float _launchDelayRemaining;
        private bool _waitingForGo;

        public float CurrentSpeed => _speed;

        public void Configure(
            TrafficRoute route,
            TrafficVehicleDefinition definition,
            float cruise,
            float startDist,
            float startSpin,
            TrafficDriverProfile driver,
            int senseMask)
        {
            _route = route;
            _definition = definition;
            _cruise = cruise;
            _speed = cruise;
            _dist = startDist;
            _spin = startSpin;
            _driver = driver;
            _senseMask = senseMask;
            _body = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            TrafficVehicleVisual visual = GetComponentInChildren<TrafficVehicleVisual>();
            _wheelPivots = visual != null && visual.WheelPivots != null
                ? visual.WheelPivots
                : System.Array.Empty<Transform>();

            float yaw = _route.Sample(_dist, ref _pointer, out float x, out float z);
            _prevYaw = yaw;
            transform.SetPositionAndRotation(
                new Vector3(x, CityLayout.RoadY, z),
                Quaternion.AngleAxis(yaw * Mathf.Rad2Deg, Vector3.up));
        }

        /// <summary>Advance the vehicle. Called by TrafficSystem from FixedUpdate.</summary>
        public void Tick(float dt, TrafficSignalNetwork signalNetwork, bool senseThisStep)
        {
            float yaw0 = _route.Sample(_dist, ref _pointer, out float x0, out float z0);
            float target = _cruise;

            int lookaheadPointer = _pointer;
            float yawAhead = _route.Sample(_dist + 10f, ref lookaheadPointer, out _, out _);
            float turnAmount = Mathf.Clamp01(Mathf.Abs(WrapAngle(yawAhead - yaw0)) / 0.8f);
            target = Mathf.Min(target, Mathf.Lerp(_cruise, _driver.CornerSpeed, turnAmount));

            if (_stunTimer > 0f)
            {
                _stunTimer -= dt;
                target = 0f;
            }
            else
            {
                // Ease to a stop behind whatever is ahead in the lane; casts
                // are staggered across vehicles, coasting on the cached result.
                if (_creepTimer > 0f)
                {
                    _creepTimer -= dt;
                }
                if (senseThisStep)
                {
                    _sensedTarget = SenseObstacleTarget(x0, z0, yaw0, _creepTimer > 0f);
                }
                if (_sensedTarget < target)
                {
                    target = _sensedTarget;
                }

                // Two vehicles blocking each other's crossing corridors can
                // never resolve on their own: after a few seconds at a full
                // stop, creep through while ignoring other traffic (the
                // player and pedestrians are still respected).
                if (_sensedTarget < 0.5f && _speed < 0.4f)
                {
                    _blockedTime += dt;
                    if (_blockedTime >= CreepAfterBlockedSeconds)
                    {
                        _blockedTime = 0f;
                        _creepTimer = CreepSeconds;
                    }
                }
                else
                {
                    _blockedTime = 0f;
                }

                // Obey the signal ahead: stop at the line on red, run a late
                // yellow when already within 8 m of the crossing.
                if (CityLayout.TryGetStopLineDistance(
                        x0, z0, yaw0, out bool ns, out float d,
                        out float intersectionX, out float intersectionZ)
                    && d < SignalLookahead)
                {
                    SignalState signal = signalNetwork.StateAt(intersectionX, intersectionZ);
                    bool go = ns ? signal.NsGo : signal.EwGo;
                    bool caution = ns ? signal.NsYellow : signal.EwYellow;
                    if ((!go && !caution) || (caution && d > _driver.YellowCommitDistance))
                    {
                        target = Mathf.Min(target, Mathf.Max(0f, (d - 2.2f) * 0.9f));
                    }
                }
            }

            if (target < 0.25f)
            {
                _waitingForGo = true;
                _launchDelayRemaining = _driver.ReactionDelay;
            }
            else if (_waitingForGo)
            {
                if (_launchDelayRemaining > 0f)
                {
                    _launchDelayRemaining -= dt;
                    target = 0f;
                }
                else
                {
                    _waitingForGo = false;
                }
            }

            float previousSpeed = _speed;
            _speed += Mathf.Clamp(
                target - _speed,
                -Brake * _driver.BrakingScale * dt,
                DriveAccel * _driver.AccelerationScale * dt);
            _dist += _speed * dt;
            if (_dist >= _route.TotalLength)
            {
                _dist -= _route.TotalLength;
            }

            float yaw = _route.Sample(_dist, ref _pointer, out float x, out float z);
            float yawRate = dt > 0f ? WrapAngle(yaw - _prevYaw) / dt : 0f;
            _prevYaw = yaw;
            float targetRoll = Mathf.Clamp(-yawRate * _speed * 0.006f, -0.14f, 0.14f);
            _roll += (targetRoll - _roll) * Mathf.Min(1f, 8f * dt);
            // Suspension read: nose dips under braking, lifts under throttle.
            float accel = dt > 0f ? (_speed - previousSpeed) / dt : 0f;
            float targetPitch = Mathf.Clamp(-accel * 0.012f, -0.05f, 0.06f);
            _pitch += (targetPitch - _pitch) * Mathf.Min(1f, 6f * dt);
            _spin += _speed / 0.34f * dt;

            Quaternion rotation =
                Quaternion.AngleAxis(yaw * Mathf.Rad2Deg, Vector3.up)
                * Quaternion.AngleAxis(_pitch * Mathf.Rad2Deg, Vector3.right)
                * Quaternion.AngleAxis(_roll * Mathf.Rad2Deg, Vector3.forward);
            _body.MovePosition(new Vector3(x, CityLayout.RoadY, z));
            _body.MoveRotation(rotation);

            if (_wheelPivots.Length > 0)
            {
                Quaternion spinRotation = Quaternion.AngleAxis(_spin * Mathf.Rad2Deg, Vector3.right);
                for (int i = 0; i < _wheelPivots.Length; i++)
                {
                    _wheelPivots[i].localRotation = spinRotation;
                }
            }
        }

        private float SenseObstacleTarget(float x, float z, float yaw, bool ignoreOtherTraffic)
        {
            Vector3 forward = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            float halfLength = _definition.HalfLength;
            Vector3 origin = new Vector3(
                x + forward.x * (halfLength + 0.35f),
                CityLayout.RoadY + _definition.HalfHeight,
                z + forward.z * (halfLength + 0.35f));
            float length = Mathf.Max(12f, _speed * _speed / (2f * Brake) + halfLength + 12f);
            Vector3 halfExtents = new Vector3(
                _definition.HalfWidth + 0.28f,
                _definition.HalfHeight * 0.8f,
                0.3f);
            Quaternion orientation = Quaternion.AngleAxis(yaw * Mathf.Rad2Deg, Vector3.up);

            int hits = Physics.BoxCastNonAlloc(
                origin, halfExtents, forward, HitBuffer, orientation, length,
                _senseMask, QueryTriggerInteraction.Ignore);
            float nearestTarget = float.PositiveInfinity;
            for (int i = 0; i < hits; i++)
            {
                Collider hit = HitBuffer[i].collider;
                if (hit == _collider || hit.transform.IsChildOf(transform))
                {
                    continue;
                }
                // Only living obstacles brake traffic (legacy obstacle kinds:
                // player, pedestrian, moving car). Static city geometry —
                // parked cars, signal posts, bollards — must never wedge a
                // route into a permanent stop.
                Rigidbody hitBody = hit.attachedRigidbody;
                if (hitBody == null && !(hit is CharacterController))
                {
                    continue;
                }
                if (ignoreOtherTraffic
                    && hitBody != null
                    && hitBody.isKinematic
                    && hitBody.TryGetComponent(out TrafficVehicle _))
                {
                    continue;
                }

                float gap = _driver.FollowingGap;
                float responseScale = 0.85f;
                PedestrianAgent pedestrian = hitBody != null
                    ? hitBody.GetComponent<PedestrianAgent>()
                    : hit.GetComponentInParent<PedestrianAgent>();
                if (pedestrian != null)
                {
                    // A person already in the road gets a wider stopping
                    // envelope than another car. Curb-side walkers retain a
                    // smaller margin so they do not hold a lane from safety.
                    gap += pedestrian.IsCrossing ? 2.6f : 0.8f;
                    responseScale = pedestrian.IsCrossing ? 0.68f : 0.78f;
                }

                float candidateTarget = Mathf.Max(
                    0f,
                    (HitBuffer[i].distance - gap) * responseScale);
                nearestTarget = Mathf.Min(nearestTarget, candidateTarget);
            }

            return nearestTarget;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Rammed by a dynamic body (the player car): hold still briefly
            // instead of grinding forward against it, then resume.
            if (collision.rigidbody == null || collision.rigidbody.isKinematic)
            {
                return;
            }
            if (collision.relativeVelocity.magnitude < StunImpactSpeed)
            {
                return;
            }
            _speed = 0f;
            _stunTimer = StunSeconds;
        }

        private static float WrapAngle(float a)
        {
            while (a > Mathf.PI)
            {
                a -= Mathf.PI * 2f;
            }
            while (a < -Mathf.PI)
            {
                a += Mathf.PI * 2f;
            }
            return a;
        }
    }
}
