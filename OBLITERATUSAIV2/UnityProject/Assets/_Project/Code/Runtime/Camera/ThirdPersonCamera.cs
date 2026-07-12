using UnityEngine;
using UnityEngine.InputSystem;

namespace ObliteratusAI.CameraSystem
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.45f, 0f);
        [SerializeField] private float distance = 5f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float followSharpness = 18f;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-25f, 65f);
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private float collisionPadding = 0.15f;

        private readonly RaycastHit[] collisionHits = new RaycastHit[12];
        private float yaw;
        private float pitch = 15f;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetTarget(Transform newTarget, Vector3 newOffset, float newDistance)
        {
            target = newTarget;
            targetOffset = newOffset;
            distance = newDistance;
        }

        /// <summary>
        /// Align the internal orbit angles with the camera's current view.
        /// Debug free-fly calls this before re-enabling follow mode so the
        /// camera resumes smoothly instead of snapping to stale angles.
        /// </summary>
        public void AdoptCurrentRotation()
        {
            Vector3 euler = transform.rotation.eulerAngles;
            yaw = euler.y;
            float signedPitch = euler.x > 180f ? euler.x - 360f : euler.x;
            pitch = Mathf.Clamp(signedPitch, pitchLimits.x, pitchLimits.y);
        }

        private void Start()
        {
            LockCursor(true);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                LockCursor(Cursor.lockState != CursorLockMode.Locked);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                yaw += mouseDelta.x * mouseSensitivity;
                pitch = Mathf.Clamp(
                    pitch - mouseDelta.y * mouseSensitivity,
                    pitchLimits.x,
                    pitchLimits.y);
            }

            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + targetOffset;
            Vector3 desiredPosition = pivot - orbit * Vector3.forward * distance;
            desiredPosition = ResolveCameraCollision(pivot, desiredPosition);
            float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);

            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, desiredPosition, blend),
                orbit);
        }

        private Vector3 ResolveCameraCollision(Vector3 pivot, Vector3 desiredPosition)
        {
            Vector3 offset = desiredPosition - pivot;
            float desiredDistance = offset.magnitude;
            if (desiredDistance <= 0.001f)
            {
                return desiredPosition;
            }

            Vector3 direction = offset / desiredDistance;
            int hitCount = Physics.SphereCastNonAlloc(
                pivot,
                collisionRadius,
                direction,
                collisionHits,
                desiredDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = desiredDistance;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = collisionHits[i].collider;
                if (hitCollider == null || hitCollider.transform.IsChildOf(target))
                {
                    continue;
                }

                nearestDistance = Mathf.Min(nearestDistance, collisionHits[i].distance);
            }

            return pivot + direction * Mathf.Max(0.05f, nearestDistance - collisionPadding);
        }

        private static void LockCursor(bool shouldLock)
        {
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }
    }
}
