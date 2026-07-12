using ObliteratusAI.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ObliteratusAI.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float turnSpeed = 14f;

        [Header("Water wading")]
        [SerializeField, Range(0.2f, 1f)] private float wadeSpeedMultiplier = 0.55f;
        [SerializeField, Range(0.5f, 1.5f)] private float maximumWadeDepth = 1.1f;
        [SerializeField, Range(0f, 0.3f)] private float waterEntryTolerance = 0.12f;

        private CharacterController controller;
        private Transform cameraTransform;
        private float verticalVelocity;

        public bool IsWading { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void ResetMotion()
        {
            verticalVelocity = -2f;
            IsWading = false;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            Vector2 input = Vector2.zero;
            input.x = ReadAxis(
                keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed,
                keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed);
            input.y = ReadAxis(
                keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed,
                keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed);
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 move = GetCameraRelativeMove(input);
            if (move.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime);
            }

            bool supportedByWater = TryGetWadingFloor(
                transform.position,
                out float wadingFloor)
                && transform.position.y <= wadingFloor + 0.08f;
            bool grounded = controller.isGrounded || supportedByWater;
            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (grounded && keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
            bool sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            float speed = sprint ? sprintSpeed : walkSpeed;
            if (IsWading)
            {
                speed *= wadeSpeedMultiplier;
            }
            Vector3 velocity = move * speed;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);

            IsWading = TryGetWadingFloor(transform.position, out wadingFloor);
            if (IsWading && transform.position.y < wadingFloor)
            {
                controller.Move(Vector3.up * (wadingFloor - transform.position.y));
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f;
                }
            }
        }

        private bool TryGetWadingFloor(Vector3 position, out float floorCenterY)
        {
            float terrainHeight = TerrainSampler.HeightAt(position.x, position.z);
            float halfHeight = controller.height * 0.5f;
            float feetY = position.y + controller.center.y - halfHeight;
            bool riverIsBelowWater = terrainHeight < TerrainSampler.WaterLevel - 0.15f;
            bool feetEnteredWater = feetY <= TerrainSampler.WaterLevel + waterEntryTolerance;
            floorCenterY = TerrainSampler.WaterLevel
                - maximumWadeDepth
                - controller.center.y
                + halfHeight;
            return riverIsBelowWater && feetEnteredWater;
        }

        private Vector3 GetCameraRelativeMove(Vector2 input)
        {
            if (cameraTransform == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }

        private static float ReadAxis(bool negative, bool positive)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
