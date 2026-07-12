using UnityEngine;

namespace ObliteratusAI.Vehicles
{
    /// <summary>
    /// Returns the player's car to its home garage when abandoned: once the
    /// player exits away from home, a timer runs and the car teleports back to
    /// its garage slot, disappearing from wherever it was left.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleGarageReturn : MonoBehaviour
    {
        [SerializeField] private Vector3 garagePosition;
        [SerializeField] private float garageYawDegrees;
        [SerializeField] private float respawnSeconds = 30f;
        [SerializeField] private float atHomeRadius = 3f;

        private Rigidbody _body;
        private VehicleSeat _seat;
        private float _timer;
        private bool _wasOccupied;

        /// <summary>Editor-time wiring used by the sandbox builder.</summary>
        public void Configure(Vector3 position, float yawDegrees)
        {
            garagePosition = position;
            garageYawDegrees = yawDegrees;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _seat = GetComponent<VehicleSeat>();
        }

        private void Update()
        {
            if (_seat != null && _seat.IsOccupied)
            {
                _timer = 0f;
                _wasOccupied = true;
                return;
            }
            if ((transform.position - garagePosition).sqrMagnitude < atHomeRadius * atHomeRadius)
            {
                _timer = 0f;
                _wasOccupied = false;
                return;
            }

            // Only a car that has actually been occupied can be abandoned.
            // This prevents incidental physics movement from arming a return.
            if (!_wasOccupied)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < respawnSeconds)
            {
                return;
            }
            _timer = 0f;
            ReturnToGarage();
        }

        private void ReturnToGarage()
        {
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            Quaternion rotation = Quaternion.Euler(0f, garageYawDegrees, 0f);
            _body.position = garagePosition;
            _body.rotation = rotation;
            transform.SetPositionAndRotation(garagePosition, rotation);
            _body.Sleep();
            _wasOccupied = false;
        }
    }
}
