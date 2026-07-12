using UnityEngine;
using UnityEngine.InputSystem;

namespace ObliteratusAI.Interactions
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private readonly Collider[] nearbyColliders = new Collider[16];
        private IInteractable focusedInteractable;
        private GUIStyle promptStyle;

        private void Update()
        {
            focusedInteractable = FindFocusedInteractable();

            Keyboard keyboard = Keyboard.current;
            if (focusedInteractable != null &&
                keyboard != null &&
                keyboard.eKey.wasPressedThisFrame)
            {
                focusedInteractable.Interact(gameObject);
            }
        }

        private IInteractable FindFocusedInteractable()
        {
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                return null;
            }

            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask,
                    QueryTriggerInteraction.Collide))
            {
                IInteractable aimedInteractable = hit.collider.GetComponentInParent<IInteractable>();
                if (aimedInteractable != null)
                {
                    return aimedInteractable;
                }
            }

            return FindNearestInteractable();
        }

        private IInteractable FindNearestInteractable()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                interactionDistance,
                nearbyColliders,
                interactionMask,
                QueryTriggerInteraction.Collide);

            IInteractable nearest = null;
            float nearestDistanceSquared = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider candidateCollider = nearbyColliders[i];
                if (candidateCollider == null || candidateCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                IInteractable candidate = candidateCollider.GetComponentInParent<IInteractable>();
                if (candidate == null)
                {
                    continue;
                }

                Vector3 closestPoint = candidateCollider.ClosestPoint(transform.position);
                float distanceSquared = (closestPoint - transform.position).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearest = candidate;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            return nearest;
        }

        private void OnGUI()
        {
            if (focusedInteractable == null)
            {
                return;
            }

            promptStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            const float width = 260f;
            const float height = 38f;
            Rect rect = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height * 0.68f,
                width,
                height);
            GUI.Box(rect, $"E  {focusedInteractable.InteractionPrompt}", promptStyle);
        }
    }
}
