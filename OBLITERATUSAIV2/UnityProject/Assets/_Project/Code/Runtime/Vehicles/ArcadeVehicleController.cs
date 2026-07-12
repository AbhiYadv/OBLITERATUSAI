using UnityEngine;
using UnityEngine.InputSystem;

namespace ObliteratusAI.Vehicles
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeVehicleController : MonoBehaviour
    {
        [SerializeField] private VehicleDefinition definition;

        private Rigidbody vehicleBody;
        private bool controlsEnabled;

        public void Configure(VehicleDefinition vehicleDefinition)
        {
            definition = vehicleDefinition;
        }

        public void SetControlsEnabled(bool enabled)
        {
            controlsEnabled = enabled;
        }

        private void Awake()
        {
            vehicleBody = GetComponent<Rigidbody>();
            if (definition != null)
            {
                vehicleBody.mass = definition.Mass;
            }

            vehicleBody.centerOfMass = new Vector3(0f, -0.3f, 0f);
        }

        private void FixedUpdate()
        {
            if (!controlsEnabled || definition == null || Keyboard.current == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            float throttle = ReadAxis(
                keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed,
                keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed);
            float steering = ReadAxis(
                keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed,
                keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed);
            Vector3 velocity = vehicleBody.linearVelocity;
            float forwardSpeed = Vector3.Dot(velocity, transform.forward);

            if (Mathf.Abs(throttle) > 0.01f)
            {
                float force = throttle > 0f ? definition.Acceleration : definition.ReverseAcceleration;
                bool belowSpeedLimit = throttle > 0f
                    ? forwardSpeed < definition.MaximumSpeed
                    : forwardSpeed > -definition.MaximumSpeed * 0.45f;
                if (belowSpeedLimit)
                {
                    vehicleBody.AddForce(transform.forward * (throttle * force), ForceMode.Acceleration);
                }
            }

            if (Mathf.Abs(steering) > 0.01f && Mathf.Abs(forwardSpeed) > 0.25f)
            {
                float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 3f);
                float direction = Mathf.Sign(forwardSpeed);
                float yaw = steering * direction * definition.SteeringSpeed * speedFactor * Time.fixedDeltaTime;
                vehicleBody.MoveRotation(vehicleBody.rotation * Quaternion.Euler(0f, yaw, 0f));
            }

            Vector3 lateralVelocity = transform.right * Vector3.Dot(velocity, transform.right);
            vehicleBody.AddForce(-lateralVelocity * definition.LateralGrip, ForceMode.Acceleration);

            if (keyboard.spaceKey.isPressed)
            {
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(vehicleBody.linearVelocity, Vector3.up);
                vehicleBody.AddForce(-horizontalVelocity * definition.Braking, ForceMode.Acceleration);
            }
        }

        private static float ReadAxis(bool negative, bool positive)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
